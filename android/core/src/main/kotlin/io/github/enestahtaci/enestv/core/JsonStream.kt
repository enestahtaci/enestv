package io.github.enestahtaci.enestv.core

import java.io.Reader

/**
 * Pull-style JSON reader over a [Reader] with its own buffer: large Xtream responses (tens of MB) are read one value at
 * a time, so memory stays flat. Values are read as text with the same rules as [Json.text] (CORE-SPEC §3.2).
 */
class JsonStream(private val reader: Reader) {
    private val buffer = CharArray(64 * 1024)
    private var pos = 0
    private var limit = 0
    private val text = StringBuilder()

    private fun fill(): Boolean {
        limit = reader.read(buffer)
        pos = 0
        return limit > 0
    }

    private fun peekRaw(): Int {
        if (pos >= limit && !fill()) return -1
        return buffer[pos].code
    }

    /** Next non-whitespace character without consuming it, or -1 at the end. */
    fun peek(): Int {
        while (true) {
            val c = peekRaw()
            if (c < 0 || !(c == ' '.code || c == '\n'.code || c == '\r'.code || c == '\t'.code)) return c
            pos++
        }
    }

    private fun next(): Char {
        if (pos >= limit && !fill()) throw Json.JsonException("unexpected end")
        return buffer[pos++]
    }

    private fun expect(c: Char) {
        val p = peek()
        if (p != c.code) throw Json.JsonException("expected '$c'")
        pos++
    }

    fun beginArray() = expect('[')
    fun beginObject() = expect('{')

    /** True if the current array/object has another element; consumes separating commas and the closing bracket. */
    fun hasNext(): Boolean {
        when (peek()) {
            ','.code -> { pos++; return true }
            ']'.code, '}'.code -> { pos++; return false }
            -1 -> throw Json.JsonException("unexpected end")
            else -> return true
        }
    }

    fun nextName(): String {
        val name = string()
        expect(':')
        return name
    }

    /** A scalar as text (string as is, number as written, booleans "True"/"False", null ""); objects/arrays are skipped as "". */
    fun nextText(): String = when (peek()) {
        '"'.code -> string()
        '{'.code, '['.code -> { skipValue(); "" }
        't'.code -> { literal("true"); "True" }
        'f'.code -> { literal("false"); "False" }
        'n'.code -> { literal("null"); "" }
        else -> number()
    }

    fun skipValue() {
        when (peek()) {
            '{'.code -> { beginObject(); while (hasNext()) { nextName(); skipValue() } }
            '['.code -> { beginArray(); while (hasNext()) skipValue() }
            else -> nextText()
        }
    }

    private fun literal(word: String) {
        for (c in word) if (next() != c) throw Json.JsonException("bad literal")
    }

    private fun number(): String {
        text.setLength(0)
        while (true) {
            val c = peekRaw()
            if (c < 0 || !(c.toChar().isDigit() || c.toChar() in "+-.eE")) break
            text.append(c.toChar()); pos++
        }
        if (text.isEmpty()) throw Json.JsonException("unexpected character")
        return text.toString()
    }

    private fun string(): String {
        if (peek() != '"'.code) throw Json.JsonException("expected string")
        pos++
        text.setLength(0)
        while (true) {
            // Copy runs without escapes straight from the buffer.
            val start = pos
            while (pos < limit && buffer[pos] != '"' && buffer[pos] != '\\') pos++
            text.append(buffer, start, pos - start)
            if (pos >= limit) { if (!fill()) throw Json.JsonException("unterminated string"); continue }
            if (buffer[pos++] == '"') return text.toString()
            when (val e = next()) {
                'n' -> text.append('\n'); 't' -> text.append('\t'); 'r' -> text.append('\r'); 'b' -> text.append('\b')
                'f' -> text.append('\u000C')
                'u' -> { var v = 0; repeat(4) { v = v * 16 + Character.digit(next(), 16) }; text.append(v.toChar()) }
                else -> text.append(e)
            }
        }
    }
}
