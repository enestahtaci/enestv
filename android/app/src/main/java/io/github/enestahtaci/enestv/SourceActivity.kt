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
import io.github.enestahtaci.enestv.data.Source
import io.github.enestahtaci.enestv.data.SourceStore
import java.io.File
import kotlin.concurrent.thread

/**
 * Spike source screen: one M3U address, one Xtream account, or the source from an .enestv backup made on another
 * device (copied to the app's folder as [BACKUP_NAME]; deleted after a successful import).
 */
class SourceActivity : Activity() {
    private enum class Mode { M3U, XTREAM, BACKUP }

    private var mode = Mode.M3U
    private var busy = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_source)
        val store = SourceStore(this)
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
            visible(m3uUrl, m == Mode.M3U)
            listOf(server, username, password).forEach { visible(it, m == Mode.XTREAM) }
            visible(backupPassphrase, m == Mode.BACKUP)
            visible(backupInfo, m == Mode.BACKUP)
            backupInfo.text = if (backupFile.isFile) "Yedek dosyası hazır. Yedek parolasını girin." else "TV'de yedek dosyası bulunamadı ($BACKUP_NAME)."
            visible(error, false)
        }
        fun showError(message: String?) { error.text = message; visible(error, true) }

        val saved = store.load()
        when {
            backupFile.isFile -> showMode(Mode.BACKUP)
            saved?.kind == Source.Kind.XTREAM -> { server.setText(saved.location); username.setText(saved.username); showMode(Mode.XTREAM) }
            else -> { saved?.let { m3uUrl.setText(it.location) }; showMode(Mode.M3U) }
        }
        choices.forEach { (k, v) -> v.setOnClickListener { showMode(k) } }
        choices.getValue(mode).requestFocus()

        val main = Handler(Looper.getMainLooper())
        findViewById<View>(R.id.save).setOnClickListener {
            if (busy) return@setOnClickListener
            try {
                when (mode) {
                    Mode.M3U -> {
                        val url = m3uUrl.text.toString().trim()
                        if (url.isEmpty()) throw UserInputException("M3U adresini girin.")
                        done(store, Source(Source.Kind.M3U, url))
                    }
                    Mode.XTREAM -> {
                        // An empty password keeps the saved one, so the account can be edited without retyping it.
                        val pass = password.text.toString().ifEmpty { saved?.takeIf { it.kind == Source.Kind.XTREAM }?.password.orEmpty() }
                        if (username.text.isBlank() || pass.isEmpty()) throw UserInputException("Kullanıcı adı ve parola gerekli.")
                        done(store, Source(Source.Kind.XTREAM, Xtream.normalizeServer(server.text.toString()), username.text.toString().trim(), pass))
                    }
                    Mode.BACKUP -> {
                        if (!backupFile.isFile) throw UserInputException("TV'de yedek dosyası bulunamadı.")
                        val passphrase = backupPassphrase.text.toString()
                        busy = true
                        backupInfo.text = "Yedek açılıyor…"
                        visible(error, false)
                        // Key derivation (600,000 PBKDF2 rounds) takes seconds on a TV: keep it off the UI thread.
                        thread(name = "backup") {
                            val t0 = SystemClock.elapsedRealtime()
                            val result = runCatching {
                                val payload = BackupFile.decrypt(backupFile.readBytes(), passphrase)
                                Log.i(MainActivity.PERF_TAG, "backup decrypted in ${SystemClock.elapsedRealtime() - t0}ms")
                                val sources = BackupFile.sources(payload)
                                val chosen = sources.firstOrNull { it.kind == "Xtream" } ?: sources.firstOrNull { it.kind == "M3uUrl" }
                                    ?: throw UserInputException("Yedekte bu sürümün açabileceği bir kaynak yok (Xtream veya M3U adresi).")
                                if (chosen.kind == "Xtream") Source(Source.Kind.XTREAM, Xtream.normalizeServer(chosen.location), chosen.username, chosen.password)
                                else Source(Source.Kind.M3U, chosen.location)
                            }
                            main.post {
                                busy = false
                                result.onSuccess { source ->
                                    backupFile.delete()
                                    done(store, source)
                                }.onFailure { e ->
                                    backupInfo.text = "Yedek dosyası hazır. Yedek parolasını girin."
                                    showError(if (e is UserInputException) e.message else "Yedek açılamadı.")
                                }
                            }
                        }
                    }
                }
            } catch (e: UserInputException) {
                showError(e.message)
            }
        }
    }

    private fun done(store: SourceStore, source: Source) {
        store.save(source)
        setResult(RESULT_OK)
        finish()
    }

    companion object {
        /** adb push target: /sdcard/Android/data/io.github.enestahtaci.enestv/files/enestv-yedek.enestv */
        const val BACKUP_NAME = "enestv-yedek.enestv"
    }
}
