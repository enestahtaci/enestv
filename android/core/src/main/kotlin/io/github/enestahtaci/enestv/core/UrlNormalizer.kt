package io.github.enestahtaci.enestv.core

/**
 * Absolute-URL normalization identical to the Windows core (`Uri.AbsoluteUri` of .NET on Windows), because channel
 * keys are hashed from the URL (CORE-SPEC §2, fixtures/m3u/urls.json). java.net.URI differs in many details
 * (escaping, default ports, Unicode hosts), so this is a small hand-written parser instead.
 */
object UrlNormalizer {
    private val defaultPorts = mapOf("http" to 80, "https" to 443, "ftp" to 21)
    private const val HEX = "0123456789ABCDEF"

    /** Normalized absolute URL, or null when [input] is not an absolute hierarchical URL. */
    fun absolute(input: String): String? {
        val colon = schemeEnd(input)
        if (colon < 0 || !input.startsWith("//", colon + 1)) return null
        val scheme = input.substring(0, colon).lowercase()
        var rest = input.substring(colon + 3)

        // Backslashes before the query/fragment are path separators, as in .NET.
        val tail = rest.indexOfAny(charArrayOf('?', '#')).let { if (it < 0) rest.length else it }
        rest = rest.substring(0, tail).replace('\\', '/') + rest.substring(tail)

        val authorityEnd = rest.indexOfAny(charArrayOf('/', '?', '#')).let { if (it < 0) rest.length else it }
        val authority = rest.substring(0, authorityEnd)
        var remainder = rest.substring(authorityEnd)

        val at = authority.lastIndexOf('@')
        val userInfo = if (at >= 0) authority.substring(0, at) else null
        val hostPort = if (at >= 0) authority.substring(at + 1) else authority
        val portStart = if (hostPort.startsWith("[")) hostPort.indexOf(']').let { if (it < 0) return null else hostPort.indexOf(':', it) } else hostPort.lastIndexOf(':')
        val host = if (portStart >= 0) hostPort.substring(0, portStart) else hostPort
        val portText = if (portStart >= 0) hostPort.substring(portStart + 1) else ""
        if (host.isEmpty() && scheme != "file") return null
        var port: Int? = null
        if (portText.isNotEmpty()) {
            if (portText.any { it !in '0'..'9' } || portText.length > 5) return null
            port = portText.toInt().takeIf { it <= 65535 } ?: return null
            if (defaultPorts[scheme] == port) port = null
        }

        val fragmentAt = remainder.indexOf('#')
        val fragment = if (fragmentAt >= 0) remainder.substring(fragmentAt + 1) else null
        if (fragmentAt >= 0) remainder = remainder.substring(0, fragmentAt)
        val queryAt = remainder.indexOf('?')
        val query = if (queryAt >= 0) remainder.substring(queryAt + 1) else null
        val path = removeDotSegments(escape(if (queryAt >= 0) remainder.substring(0, queryAt) else remainder)).ifEmpty { "/" }

        return buildString {
            append(scheme).append("://")
            if (userInfo != null) append(escape(userInfo)).append('@')
            append(lowerHost(host))
            if (port != null) append(':').append(port)
            append(path)
            if (query != null) append('?').append(escape(query))
            if (fragment != null) append('#').append(escape(fragment))
        }
    }

    /** Resolves [reference] against an absolute [base] URL (RFC 3986 §5.2) and normalizes the result. */
    fun resolve(base: String, reference: String): String? {
        if (schemeEnd(reference) >= 0) return absolute(reference)
        val b = absolute(base) ?: return null
        val schemeEnd = b.indexOf("://") + 3
        val pathStart = b.indexOf('/', schemeEnd).let { if (it < 0) b.length else it }
        val withoutFragment = b.substringBefore('#')
        val withoutQuery = withoutFragment.substringBefore('?')
        return absolute(
            when {
                reference.startsWith("//") -> b.substring(0, schemeEnd - 2) + reference
                reference.startsWith("/") || reference.startsWith("\\") -> b.substring(0, pathStart) + reference
                reference.startsWith("?") -> withoutQuery + reference
                reference.startsWith("#") -> withoutFragment + reference
                reference.isEmpty() -> withoutFragment
                else -> withoutQuery.substring(0, withoutQuery.lastIndexOf('/') + 1) + reference
            },
        )
    }

