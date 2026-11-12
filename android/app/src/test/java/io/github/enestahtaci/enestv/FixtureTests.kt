package io.github.enestahtaci.enestv

import io.github.enestahtaci.enestv.core.BackupFile
import io.github.enestahtaci.enestv.core.Channel
import io.github.enestahtaci.enestv.core.FoldedIndex
import io.github.enestahtaci.enestv.core.Json
import io.github.enestahtaci.enestv.core.M3uParser
import io.github.enestahtaci.enestv.core.TextSearch
import io.github.enestahtaci.enestv.core.UrlNormalizer
import io.github.enestahtaci.enestv.core.UserInputException
import io.github.enestahtaci.enestv.core.Xtream
import org.junit.Assert.assertEquals
import org.junit.Assert.assertThrows
import org.junit.Assert.assertTrue
import org.junit.Test
import java.io.File

/** Runs the language-neutral fixtures shared with the Windows core (fixtures/README.md). */
class FixtureTests {
    private val root = File(System.getProperty("enestv.fixtures") ?: "../../fixtures")

    private fun read(path: String) = Json.parse(File(root, path).readText(Charsets.UTF_8))

    @Suppress("UNCHECKED_CAST")
    private fun json(path: String) = read(path) as Map<String, Any?>

    @Suppress("UNCHECKED_CAST")
    private fun Map<String, Any?>.list(key: String) = this[key] as List<Map<String, Any?>>

    private fun channels(expected: Map<String, Any?>) = expected.list("channels").map {
        Channel(it["id"] as String, it["sourceId"] as String, it["name"] as String, it["url"] as String,
            it["tvgId"] as String, it["tvgName"] as String, it["logo"] as String, it["category"] as String)
    }

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
            val actual = M3uParser.parse({ file.inputStream() }, expected["sourceId"] as String, expected["baseUri"] as String?)
            assertEquals(file.name, channels(expected), actual)
        }
    }

    @Test
    fun xtreamServerNormalization() {
        val cases = json("xtream/normalize-server.json")
        for (c in cases.list("valid")) assertEquals(c["input"] as String, c["expected"], Xtream.normalizeServer(c["input"] as String))
        for (input in cases["rejected"] as List<*>) assertThrows(input as String, UserInputException::class.java) { Xtream.normalizeServer(input) }
    }

    @Test
    fun xtreamLiveStreams() {
        val account = json("xtream/account.json")
        val expected = json("xtream/live.expected.json")
        val actual = Xtream.parseLiveStreams(expected["sourceId"] as String, account["server"] as String, account["username"] as String,
            account["password"] as String, read("xtream/live-categories.json"), read("xtream/live-streams.json"))
        assertEquals(channels(expected), actual)
    }

    // Cross-platform backup: the sample made by the Windows core must decrypt to exactly sample.expected.json.
    @Test
    fun backupSampleDecrypts() {
        val file = File(root, "backup/sample.enestv").readBytes()
        val payload = BackupFile.decrypt(file, "enestv-fixture-parola")
        assertEquals(read("backup/sample.expected.json"), payload)
        val xtream = BackupFile.sources(payload).first { it.kind == "Xtream" }
        assertEquals("p&ss/ğüş", xtream.password)
        // NFC: the same passphrase typed in decomposed form still opens the file.
        val decomposed = java.text.Normalizer.normalize("enestv-fixture-parola", java.text.Normalizer.Form.NFD)
        assertEquals(payload, BackupFile.decrypt(file, decomposed))
        val wrong = assertThrows(UserInputException::class.java) { BackupFile.decrypt(file, "yanlış-parola-123") }
        assertTrue(wrong.message!!.contains("Parola yanlış"))
        assertThrows(UserInputException::class.java) { BackupFile.decrypt("{\"hello\":1}".toByteArray(), "enestv-fixture-parola") }
    }

    @Test
    fun pbkdf2MatchesRfcVector() {
        // RFC 7914 §11 PBKDF2-HMAC-SHA256 test vector (P="passwd", S="salt", c=1), first 32 bytes.
        val key = BackupFile.pbkdf2Sha256("passwd".toByteArray(), "salt".toByteArray(), 1)
        assertEquals("55ac046e56e3089fec1691c22544b605f94185216dde0465e68b9d57c20dacbc", key.joinToString("") { "%02x".format(it) })
    }

    @Test
    fun xtreamAuthentication() {
        assertTrue(Xtream.isAuthenticated(Json.parse("""{"user_info":{"auth":1,"status":"Active"}}""")))
        assertTrue(Xtream.isAuthenticated(Json.parse("""{"user_info":{"auth":"1"}}""")))
        assertTrue(!Xtream.isAuthenticated(Json.parse("""{"user_info":{"auth":0}}""")))
        assertTrue(!Xtream.isAuthenticated(Json.parse("""{"user_info":{"auth":"1","status":"Expired"}}""")))
        assertTrue(!Xtream.isAuthenticated(Json.parse("""[]""")))
    }
}
