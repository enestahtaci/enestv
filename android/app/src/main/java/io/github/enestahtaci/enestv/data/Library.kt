package io.github.enestahtaci.enestv.data

import android.content.ContentValues
import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper
import android.os.SystemClock
import android.util.Log
import io.github.enestahtaci.enestv.core.AuthenticationException
import io.github.enestahtaci.enestv.core.BackupFile
import io.github.enestahtaci.enestv.core.Channel
import io.github.enestahtaci.enestv.core.ChannelSnapshot
import io.github.enestahtaci.enestv.core.Json
import io.github.enestahtaci.enestv.core.JsonStream
import io.github.enestahtaci.enestv.core.M3uParser
import io.github.enestahtaci.enestv.core.UrlNormalizer
import io.github.enestahtaci.enestv.core.UserInputException
import io.github.enestahtaci.enestv.core.Xtream
import java.io.File
import java.util.UUID

/** One playlist or account. [kind] uses the backup's names (CORE-SPEC §6a). */
data class Source(
    val id: String,
    val name: String,
    val kind: Kind,
    val location: String,
    val username: String = "",
    val password: String = "",
    val updated: Long? = null, // epoch ms of the last successful refresh
) {
    enum class Kind(val backupName: String) {
        M3U_URL("M3uUrl"), XTREAM("Xtream");

        companion object {
            fun fromBackup(name: String) = entries.firstOrNull { it.backupName == name }
        }
    }

    companion object {
        /** Same form as Windows (Guid "N"): 32 lower-case hex characters. */
        fun newId(): String = UUID.randomUUID().toString().replace("-", "")
    }
}

/**
 * The user's library on this device. Channel lists: one encrypted [ChannelSnapshot] file per source (fast start).
 * User data (sources, favorites, recent, settings, layout, progress): SQLite, secrets encrypted with [Crypto].
 * Call from a background thread; the instance is shared by the app.
 */
class Library private constructor(private val context: Context, private val crypto: Crypto) {
    private val db = Db(context).writableDatabase
    private val channelDir = File(context.filesDir, "channels").apply { mkdirs() }

    // ---- sources ----

    fun sources(): List<Source> = db.rawQuery("SELECT id,name,kind,secret,updated FROM sources ORDER BY position,rowid", null).use { c ->
        buildList {
            while (c.moveToNext()) {
                val kind = runCatching { Source.Kind.valueOf(c.getString(2)) }.getOrNull() ?: continue
                val secret = Json.parse(crypto.openText(c.getBlob(3)))
                add(Source(c.getString(0), c.getString(1), kind, Json.text(secret, "location"), Json.text(secret, "username"),
                    Json.text(secret, "password"), if (c.isNull(4)) null else c.getLong(4)))
            }
        }
    }

    /** Inserts or replaces (same id) a source; a new source goes to the end of the list. */
    fun saveSource(source: Source) {
        val values = ContentValues().apply {
            put("id", source.id); put("name", source.name); put("kind", source.kind.name)
            put("secret", crypto.sealText(Json.obj("location" to source.location, "username" to source.username, "password" to source.password)))
            if (source.updated == null) putNull("updated") else put("updated", source.updated)
        }
        db.beginTransaction()
        try {
            if (db.update("sources", values, "id=?", arrayOf(source.id)) == 0) {
                values.put("position", db.compileStatement("SELECT IFNULL(MAX(position),0)+1 FROM sources").simpleQueryForLong())
                db.insertOrThrow("sources", null, values)
            }
            db.setTransactionSuccessful()
        } finally {
            db.endTransaction()
        }
    }

    /** Removes the source with its channels, and the favorites/recent/progress that belonged to it (CORE-SPEC §6). */
    fun deleteSource(id: String) {
        val keys = channelsOf(id).map { it.id }
        db.beginTransaction()
        try {
            db.delete("sources", "id=?", arrayOf(id))
            db.delete("progress", "source_id=?", arrayOf(id))
            for (key in keys) {
                db.delete("favorites", "channel_id=?", arrayOf(key))
                db.delete("recent", "channel_id=?", arrayOf(key))
            }
            db.setTransactionSuccessful()
        } finally {
            db.endTransaction()
        }
        snapshotFile(id).delete()
    }

    // ---- channels ----

    /** All channels in source order, from the local snapshots (no network). Sources never loaded are skipped. */
    fun channels(sources: List<Source> = sources()): List<Channel> {
        val all = ArrayList<Channel>()
        for (s in sources) all.addAll(channelsOf(s.id))
        return all
    }

