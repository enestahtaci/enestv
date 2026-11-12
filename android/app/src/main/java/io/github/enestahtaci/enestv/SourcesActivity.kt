package io.github.enestahtaci.enestv

import android.app.Activity
import android.app.AlertDialog
import android.content.Intent
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.widget.LinearLayout
import android.widget.TextView
import io.github.enestahtaci.enestv.core.AuthenticationException
import io.github.enestahtaci.enestv.core.UserInputException
import io.github.enestahtaci.enestv.data.Library
import io.github.enestahtaci.enestv.data.Source
import java.text.DateFormat
import java.util.Date
import java.util.Locale
import java.util.concurrent.Executors

/** Lists the sources: refresh or delete one, or add a new one. Returns RESULT_OK if anything changed. */
class SourcesActivity : Activity() {
    private val io = Executors.newSingleThreadExecutor()
    private val main = Handler(Looper.getMainLooper())
    private lateinit var list: LinearLayout
    private lateinit var status: TextView
    private var busy = false

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_sources)
        list = findViewById(R.id.list)
        status = findViewById(R.id.status)
        findViewById<View>(R.id.add).setOnClickListener {
            @Suppress("DEPRECATION") startActivityForResult(Intent(this, SourceActivity::class.java), REQUEST_ADD)
        }
        reload()
    }

    @Deprecated("platform Activity API")
    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        @Suppress("DEPRECATION") super.onActivityResult(requestCode, resultCode, data)
        if (requestCode == REQUEST_ADD && resultCode == RESULT_OK) { setResult(RESULT_OK); reload() }
    }

    private fun reload() {
        io.execute {
            val sources = Library.get(this).sources()
            main.post { show(sources) }
        }
    }

    private fun show(sources: List<Source>) {
        list.removeAllViews()
        status.text = if (sources.isEmpty()) "Henüz kaynak yok. Bir M3U adresi, Xtream hesabı ya da yedek ekleyin." else "${sources.size} kaynak"
        val format = DateFormat.getDateTimeInstance(DateFormat.SHORT, DateFormat.SHORT, Locale.forLanguageTag("tr-TR"))
        for (source in sources) {
            val row = LayoutInflater.from(this).inflate(R.layout.item_source, list, false) as TextView
            val kind = if (source.kind == Source.Kind.XTREAM) "Xtream" else "M3U"
            val updated = source.updated?.let { "güncellendi " + format.format(Date(it)) } ?: "henüz yüklenmedi"
            row.text = "${source.name}\n$kind · $updated"
            row.setOnClickListener { if (!busy) actions(source) }
            list.addView(row)
        }
        (list.getChildAt(0) ?: findViewById(R.id.add)).requestFocus()
    }

    private fun actions(source: Source) {
        AlertDialog.Builder(this)
            .setTitle(source.name)
            .setItems(arrayOf("Şimdi yenile", "Sil", "Vazgeç")) { _, which ->
                when (which) {
                    0 -> refresh(source)
                    1 -> AlertDialog.Builder(this)
                        .setTitle("Kaynak silinsin mi?")
                        .setMessage("${source.name} ve bu kaynağa ait kanallar, favoriler, son izlenenler ve izleme konumları bu TV'den silinir.")
                        .setPositiveButton("Sil") { _, _ -> run("Siliniyor…") { Library.get(this).deleteSource(source.id); "Silindi." } }
                        .setNegativeButton("Vazgeç", null)
                        .show()
                }
            }
            .show()
    }

    private fun refresh(source: Source) = run("${source.name} yenileniyor…") {
        "${Library.get(this).refresh(source).size} kanal yüklendi."
    }

    private fun run(progress: String, work: () -> String) {
        busy = true
        status.text = progress
        io.execute {
            val message = try {
                work().also { main.post { setResult(RESULT_OK) } }
            } catch (e: AuthenticationException) {
                "Kullanıcı adı veya parola kabul edilmedi."
            } catch (e: UserInputException) {
                e.message ?: "İşlem yapılamadı."
            } catch (e: Exception) {
                "İşlem yapılamadı; bağlantıyı kontrol edin."
            }
            val sources = Library.get(this).sources()
            main.post { busy = false; show(sources); status.text = message }
        }
    }

    override fun onDestroy() {
        io.shutdown()
        super.onDestroy()
    }

    private companion object {
        const val REQUEST_ADD = 1
    }
}
