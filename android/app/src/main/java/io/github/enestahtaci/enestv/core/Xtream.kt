package io.github.enestahtaci.enestv.core

/** Errors shown to the user as written (Turkish), same texts as the Windows core. */
class UserInputException(message: String) : Exception(message)

/** Wrong username/password or inactive account (CORE-SPEC §3.2). */
class AuthenticationException : Exception()

/** Xtream Codes live channels, same rules as EnesTV.Core XtreamService (CORE-SPEC §3, fixtures/xtream). */
object Xtream {
    fun normalizeServer(input: String): String {
        var text = input.trim()
        if (!text.contains("://")) text = "https://$text"
        val url = UrlNormalizer.absolute(text)
        val scheme = url?.substringBefore("://")
        if (url == null || (scheme != "http" && scheme != "https")) throw UserInputException("Geçerli bir http:// veya https:// adresi girin.")
        val afterScheme = url.substring(scheme.length + 3)
        val authority = afterScheme.substringBefore('/')
        if ('@' in authority || '?' in afterScheme || '#' in afterScheme) {
            throw UserInputException("Sunucu adresine kullanıcı bilgisi, sorgu veya # eklemeyin.")
        }
        var path = afterScheme.substring(authority.length).trimEnd('/')
        if (path.endsWith("/player_api.php", ignoreCase = true)) path = path.dropLast(15)
        return "$scheme://$authority$path".trimEnd('/')
    }

    private fun query(username: String, password: String) = "username=${escapeData(username)}&password=${escapeData(password)}"

    fun apiUrl(server: String, username: String, password: String, action: String = "") =
        "${normalizeServer(server)}/player_api.php?${query(username, password)}" + if (action.isEmpty()) "" else "&action=$action"

    /** user_info.auth must be "1" and status empty or Active. */
    fun isAuthenticated(response: Any?): Boolean {
        val info = (response as? Map<*, *>)?.get("user_info") ?: return false
        val status = Json.text(info, "status")
        return Json.text(info, "auth") == "1" && (status.isEmpty() || status.equals("Active", ignoreCase = true))
    }

    fun parseLiveStreams(sourceId: String, server: String, username: String, password: String, categories: Any?, streams: Any?): List<Channel> {
        val groups = HashMap<String, String>()
        (categories as? List<*>)?.forEach { groups[Json.text(it, "category_id")] = Json.text(it, "category_name") }
        if (streams !is List<*>) throw UserInputException("Sunucu geçerli bir canlı kanal listesi döndürmedi.")
        val base = "${normalizeServer(server)}/live/${escapeData(username)}/${escapeData(password)}/"
        val list = ArrayList<Channel>(streams.size)
        val seen = HashSet<String>()
        for (item in streams) {
            val id = Json.text(item, "stream_id")
            if (id.isEmpty()) continue
            val name = Json.text(item, "name").ifEmpty { "Kanal $id" }
            val key = Channel.key(sourceId, id, name)
            if (seen.add(key)) {
                list.add(Channel(key, sourceId, name, "$base${escapeData(id)}.ts", Json.text(item, "epg_channel_id"), name,
                    Json.text(item, "stream_icon"), groups[Json.text(item, "category_id")] ?: Channel.UNCATEGORIZED))
            }
        }
        return list
    }

    /** .NET Uri.EscapeDataString: everything except unreserved characters is percent-encoded (UTF-8, upper-case). */
    fun escapeData(s: String): String {
        val out = StringBuilder(s.length + 8)
        for (b in s.toByteArray(Charsets.UTF_8)) {
            val v = b.toInt() and 0xFF
            val c = v.toChar()
            if (v < 0x80 && (c in 'A'..'Z' || c in 'a'..'z' || c in '0'..'9' || c in "-._~")) out.append(c)
            else out.append('%').append("0123456789ABCDEF"[v ushr 4]).append("0123456789ABCDEF"[v and 0x0F])
        }
        return out.toString()
    }
}
