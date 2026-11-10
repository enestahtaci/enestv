package io.github.enestahtaci.enestv

import io.github.enestahtaci.enestv.core.Channel
import io.github.enestahtaci.enestv.core.FoldedIndex
import io.github.enestahtaci.enestv.core.M3uParser
import io.github.enestahtaci.enestv.core.TextSearch
import io.github.enestahtaci.enestv.core.UrlNormalizer
import org.junit.Assert.assertEquals
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/** Runs the language-neutral fixtures shared with the Windows core (fixtures/README.md). */
class FixtureTests {
    private val root = File(System.getProperty("enestv.fixtures") ?: "../../fixtures")

    @Suppress("UNCHECKED_CAST")
    private fun json(path: String) = MiniJson.parse(File(root, path).readText(Charsets.UTF_8)) as Map<String, Any?>

    @Suppress("UNCHECKED_CAST")
    private fun Map<String, Any?>.list(key: String) = this[key] as List<Map<String, Any?>>

    @Test
    fun searchCases() {
        val cases = json("search/cases.json")
        for (c in cases.list("contains")) {
            val text = c["text"] as String; val query = c["query"] as String
            assertEquals("contains('$text', '$query')", c["expected"], TextSearch.contains(text, query))
            assertEquals("index('$text', '$query')", c["expected"], FoldedIndex(listOf(text)).search(query).isNotEmpty())
        }
        for (c in cases.list("equals")) {
            assertEquals("equals('${c["a"]}', '${c["b"]}')", c["expected"], TextSearch.equals(c["a"] as String, c["b"] as String))
        }
    }

    @Test
    fun urlNormalization() {
        val cases = json("m3u/urls.json")
        val failures = ArrayList<String>()
        for (c in cases.list("absolute")) {
            val actual = UrlNormalizer.absolute(c["input"] as String)
            if (actual != c["expected"]) failures += "absolute ${c["input"]}: expected ${c["expected"]}, got $actual"
        }
        val base = cases["baseUri"] as String
        for (c in cases.list("relative")) {
            val actual = UrlNormalizer.resolve(base, c["input"] as String)
            if (actual != c["expected"]) failures += "relative ${c["input"]}: expected ${c["expected"]}, got $actual"
        }
        assertTrue(failures.joinToString("\n"), failures.isEmpty())
    }

    @Test
    fun m3uPlaylists() {
        val files = File(root, "m3u").listFiles { f -> f.name.endsWith(".m3u") }!!.sortedBy { it.name }
        assertTrue(files.size >= 4)
        for (file in files) {
            val expected = json("m3u/${file.nameWithoutExtension}.expected.json")
            val channels = M3uParser.parse({ file.inputStream() }, expected["sourceId"] as String, expected["baseUri"] as String?)
            val want = expected.list("channels").map {
                Channel(it["id"] as String, it["sourceId"] as String, it["name"] as String, it["url"] as String,
                    it["tvgId"] as String, it["tvgName"] as String, it["logo"] as String, it["category"] as String)
            }
            assertEquals(file.name, want, channels)
        }
    }
}
