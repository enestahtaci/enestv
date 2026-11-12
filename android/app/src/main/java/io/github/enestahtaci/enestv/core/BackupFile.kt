package io.github.enestahtaci.enestv.core

import java.text.Normalizer
import java.util.Base64
import javax.crypto.AEADBadTagException
import javax.crypto.Cipher
import javax.crypto.Mac
import javax.crypto.spec.GCMParameterSpec
import javax.crypto.spec.SecretKeySpec

/**
 * Reads the passphrase-encrypted .enestv backup made by any enestv platform (CORE-SPEC §6a, fixtures/backup).
 * Same checks and Turkish messages as EnesTV.Core BackupFile.Decrypt.
 */
object BackupFile {
    const val FORMAT = "enestv-backup"
    const val VERSION = 1
    const val MAX_FILE_BYTES = 64L * 1024 * 1024
    private val associatedData = "enestv-backup/v1".toByteArray(Charsets.UTF_8)

    data class BackupSource(val id: String, val name: String, val kind: String, val location: String, val username: String, val password: String)

    /** Decrypted library as a JSON tree (camelCase fields, enums as text). */
    fun decrypt(file: ByteArray, passphrase: String): Map<*, *> {
        if (file.size > MAX_FILE_BYTES) throw UserInputException("Yedek dosyası çok büyük.")
        val envelope = try {
            Json.parse(String(file, Charsets.UTF_8)) as? Map<*, *>
        } catch (_: Json.JsonException) {
            null
        }
        if (envelope == null || Json.text(envelope, "format") != FORMAT) throw UserInputException("Bu dosya bir enestv yedeği değil.")
        val version = Json.text(envelope, "version").toIntOrNull() ?: 0
        if (version < 1 || version > VERSION) throw newer()

        val kdf = envelope["kdf"]; val cipher = envelope["cipher"]
        val iterations = Json.text(kdf, "iterations").toIntOrNull() ?: 0
        val salt: ByteArray; val nonce: ByteArray; val data: ByteArray
        try {
            salt = Base64.getDecoder().decode(Json.text(kdf, "salt"))
            nonce = Base64.getDecoder().decode(Json.text(cipher, "nonce"))
            data = Base64.getDecoder().decode(Json.text(envelope, "data"))
        } catch (_: IllegalArgumentException) {
            throw UserInputException("Yedek dosyası bozulmuş.")
        }
        if (iterations !in 10_000..10_000_000 || salt.size < 16 || nonce.size != 12 || data.size < 16) throw UserInputException("Yedek dosyası bozulmuş.")

        val key = pbkdf2Sha256(Normalizer.normalize(passphrase, Normalizer.Form.NFC).toByteArray(Charsets.UTF_8), salt, iterations)
        val plain = try {
            Cipher.getInstance("AES/GCM/NoPadding").run {
                init(Cipher.DECRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, nonce))
                updateAAD(associatedData)
                doFinal(data)
            }
        } catch (_: AEADBadTagException) {
            throw UserInputException("Parola yanlış ya da yedek dosyası bozulmuş.")
        } finally {
            key.fill(0)
        }
        val payload = try {
            Json.parse(String(plain, Charsets.UTF_8)) as? Map<*, *>
        } catch (_: Json.JsonException) {
            null
        } finally {
            plain.fill(0)
        } ?: throw UserInputException("Yedek dosyası bozulmuş.")
        if ((Json.text(payload, "version").toIntOrNull() ?: 0) > VERSION) throw newer()
        return payload
    }

    fun sources(payload: Map<*, *>): List<BackupSource> = (payload["sources"] as? List<*>).orEmpty().map {
        BackupSource(Json.text(it, "id"), Json.text(it, "name"), Json.text(it, "kind"), Json.text(it, "location"), Json.text(it, "username"), Json.text(it, "password"))
    }

    private fun newer() = UserInputException("Bu yedek daha yeni bir enestv sürümüyle oluşturulmuş. enestv'yi güncelleyip tekrar deneyin.")

    /**
     * PBKDF2-HMAC-SHA256 for one 32-byte block (RFC 8018), on raw UTF-8 bytes. Written out because platform
     * "PBKDF2WithHmacSHA256" implementations differ in how they turn a char[] password into bytes.
     */
    internal fun pbkdf2Sha256(password: ByteArray, salt: ByteArray, iterations: Int): ByteArray {
        val mac = Mac.getInstance("HmacSHA256")
        mac.init(SecretKeySpec(password, "HmacSHA256")) // never empty: passphrases have at least 8 characters
        mac.update(salt)
        mac.update(byteArrayOf(0, 0, 0, 1))
        var u = mac.doFinal()
        val result = u.copyOf()
        for (i in 1 until iterations) {
            u = mac.doFinal(u)
            for (j in result.indices) result[j] = (result[j].toInt() xor u[j].toInt()).toByte()
        }
        return result
    }
}
