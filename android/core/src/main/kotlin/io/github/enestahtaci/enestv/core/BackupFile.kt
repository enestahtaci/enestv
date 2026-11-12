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

    const val ITERATIONS = 600_000
    const val MIN_PASSPHRASE_LENGTH = 8

    data class BackupSource(val id: String, val name: String, val kind: String, val location: String, val username: String, val password: String, val updated: String = "")

    fun checkPassphrase(passphrase: String) {
        if (Normalizer.normalize(passphrase, Normalizer.Form.NFC).length < MIN_PASSPHRASE_LENGTH) {
            throw UserInputException("Yedek parolası en az $MIN_PASSPHRASE_LENGTH karakter olmalı.")
        }
    }

    /**
     * Encrypts a library payload (the JSON tree described in CORE-SPEC §6a) into an .enestv file. [iterations] is a
     * parameter only so tests and fixtures can be fast; the app always uses [ITERATIONS].
     */
    fun encrypt(payload: Map<String, Any?>, passphrase: String, iterations: Int = ITERATIONS): ByteArray {
        checkPassphrase(passphrase)
        val random = java.security.SecureRandom()
        val salt = ByteArray(16).also(random::nextBytes)
        val nonce = ByteArray(12).also(random::nextBytes)
        val key = pbkdf2Sha256(Normalizer.normalize(passphrase, Normalizer.Form.NFC).toByteArray(Charsets.UTF_8), salt, iterations)
        val plain = Json.write(payload).toByteArray(Charsets.UTF_8)
        val data = try {
            Cipher.getInstance("AES/GCM/NoPadding").run {
                init(Cipher.ENCRYPT_MODE, SecretKeySpec(key, "AES"), GCMParameterSpec(128, nonce))
                updateAAD(associatedData)
                doFinal(plain)
            }
        } finally {
            key.fill(0)
            plain.fill(0)
        }
        val b64 = Base64.getEncoder()
        val envelope = linkedMapOf<String, Any?>(
            "format" to FORMAT, "version" to VERSION, "created" to (payload["created"] as? String ?: now()),
            "kdf" to linkedMapOf("name" to "PBKDF2-HMAC-SHA256", "iterations" to iterations, "salt" to b64.encodeToString(salt)),
            "cipher" to linkedMapOf("name" to "AES-256-GCM", "nonce" to b64.encodeToString(nonce), "tagLength" to 16),
            "data" to b64.encodeToString(data),
        )
        return Json.write(envelope).toByteArray(Charsets.UTF_8)
    }

    /** ISO-8601 with offset, as .NET DateTimeOffset "O" writes it. */
    fun now(): String = java.time.OffsetDateTime.now(java.time.ZoneOffset.UTC).truncatedTo(java.time.temporal.ChronoUnit.MILLIS).toString()

    fun isoTime(epochMs: Long): String =
        java.time.Instant.ofEpochMilli(epochMs).atOffset(java.time.ZoneOffset.UTC).toString()

    /** Epoch ms of an ISO-8601 time from a backup, or null. */
    fun epochMs(iso: String): Long? = runCatching { java.time.OffsetDateTime.parse(iso).toInstant().toEpochMilli() }.getOrNull()

    fun channels(payload: Map<*, *>): List<Channel> = (payload["channels"] as? List<*>).orEmpty().mapNotNull {
        val id = Json.text(it, "id")
        if (id.isEmpty()) null
        else Channel(id, Json.text(it, "sourceId"), Json.text(it, "name"), Json.text(it, "url"), Json.text(it, "tvgId"),
            Json.text(it, "tvgName"), Json.text(it, "logo"), Json.text(it, "category").ifEmpty { Channel.UNCATEGORIZED })
    }

    fun strings(payload: Map<*, *>, field: String): List<String> = (payload[field] as? List<*>).orEmpty().mapNotNull { it as? String }

    fun channelTree(c: Channel): Map<String, Any?> = linkedMapOf(
        "id" to c.id, "sourceId" to c.sourceId, "name" to c.name, "url" to c.url,
        "tvgId" to c.tvgId, "tvgName" to c.tvgName, "logo" to c.logo, "category" to c.category,
    )

    /** Defaults of a new library (CORE-SPEC §6). */
    fun defaultSettings(): Map<String, Any?> = linkedMapOf("theme" to "Dark", "defaultVolume" to 75, "autoReconnect" to true, "epgUrl" to "")

    fun emptyLayout(): Map<String, Any?> = linkedMapOf("channels" to linkedMapOf<String, Any?>(), "hiddenCategories" to emptyList<Any?>(),
        "favoriteLists" to emptyList<Any?>(), "shortcuts" to linkedMapOf<String, Any?>())

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
        BackupSource(Json.text(it, "id"), Json.text(it, "name"), Json.text(it, "kind"), Json.text(it, "location"), Json.text(it, "username"),
            Json.text(it, "password"), Json.text(it, "updated"))
    }

    private fun newer() = UserInputException("Bu yedek daha yeni bir enestv sürümüyle oluşturulmuş. enestv'yi güncelleyip tekrar deneyin.")

    /**
     * PBKDF2-HMAC-SHA256 for one 32-byte block (RFC 8018), on raw UTF-8 bytes. Written out because platform
     * "PBKDF2WithHmacSHA256" implementations differ in how they turn a char[] password into bytes.
     */
    internal fun pbkdf2Sha256(password: ByteArray, salt: ByteArray, iterations: Int): ByteArray {
        val mac = Mac.getInstance("HmacSHA256")
        // SecretKeySpec rejects an empty key. HMAC pads keys with zeros, so an empty key and a single zero byte give the
        // same result as .NET's PBKDF2 with an empty password (a wrong, empty passphrase must just fail to decrypt).
        mac.init(SecretKeySpec(if (password.isEmpty()) ByteArray(1) else password, "HmacSHA256"))
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