    private fun channelsOf(sourceId: String): List<Channel> {
        val file = snapshotFile(sourceId)
        if (!file.isFile) return emptyList()
        return try {
            ChannelSnapshot.decode(crypto.open(file.readBytes()))
        } catch (e: Exception) {
            Log.w(TAG, "channel snapshot unreadable: ${e.javaClass.simpleName}")
            file.delete()
            emptyList()
        }
    }

    fun hasChannels(sourceId: String) = snapshotFile(sourceId).isFile

    /** Downloads and parses the source, stores its channels and returns them. Raw downloads are deleted afterwards. */
    fun refresh(source: Source): List<Channel> {
        val t0 = SystemClock.elapsedRealtime()
        val channels = when (source.kind) {
            Source.Kind.M3U_URL -> {
                val url = UrlNormalizer.absolute(source.location.trim())
                if (url == null || !(url.startsWith("http://") || url.startsWith("https://"))) throw UserInputException("Geçerli bir http:// veya https:// adresi girin.")
                withTemp("playlist.m3u") { file ->
                    Downloader.download(source.location.trim(), file, M3U_LIMIT)
                    M3uParser.parse({ file.inputStream() }, source.id, url)
                }.ifEmpty { throw UserInputException("Kaynakta oynatılabilir kanal bulunamadı. Bir kanal listesi seçin.") }
            }
            Source.Kind.XTREAM -> refreshXtream(source)
        }
        writeSnapshot(source.id, channels)
        saveSource(source.copy(updated = System.currentTimeMillis()))
        Log.i(TAG, "refresh ${source.kind} in ${SystemClock.elapsedRealtime() - t0}ms, channels=${channels.size}")
        return channels
    }

    private fun refreshXtream(source: Source): List<Channel> {
        if (source.username.isBlank() || source.password.isEmpty()) throw UserInputException("Kullanıcı adı ve parola gerekli.")
        fun api(action: String = "") = Xtream.apiUrl(source.location, source.username, source.password, action)
        withTemp("auth.json") { file ->
            Downloader.download(api(), file, API_LIMIT)
            if (!Xtream.isAuthenticated(readJson(file))) throw AuthenticationException()
        }
        val categories = withTemp("categories.json") { file -> Downloader.download(api("get_live_categories"), file, API_LIMIT); readJson(file) }
        return withTemp("streams.json") { file ->
            Downloader.download(api("get_live_streams"), file, API_LIMIT)
            // A JSON object instead of the stream array is an error response (e.g. user_info.auth = 0); it is small.
            if (file.reader(Charsets.UTF_8).use { JsonStream(it).peek() } == '{'.code) {
                readJson(file)
                throw UserInputException("Sunucu geçerli bir canlı kanal listesi döndürmedi.")
            }
            file.reader(Charsets.UTF_8).use { Xtream.parseLiveStreams(source.id, source.location, source.username, source.password, categories, it) }
        }
    }

    private fun <T> withTemp(name: String, block: (File) -> T): T {
        val file = File(context.cacheDir, "download-$name")
        try {
            return block(file)
        } finally {
            file.delete()
            File(file.path + ".part").delete()
        }
    }

    private fun readJson(file: File): Any? {
        val value = try {
            Json.parse(file.readText(Charsets.UTF_8))
        } catch (_: Json.JsonException) {
            throw UserInputException("Sunucu geçerli bir yanıt döndürmedi.")
        }
        if (Json.text((value as? Map<*, *>)?.get("user_info"), "auth") == "0") throw AuthenticationException()
        return value
    }

    private fun snapshotFile(sourceId: String) = File(channelDir, "$sourceId.bin")

    private fun writeSnapshot(sourceId: String, channels: List<Channel>) {
        val file = snapshotFile(sourceId)
        val tmp = File(file.path + ".tmp")
        tmp.writeBytes(crypto.seal(ChannelSnapshot.encode(channels)))
        if (!tmp.renameTo(file)) { file.delete(); tmp.renameTo(file) }
    }

    // ---- favorites and recent ----

    fun favorites(): MutableSet<String> = db.rawQuery("SELECT channel_id FROM favorites", null).use { c ->
        HashSet<String>().apply { while (c.moveToNext()) add(c.getString(0)) }
    }

    fun setFavorite(channelId: String, favorite: Boolean) {
        if (favorite) db.insertWithOnConflict("favorites", null, ContentValues().apply { put("channel_id", channelId); put("added", System.currentTimeMillis()) }, SQLiteDatabase.CONFLICT_IGNORE)
        else db.delete("favorites", "channel_id=?", arrayOf(channelId))
    }

