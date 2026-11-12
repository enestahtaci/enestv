package io.github.enestahtaci.enestv

import android.annotation.SuppressLint
import android.app.Activity
import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.os.Process
import android.os.SystemClock
import android.text.Editable
import android.text.TextWatcher
import android.util.Log
import android.view.KeyEvent
import android.view.LayoutInflater
import android.view.SurfaceView
import android.view.View
import android.view.ViewGroup
import android.view.WindowManager
import android.widget.EditText
import android.widget.FrameLayout
import android.widget.TextView
import android.window.OnBackInvokedCallback
import android.window.OnBackInvokedDispatcher
import androidx.recyclerview.widget.LinearLayoutManager
import androidx.recyclerview.widget.RecyclerView
import io.github.enestahtaci.enestv.core.AuthenticationException
import io.github.enestahtaci.enestv.core.Channel
import io.github.enestahtaci.enestv.core.FoldedIndex
import io.github.enestahtaci.enestv.core.UserInputException
import io.github.enestahtaci.enestv.data.ChannelLibrary
import io.github.enestahtaci.enestv.data.Source
import io.github.enestahtaci.enestv.data.SourceStore
import io.github.enestahtaci.enestv.playback.LivePlayer
import java.util.concurrent.Executors

/**
 * Spike step 3: real source, playback and channel switching. OK plays the focused channel, OK on the playing channel
 * goes fullscreen; CH+/CH- (and up/down in fullscreen) switch channels; Back leaves fullscreen.
 * Logged under [PERF_TAG]: start, list load, search and zap times (never URLs or account details).
 */
class MainActivity : Activity(), LivePlayer.Listener {
    private val io = Executors.newSingleThreadExecutor()
    private val main = Handler(Looper.getMainLooper())
    private lateinit var store: SourceStore
    private lateinit var library: ChannelLibrary
    private lateinit var player: LivePlayer
    private lateinit var adapter: ChannelAdapter
    private lateinit var list: RecyclerView
    private lateinit var status: TextView
    private lateinit var playerStatus: TextView
    private lateinit var sidebar: View
    private lateinit var video: SurfaceView
    private lateinit var search: EditText

    private var channels: List<Channel> = emptyList()
    private var index = FoldedIndex(emptyList())
    private var playing = -1 // channel index in [channels]
    private var fullscreen = false
    private var loadGeneration = 0
    private var listShown = false

