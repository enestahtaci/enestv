package io.github.enestahtaci.enestv

/** Small JSON reader for the shared fixtures (tests only), so no JSON library is needed. Objects keep key order. */
object MiniJson {
    fun parse(text: String): Any? = Reader(text).run { value().also { ws(); require(pos == text.length) { "trailing data at $pos" } } }

    private class Reader(val s: String) {
        var pos = 0

        fun ws() { while (pos < s.length && s[pos].isWhitespace()) pos++ }

        fun value(): Any? {
            ws()
            return when (val c = s[pos]) {
                '{' -> obj()
                '[' -> arr()
                '"' -> str()
                't' -> literal("true", true)
                'f' -> literal("false", false)
                'n' -> literal("null", null)
                else -> if (c == '-' || c.isDigit()) num() else error("unexpected '$c' at $pos")
            }
        }

        fun literal(word: String, v: Any?): Any? { require(s.startsWith(word, pos)); pos += word.length; return v }

        fun obj(): Map<String, Any?> {
            val map = LinkedHashMap<String, Any?>(); pos++; ws()
            if (s[pos] == '}') { pos++; return map }
            while (true) {
                ws(); val k = str(); ws(); require(s[pos++] == ':'); map[k] = value(); ws()
                if (s[pos++] == '}') return map
            }
        }

        fun arr(): List<Any?> {
            val list = ArrayList<Any?>(); pos++; ws()
            if (s[pos] == ']') { pos++; return list }
            while (true) {
                list.add(value()); ws()
                if (s[pos++] == ']') return list
            }
        }

        fun str(): String {
            require(s[pos++] == '"')
            val sb = StringBuilder()
            while (true) {
                val c = s[pos++]
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

        fun num(): Number {
            val start = pos
            while (pos < s.length && (s[pos].isDigit() || s[pos] in "+-.eE")) pos++
            val t = s.substring(start, pos)
            return if (t.any { it in ".eE" }) t.toDouble() else t.toLong()
        }
    }
}