    private fun schemeEnd(s: String): Int {
        if (s.isEmpty() || !s[0].isAsciiLetter()) return -1
        for (i in 1 until s.length) {
            val c = s[i]
            if (c == ':') return i
            if (!(c.isAsciiLetter() || c in '0'..'9' || c == '+' || c == '-' || c == '.')) return -1
        }
        return -1
    }

    private fun Char.isAsciiLetter() = this in 'a'..'z' || this in 'A'..'Z'

    // .NET lower-cases the host but leaves dotted capital İ unchanged.
    private fun lowerHost(host: String) = buildString(host.length) { for (c in host) append(if (c == 'İ') c else c.lowercaseChar()) }

    private fun isUnreserved(c: Char) = c.isAsciiLetter() || c in '0'..'9' || c == '-' || c == '.' || c == '_' || c == '~'

    // Kept literally: unreserved, sub-delims, ':', '@', '/', '?', '[', ']'. Everything else is percent-encoded as UTF-8.
    private fun isAllowed(c: Char) = isUnreserved(c) || c in "!$&'()*+,;=:@/?[]"

    /**
     * Valid escapes of unreserved ASCII are decoded (%7E → ~), escapes of other ASCII are kept as written (%2f stays),
     * escapes of non-ASCII bytes are upper-cased, a stray % becomes %25, disallowed characters are encoded.
     */
    private fun escape(s: String): String {
        val out = StringBuilder(s.length + 8)
        var i = 0
        while (i < s.length) {
            val c = s[i]
            if (c == '%') {
                val hi = if (i + 2 < s.length) Character.digit(s[i + 1], 16) else -1
                val lo = if (hi >= 0) Character.digit(s[i + 2], 16) else -1
                if (hi >= 0 && lo >= 0) {
                    val b = hi * 16 + lo
                    when {
                        b < 0x80 && isUnreserved(b.toChar()) -> out.append(b.toChar())
                        b < 0x80 -> out.append(s, i, i + 3)
                        else -> out.append('%').append(HEX[hi]).append(HEX[lo])
                    }
                    i += 3
                    continue
                }
                out.append("%25")
            } else if (c.code < 0x80 && isAllowed(c)) {
                out.append(c)
            } else {
                val end = if (Character.isHighSurrogate(c) && i + 1 < s.length) i + 2 else i + 1
                for (byte in s.substring(i, end).toByteArray(Charsets.UTF_8)) {
                    val v = byte.toInt() and 0xFF
                    out.append('%').append(HEX[v ushr 4]).append(HEX[v and 0x0F])
                }
                i = end
                continue
            }
            i++
        }
        return out.toString()
    }

    /** RFC 3986 §5.2.4. */
    private fun removeDotSegments(path: String): String {
        if (!path.contains('.')) return path
        var input = path
        val output = StringBuilder()
        while (input.isNotEmpty()) {
            when {
                input.startsWith("../") -> input = input.substring(3)
                input.startsWith("./") -> input = input.substring(2)
                input.startsWith("/./") -> input = input.substring(2)
                input == "/." -> input = "/"
                input.startsWith("/../") -> { input = input.substring(3); output.setLength(maxOf(0, output.lastIndexOf("/"))) }
                input == "/.." -> { input = "/"; output.setLength(maxOf(0, output.lastIndexOf("/"))) }
                input == "." || input == ".." -> input = ""
                else -> {
                    val next = input.indexOf('/', 1).let { if (it < 0) input.length else it }
                    output.append(input, 0, next)
                    input = input.substring(next)
                }
            }
        }
        return output.toString()
    }
}
