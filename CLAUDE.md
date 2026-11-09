# enestv — Claude için proje rehberi

## Proje
Windows 11 x64 için açık kaynak IPTV oynatıcı. Lisans: **GPL-3.0-or-later**. Repo: https://github.com/enestahtaci/enestv
C# / .NET 10 / WPF / MVVM · LibVLCSharp(.WPF) 3.10.1 + VideoLAN.LibVLC.Windows 3.0.23.1 · SQLite · DPAPI · Inno Setup 6.
Uygulama yayın, liste veya hesap içermez; kullanıcı kendi M3U/Xtream kaynağını ekler. Veriler `%LOCALAPPDATA%\enestv` (veya `ENESTV_DATA_DIR`).

## Yapı
- `EnesTV.Core/` — modeller, M3U/Xtream/XMLTV ayrıştırıcıları, SQLite store, ağ, DPAPI (UI bağımsız).
- `EnesTV.App/` — WPF uygulaması, **tek ana pencere**.
  - `MainWindow.xaml` — sol menü (Canlı TV, Favoriler, Son izlenenler, Kategoriler, Filmler, Diziler); orta sütun kanal listesi
    veya film/dizi kataloğu; sağda tek ortak video alanı + moda göre kontroller/EPG/detaylar.
  - `MainWindow.xaml.cs` (kapanış, tam ekran, ses/altyazı izleri), `MainWindow.Navigation.cs` (canlı TV klavyesi, tam ekran
    kontrolleri), `MainWindow.Vod.cs` (film/dizi: sarma, süre çubuğu, klavye).
  - `ViewModels/MainViewModel*` — partial: Main, Library, Organization, Epg, `ModeViewModel.cs` (mod geçişi, `Vod`,
    `ActivePlayback`/`ActivePlayer`, boş ekran önerileri). `VodViewModel` film/dizi kataloğu ve oynatma.
  - `Playback/PlaybackService` — libvlc çağrılarının TEK geçiş noktası (`_gate` + `Task.Run`). Her modun kendi servisi var.
  - `Views/` — `Styles.xaml` + `Theme.cs` (koyu/açık renkler iki yerde: ikisini birlikte güncelle), `Icon.cs` (Lucide ikonları),
    `Hint.cs` (placeholder), `FullscreenWindow.cs`, diyalog pencereleri (Rehber, Kanal düzeni, Ayarlar, Kaynak, Kısayollar).
- `EnesTV.Tests/` birim testleri · `EnesTV.Smoke/` gerçek VLC ve WPF ile entegrasyon/UI doğrulaması · `Installer/` Inno Setup.

## Komutlar
.NET 10 SDK `C:\Program Files\dotnet\dotnet.exe`, Inno Setup `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`.
```powershell
dotnet build enestv.sln -c Debug
dotnet test EnesTV.Tests -c Debug --no-build
dotnet run --project EnesTV.Smoke -c Debug --no-build -- <çıktı-klasörü>          # çekirdek: oynatma, yeniden bağlanma
dotnet run --project EnesTV.Smoke -c Debug --no-build -- --vod <çıktı-klasörü>    # film/dizi ana pencerede
dotnet run --project EnesTV.Smoke -c Debug --no-build -- --ui <çıktı-klasörü>     # canlı TV arayüzü, tam ekran, kanal düzeni
powershell -ExecutionPolicy Bypass -File Installer\Build-Setup.ps1 -InnoCompiler "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" -OutputDirectory dist
```
- Smoke testleri uygulama penceresi açar: kullanıcıya sormadan çalıştırma. Çıktıları geçici klasöre yaz.
- `--ui` gerçek imleci hareket ettirir; kullanıcı o sırada fareyi/pencereleri kullanırsa "Mouse movement reveals fullscreen
  controls" düşebilir. Hata mesajı imlecin altındaki programı yazar; başka bir uygulamaysa ortam kaynaklıdır, tekrar çalıştır.
- Windows PowerShell 5.1'de `Build-Setup.ps1` için `-OutputDirectory` verilmeli (parametre varsayılanında `$PSScriptRoot` boş).
- Setup çalışırken `dist\*.exe` kilitlidir; yeniden derlemeden önce açık sihirbaz kapatılmalı.

