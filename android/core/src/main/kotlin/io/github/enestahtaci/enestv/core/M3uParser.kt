package io.github.enestahtaci.enestv.core

import java.io.BufferedReader
import java.io.InputStream
import java.io.InputStreamReader
import java.nio.charset.CharacterCodingException
import java.nio.charset.Charset
import java.nio.charset.CodingErrorAction

/** M3U playlist parser with the same rules as the Windows core (CORE-SPEC §2, fixtures/m3u). Streams line by line. */
object M3uParser {
    private val attributes = Regex("(?U)([\\w-]+)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s,]+))")
    private val allowedSchemes = setOf("http", "https", "rtsp", "rtmp", "udp", "rtp", "file")
    private const val UNNAMED = "İsimsiz kanal"

    /**
     * Reads strict UTF-8 first (a BOM is skipped); on the first invalid byte the whole playlist is read again as
     * Windows-1254 (Turkish). [open] must return a fresh stream each time it is called.
     */
    fun parse(open: () -> InputStream, sourceId: String, baseUri: String?): List<Channel> =
        try {
            read(open, Charsets.UTF_8, sourceId, baseUri)
        } catch (_: CharacterCodingException) {
            read(open, Charset.forName("windows-1254"), sourceId, baseUri)
        }

    private fun read(open: () -> InputStream, charset: Charset, sourceId: String, baseUri: String?): List<Channel> {
        val decoder = charset.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT)
        return open().use { stream -> parse(BufferedReader(InputStreamReader(stream, decoder), 64 * 1024), sourceId, baseUri) }
    }

    fun parse(reader: BufferedReader, sourceId: String, baseUri: String?): List<Channel> {
        val result = ArrayList<Channel>()
        val seen = HashSet<String>()
        val metadata = HashMap<String, String>()
        var name = ""
        while (true) {
            var line = reader.readLine() ?: break
            line = line.trim().trimStart('\uFEFF')
            if (line.isEmpty()) continue
            if (line.startsWith("#EXTINF:", ignoreCase = true)) {
                metadata.clear()
                name = ""
                var quote = '\u0000'
                var comma = -1
                for (i in 8 until line.length) {
                    val ch = line[i]
                    if (ch == '"' || ch == '\'') {
                        if (quote == ch) quote = '\u0000' else if (quote == '\u0000') quote = ch
                    }
                    if (ch == ',' && quote == '\u0000') { comma = i; break }
                }
                if (comma >= 0) name = line.substring(comma + 1).trim()
                for (match in attributes.findAll(if (comma >= 0) line.substring(0, comma) else line)) {
                    val g = match.groups
                    metadata[g[1]!!.value.lowercase()] = (g[2] ?: g[3] ?: g[4])!!.value
                }
                continue
            }
            if (line.startsWith("#EXTGRP:", ignoreCase = true)) { metadata["group-title"] = line.substring(8).trim(); continue }
            if (line.startsWith("#")) continue

            val url = UrlNormalizer.absolute(line) ?: baseUri?.let { UrlNormalizer.resolve(it, line) }
            if (url != null && url.substringBefore(':') in allowedSchemes) {
                val display = name.ifEmpty { metadata["tvg-name"] ?: UNNAMED }
                val key = Channel.key(sourceId, url, display)
                val category = metadata["group-title"].takeUnless { it.isNullOrBlank() } ?: Channel.UNCATEGORIZED
                var logo = metadata["tvg-logo"].orEmpty()
                if (baseUri != null && logo.isNotEmpty()) UrlNormalizer.resolve(baseUri, logo)?.let { logo = it }
                if (seen.add(key)) result.add(Channel(key, sourceId, display, url, metadata["tvg-id"].orEmpty(), metadata["tvg-name"].orEmpty(), logo, category))
            }
            metadata.clear()
            name = ""
        }
        return result
    }
}
