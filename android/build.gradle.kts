plugins {
    alias(libs.plugins.android.application) apply false
    // AGP 9 compiles Kotlin itself; this only pins the Kotlin compiler version it uses.
    alias(libs.plugins.kotlin.android) apply false
}
