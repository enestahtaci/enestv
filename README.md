[English](README.md) | [Türkçe](README.tr.md)

# enestv

## License and content

Copyright (c) 2026 enestv katkıcıları.
enestv is provided under the GNU General Public License version 3 or (at your option) any later version: **GPL-3.0-or-later**. Full text: [LICENSE](LICENSE). Third-party licenses are preserved separately: [notices](THIRD-PARTY-NOTICES.md).
Contributor approvals are obtained by the project owner; this text is not a statement that those approvals have been completed.

enestv does not provide any streams, playlists, movies/series, EPG data or accounts. Users add sources they have the right to access. It is not affiliated with, partnered with or endorsed by VideoLAN or any broadcaster.

**HTTP sources:** When using HTTP addresses, usernames, passwords, playlists and stream requests may be transmitted over the network without encryption. Use HTTPS where possible. Local DPAPI protection does not encrypt network traffic.

## Source code and third-party source archives

The [enestv source repository](https://github.com/enestahtaci/enestv) and the corresponding version tag contain the application code. Each GitHub Release must include the enestv source archive for that version, third-party sources, required patches/build recipes and SHA256SUMS.txt.
[Full official download URLs, FFmpeg contrib patch/configuration review and outstanding verification items](SOURCE-ACCESS.md).
[GitHub Releases](https://github.com/enestahtaci/enestv/releases). These addresses have been designated for publication; the repository has not yet been created and their availability has not been verified. A Release is not ready until the outstanding native source verification items have been resolved.

## Building the application and installer

This version targets .NET 8; migration to .NET 10 is a separate task. .NET 8 support ends on November 10, 2026; maintenance should be planned accordingly.
On Windows 11 x64 with the .NET 8 SDK, run from the repository root:

```powershell
dotnet restore enestv.sln
dotnet build enestv.sln -c Release
dotnet test EnesTV.Tests -c Release
```

Inno Setup is required to build the installer. Pass the installed ISCC.exe path to `Installer/Build-Setup.ps1` using `-InnoCompiler`. The script publishes a self-contained x64 application and builds the installer using a fresh staging directory.
These commands are documentation; they were not run during Stage 2 preparation. [Release checklist](RELEASE-CHECKLIST.md).

### 1.4.0 — single window, fullscreen fixes

Update using `enestv-Setup-1.4.0-x64.exe`. Close any running instance of enestv first. Accounts, favorites, history and playback positions are preserved; the database schema does not change.

- **Movies** and **Series** no longer open a separate window. Selecting them in the left menu shows the catalog in the middle column and movie controls and details under the same player on the right. The app still starts in Live TV.
- Only one stream is open at a time: switching to Movies/Series stops live TV, returning to Live TV stops the movie (the playback position is saved). In Movies/Series, Esc returns to Live TV.
- The empty player screen is now useful: **Continue** for the last watched channel, shortcuts to recent and favorite channels, and **Add source** when no source exists. Stop also returns to this screen.
- Fixed in fullscreen: Esc not working after clicking the video, controls not reappearing on mouse movement, and the empty player turning white.
- An unresponsive stream no longer locks the app while stopping, switching episodes or closing.

### 1.3.0 — interface and usability improvements

Update using `enestv-Setup-1.3.0-x64.exe`. Close any running instance of enestv first. Existing accounts, favorites, history and playback positions are preserved; this version does not change the database schema.

- The menu was localized into Turkish, with updated vector icons and selected-section styling.
- Channel rows were simplified. First launch, empty favorites/history, loading and searches with no results have separate explanations; search/filter can be cleared with one button.
- The play/pause button is more prominent and its icon reflects playback state. Open **Görüntü, ses ve altyazı** (Picture, audio and subtitles) for additional playback options. On the movies/series screen, this section is called **Ses ve altyazı** (Audio and subtitles).
- Stream errors appear over the video with an explanation and a **Yeniden dene** (Retry) button.
- Movie details use the space otherwise occupied by the episode column. Episode options appear only for series. Missing accounts and empty catalogs are explained.
- Settings are arranged in cards. The add-source form explains the selected source type; fields scroll in short windows while the save button remains visible.
- Input fields, checkboxes, volume/progress sliders and focus/selection styling share a consistent design in dark and light themes.

A 10,000-channel list, real VLC playback, fullscreen, small windows, keyboard shortcuts and movie/series flows were checked again. Details are in `UI-DESIGN.md` and `VERIFICATION-1.3.0.md`. A single Setup EXE can be shared; .NET and VLC are included. The installer contains no personal account.

### 1.2.0 — movies, series and resume playback

Update using `enestv-Setup-1.2.0-x64.exe`. **Filmler** (Movies) and **Diziler** (Series) were added to the main menu. These catalogs come from an account added as an **Xtream Codes** source. Users who only use M3U need to add an Xtream source with a server URL, username and password; the provider must enable movie/series access for the account.

1. Select **Filmler** or **Diziler** in the main menu. Switch the Xtream account at the top left.
2. Filter by category or search by name. Select an item to open its description.
3. For series, select a season and episode, then press **İzle / devam et** (Watch / resume).
4. Use the progress bar or **−10 sn / +10 sn** to seek. While the player has focus, Space pauses, left/right seeks 10 seconds, up/down adjusts volume and M mutes.
5. The position is saved approximately every 5 seconds and when stopping, switching content or closing the window. **Baştan başlat** (Start over) ignores the saved position. Watched episodes are marked; selecting a series again selects the last watched episode, or the next episode if it was completed.
6. **Sonraki bölüm** (Next episode) moves to the next episode in the same season, or to the next season when the season ends. Episodes do not play automatically; the button must be pressed.
7. F11 enters fullscreen; Esc exits. Progress, volume and playback controls appear on mouse movement and hide when unused. **Canlı TV'ye dön** (Return to Live TV) closes the catalog window.

Live TV stops when the movie/series screen opens; two streams are not played simultaneously. After returning, select a channel to restart playback. Existing general favorites and channel organization apply to Live TV.

Catalogs are downloaded on first use, kept in an encrypted SQLite cache and refreshed when more than 6 hours old. Use **↻** to refresh manually; a failed refresh does not erase the last successful catalog. Episode details are fetched from the server when selected. The catalog can be viewed offline; playback requires a connection.

Schema 1 or 2 records are upgraded to version 3 without deleting data. Accounts, favorites, history and channel organization are preserved. New catalog/playback-position data is also encrypted with DPAPI. Opening the new database with an older application version is unsupported. The installer contains no personal account or bundled content.

Verification details are in `VERIFICATION-1.2.0.md`. New test flow: `dotnet run --project EnesTV.Smoke -c Release -- --vod ./test-results/vod`.


### 1.1.0 — fullscreen controls, channel switching and channel organization

`enestv-Setup-1.1.0-x64.exe` can be installed over the previous installation. Close any running instance of enestv first. Accounts, sources, general favorites and history are preserved; the database is automatically upgraded to version 2 for the new organization data.

- **Fullscreen:** F11 or the fullscreen button. The bottom control bar appears on mouse movement and hides after 3 seconds of inactivity; it stays visible while the pointer is over it or a control is being dragged. Channel/EPG information panels remain hidden. Esc exits.
- **Channel switching:** Previous, next and return-to-last-channel buttons. Previous/next follows the current search, category or favorites list and wraps around at the end. The last-channel button switches between two channels.
- **Channel number:** Type the number displayed in the list, then press Enter or wait 1.2 seconds. Numbers use the order of all visible channels, independently of search/favorites filters. Changing organization or hidden channels recalculates the numbers. Channel shortcuts do not run while typing in a text field.
- **Shortcuts:** Reassign shortcuts or restore defaults on the Kısayollar (Shortcuts) screen in the left menu. Defaults: PageDown next, PageUp previous, Backspace last channel, Space play/pause, M mute, up/down volume. F11, Esc and digits are fixed. Conflicting shortcuts cannot be saved.
- **Channel organization:** Open from the left menu. Hide categories/channels, edit display names, move up/down or sort A–Z. An empty display name uses the provider's name. Save applies all changes; Cancel discards them.
- **Custom favorites lists:** Create, rename or delete lists in channel organization. Add or remove channels selected with Ctrl/Shift. On the main screen, select a list from the Favorites dropdown. General starred favorites remain separate. Clicking the star in a custom list removes the channel only from that list.
- Channel organization, custom lists and shortcuts are stored in SQLite encrypted with DPAPI. Refreshing a playlist preserves organization for channels with the same identity. A provider changing channel identifiers may break this matching.

The installer can be shared as a single EXE; it includes .NET and LibVLC. The icon and nonfunctional “enes'e teşekkür et” checkbox from the previous version are preserved. Detailed results are in `VERIFICATION-1.1.0.md`.


### 1.0.2 — Windows installer and icon

Install using `enestv-Setup-1.0.2-x64.exe`. The Turkish wizard installs the application to `%LOCALAPPDATA%\Programs\enestv` by default without administrator permission. It creates a Start menu shortcut; a desktop shortcut is optional. .NET and LibVLC are included. Uninstall through Windows Settings → Apps; playlist/account/settings data is preserved in `%LOCALAPPDATA%\enestv`.

The final page has an optional “enestv'yi şimdi aç” (Launch enestv now) option and a joke-only **“enes'e teşekkür et”** (Thank Enes) checkbox. The thank-you checkbox has no event handler, command, network request or persistent record.

The new icon was added to Windows EXE resources, application windows, Setup/uninstaller and shortcuts. PNG/ICO assets are in `EnesTV.App/Assets`. Setup is not code-signed.

Rebuilding the installer requires the .NET 8 SDK and Inno Setup 6.7.3 or later:

```powershell
./Installer/Build-Setup.ps1 -InnoCompiler 'C:\Path\To\Inno Setup 6\ISCC.exe'
```

The script publishes the self-contained x64 application, copies license files and creates Setup under `dist`. Observe Inno Setup's license terms for distribution/commercial use: https://jrsoftware.org/isinfo.php

### 1.0.1 — window and fullscreen fixes

The Windows title bar, text and border now match the selected dark/light theme. The fullscreen button or F11 displays only the video across the entire monitor, including the taskbar area; channel information, EPG, controls and margins are hidden. Esc or F11 restores the previous normal/maximized window layout. The video aspect ratio setting is preserved.

A C# / .NET 8 / WPF IPTV desktop application for Windows 11 x64. It plays the user's own legal sources. No provider, preloaded channel list or streaming account is supplied with the application.

## Quick start

Open **enestv.exe** in the prepared `enestv-win-x64` folder. The distribution includes the .NET runtime and LibVLC; no separate .NET or VLC installation is required. If using a ZIP, extract it completely first. Keep the EXE together with the DLL files and `libvlc` directory.

1. Select **Kaynak ekle → M3U URL** (Add source → M3U URL), enter a source name and your own playlist address.
2. The playlist is downloaded, categories are created and the last successful result is saved.
3. Click a channel row. The star button toggles its favorite status.
4. In **Ayarlar** (Settings), edit, rename, refresh or delete sources.
5. Enter an XMLTV address in Settings and select **Kaydet ve rehberi güncelle** (Save and update guide). XMLTV addresses for Xtream accounts are generated automatically; update them with the same button.

**Dosya aç** (Open file) imports `.m3u` and `.m3u8` channel lists. For Xtream, select **Xtream Codes** in the Add source menu and use the server, username and password fields.

## Development requirements

- Windows 11 x64
- .NET 8 SDK (8.0.425 was used during development)
- NuGet access for the initial package restore
- Optional: Visual Studio 2022 with the .NET desktop development workload

## Build and run

From the directory containing enestv.sln in the source package:

```powershell
dotnet restore enestv.sln
dotnet build enestv.sln -c Release
dotnet test EnesTV.Tests -c Release
dotnet run --project EnesTV.App -c Release
```

Self-contained Windows distribution:

```powershell
dotnet publish EnesTV.App -c Release -r win-x64 --self-contained true -o dist/enestv-win-x64
```

`Directory.Build.props` sets the x64 target and treats compiler warnings as errors. Trimming and single-file publishing are not used; LibVLC's replaceable DLL/plugin files are preserved. Include third-party notices in the distribution.

## Architecture

```text
EnesTV.Core/
  Models/       Channel, source, program and settings records
  Services/     HttpClient, M3U, Xtream, XMLTV, filtering, DPAPI, rotating logs
  Data/         SQLite queries, transactions, EPG cache, schema versioning
EnesTV.App/
  ViewModels/   MVVM state and asynchronous commands
  Views/        Source, settings and TV guide windows; styles
  Services/     Logo cache with bounded concurrency and asynchronous loading
  Playback/     LibVLC lifecycle and connection states
EnesTV.Tests/   Parser, normalization, filtering, time and persistence tests
EnesTV.Smoke/   Local HTTP server, synthetic media and integration tests
```

SQLite is versioned using `PRAGMA user_version=3`; version 1/2 records are upgraded without deleting existing data, and initial setup runs in a transaction. Future migrations are added sequentially in `LibraryStore.InitializeAsync`. Unknown newer schema versions are not overwritten. Each connection closes after its operation; bulk operations run off the UI thread.

## Features and application behavior

- Multiple local/remote M3U and Xtream sources; add, edit, rename, delete, manually refresh and view the last update time.
- `tvg-id`, `tvg-name`, `tvg-logo`, `group-title`; missing categories become `Uncategorized`; parsing tolerates malformed metadata and removes exact duplicates.
- UTF-8/BOM and UTF-16 BOM; Windows-1254 fallback when UTF-8 decoding fails.
- Case-insensitive name/category search with a 180 ms debounce; virtualized and recycled channel rows.
- Logos load only for visible rows; 4 concurrent downloads, at most 200 memory and 500 disk entries, a 7-day cache lifetime and a 4 MB file limit. Invalid logos use a TV icon.
- Play/pause, stop, volume, mute, fullscreen, aspect ratio and available audio/subtitle tracks.
- F11 fullscreen, Esc exit fullscreen; Space play/pause when focus permits.
- Changing channels cancels the old operation, stops the player and releases the old Media. Native VLC calls run through a single queue on a worker thread.
- Idle, Loading, Playing, Paused, Buffering, Reconnecting and Error states. Reconnection is limited to 3 attempts (1, 2 and 4 second delays); selecting a new channel or manually reconnecting resets the budget. A stream that stops progressing is checked after approximately 25–30 seconds.
- Favorites, the last 20 played channels, settings, sources and channel metadata persist in SQLite. Network errors do not erase the saved playlist. The local playlist loads at startup without a network connection.
- XMLTV/XMLTV gzip; `tvg-id` matching, time-zone offsets and local-time display; current/next program, times, descriptions and a TV Guide by channel/day.
- EPG is cached in a transaction only for relevant channel IDs, approximately covering the past day and next 8 days. The full XML document tree is not loaded into memory. Current/next information refreshes at the start of each minute.
- Dark and light themes, default volume, automatic reconnection, XMLTV and source/account management.

## Data and privacy

Default data directory: `%LOCALAPPDATA%\enestv`:

- `library.db`: sources, channels, favorites, history, settings and EPG.
- `logos/`: downloaded channel logos.
- `logs/enestv.log`: log rotating at 1 MB with up to three older files.

Source addresses, Xtream usernames/passwords, channel URLs/metadata and settings are encrypted with Windows DPAPI CurrentUser. Passwords are not stored in plaintext in SQLite. Source names, favorite identifiers and EPG program text are not encrypted. Logs do not contain URLs, credentials or exception messages; they record fixed event codes and exception types. An encrypted database cannot be transferred between Windows accounts. The original playlist file supplied by the user is not modified.

HTTP operations use one long-lived HttpClient, supporting redirects, connection timeouts, cancellation and limited backoff for transient HTTP errors. Downloads have a 2-minute timeout and size limits. Temporary download files are deleted when the operation ends. Unencrypted HTTP does not provide HTTPS transport protection.

For tests, the `ENESTV_DATA_DIR` environment variable can override the data location; it is unnecessary for normal use.

## Tests and acceptance verification

```powershell
dotnet test EnesTV.Tests -c Release
dotnet run --project EnesTV.Smoke -c Release -- --verify ./test-results/integration
dotnet run --project EnesTV.Smoke -c Release -- --ui ./test-results/ui
```

Use a new, empty results directory for each test run. Smoke tests start a local server on loopback only, generate their own animated AVI/silent WAV and use no external IPTV source. UI tests open real WPF windows and check search, favorites, program information, fullscreen and window size. UI images are WPF render output; the native VLC surface appears black in these images. The actual video decoder is separately verified using a LibVLC snapshot.

For a manual 10,000-channel test:

```powershell
dotnet run --project EnesTV.Smoke -c Release -- --serve ./test-fixtures
```

The command starts a server at 127.0.0.1:18765 and generates `test-fixtures/test-10000.m3u`. Add `http://127.0.0.1:18765/playlist.m3u` to the main application. This address is for developer testing only; it is not included in the shipped application. Ctrl+C stops the server. Results are summarized in `VERIFICATION.md`.

## Main dependencies

- [LibVLCSharp.WPF 3.10.1](https://www.nuget.org/packages/LibVLCSharp.WPF/3.10.1)
- [VideoLAN.LibVLC.Windows 3.0.23.1](https://www.nuget.org/packages/VideoLAN.LibVLC.Windows/3.0.23.1)
- [Microsoft.Data.Sqlite 8.0.30](https://www.nuget.org/packages/Microsoft.Data.Sqlite/8.0.30)
- System.Security.Cryptography.ProtectedData 8.0.0 and System.Text.Encoding.CodePages 8.0.0
- Tests use xUnit and Microsoft.NET.Test.Sdk

No additional MVVM framework dependency is used.

## Known limitations

- DRM, portal/MAC login, catch-up, recording and timeshift are unavailable in this version. Movie/series catalogs are supported through Xtream accounts; generic M3U lists are not automatically converted into series catalogs.
- Xtream live channels, movie/series catalogs and XMLTV EPG are implemented; provider-specific nonstandard API variants and streams requiring custom HTTP headers/cookies are unsupported.
- Direct HLS manifests are passed to LibVLC as channel URLs; file import is for IPTV channel lists. An HLS segment manifest is not a channel catalog.
- XMLTV without a specified offset is treated as UTC; programs with invalid or missing end times are skipped. Guide downloads are triggered manually.
- The last 20 channels are recorded when playback starts; failed connection attempts alone are not added to history.
- Decoder/audio/subtitle capabilities are limited to the tracks supplied by the source. How a live source supports pausing depends on the provider.
- DPAPI-encrypted sources cannot be opened under another Windows account. The installer EXE is not code-signed; a folder distribution can also be produced.
- Long-duration testing with a real user's provider account has not been performed. Network/provider, GPU and codec variety require real-world verification.
