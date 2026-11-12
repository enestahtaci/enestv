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
            account["password"] as String, read("xtream/live-categories.json"), File(root, "xtream/live-streams.json").reader(Charsets.UTF_8))
        assertEquals(channels(expected), actual)
    }

    @Test
    fun jsonStreamReadsTextLikeTheTreeReader() {
        val text = """[ {"a":"x\"yç","n":-12.5e3,"t":true,"f":false,"z":null,"o":{"k":[1,{"d":2}]},"s":""}, 7, [] ]"""
        val tree = (Json.parse(text) as List<*>)[0] as Map<*, *>
        val stream = io.github.enestahtaci.enestv.core.JsonStream(java.io.StringReader(text))
        stream.beginArray(); assertTrue(stream.hasNext()); stream.beginObject()
        while (stream.hasNext()) { val name = stream.nextName(); assertEquals(name, Json.text(tree, name), stream.nextText()) }
        assertTrue(stream.hasNext()); stream.skipValue(); assertTrue(stream.hasNext()); stream.skipValue()
        assertTrue(!stream.hasNext())
    }

    @Test
    fun channelSnapshotRoundTrips() {
        val channels = listOf(
            Channel("A1", "src", "ŞİMDİ TV", "http://example.invalid/1", "t1", "ŞİMDİ", "", "Haber"),
            Channel("B2", "src", "", "rtmp://example.invalid/2", category = "Haber"),
        )
        assertEquals(channels, io.github.enestahtaci.enestv.core.ChannelSnapshot.decode(io.github.enestahtaci.enestv.core.ChannelSnapshot.encode(channels)))
        assertEquals(emptyList<Channel>(), io.github.enestahtaci.enestv.core.ChannelSnapshot.decode(io.github.enestahtaci.enestv.core.ChannelSnapshot.encode(emptyList())))
        assertThrows(java.io.IOException::class.java) { io.github.enestahtaci.enestv.core.ChannelSnapshot.decode(byteArrayOf(1, 2, 3, 4, 5, 6, 7, 8)) }
    }

    // Large account: 30,000 channels (≈ 10 MB of JSON) through the streaming parser.
    @Test
    fun xtreamLargeAccountStreams() {
        val file = File.createTempFile("streams", ".json")
        try {
            file.bufferedWriter().use { w ->
                w.write("[")
                for (i in 1..30_000) {
                    if (i > 1) w.write(",")
                    w.write("""{"num":$i,"name":"Kanal Ünlü $i HD","stream_type":"live","stream_id":$i,"stream_icon":"http://logo.example.invalid/$i.png","epg_channel_id":"k$i.tr","added":"1700000000","category_id":"${i % 40}","custom_sid":"","tv_archive":0,"direct_source":"","tv_archive_duration":0}""")
                }
                w.write("]")
            }
            val t0 = System.nanoTime()
            val channels = Xtream.parseLiveStreams("acc", "http://example.invalid", "u", "p", null, file.reader(Charsets.UTF_8))
            val ms = (System.nanoTime() - t0) / 1_000_000
            println("30k channels (${file.length() / 1024} KB) parsed in $ms ms")
            assertEquals(30_000, channels.size)
            assertEquals("http://example.invalid/live/u/p/30000.ts", channels.last().url)
        } finally {
            file.delete()
        }
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
        val empty = assertThrows(UserInputException::class.java) { BackupFile.decrypt(file, "") }
        assertTrue(empty.message!!.contains("Parola yanlış"))
        assertThrows(UserInputException::class.java) { BackupFile.decrypt("{\"hello\":1}".toByteArray(), "enestv-fixture-parola") }
    }

    // Reverse direction: a backup encrypted by this core must open on Windows (EnesTV.Tests decrypts the same file).
    // Regenerate with: gradlew :core:test -PgenerateFixtures=true (then review the diff).
    @Test
    fun androidSampleFixture() {
        @Suppress("UNCHECKED_CAST")
        val expected = read("backup/sample.expected.json") as Map<String, Any?>
        val file = File(root, "backup/sample-android.enestv")
        if (System.getProperty("enestv.generateFixtures") == "true") file.writeBytes(BackupFile.encrypt(expected, "enestv-fixture-parola", 10_000))
        assertEquals(expected, BackupFile.decrypt(file.readBytes(), "enestv-fixture-parola"))
    }

    @Test
    fun backupRoundTripsAndChecksPassphrase() {
        @Suppress("UNCHECKED_CAST")
        val payload = read("backup/sample.expected.json") as Map<String, Any?>
        val file = BackupFile.encrypt(payload, "Şifre-Ğüç-2026", 10_000)
        assertEquals(payload, BackupFile.decrypt(file, java.text.Normalizer.normalize("Şifre-Ğüç-2026", java.text.Normalizer.Form.NFD)))
        assertEquals(BackupFile.channels(payload).size, 2)
        assertThrows(UserInputException::class.java) { BackupFile.encrypt(payload, "kısa", 10_000) }
        // Types survive: the Windows reader rejects "75" for a number or "true" for a boolean.
        val text = Json.write(payload)
        assertTrue(text.contains("\"defaultVolume\":") && !text.contains("\"defaultVolume\":\""))
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
