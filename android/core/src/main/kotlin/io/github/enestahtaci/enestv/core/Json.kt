package io.github.enestahtaci.enestv.core

/** A JSON number kept as written, so "12" and 12 read the same as text (CORE-SPEC §3.2). */
@JvmInline
value class JsonNumber(val text: String) {
    override fun toString() = text
}

/**
 * Small dependency-free JSON reader (objects keep key order; numbers stay as [JsonNumber]). Used by the core and the
 * fixture tests; android.util.JsonReader and org.json are not available in plain JVM unit tests.
 */
object Json {
    fun parse(text: String): Any? = Reader(text).run { value().also { ws(); if (pos != text.length) fail("trailing data") } }

    /** CORE-SPEC §3.2: a field read as text; null, missing and non-scalar values are "". */
    fun text(obj: Any?, name: String): String = when (val v = (obj as? Map<*, *>)?.get(name)) {
        is String -> v
        is JsonNumber -> v.text
        true -> "True"
        false -> "False"
        else -> ""
    }

    /** A JSON string literal. */
    fun quote(s: String): String = buildString(s.length + 2) {
        append('"')
        for (c in s) when {
            c == '"' -> append("\\\"")
            c == '\\' -> append("\\\\")
            c == '\n' -> append("\\n")
            c == '\r' -> append("\\r")
            c == '\t' -> append("\\t")
            c < ' ' -> append("\\u").append(String.format("%04x", c.code))
            else -> append(c)
        }
        append('"')
    }

    /** Writes a JSON object from string fields (null values become JSON null). */
    fun obj(vararg fields: Pair<String, String?>): String =
        fields.joinToString(",", "{", "}") { (k, v) -> quote(k) + ":" + (v?.let(::quote) ?: "null") }

    class JsonException(message: String) : Exception(message)

    private class Reader(val s: String) {
        var pos = 0

        fun fail(what: String): Nothing = throw JsonException("$what at $pos")

        fun ws() { while (pos < s.length && s[pos].isWhitespace()) pos++ }

        fun peek(): Char = if (pos < s.length) s[pos] else fail("unexpected end")

        fun value(): Any? {
            ws()
            return when (val c = peek()) {
                '{' -> obj()
                '[' -> arr()
                '"' -> str()
                't' -> literal("true", true)
                'f' -> literal("false", false)
                'n' -> literal("null", null)
                else -> if (c == '-' || c.isDigit()) num() else fail("unexpected '$c'")
            }
        }

        fun literal(word: String, v: Any?): Any? {
            if (!s.startsWith(word, pos)) fail("bad literal")
            pos += word.length
            return v
        }

        fun obj(): Map<String, Any?> {
            val map = LinkedHashMap<String, Any?>()
            pos++; ws()
            if (peek() == '}') { pos++; return map }
            while (true) {
                ws(); val k = str(); ws()
                if (s[pos++] != ':') fail("expected ':'")
                map[k] = value(); ws()
                when (s[pos++]) { '}' -> return map; ',' -> Unit; else -> fail("expected ',' or '}'") }
            }
        }

        fun arr(): List<Any?> {
            val list = ArrayList<Any?>()
            pos++; ws()
            if (peek() == ']') { pos++; return list }
            while (true) {
                list.add(value()); ws()
                when (s[pos++]) { ']' -> return list; ',' -> Unit; else -> fail("expected ',' or ']'") }
            }
        }

        fun str(): String {
            if (peek() != '"') fail("expected string")
            pos++
            val start = pos
            while (pos < s.length && s[pos] != '"' && s[pos] != '\\') pos++
            if (pos < s.length && s[pos] == '"') return s.substring(start, pos++) // fast path: no escapes
            val sb = StringBuilder().append(s, start, pos)
            while (true) {
                val c = if (pos < s.length) s[pos++] else fail("unterminated string")
                when (c) {
                    '"' -> return sb.toString()
                    '\\' -> when (val e = s[pos++]) {
                        'n' -> sb.append('\n'); 't' -> sb.append('\t'); 'r' -> sb.append('\r'); 'b' -> sb.append('\b')
                        'f' -> sb.append('\u000C'); 'u' -> { sb.append(s.substring(pos, pos + 4).toInt(16).toChar()); pos += 4 }
                        else -> sb.append(e)
                    }
                    else -> sb.append(c)
                }
            }
        }

        fun num(): JsonNumber {
            val start = pos
            while (pos < s.length && (s[pos].isDigit() || s[pos] in "+-.eE")) pos++
            return JsonNumber(s.substring(start, pos))
        }
    }
}
