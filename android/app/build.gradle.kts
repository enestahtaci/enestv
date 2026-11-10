plugins {
    alias(libs.plugins.android.application)
}

android {
    namespace = "io.github.enestahtaci.enestv"
    compileSdk = 37

    defaultConfig {
        applicationId = "io.github.enestahtaci.enestv"
        minSdk = 28
        targetSdk = 37
        versionCode = 1
        versionName = "0.1.0-spike"
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(getDefaultProguardFile("proguard-android-optimize.txt"), "proguard-rules.pro")
            // Spike only: signed with the debug key so it can be installed on the test TV. Not for distribution.
            signingConfig = signingConfigs.getByName("debug")
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    testOptions {
        unitTests.all {
            // Shared, language-neutral test data at the repository root (fixtures/README.md).
            it.systemProperty("enestv.fixtures", rootProject.file("../fixtures").absolutePath)
            it.inputs.dir(rootProject.file("../fixtures"))
        }
    }
}

dependencies {
    implementation(libs.androidx.recyclerview)
    testImplementation(libs.junit)
}
