package io.github.enestahtaci.enestv

import android.app.Activity
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.SystemClock
import android.util.Log
import android.view.View
import android.widget.EditText
import android.widget.TextView
import io.github.enestahtaci.enestv.core.BackupFile
import io.github.enestahtaci.enestv.core.UserInputException
import io.github.enestahtaci.enestv.core.Xtream
import io.github.enestahtaci.enestv.data.Library
import io.github.enestahtaci.enestv.data.Source
import java.io.File
import kotlin.concurrent.thread

/**
 * Adds a source: an M3U address, an Xtream account, or the sources in an .enestv backup made on another device
 * (copied to the app's folder as [BACKUP_NAME]; deleted after a successful import). Returns RESULT_OK when added.
 */
class SourceActivity : Activity() {
    private enum class Mode { M3U, XTREAM, BACKUP }

    private var mode = Mode.M3U
    private var busy = false
    private val main = Handler(Looper.getMainLooper())

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_source)
        val name = findViewById<EditText>(R.id.sourceName)
        val m3uUrl = findViewById<EditText>(R.id.m3uUrl)
        val server = findViewById<EditText>(R.id.server)
        val username = findViewById<EditText>(R.id.username)
        val password = findViewById<EditText>(R.id.password)
        val backupPassphrase = findViewById<EditText>(R.id.backupPassphrase)
        val backupInfo = findViewById<TextView>(R.id.backupInfo)
        val error = findViewById<TextView>(R.id.error)
        val choices = mapOf(Mode.M3U to findViewById<View>(R.id.kindM3u), Mode.XTREAM to findViewById(R.id.kindXtream), Mode.BACKUP to findViewById(R.id.kindBackup))
        val backupFile = File(getExternalFilesDir(null), BACKUP_NAME)

        fun visible(view: View, on: Boolean) { view.visibility = if (on) View.VISIBLE else View.GONE }
        fun showMode(m: Mode) {
            mode = m
            choices.forEach { (k, v) -> v.isSelected = k == m }
            visible(name, m != Mode.BACKUP)
            visible(m3uUrl, m == Mode.M3U)
            listOf(server, username, password).forEach { visible(it, m == Mode.XTREAM) }
            visible(backupPassphrase, m == Mode.BACKUP)
            visible(backupInfo, m == Mode.BACKUP)
            backupInfo.text = if (backupFile.isFile) "Yedek dosyası hazır. Yedek parolasını girin." else "TV'de yedek dosyası bulunamadı ($BACKUP_NAME)."
            visible(error, false)
        }
        fun showError(message: String?) { error.text = message; visible(error, true) }

        showMode(if (backupFile.isFile) Mode.BACKUP else Mode.M3U)
        choices.forEach { (k, v) -> v.setOnClickListener { showMode(k) } }
        choices.getValue(mode).requestFocus()

        findViewById<View>(R.id.save).setOnClickListener {
            if (busy) return@setOnClickListener
            try {
                val work: () -> Unit = when (mode) {
                    Mode.M3U -> {
                        val url = m3uUrl.text.toString().trim()
                        if (url.isEmpty()) throw UserInputException("M3U adresini girin.")
                        val source = Source(Source.newId(), name.text.toString().trim().ifEmpty { "M3U listesi" }, Source.Kind.M3U_URL, url)
                        ({ Library.get(this).saveSource(source) })
                    }
                    Mode.XTREAM -> {
                        if (username.text.isBlank() || password.text.isEmpty()) throw UserInputException("Kullanıcı adı ve parola gerekli.")
                        val source = Source(Source.newId(), name.text.toString().trim().ifEmpty { "Xtream hesabı" }, Source.Kind.XTREAM,
                            Xtream.normalizeServer(server.text.toString()), username.text.toString().trim(), password.text.toString())
                        ({ Library.get(this).saveSource(source) })
                    }
                    Mode.BACKUP -> {
                        if (!backupFile.isFile) throw UserInputException("TV'de yedek dosyası bulunamadı.")
                        val passphrase = backupPassphrase.text.toString()
                        backupInfo.text = "Yedek açılıyor…"
                        ({ importBackup(backupFile, passphrase) })
                    }
                }
                busy = true
                visible(error, false)
                thread(name = "source") {
                    val result = runCatching(work)
                    main.post {
                        busy = false
                        result.onSuccess { setResult(RESULT_OK); finish() }.onFailure { e ->
                            if (mode == Mode.BACKUP) backupInfo.text = "Yedek dosyası hazır. Yedek parolasını girin."
                            showError(if (e is UserInputException) e.message else "Kaynak eklenemedi.")
                        }
                    }
                }
            } catch (e: UserInputException) {
                showError(e.message)
            }
        }
    }

    /** Adds every supported source of the backup with its original id, so channel keys (favorites) stay the same. */
    private fun importBackup(file: File, passphrase: String) {
        // Key derivation (600,000 PBKDF2 rounds) takes seconds on a TV: this runs on a background thread.
        val t0 = SystemClock.elapsedRealtime()
        val payload = BackupFile.decrypt(file.readBytes(), passphrase)
        Log.i(Library.TAG, "backup decrypted in ${SystemClock.elapsedRealtime() - t0}ms")
        val sources = BackupFile.sources(payload).mapNotNull { s ->
            val kind = Source.Kind.fromBackup(s.kind) ?: return@mapNotNull null
            Source(s.id, s.name, kind, if (kind == Source.Kind.XTREAM) Xtream.normalizeServer(s.location) else s.location, s.username, s.password)
        }
        if (sources.isEmpty()) throw UserInputException("Yedekte bu sürümün açabileceği bir kaynak yok (Xtream veya M3U adresi).")
        val library = Library.get(this)
        sources.forEach(library::saveSource)
        file.delete()
    }

    companion object {
        /** adb push target: /sdcard/Android/data/io.github.enestahtaci.enestv/files/enestv-yedek.enestv */
        const val BACKUP_NAME = "enestv-yedek.enestv"
    }
}
