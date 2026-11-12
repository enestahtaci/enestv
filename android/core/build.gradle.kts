import org.jetbrains.kotlin.gradle.dsl.JvmTarget

// Platform-independent core (CORE-SPEC): parsers, identifiers, search, backup format. Plain Kotlin/JVM, no Android
// APIs, so it can be tested on the desktop and later shared with other platforms.
plugins {
    alias(libs.plugins.kotlin.jvm)
}

java {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
}

kotlin {
    compilerOptions { jvmTarget.set(JvmTarget.JVM_17) }
}

dependencies {
    testImplementation(libs.junit)
}

tasks.test {
    // Shared, language-neutral test data at the repository root (fixtures/README.md).
    val fixtures = rootProject.file("../fixtures")
    systemProperty("enestv.fixtures", fixtures.absolutePath)
    inputs.dir(fixtures)
}