    /** Newest first, at most [RECENT_LIMIT]. */
    fun recent(): List<String> = db.rawQuery("SELECT channel_id FROM recent ORDER BY watched DESC LIMIT $RECENT_LIMIT", null).use { c ->
        buildList { while (c.moveToNext()) add(c.getString(0)) }
    }

    fun addRecent(channelId: String) {
        db.beginTransaction()
        try {
            db.insertWithOnConflict("recent", null, ContentValues().apply { put("channel_id", channelId); put("watched", System.currentTimeMillis()) }, SQLiteDatabase.CONFLICT_REPLACE)
            db.execSQL("DELETE FROM recent WHERE channel_id NOT IN (SELECT channel_id FROM recent ORDER BY watched DESC LIMIT $RECENT_LIMIT)")
            db.setTransactionSuccessful()
        } finally {
            db.endTransaction()
        }
    }

    // ---- backup (CORE-SPEC §6a) ----

    /** The whole library as the backup payload tree (sources with secrets, channels, user data). */
    fun exportPayload(appVersion: String): Map<String, Any?> {
        val sources = sources()
        val progress = db.rawQuery("SELECT value FROM progress", null).use { c ->
            buildList { while (c.moveToNext()) add(Json.parse(crypto.openText(c.getBlob(0)))) }
        }
        return linkedMapOf(
            "version" to BackupFile.VERSION,
            "created" to BackupFile.now(),
            "appVersion" to appVersion,
            "sources" to sources.map { s ->
                linkedMapOf("id" to s.id, "name" to s.name, "kind" to s.kind.backupName, "location" to s.location,
                    "username" to s.username, "password" to s.password, "updated" to s.updated?.let(BackupFile::isoTime))
            },
            "channels" to channels(sources).map(BackupFile::channelTree),
            "favorites" to favorites().toList(),
            "recent" to recent(),
            "settings" to (readTree("settings") ?: BackupFile.defaultSettings()),
            "layout" to (readTree("layout") ?: BackupFile.emptyLayout()),
            "progress" to progress,
        )
    }

    data class RestoreSummary(val sources: Int, val skippedSources: Int, val channels: Int, val favorites: Int)

    fun summarize(payload: Map<*, *>): RestoreSummary {
        val all = BackupFile.sources(payload)
        val supported = all.filter { Source.Kind.fromBackup(it.kind) != null }.map { it.id }.toSet()
        val channels = BackupFile.channels(payload).filter { it.sourceId in supported }
        val keys = channels.mapTo(HashSet()) { it.id }
        return RestoreSummary(supported.size, all.size - supported.size, channels.size, BackupFile.strings(payload, "favorites").count { it in keys })
    }

    /**
     * Replaces the whole library with the backup (CORE-SPEC §6a): a local safety copy first, then sources, channels,
     * favorites and recent (only for channels in the backup), settings, layout and progress (only for its sources).
     * Sources this version cannot open (local M3U files) are skipped. Returns the safety copy folder.
     */
    fun restore(payload: Map<*, *>): File {
        val safety = safetyCopy()
        val sources = BackupFile.sources(payload).mapNotNull { s ->
            val kind = Source.Kind.fromBackup(s.kind) ?: return@mapNotNull null
            Source(s.id, s.name, kind, if (kind == Source.Kind.XTREAM) Xtream.normalizeServer(s.location) else s.location,
                s.username, s.password, BackupFile.epochMs(s.updated))
        }
        val ids = sources.mapTo(HashSet()) { it.id }
        val channels = BackupFile.channels(payload).filter { it.sourceId in ids }
        val keys = channels.mapTo(HashSet()) { it.id }

        db.beginTransaction()
        try {
            for (table in listOf("sources", "favorites", "recent", "settings", "layout", "progress")) db.delete(table, null, null)
            sources.forEach(::saveSource)
            val now = System.currentTimeMillis()
            for (key in BackupFile.strings(payload, "favorites")) if (key in keys) setFavorite(key, true)
            BackupFile.strings(payload, "recent").filter { it in keys }.take(RECENT_LIMIT).forEachIndexed { i, key ->
                db.insertWithOnConflict("recent", null, ContentValues().apply { put("channel_id", key); put("watched", now - i) }, SQLiteDatabase.CONFLICT_IGNORE)
            }
            (payload["settings"] as? Map<*, *>)?.let { writeTree("settings", it) }
            (payload["layout"] as? Map<*, *>)?.let { writeTree("layout", it) }
            for (p in (payload["progress"] as? List<*>).orEmpty()) {
                val sourceId = Json.text(p, "sourceId"); val key = Json.text(p, "key")
                if (sourceId in ids && key.isNotEmpty()) {
                    db.insertWithOnConflict("progress", null, ContentValues().apply {
                        put("source_id", sourceId); put("item_key", key); put("value", crypto.sealText(Json.write(p)))
                    }, SQLiteDatabase.CONFLICT_REPLACE)
                }
            }
            db.setTransactionSuccessful()
        } finally {
            db.endTransaction()
        }
        channelDir.listFiles()?.forEach { it.delete() }
        for ((sourceId, list) in channels.groupBy { it.sourceId }) writeSnapshot(sourceId, list)
        return safety
    }

