package io.github.enestahtaci.enestv.data

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import java.security.KeyStore
import java.security.SecureRandom
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec

/**
 * At-rest encryption (the Android counterpart of DPAPI, CORE-SPEC §6). A random 256-bit data key encrypts the library
 * in-process with AES-GCM; the data key itself is stored wrapped by a non-exportable Android Keystore key. Keystore
 * calls go through a system service and are slow, so they happen once per start, not once per record.
 */
class Crypto private constructor(private val dataKey: SecretKey) {
    private val random = SecureRandom()

    /** nonce (12 bytes) || ciphertext || tag */
    fun seal(plain: ByteArray): ByteArray {
        val nonce = ByteArray(12).also(random::nextBytes)
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.ENCRYPT_MODE, dataKey, GCMParameterSpec(128, nonce))
        return nonce + cipher.doFinal(plain)
    }

    fun open(sealed: ByteArray): ByteArray {
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.DECRYPT_MODE, dataKey, GCMParameterSpec(128, sealed, 0, 12))
        return cipher.doFinal(sealed, 12, sealed.size - 12)
    }

    fun sealText(text: String) = seal(text.toByteArray(Charsets.UTF_8))
    fun openText(sealed: ByteArray) = String(open(sealed), Charsets.UTF_8)

    companion object {
        private const val TRANSFORMATION = "AES/GCM/NoPadding"
        private const val WRAP_ALIAS = "enestv.datakey.wrap.v1"
        private const val PREFS = "crypto"
        private const val WRAPPED = "wrappedDataKey"

        fun load(context: Context): Crypto {
            val prefs = context.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
            val wrap = wrappingKey()
            prefs.getString(WRAPPED, null)?.let { stored ->
                val blob = Base64.decode(stored, Base64.NO_WRAP)
                val cipher = Cipher.getInstance(TRANSFORMATION)
                cipher.init(Cipher.DECRYPT_MODE, wrap, GCMParameterSpec(128, blob, 0, 12))
                return Crypto(SecretKeySpec(cipher.doFinal(blob, 12, blob.size - 12), "AES"))
            }
            val raw = ByteArray(32).also(SecureRandom()::nextBytes)
            val cipher = Cipher.getInstance(TRANSFORMATION)
            cipher.init(Cipher.ENCRYPT_MODE, wrap) // Keystore picks the nonce
            val blob = cipher.iv + cipher.doFinal(raw)
            prefs.edit().putString(WRAPPED, Base64.encodeToString(blob, Base64.NO_WRAP)).commit()
            return Crypto(SecretKeySpec(raw, "AES"))
        }

        private fun wrappingKey(): SecretKey {
            val store = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
            (store.getKey(WRAP_ALIAS, null) as? SecretKey)?.let { return it }
            return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore").apply {
                init(KeyGenParameterSpec.Builder(WRAP_ALIAS, KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
                    .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                    .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                    .setKeySize(256)
                    .build())
            }.generateKey()
        }
    }
}
