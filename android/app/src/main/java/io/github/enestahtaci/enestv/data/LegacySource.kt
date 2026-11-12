package io.github.enestahtaci.enestv.data

import android.util.Base64
import io.github.enestahtaci.enestv.core.Json
import java.security.KeyStore
import javax.crypto.Cipher
import javax.crypto.SecretKey
import javax.crypto.spec.GCMParameterSpec

/** Reads the single source saved by the spike build (Keystore alias enestv.source.v1), for [Library.migrateLegacy]. */
internal object LegacySource {
    private const val ALIAS = "enestv.source.v1"

    fun read(stored: String?): Source? = runCatching {
        val blob = Base64.decode(stored ?: return null, Base64.NO_WRAP)
        val key = KeyStore.getInstance("AndroidKeyStore").apply { load(null) }.getKey(ALIAS, null) as? SecretKey ?: return null
        val cipher = Cipher.getInstance("AES/GCM/NoPadding")
        cipher.init(Cipher.DECRYPT_MODE, key, GCMParameterSpec(128, blob, 0, 12))
        val map = Json.parse(String(cipher.doFinal(blob, 12, blob.size - 12), Charsets.UTF_8))
        val kind = if (Json.text(map, "kind") == "XTREAM") Source.Kind.XTREAM else Source.Kind.M3U_URL
        Source(Source.newId(), if (kind == Source.Kind.XTREAM) "Xtream hesabı" else "M3U listesi", kind,
            Json.text(map, "location"), Json.text(map, "username"), Json.text(map, "password"))
    }.getOrNull()

    fun deleteKey() {
        runCatching { KeyStore.getInstance("AndroidKeyStore").apply { load(null) }.deleteEntry(ALIAS) }
    }
}
