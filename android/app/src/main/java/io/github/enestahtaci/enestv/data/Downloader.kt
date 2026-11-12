package io.github.enestahtaci.enestv.data

import io.github.enestahtaci.enestv.core.AuthenticationException
import io.github.enestahtaci.enestv.core.UserInputException
import java.io.File
import java.io.IOException
import java.net.HttpURLConnection
import java.net.URL

/**
 * Plain HttpURLConnection download to a file (no HTTP library). Follows redirects across http/https, which
 * HttpURLConnection does not do by itself. Error messages never contain the URL: it may hold account details.
 */
object Downloader {
    const val USER_AGENT = "enestv/1.0"

    fun download(url: String, destination: File, maxBytes: Long) {
        var current = url
        repeat(8) {
            val connection = (URL(current).openConnection() as HttpURLConnection).apply {
                instanceFollowRedirects = false
                connectTimeout = 12_000
                readTimeout = 30_000
                setRequestProperty("User-Agent", USER_AGENT)
            }
            try {
                when (val code = connection.responseCode) {
                    in 300..399 -> {
                        val location = connection.getHeaderField("Location") ?: throw IOException("redirect without location")
                        current = URL(URL(current), location).toString()
                        return@repeat
                    }
                    401, 403 -> throw AuthenticationException()
                    !in 200..299 -> throw UserInputException("Sunucu isteği reddetti (HTTP $code).")
                }
                if (connection.contentLengthLong > maxBytes) throw UserInputException("Kaynak izin verilen boyuttan büyük.")
                val partial = File(destination.path + ".part")
                connection.inputStream.use { input ->
                    partial.outputStream().buffered(64 * 1024).use { output ->
                        val buffer = ByteArray(64 * 1024)
                        var total = 0L
                        while (true) {
                            val n = input.read(buffer)
                            if (n < 0) break
                            total += n
                            if (total > maxBytes) throw UserInputException("Kaynak izin verilen boyuttan büyük.")
                            output.write(buffer, 0, n)
                        }
                    }
                }
                if (!partial.renameTo(destination)) { destination.delete(); partial.renameTo(destination) }
                return
            } finally {
                connection.disconnect()
            }
        }
        throw UserInputException("Sunucu çok fazla yönlendirme yaptı.")
    }
}
