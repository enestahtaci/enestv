package io.github.enestahtaci.enestv.data

import android.content.Context
import android.security.keystore.KeyGenParameterSpec
import android.security.keystore.KeyProperties
import android.util.Base64
import io.github.enestahtaci.enestv.core.Json
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.KeyGenerator
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/** The user's single source for the spike. */
data class Source(val kind: Kind, val location: String, val username: String = "", val password: String = "") {
    enum class Kind { M3U, XTREAM }

    val id: String get() = "src1"
}

/**
 * Keeps the source encrypted with an Android Keystore AES-GCM key (the Android counterpart of DPAPI on Windows,
 * CORE-SPEC §7). The key never leaves the Keystore; backups are disabled in the manifest.
 */
class SourceStore(context: Context) {
    private val prefs = context.getSharedPreferences("source", Context.MODE_PRIVATE)

    fun load(): Source? = runCatching {
        val blob = Base64.decode(prefs.getString(KEY_BLOB, null) ?: return null, Base64.NO_WRAP)
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.DECRYPT_MODE, key(), GCMParameterSpec(128, blob, 0, 12))
        val map = Json.parse(String(cipher.doFinal(blob, 12, blob.size - 12), Charsets.UTF_8))
        Source(Source.Kind.valueOf(Json.text(map, "kind")), Json.text(map, "location"), Json.text(map, "username"), Json.text(map, "password"))
    }.getOrNull()

    fun save(source: Source) {
        val plain = buildString {
            append("{\"kind\":").append(quote(source.kind.name)).append(",\"location\":").append(quote(source.location))
            append(",\"username\":").append(quote(source.username)).append(",\"password\":").append(quote(source.password)).append('}')
        }.toByteArray(Charsets.UTF_8)
        val cipher = Cipher.getInstance(TRANSFORMATION)
        cipher.init(Cipher.ENCRYPT_MODE, key())
        val blob = cipher.iv + cipher.doFinal(plain)
        prefs.edit().putString(KEY_BLOB, Base64.encodeToString(blob, Base64.NO_WRAP)).apply()
    }

    private fun key(): SecretKey {
        val store = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }
        (store.getKey(ALIAS, null) as? SecretKey)?.let { return it }
        return KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore").apply {
            init(KeyGenParameterSpec.Builder(ALIAS, KeyProperties.PURPOSE_ENCRYPT or KeyProperties.PURPOSE_DECRYPT)
                .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                .setKeySize(256)
                .build())
        }.generateKey()
    }

    private fun quote(s: String) = buildString {
        append('"')
        for (c in s) when {
            c == '"' || c == '\\' -> append('\\').append(c)
            c < ' ' -> append("\\u").append(String.format("%04x", c.code))
            else -> append(c)
        }
        append('"')
    }

    private companion object {
        const val ALIAS = "enestv.source.v1"
        const val KEY_BLOB = "blob"
        const val TRANSFORMATION = "AES/GCM/NoPadding"
    }
}
