plugins {
    alias(libs.plugins.android.application) apply false
    // AGP 9 compiles Android Kotlin itself; these pin the Kotlin compiler version (and provide the JVM plugin for :core).
    alias(libs.plugins.kotlin.android) apply false
    alias(libs.plugins.kotlin.jvm) apply false
}