    // Rows exist only after RecyclerView's next layout pass, so the focus request waits for it.
    private var focusFirstRowAfterLayout = false
    private var videoAspect = 16f / 9f

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_main)
        store = SourceStore(this)
        library = ChannelLibrary(filesDir)
        list = findViewById(R.id.channels)
        status = findViewById(R.id.status)
        playerStatus = findViewById(R.id.playerStatus)
        sidebar = findViewById(R.id.sidebar)
        video = findViewById(R.id.video)
        search = findViewById(R.id.search)
        player = LivePlayer(this, video, this)

        adapter = ChannelAdapter { channel -> onChannelClicked(channel) }
        list.layoutManager = LinearLayoutManager(this)
        list.setHasFixedSize(true)
        list.setItemViewCacheSize(8)
        list.adapter = adapter
        list.viewTreeObserver.addOnGlobalLayoutListener {
            if (focusFirstRowAfterLayout) list.getChildAt(0)?.let { focusFirstRowAfterLayout = false; it.requestFocus() }
        }

        findViewById<View>(R.id.sourceButton).setOnClickListener { openSource() }
        search.addTextChangedListener(object : TextWatcher {
            override fun beforeTextChanged(s: CharSequence?, start: Int, count: Int, after: Int) = Unit
            override fun onTextChanged(s: CharSequence?, start: Int, before: Int, count: Int) = Unit
            override fun afterTextChanged(s: Editable?) = applySearch()
        })
        findViewById<View>(R.id.videoFrame).addOnLayoutChangeListener { _, _, _, _, _, _, _, _, _ -> fitVideo() }

        window.decorView.post {
            Log.i(PERF_TAG, "first frame after ${SystemClock.uptimeMillis() - Process.getStartUptimeMillis()}ms")
            reportFullyDrawn()
        }
        loadLibrary(refresh = true)
    }

    private fun openSource() {
        @Suppress("DEPRECATION") // platform Activity API; no AndroidX activity dependency in the spike
        startActivityForResult(Intent(this, SourceActivity::class.java), REQUEST_SOURCE)
    }

    @Deprecated("platform Activity API")
    override fun onActivityResult(requestCode: Int, resultCode: Int, data: Intent?) {
        @Suppress("DEPRECATION") super.onActivityResult(requestCode, resultCode, data)
        if (requestCode == REQUEST_SOURCE && resultCode == RESULT_OK) {
            stopPlayback()
            io.execute { library.clearCache() }
            loadLibrary(refresh = true)
        }
    }

    /** Shows cached channels at once, then refreshes from the network in the background. */
    private fun loadLibrary(refresh: Boolean) {
        val generation = ++loadGeneration
        status.text = "Yükleniyor…"
        io.execute {
            val source = store.load()
            if (source == null) {
                main.post { if (generation == loadGeneration) { status.text = "Kaynak eklenmedi."; openSource() } }
                return@execute
            }
            val t0 = SystemClock.elapsedRealtime()
            val cached = runCatching { library.loadCached(source) }.getOrNull()
            if (cached != null) {
                val ms = SystemClock.elapsedRealtime() - t0
                Log.i(PERF_TAG, "cached list parsed in ${ms}ms, channels=${cached.size}")
                main.post { if (generation == loadGeneration) show(cached, "${cached.size} kanal (kayıtlı liste, $ms ms) · yenileniyor…") }
            }
            if (!refresh) return@execute
            val t1 = SystemClock.elapsedRealtime()
            try {
                val fresh = library.refresh(source)
                val ms = SystemClock.elapsedRealtime() - t1
                Log.i(PERF_TAG, "refresh (download+parse) in ${ms}ms, channels=${fresh.size}")
                main.post { if (generation == loadGeneration) show(fresh, "${fresh.size} kanal · güncellendi ($ms ms)") }
            } catch (e: Exception) {
                val message = when (e) {
                    is AuthenticationException -> "Kullanıcı adı veya parola kabul edilmedi."
                    is UserInputException -> e.message
                    else -> "Kaynak yüklenemedi; bağlantıyı kontrol edin."
                }
                Log.w(PERF_TAG, "refresh failed: ${e.javaClass.simpleName}")
                main.post { if (generation == loadGeneration) status.text = if (cached != null) "${cached.size} kanal (kayıtlı) · $message" else message }
            }
        }
    }

    private fun show(newChannels: List<Channel>, text: String) {
        val playingKey = channels.getOrNull(playing)?.id
        channels = newChannels
        index = FoldedIndex(newChannels.map { it.name })
        playing = if (playingKey == null) -1 else newChannels.indexOfFirst { it.id == playingKey }
        adapter.channels = newChannels
        applySearch()
        status.text = text
        // The first list goes straight to the channels (the window's default focus is the Kaynak button above them).
        // Later refreshes do not move the user's focus.
        val firstList = !listShown
        listShown = true
        if (!fullscreen && (firstList || currentFocus == null)) focusFirstRowAfterLayout = true
    }

    private fun applySearch() {
        val t0 = System.nanoTime()
        val hits = index.search(search.text?.toString())
        val us = (System.nanoTime() - t0) / 1000
        adapter.show(hits)
        if (search.text.isNotEmpty()) {
            status.text = "${hits.size} sonuç · arama ${"%.1f".format(us / 1000.0)} ms"
            Log.i(PERF_TAG, "search len=${search.text.length} hits=${hits.size} in ${us}us")
        }
    }

    private fun onChannelClicked(channel: Int) {
        if (channel == playing) setFullscreen(true) else play(channel)
    }

    private fun play(channel: Int) {
        val c = channels.getOrNull(channel) ?: return
        playing = channel
        adapter.playing = channel
        window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        showPlayerStatus("${channelNumber(channel)}  ${c.name}\nAçılıyor…")
        player.play(c.url)
    }

    /** Next/previous channel within the current (possibly filtered) list. */
    private fun zap(step: Int) {
        val visible = adapter.positions
        if (visible.isEmpty()) return
        val at = visible.indexOf(playing)
        val next = if (at < 0) 0 else (at + step + visible.size) % visible.size
        play(visible[next])
        list.scrollToPosition(next)
    }

    private fun stopPlayback() {
        player.stop()
        playing = -1
        adapter.playing = -1
        window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
        playerStatus.visibility = View.GONE
    }

    // Back leaves fullscreen. Android 13+ uses the back-callback API (predictive back); older versions onBackPressed.
    private val backCallback: Any? = if (Build.VERSION.SDK_INT >= 33) OnBackInvokedCallback { setFullscreen(false) } else null

    @Deprecated("Android 12 and older only; Android 13+ uses OnBackInvokedCallback")
    @SuppressLint("GestureBackNavigation") // only reached below API 33, where back gestures still call it
    override fun onBackPressed() {
        if (fullscreen) setFullscreen(false) else @Suppress("DEPRECATION") super.onBackPressed()
    }

    private fun setFullscreen(on: Boolean) {
        if (fullscreen == on) return
        fullscreen = on
        if (Build.VERSION.SDK_INT >= 33) {
            val callback = backCallback as OnBackInvokedCallback
            if (on) onBackInvokedDispatcher.registerOnBackInvokedCallback(OnBackInvokedDispatcher.PRIORITY_DEFAULT, callback)
            else onBackInvokedDispatcher.unregisterOnBackInvokedCallback(callback)
        }
        sidebar.visibility = if (on) View.GONE else View.VISIBLE
        if (!on) list.post { list.findViewHolderForAdapterPosition(adapter.positions.indexOf(playing).coerceAtLeast(0))?.itemView?.requestFocus() }
    }

    override fun dispatchKeyEvent(event: KeyEvent): Boolean {
        if (event.action == KeyEvent.ACTION_DOWN) {
            when (event.keyCode) {
                KeyEvent.KEYCODE_CHANNEL_UP, KeyEvent.KEYCODE_PAGE_UP -> { zap(-1); return true }
                KeyEvent.KEYCODE_CHANNEL_DOWN, KeyEvent.KEYCODE_PAGE_DOWN -> { zap(1); return true }
            }
            if (fullscreen) when (event.keyCode) {
                KeyEvent.KEYCODE_DPAD_UP -> { zap(-1); return true }
                KeyEvent.KEYCODE_DPAD_DOWN -> { zap(1); return true }
                KeyEvent.KEYCODE_DPAD_CENTER, KeyEvent.KEYCODE_ENTER -> { showPlayerStatus(playerStatus.text); return true }
            }
        }
        return super.dispatchKeyEvent(event)
    }

    override fun onFirstFrame(zapMs: Long) {
        val c = channels.getOrNull(playing) ?: return
        Log.i(PERF_TAG, "zap to first frame ${zapMs}ms")
        showPlayerStatus("${channelNumber(playing)}  ${c.name}\nKanal geçişi: $zapMs ms")
    }

    override fun onVideoSize(width: Int, height: Int, pixelRatio: Float) {
        videoAspect = width * pixelRatio / height
        fitVideo()
    }

    override fun onError(message: String) {
        Log.w(PERF_TAG, "playback error")
        val c = channels.getOrNull(playing)
        showPlayerStatus(if (c == null) message else "${channelNumber(playing)}  ${c.name}\n$message")
    }

    private val hideStatus = Runnable { playerStatus.visibility = View.GONE }

    private fun showPlayerStatus(text: CharSequence) {
        playerStatus.text = text
        playerStatus.visibility = View.VISIBLE
        main.removeCallbacks(hideStatus)
        main.postDelayed(hideStatus, 4_000)
    }

    /** Letterboxes the SurfaceView to the video's aspect ratio inside the video area. */
    private fun fitVideo() {
        val frame = video.parent as View
        val w = frame.width; val h = frame.height
        if (w == 0 || h == 0) return
        val (vw, vh) = if (w.toFloat() / h > videoAspect) (h * videoAspect).toInt() to h else w to (w / videoAspect).toInt()
        val lp = video.layoutParams as FrameLayout.LayoutParams
        if (lp.width != vw || lp.height != vh) { lp.width = vw; lp.height = vh; video.layoutParams = lp }
    }

    private fun channelNumber(channel: Int) = channel + 1

    override fun onStop() {
        super.onStop()
        // Free the provider's single connection when the app is not visible.
        if (playing >= 0) stopPlayback()
        if (fullscreen) setFullscreen(false)
    }

    override fun onDestroy() {
        player.release()
        io.shutdownNow()
        super.onDestroy()
    }

    /** Shows the channels at [positions]; the row number is the channel's own number. */
    private class ChannelAdapter(private val onClick: (Int) -> Unit) : RecyclerView.Adapter<ChannelAdapter.Row>() {
        // Stable ids let RecyclerView keep the focused row across a full rebind (list refresh, playing marker);
        // without them the focus falls back to the first focusable view of the window (the Kaynak button).
        init { setHasStableIds(true) }

        override fun getItemId(position: Int): Long = channels[positions[position]].id.hashCode().toLong()

        var channels: List<Channel> = emptyList()
        var positions = IntArray(0)
            private set
        var playing = -1
            @SuppressLint("NotifyDataSetChanged") set(value) { field = value; notifyDataSetChanged() }

        class Row(view: View) : RecyclerView.ViewHolder(view) {
            val number: TextView = view.findViewById(R.id.number)
            val name: TextView = view.findViewById(R.id.name)
        }

        @SuppressLint("NotifyDataSetChanged") // a new result set replaces the whole list
        fun show(newPositions: IntArray) {
            positions = newPositions
            notifyDataSetChanged()
        }

        override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): Row {
            val row = Row(LayoutInflater.from(parent.context).inflate(R.layout.item_channel, parent, false))
            row.itemView.setOnClickListener { row.bindingAdapterPosition.takeIf { it >= 0 }?.let { onClick(positions[it]) } }
            return row
        }

        override fun onBindViewHolder(holder: Row, position: Int) {
            val channel = positions[position]
            holder.number.text = (channel + 1).toString()
            holder.name.text = channels[channel].name
            holder.itemView.isSelected = channel == playing
        }

        override fun getItemCount() = positions.size
    }

    companion object {
        const val PERF_TAG = "enestv.perf"
        private const val REQUEST_SOURCE = 1
    }
}