    /** Copies the database and channel files to files/backups/before-restore-<time>; keeps the newest three copies. */
    private fun safetyCopy(): File {
        val root = File(context.filesDir, "backups").apply { mkdirs() }
        val stamp = java.text.SimpleDateFormat("yyyyMMdd-HHmmss", java.util.Locale.ROOT).format(java.util.Date())
        val target = File(root, "before-restore-$stamp").apply { mkdirs() }
        db.rawQuery("PRAGMA wal_checkpoint(FULL)", null).use { it.moveToFirst() }
        context.getDatabasePath("library.db").copyTo(File(target, "library.db"), overwrite = true)
        channelDir.listFiles()?.forEach { it.copyTo(File(target, "channels/${it.name}"), overwrite = true) }
        // The data key that encrypts these copies stays on this device (Keystore), so they are only usable here.
        root.listFiles()?.filter { it.isDirectory }?.sortedByDescending { it.name }?.drop(3)?.forEach { it.deleteRecursively() }
        return target
    }

    private fun readTree(table: String): Map<*, *>? = db.rawQuery("SELECT value FROM $table WHERE id=1", null).use { c ->
        if (c.moveToFirst()) Json.parse(crypto.openText(c.getBlob(0))) as? Map<*, *> else null
    }

    private fun writeTree(table: String, value: Map<*, *>) {
        db.insertWithOnConflict(table, null, ContentValues().apply { put("id", 1); put("value", crypto.sealText(Json.write(value))) }, SQLiteDatabase.CONFLICT_REPLACE)
    }

    // ---- spike data (single source in SharedPreferences, downloads kept as cache) ----

    /** Moves a source saved by the spike build into the library and deletes the old files. Runs once. */
    fun migrateLegacy() {
        val prefs = context.getSharedPreferences("source", Context.MODE_PRIVATE)
        if (prefs.contains("blob")) {
            LegacySource.read(prefs.getString("blob", null))?.let { saveSource(it) }
            prefs.edit().clear().commit()
            LegacySource.deleteKey()
        }
        for (name in listOf("playlist.m3u", "live-categories.json", "live-streams.json", "auth.json")) File(context.filesDir, name).delete()
    }

    private class Db(context: Context) : SQLiteOpenHelper(context, "library.db", null, 1) {
        override fun onCreate(db: SQLiteDatabase) {
            db.execSQL("CREATE TABLE sources(id TEXT PRIMARY KEY, name TEXT NOT NULL, kind TEXT NOT NULL, secret BLOB NOT NULL, updated INTEGER, position INTEGER NOT NULL)")
            db.execSQL("CREATE TABLE favorites(channel_id TEXT PRIMARY KEY, added INTEGER NOT NULL)")
            db.execSQL("CREATE TABLE recent(channel_id TEXT PRIMARY KEY, watched INTEGER NOT NULL)")
            db.execSQL("CREATE TABLE settings(id INTEGER PRIMARY KEY CHECK(id=1), value BLOB NOT NULL)")
            db.execSQL("CREATE TABLE layout(id INTEGER PRIMARY KEY CHECK(id=1), value BLOB NOT NULL)")
            db.execSQL("CREATE TABLE progress(source_id TEXT NOT NULL, item_key TEXT NOT NULL, value BLOB NOT NULL, PRIMARY KEY(source_id, item_key))")
        }

        override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) = Unit
    }

    companion object {
        const val TAG = "enestv.perf"
        const val RECENT_LIMIT = 20
        private const val M3U_LIMIT = 256L * 1024 * 1024
        private const val API_LIMIT = 128L * 1024 * 1024

        @Volatile private var instance: Library? = null

        /** Opens the library (one Keystore call); call from a background thread. */
        fun get(context: Context): Library = instance ?: synchronized(this) {
            instance ?: Library(context.applicationContext, Crypto.load(context.applicationContext)).also { instance = it }
        }
    }
}
