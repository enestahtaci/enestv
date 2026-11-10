package io.github.enestahtaci.enestv.core

/**
 * Case-insensitive search that understands Turkish letters (docs/CORE-SPEC.md §4a, fixtures/search/cases.json).
 * Every i-letter (I, ı, İ, i) is one letter; other characters compare by their locale-independent lower case.
 * Folding is one char to one char, so a folded text can be searched with a plain indexOf.
 */
object TextSearch {
    fun fold(c: Char): Char = when (c) {
        'I', 'ı', 'İ' -> 'i'
        else -> c.lowercaseChar()
    }

    fun fold(text: String): String {
        val chars = CharArray(text.length)
        for (i in text.indices) chars[i] = fold(text[i])
        return String(chars)
    }

    /** True when the trimmed [query] occurs in [text]; an empty query matches everything. */
    fun contains(text: String, query: String?): Boolean {
        val q = query.orEmpty().trim()
        return q.isEmpty() || fold(text).contains(fold(q))
    }

    fun equals(a: String, b: String): Boolean {
        if (a.length != b.length) return false
        for (i in a.indices) if (fold(a[i]) != fold(b[i])) return false
        return true
    }
}

/**
 * Search index for large lists: names are folded once, then every keystroke is a scan with indexOf over
 * pre-folded strings — no allocation per item. Returns matching positions in list order.
 */
class FoldedIndex(texts: List<String>) {
    private val folded: Array<String> = Array(texts.size) { TextSearch.fold(texts[it]) }

    val size: Int get() = folded.size

    fun search(query: String?): IntArray {
        val q = TextSearch.fold(query.orEmpty().trim())
        if (q.isEmpty()) return IntArray(folded.size) { it }
        val hits = IntArray(folded.size)
        var count = 0
        for (i in folded.indices) if (folded[i].contains(q)) hits[count++] = i
        return hits.copyOf(count)
    }
}
