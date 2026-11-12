package io.github.enestahtaci.enestv.playback

import android.content.Context
import android.os.SystemClock
import android.view.SurfaceView
import androidx.annotation.OptIn
import androidx.media3.common.MediaItem
import androidx.media3.common.MimeTypes
import androidx.media3.common.PlaybackException
import androidx.media3.common.Player
import androidx.media3.common.VideoSize
import androidx.media3.common.util.UnstableApi
import androidx.media3.datasource.DefaultHttpDataSource
import androidx.media3.exoplayer.DefaultLoadControl
import androidx.media3.exoplayer.ExoPlayer
import androidx.media3.exoplayer.source.DefaultMediaSourceFactory
import androidx.media3.extractor.DefaultExtractorsFactory
import androidx.media3.extractor.ts.DefaultTsPayloadReaderFactory
import io.github.enestahtaci.enestv.data.Downloader

/**
 * One ExoPlayer for the whole app, reused for every channel (no per-zap setup cost). Tuned for live IPTV:
 * playback starts after 0.5 s of buffer; at most 30 s is held to stay small on 2 GB TVs.
 */
@OptIn(UnstableApi::class)
class LivePlayer(context: Context, surface: SurfaceView, private val listener: Listener) {
    interface Listener {
        fun onFirstFrame(zapMs: Long)
        fun onVideoSize(width: Int, height: Int, pixelRatio: Float)
        fun onError(message: String)
    }

    private val player: ExoPlayer
    private var zapStart = 0L
    private var currentUrl: String? = null
    private var triedHls = false

    init {
        val http = DefaultHttpDataSource.Factory()
            .setUserAgent(Downloader.USER_AGENT)
            .setAllowCrossProtocolRedirects(true)
            .setConnectTimeoutMs(12_000)
            .setReadTimeoutMs(15_000)
        // IPTV .ts streams often start mid-GOP; accept non-IDR keyframes so the first picture comes sooner.
        val extractors = DefaultExtractorsFactory().setTsExtractorFlags(
            DefaultTsPayloadReaderFactory.FLAG_ALLOW_NON_IDR_KEYFRAMES or DefaultTsPayloadReaderFactory.FLAG_DETECT_ACCESS_UNITS,
        )
        player = ExoPlayer.Builder(context)
            .setMediaSourceFactory(DefaultMediaSourceFactory(http, extractors))
            .setLoadControl(DefaultLoadControl.Builder().setBufferDurationsMs(15_000, 30_000, 500, 2_000).build())
            .build()
        player.setVideoSurfaceView(surface)
        player.addListener(object : Player.Listener {
            override fun onRenderedFirstFrame() {
                if (zapStart != 0L) listener.onFirstFrame(SystemClock.elapsedRealtime() - zapStart)
                zapStart = 0L
            }

            override fun onVideoSizeChanged(videoSize: VideoSize) {
                if (videoSize.width > 0) listener.onVideoSize(videoSize.width, videoSize.height, videoSize.pixelWidthHeightRatio)
            }

            override fun onPlayerError(error: PlaybackException) {
                val url = currentUrl
                // Extension-less HLS links are first tried as a progressive stream; retry once as HLS.
                if (!triedHls && url != null && error.errorCode == PlaybackException.ERROR_CODE_PARSING_CONTAINER_UNSUPPORTED) {
                    triedHls = true
                    start(url, hls = true)
                    return
                }
                if (error.errorCode == PlaybackException.ERROR_CODE_BEHIND_LIVE_WINDOW) {
                    player.seekToDefaultPosition()
                    player.prepare()
                    return
                }
                // Only the error kind is shown: the message of a network error can contain the stream URL.
                listener.onError(message(error.errorCode))
            }
        })
    }

    fun play(url: String) {
        currentUrl = url
        triedHls = false
        zapStart = SystemClock.elapsedRealtime()
        start(url, hls = url.substringBefore('?').endsWith(".m3u8", ignoreCase = true))
    }

    private fun start(url: String, hls: Boolean) {
        val item = MediaItem.Builder().setUri(url).apply { if (hls) setMimeType(MimeTypes.APPLICATION_M3U8) }.build()
        player.setMediaItem(item)
        player.prepare()
        player.playWhenReady = true
    }

    /** Releases the provider connection (most accounts allow one stream at a time). */
    fun stop() {
        currentUrl = null
        zapStart = 0L
        player.stop()
        player.clearMediaItems()
    }

    fun release() = player.release()

    private fun message(code: Int) = when (code) {
        PlaybackException.ERROR_CODE_IO_BAD_HTTP_STATUS -> "Sunucu yayını vermedi (hesap, bağlantı sınırı veya kanal kapalı olabilir)."
        PlaybackException.ERROR_CODE_IO_NETWORK_CONNECTION_FAILED,
        PlaybackException.ERROR_CODE_IO_NETWORK_CONNECTION_TIMEOUT -> "Yayına bağlanılamadı."
        PlaybackException.ERROR_CODE_PARSING_CONTAINER_UNSUPPORTED,
        PlaybackException.ERROR_CODE_PARSING_CONTAINER_MALFORMED -> "Yayın biçimi okunamadı."
        PlaybackException.ERROR_CODE_DECODER_INIT_FAILED,
        PlaybackException.ERROR_CODE_DECODING_FORMAT_UNSUPPORTED,
        PlaybackException.ERROR_CODE_DECODING_FORMAT_EXCEEDS_CAPABILITIES -> "Bu cihaz yayının görüntü/ses biçimini çözemiyor."
        else -> "Yayın açılamadı (${PlaybackException.getErrorCodeName(code)})."
    }
}
