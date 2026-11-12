package io.github.enestahtaci.enestv.core

import java.io.ByteArrayOutputStream
import java.io.DataInputStream
import java.io.DataOutputStream
import java.io.IOException

/**
 * Compact binary form of one source's channel list, so a start-up reads one file instead of thousands of rows.
 * Platform storage detail (not exchanged between devices; the .enestv backup is the portable format):
 * "ETVC", format version, count, then per channel 8 length-prefixed UTF-8 strings.
 */
object ChannelSnapshot {
    private const val MAGIC = 0x45545643 // "ETVC"
    private const val VERSION = 1

    fun encode(channels: List<Channel>): ByteArray {
        val bytes = ByteArrayOutputStream(channels.size * 256)
        DataOutputStream(bytes).use { out ->
            out.writeInt(MAGIC); out.writeInt(VERSION); out.writeInt(channels.size)
            for (c in channels) {
                for (s in arrayOf(c.id, c.sourceId, c.name, c.url, c.tvgId, c.tvgName, c.logo, c.category)) {
                    val utf8 = s.toByteArray(Charsets.UTF_8)
                    out.writeInt(utf8.size)
                    out.write(utf8)
                }
            }
        }
        return bytes.toByteArray()
    }

    /** Throws [IOException] for a damaged or foreign file; the caller then downloads the list again. */
    fun decode(data: ByteArray): List<Channel> {
        val input = DataInputStream(data.inputStream())
        if (input.readInt() != MAGIC || input.readInt() != VERSION) throw IOException("not a channel snapshot")
        val count = input.readInt()
        if (count < 0 || count > data.size / 32) throw IOException("bad channel count")
        // Source ids and categories repeat for every channel: share one String instance each to save memory.
        val shared = HashMap<String, String>()
        fun str(share: Boolean = false): String {
            val n = input.readInt()
            if (n < 0 || n > data.size) throw IOException("bad string length")
            val s = if (n == 0) "" else String(ByteArray(n).also { input.readFully(it) }, Charsets.UTF_8)
            return if (share) shared.getOrPut(s) { s } else s
        }
        return List(count) {
            Channel(str(), str(share = true), str(), str(), str(), str(), str(), str(share = true))
        }
    }
}
