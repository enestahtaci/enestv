package io.github.enestahtaci.enestv

import android.content.Context

/** The app's versionName (written into backups as appVersion), without enabling BuildConfig generation. */
object BuildConfigVersion {
    fun name(context: Context): String =
        runCatching { context.packageManager.getPackageInfo(context.packageName, 0).versionName }.getOrNull() ?: "android"
}
