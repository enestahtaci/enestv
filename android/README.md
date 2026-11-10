# enestv for Android TV (spike)

Status: technology spike on branch `android/spike`. Not a release; nothing here is distributed yet.

Kotlin, classic Android Views (RecyclerView) and, from the playback step on, Media3 (ExoPlayer). No Compose, no
Leanback, no dependency-injection or image-loading library: the target is a light, fast UI on TV hardware.
Core behavior follows [docs/CORE-SPEC.md](../docs/CORE-SPEC.md) and must pass the shared [fixtures](../fixtures/).

## Performance targets (measured on the weakest test device)

| Metric | Target |
| --- | --- |
| Cold start to first frame | < 1.5 s |
| Channel list scrolling | 60 fps, no dropped frames |
| Search result | < 50 ms |
| Channel switch | < 1 s (stream dependent) |
| Memory with 10,000 channels | < 150 MB |
| APK size | < 10 MB |

## Versions and maintenance

All versions are in [gradle/libs.versions.toml](gradle/libs.versions.toml) and the Gradle wrapper (pinned with
SHA-256). Policy: JDK is an LTS (Android Studio's bundled JBR); everything else is the newest **stable** release (no
alpha/beta/rc). Avoid deprecated libraries (e.g. ExoPlayer 2, Leanback).

Once a year, before Google Play's August deadline for the required `targetSdk`, raise `compileSdk`/`targetSdk` and
update the versions file; run the unit tests and the device checks again.

## Build

Requires Android Studio (bundled JDK) and the Android SDK. From this folder, in PowerShell:

```powershell
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
.\gradlew.bat :app:testDebugUnitTest :app:assembleDebug :app:assembleRelease
```

`local.properties` (ignored by git) holds `sdk.dir`. The repository folder name contains a non-ASCII letter, so
`gradle.properties` sets `android.overridePathCheck=true`. If a build tool fails on the path, map an ASCII drive
letter to the repository (`subst E: "<repo path>"`) and build from `E:\android`.

The release build is signed with the debug key for the spike only; a real signing key is a separate decision.