## Çalışma düzeni
1. **Salt okunur analiz** → rapor (Türkçe, dosya:satır kanıtlı, hipotezler açıkça işaretli).
2. **Onay** bekle.
3. **Uygulama**: küçük adımlar; her adımda build + test, arayüz değiştiyse smoke ve ekran görüntüsüyle kontrol.
- Commit, push, paket ekleme/güncelleme ve uygulamayı çalıştırma yalnızca açık onayla.
- Commit'lerde **Claude atfı yok**: `Co-Authored-By` satırı veya "Generated with Claude" eklenmez; yazar kullanıcının git kimliği.
- Commit'i önce ayrı bir dala at, kullanıcı isterse `main`'e fast-forward birleştirip gönder. Türkçe/tırnaklı commit mesajını
  PowerShell 5.1'de `git commit -F <dosya>` ile ver (komut satırı tırnakları bozuyor).

## Dokunulmaz alanlar (açık izin olmadan değiştirme)
- VLC eklenti listesi / VideoLAN.LibVLC paket ayarları (`Directory.Build.props` içindeki `Vlc*Enabled`).
- Lisans dosyaları: `LICENSE`, `THIRD-PARTY-NOTICES.md`, `SOURCE-ACCESS.md`, `licenses/`.
- `Installer/` betikleri. Kullanıcının verdiği izinlerle yapılanlar: `AppVersion`, sihirbaz logoları, bilgiler sayfasının
  kurulum öncesine alınması, bitiş sayfası onay kutuları ve teşekkür notu. Yeni değişiklik için yine izin iste.
- Yeni üçüncü taraf bileşen eklenirse GPL-3.0 uyumu kontrol edilir; NOTICES satırı ve `licenses/` metni gerekir.
- `THIRD-PARTY-NOTICES.md` paketin gerçek içeriğini anlatır: sürüm yükseltince veya SDK değişince paketteki sürümlerle
  (ör. .NET runtime `enestv.runtimeconfig.json`) karşılaştırıp güncelle.

## Kurallar ve kararlar
- **Türkçe karakter önceliği:** Tüm UI metni Türkçe; ı/İ/ş/ğ/ü/ö/ç doğru görünmeli. Dosyalar UTF-8. Tarih biçimi `tr-TR`.
- **libvlc:**
  - `MediaPlayer`/`Media` çağrıları UI thread'inde yapılmaz; `PlaybackService.WithPlayerAsync` kullanılır.
  - LibVLC olay işleyicilerinden aynı player çağrılmaz; işi `Task.Run`'a devret.
  - `Stop()` takılabilir: UI asla süresiz beklemez (`BoundedAsync`, kapanışta önce pencereyi gizle).
  - `EnableMouseInput`/`EnableKeyInput` kapalı; girdi WPF'te kalır.
- **Video alanı (LibVLCSharp.WPF):**
  - `VideoView` asla unload edilmez (HwndHost yok olur). Modlar arası yalnızca bağlı `MediaPlayer` değişir (`ActivePlayer`).
  - Overlay içeriği ayrı, saydam bir pencerededir: arka planı `#01000000` olmalı (tam saydam fare olaylarını VLC'ye geçirir).
    Overlay'deki `DynamicResource` yalnızca Application kaynaklarını görür; pencere kaynakları `StaticResource` ile.
  - Boş video alanının rengi `FullscreenWindow` içindeki `WM_CTLCOLORSTATIC` ile siyah tutulur.
  - Aynı anda tek yayın (sağlayıcı bağlantı limiti): moda geçerken diğer mod durdurulur.
- **XAML:**
  - `LiveOnly`/`VodOnly` stilleri `MainViewModel.IsVod`'a bakar; aynı elemana `DataContext="{Binding Vod}"` verme, sarmalayıcı kullan.
  - Placeholder: `views:Hint.Text="…"`. Surface renkli panel üstündeki düğmeler: `Style="{StaticResource RaisedButton}"`.
  - İkonlar: `views:Icon Kind="…"`, Lucide 0.460.0 yolları `Icon.cs` içinde (ISC). Yeni ikon eklerken WPF uyumlu tek yola çevir
    (her `<path>` başındaki küçük `m` mutlak `M` olur) ve NOTICES'taki ikon sayısını güncelle.
- `.Result` / `.Wait()` yasak; `async void` yalnızca olay işleyicilerinde ve try/catch ile.
- Kullanıcı verisi (kaynak URL'leri, Xtream hesapları) loglara ve git'e girmez; sırlar DPAPI ile saklanır.
- `TreatWarningsAsErrors=true` — uyarısız derleme zorunlu.

## Bilinen açık işler
- Çok platform planı Faz 0: çekirdeği platformdan bağımsız yapmak (`ISecretProtector`), ortak test verileri, yedekle/geri yükle.
- Her Release'te `RELEASE-CHECKLIST.md` izlenir; v1.4.0 düzeni (kaynak arşivleri, `SHA256SUMS.txt`) örnektir.
- VLC eklenti ayıklama önerisi ertelendi.
