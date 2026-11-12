package io.github.enestahtaci.enestv.data

import io.github.enestahtaci.enestv.core.AuthenticationException
import io.github.enestahtaci.enestv.core.Channel
import io.github.enestahtaci.enestv.core.Json
import io.github.enestahtaci.enestv.core.M3uParser
import io.github.enestahtaci.enestv.core.UrlNormalizer
import io.github.enestahtaci.enestv.core.UserInputException
import io.github.enestahtaci.enestv.core.Xtream
import java.io.File

/**
 * Downloads the source into app-private files and parses them. The downloaded files double as a cache, so the next
 * start shows the list from disk at once and refreshes in the background. Call from a background thread.
 */
class ChannelLibrary(private val dir: File) {
    private val playlist get() = File(dir, "playlist.m3u")
    private val categories get() = File(dir, "live-categories.json")
    private val streams get() = File(dir, "live-streams.json")

    fun clearCache() {
        listOf(playlist, categories, streams).forEach { it.delete() }
    }

    /** Channels from the last download, or null when nothing is cached. */
    fun loadCached(source: Source): List<Channel>? = when (source.kind) {
        Source.Kind.M3U -> playlist.takeIf { it.isFile }?.let { parseM3u(source) }
        Source.Kind.XTREAM -> if (categories.isFile && streams.isFile) parseXtream(source) else null
    }

    fun refresh(source: Source): List<Channel> = when (source.kind) {
        Source.Kind.M3U -> {
            httpUrl(source.location)
            Downloader.download(source.location.trim(), playlist, M3U_LIMIT)
            parseM3u(source).ifEmpty { throw UserInputException("Kaynakta oynatılabilir kanal bulunamadı. Bir kanal listesi seçin.") }
        }
        Source.Kind.XTREAM -> {
            if (source.username.isBlank() || source.password.isEmpty()) throw UserInputException("Kullanıcı adı ve parola gerekli.")
            val auth = File(dir, "auth.json")
            try {
                Downloader.download(Xtream.apiUrl(source.location, source.username, source.password), auth, API_LIMIT)
                if (!Xtream.isAuthenticated(readJson(auth))) throw AuthenticationException()
            } finally {
                auth.delete()
            }
            Downloader.download(Xtream.apiUrl(source.location, source.username, source.password, "get_live_categories"), categories, API_LIMIT)
            Downloader.download(Xtream.apiUrl(source.location, source.username, source.password, "get_live_streams"), streams, API_LIMIT)
            parseXtream(source)
        }
    }

    private fun parseM3u(source: Source) = M3uParser.parse({ playlist.inputStream() }, source.id, UrlNormalizer.absolute(source.location.trim()))

    private fun parseXtream(source: Source) =
        Xtream.parseLiveStreams(source.id, source.location, source.username, source.password, readJson(categories), readJson(streams))

    private fun readJson(file: File): Any? {
        val value = try {
            Json.parse(file.readText(Charsets.UTF_8))
        } catch (_: Json.JsonException) {
            throw UserInputException("Sunucu geçerli bir yanıt döndürmedi.")
        }
        if (Json.text((value as? Map<*, *>)?.get("user_info"), "auth") == "0") throw AuthenticationException()
        return value
    }

    private fun httpUrl(input: String) {
        val url = UrlNormalizer.absolute(input.trim())
        if (url == null || !(url.startsWith("http://") || url.startsWith("https://"))) throw UserInputException("Geçerli bir http:// veya https:// adresi girin.")
    }

    private companion object {
        const val M3U_LIMIT = 256L * 1024 * 1024
        const val API_LIMIT = 128L * 1024 * 1024
    }
}
