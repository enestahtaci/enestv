package io.github.enestahtaci.enestv.core

import java.security.MessageDigest

/** Same fields and key as the Windows core (EnesTV.Core/Models/Models.cs, CORE-SPEC §1). */
data class Channel(
    val id: String,
    val sourceId: String,
    val name: String,
    val url: String,
    val tvgId: String = "",
    val tvgName: String = "",
    val logo: String = "",
    val category: String = UNCATEGORIZED,
) {
    companion object {
        const val UNCATEGORIZED = "Uncategorized"

        /** SHA-256 of UTF-8 "source\naddress\nname" as 64 upper-case hex characters. */
        fun key(source: String, address: String, name: String): String {
            val digest = MessageDigest.getInstance("SHA-256").digest("$source\n$address\n$name".toByteArray(Charsets.UTF_8))
            val hex = CharArray(digest.size * 2)
            for (i in digest.indices) {
                val v = digest[i].toInt() and 0xFF
                hex[i * 2] = HEX[v ushr 4]
                hex[i * 2 + 1] = HEX[v and 0x0F]
            }
            return String(hex)
        }

        private val HEX = "0123456789ABCDEF".toCharArray()
    }
}
