[English](README.md) | [Türkçe](README.tr.md)

# enestv

## Lisans ve içerik

Copyright (c) 2026 enestv katkıcıları.
enestv, GNU General Public License sürüm 3 veya (tercihinize bağlı olarak) sonraki sürümleri altında sunulur: **GPL-3.0-or-later**. Tam metin: [LICENSE](LICENSE). Üçüncü taraf lisansları ayrı korunur: [bildirimler](THIRD-PARTY-NOTICES.md).
Katkıcı onayları proje sahibi tarafından alınır; bu metin onayların tamamlandığı beyanı değildir.

enestv yayın, playlist, film/dizi, EPG veya hesap sağlamaz. Kullanıcı erişim hakkı olduğu kaynakları ekler. Yayıncılar veya VideoLAN ile bağlantısı, ortaklığı ya da onay ilişkisi yoktur.

**HTTP kaynakları:** HTTP adreslerinde kullanıcı adı, parola, playlist ve yayın istekleri ağ üzerinden şifrelenmeden iletilebilir. Mümkünse HTTPS kullanın. Yerel DPAPI koruması ağ trafiğini şifrelemez.

## Kaynak kodu ve üçüncü taraf kaynak arşivleri

[enestv kaynak deposu](https://github.com/enestahtaci/enestv) ve ilgili sürüm tag'i uygulama kodunu içerir. Her GitHub Release, aynı sürümün enestv kaynak arşivini, üçüncü taraf kaynaklarını, gerekli yamaları/build reçetelerini ve SHA256SUMS.txt dosyasını içermelidir.
[Tam resmi indirme adresleri, FFmpeg contrib yama/ayar incelemesi ve açık doğrulamalar](SOURCE-ACCESS.md).
[GitHub Releases](https://github.com/enestahtaci/enestv/releases). Bu adresler yayın için belirlenmiştir; depo henüz oluşturulmamıştır ve adreslerin erişilebilirliği doğrulanmış değildir. Açık native kaynak doğrulamaları tamamlanmadan Release hazır sayılmaz.

## Derleme ve kurulum paketi üretimi

Bu sürüm .NET 10 hedefler (LTS, Kasım 2028'e kadar destekli).
Windows 11 x64 ve .NET 10 SDK ile depo kökünde:

```powershell
dotnet restore enestv.sln
dotnet build enestv.sln -c Release
dotnet test EnesTV.Tests -c Release
```

Kurulum için Inno Setup gerekir. `Installer/Build-Setup.ps1` betiğine `-InnoCompiler` ile kurulu ISCC.exe yolunu verin. Betik bağımsız x64 publish ve installer üretimini yürütür; taze staging dizini kullanır.
Bu komutlar dokümantasyondur; Aşama 2 hazırlığında çalıştırılmadı. [Release kontrol listesi](RELEASE-CHECKLIST.md).

### 1.4.1 — .NET 10

`enestv-Setup-1.4.1-x64.exe` ile güncelleyin. Önce açık enestv'yi kapatın. Hesaplar, favoriler, geçmiş ve izleme konumları korunur; veritabanı şeması değişmez.

- Uygulama .NET 8 yerine .NET 10 (LTS, Kasım 2028'e kadar destekli) ile çalışır; .NET 8'in desteği 10 Kasım 2026'da sona erdi. Çalışma zamanı pakete dahildir, ayrıca kurulum gerekmez.
- SQLite bileşenleri güncellendi (Microsoft.Data.Sqlite 10.0.14, SQLitePCLRaw 2.1.13). Özellik değişikliği yoktur.

### 1.4.0 — tek pencere, tam ekran düzeltmeleri

`enestv-Setup-1.4.0-x64.exe` ile güncelleyin. Önce açık enestv'yi kapatın. Hesaplar, favoriler, geçmiş ve izleme konumları korunur; veritabanı şeması değişmez.

- **Filmler** ve **Diziler** artık ayrı pencere açmaz. Sol menüden seçildiğinde orta bölümde katalog, sağda aynı oynatıcı ile film kontrolleri ve detaylar görünür. Uygulama yine Canlı TV ile açılır.
- Aynı anda tek yayın açık kalır: Filmler/Diziler'e geçince canlı yayın, Canlı TV'ye dönünce film durur (izleme konumu kaydedilir). Filmler/Diziler'de Esc Canlı TV'ye döner.
- Boş oynatıcı ekranı işe yarar hale geldi: son izlenen kanal için **Devam et**, son izlenen ve favori kanallara kısayollar, kaynak yoksa **Kaynak ekle**. Durdur'a basınca da bu ekran gelir.
- Tam ekranda videoya tıklayınca Esc'nin çalışmaması, fareyi oynatınca kontrol çubuğunun geri gelmemesi ve boş oynatıcının beyaz görünmesi düzeltildi.
- Yanıt vermeyen bir yayın artık durdurma, bölüm değiştirme ve kapatma sırasında uygulamayı kilitlemez.

### 1.3.0 — arayüz ve kullanım düzenlemeleri

`enestv-Setup-1.3.0-x64.exe` ile güncelleyin. Önce açık enestv'yi kapatın. Mevcut hesaplar, favoriler, geçmiş ve izleme konumları korunur; bu sürüm veritabanı şemasını değiştirmez.

- Menü Türkçeleştirildi, vektör simgeler ve seçili bölüm görünümü yenilendi.
- Kanal satırları sadeleşti. İlk açılış, boş favoriler/geçmiş, yükleme ve sonuçsuz arama ayrı açıklamalar gösterir; arama/filtre tek düğmeyle temizlenir.
- Oynat/duraklat düğmesi belirginleştirildi ve simgesi oynatma durumuna bağlandı. **Görüntü, ses ve altyazı** bölümünü açarak ek oynatma seçeneklerine erişin. Film/dizi ekranında bu bölüm **Ses ve altyazı** adındadır.
- Yayın hataları video üzerinde açıklama ve **Yeniden dene** düğmesiyle gösterilir.
- Film detayları kullanılmayan bölüm sütununu kaplar. Bölüm seçenekleri yalnızca dizilerde görünür. Hesap bulunmaması ve boş katalog durumları açıklanır.
- Ayarlar kartlara ayrıldı. Kaynak ekleme formu seçilen türe göre açıklama gösterir; kısa pencerede alanlar kaydırılır, kaydet düğmesi görünür kalır.
- Giriş alanları, onay kutuları, ses/ilerleme çubukları ve odak/seçim görünümü koyu ve açık temada ortak tasarımı kullanır.

10.000 kanallı liste, gerçek VLC oynatımı, tam ekran, küçük pencere, klavye kısayolları ve film/dizi akışları yeniden kontrol edildi. Ayrıntılar `UI-DESIGN.md` ve `VERIFICATION-1.3.0.md` dosyalarındadır. Tek Setup EXE paylaşılabilir; .NET ve VLC pakete dahildir. Kurulum kişisel hesap içermez.

### 1.2.0 — filmler, diziler ve kaldığın yerden devam

`enestv-Setup-1.2.0-x64.exe` ile güncelleyin. Ana menüye **Filmler** ve **Diziler** eklendi. Bu kataloglar, kaynağı **Xtream Codes** olarak eklenmiş hesabınızdan alınır. Yalnızca M3U kullananlar için sunucu URL'si, kullanıcı adı ve parola ile bir Xtream kaynağı eklemek gerekir; sağlayıcının hesabınızda film/dizi erişimi sunması gerekir.

1. Ana menüde **Filmler** veya **Diziler** seçin. Sol üstten Xtream hesabını değiştirin.
2. Kategoriden filtreleyin veya ada göre arayın. Bir içeriği seçerek açıklamasını açın.
3. Dizilerde sezon ve bölüm seçin; **İzle / devam et** düğmesine basın.
4. Süre çubuğunu kullanın veya **−10 sn / +10 sn** ile sarın. Oynatıcı odaktayken Space duraklatır, sol/sağ 10 saniye sarar, yukarı/aşağı sesi değiştirir, M sessize alır.
5. Konum yaklaşık 5 saniyede bir ve durdurma/içerik değiştirme/pencereyi kapatma sırasında kaydedilir. **Baştan başlat** kayıtlı konumu kullanmaz. İzlenen bölüm işaretlenir; dizi yeniden seçildiğinde son izlenen bölüm, tamamlanmışsa sonraki bölüm seçilir.
6. **Sonraki bölüm** aynı sezonun sonraki bölümüne, sezon bittiyse sonraki sezona geçer. Bölümler kendiliğinden oynatılmaz; düğmeye basmak gerekir.
7. F11 ile tam ekran; Esc ile çıkış. Süre, ses ve oynatma kontrolleri fare hareketinde görünür, kullanılmadığında gizlenir. **Canlı TV'ye dön** katalog penceresini kapatır.

Canlı TV, film/dizi ekranı açılırken durdurulur; iki yayın aynı anda oynatılmaz. Geri dönünce bir kanal seçerek yeniden başlatabilirsiniz. Mevcut genel favoriler/kanal düzeni canlı TV içindir.

Kataloglar ilk açılışta indirilir, şifreli SQLite önbelleğinde tutulur ve 6 saatten eskiyse yeniden alınır. **↻** ile elle yenilenebilir; başarısız yenileme son başarılı kataloğu silmez. Bölüm ayrıntıları seçildiğinde sunucudan alınır. Ağ olmadan katalog görülebilir; içerik oynatımı için bağlantı gerekir.

Şema 1 veya 2 kayıtları veriler silinmeden sürüm 3'e yükseltilir. Hesap, favori, geçmiş ve kanal düzeni korunur. Yeni katalog/izleme konumu verileri de DPAPI ile şifrelenir. Yeni veritabanını eski uygulama sürümüyle açmak desteklenmez. Kurulum kişisel hesap veya hazır içerik içermez.

Doğrulama ayrıntıları `VERIFICATION-1.2.0.md` dosyasındadır. Yeni test akışı: `dotnet run --project EnesTV.Smoke -c Release -- --vod ./test-results/vod`.


### 1.1.0 — tam ekran kontrolleri, kanal geçişi ve kanal düzeni

`enestv-Setup-1.1.0-x64.exe` önceki kurulumun üzerine kurulabilir. Önce çalışan enestv'yi kapatın. Hesaplar, kaynaklar, genel favoriler ve geçmiş korunur; yeni düzen verileri için veritabanı otomatik olarak sürüm 2'ye yükselir.

- **Tam ekran:** F11 veya tam ekran düğmesi. Fare hareketinde görünen alt kontrol çubuğu, hareketsiz kalınca 3 saniyede gizlenir; fare çubuğun üzerindeyken veya bir kontrol sürüklenirken açık kalır. Kanal/EPG bilgi panelleri gizli kalır. Esc ile çıkılır.
- **Kanal geçişi:** Önceki, sonraki ve son izlenen kanala dön düğmeleri. Önceki/sonraki mevcut arama, kategori veya favori listesini takip eder ve listenin sonundan başına döner. Son izlenen düğmesi iki kanal arasında geçiş yapar.
- **Kanal numarası:** Listede görünen numarayı yazın; Enter'a basın veya 1,2 saniye bekleyin. Numara, arama/favori filtresinden bağımsız tüm görünür kanalların sırasını kullanır. Düzeni veya gizli kanalları değiştirince numaralar yeniden belirlenir. Metin alanına yazarken kanal kısayolları çalışmaz.
- **Kısayollar:** Sol menüde Kısayollar ekranından yeniden atama ve varsayılanlara dönüş. Varsayılanlar: PageDown sonraki, PageUp önceki, Backspace son kanal, Space duraklat/oynat, M sessiz, yukarı/aşağı ses. F11, Esc ve rakamlar sabittir. Çakışan kısayollar kaydedilmez.
- **Kanal düzeni:** Sol menüden açılır. Kategorileri/kanalları gizleyin, görünen adı düzenleyin, yukarı/aşağı taşıyın veya A–Z sıralayın. Boş görünen ad sağlayıcının adını kullanır. Kaydet tüm değişiklikleri uygular; Vazgeç değişiklikleri atar.
- **Özel favori listeleri:** Kanal düzeninde liste oluşturun, adını değiştirin veya silin. Ctrl/Shift ile seçilen kanalları ekleyip çıkarın. Ana ekranda Favoriler bölümündeki açılır menüden liste seçilir. Genel yıldızlı favoriler ayrı tutulur. Özel bir listede yıldıza basmak kanalı yalnızca o listeden çıkarır.
- Kanal düzeni, özel listeler ve kısayollar SQLite içinde DPAPI ile şifrelenerek tutulur. Liste yenilemesi aynı kimliğe sahip kanalların düzenini korur. Sağlayıcının kanal kimliğini değiştirmesi bu eşleştirmeyi bozabilir.

Kurulum tek EXE olarak paylaşılabilir; içinde .NET ve LibVLC bulunur. Önceki sürümdeki ikon ve işlevsiz “enes'e teşekkür et” kutucuğu korunmuştur. Ayrıntılı kontrol sonuçları `VERIFICATION-1.1.0.md` dosyasındadır.


### 1.0.2 — Windows kurulumu ve ikon

`enestv-Setup-1.0.2-x64.exe` ile kurulum yapılabilir. Türkçe sihirbaz uygulamayı varsayılan olarak `%LOCALAPPDATA%\Programs\enestv` konumuna, yönetici izni gerektirmeden kurar. Başlat menüsü kısayolu oluşturur; masaüstü kısayolu isteğe bağlıdır. .NET ve LibVLC dahildir. Windows Ayarlar → Uygulamalar üzerinden kaldırılabilir; playlist/hesap/ayar verileri `%LOCALAPPDATA%\enestv` içinde korunur.

Son sayfada isteğe bağlı “enestv'yi şimdi aç” seçeneği ve yalnızca şaka amaçlı **“enes'e teşekkür et”** kutucuğu vardır. Teşekkür kutucuğunun hiçbir olay işleyicisi, komutu, ağ isteği veya kalıcı kaydı yoktur.

Yeni ikon Windows EXE kaynaklarına, uygulama pencerelerine, Setup/kaldırıcıya ve kısayollara eklendi. PNG/ICO kaynakları `EnesTV.App/Assets` içindedir. Setup kod imzası içermez.

Kurulumu yeniden derlemek için .NET 10 SDK ve Inno Setup 6.7.3 veya üzeri gerekir:

```powershell
./Installer/Build-Setup.ps1 -InnoCompiler 'C:\Path\To\Inno Setup 6\ISCC.exe'
```

Script bağımsız x64 uygulamayı publish eder, lisans dosyalarını kopyalar ve `dist` altında Setup oluşturur. Dağıtım/ticari kullanımda Inno Setup'ın lisans koşullarını dikkate alın: https://jrsoftware.org/isinfo.php

### 1.0.1 — pencere ve tam ekran düzeltmeleri

Windows başlık çubuğu, yazısı ve çerçevesi artık seçili koyu/açık temayla eşleşir. Tam ekran düğmesi veya F11 yalnızca videoyu, görev çubuğu alanı dahil tüm monitörde gösterir; kanal bilgisi, EPG, kontroller ve kenar boşlukları gizlenir. Esc veya F11 önceki normal/büyütülmüş pencere düzenini geri getirir. Video en-boy oranı ayarı korunur.

Windows 11 x64 için C# / .NET 10 / WPF IPTV masaüstü uygulaması. Kullanıcının kendi yasal kaynaklarını oynatır. Uygulama ile birlikte sağlayıcı, hazır kanal listesi veya yayın hesabı verilmez.

## Hızlı başlangıç

Hazır `enestv-win-x64` klasöründeki **enestv.exe** dosyasını açın. Dağıtım .NET çalışma zamanını ve LibVLC'yi içerir; ayrı .NET veya VLC kurulumu gerekmez. ZIP kullanıyorsanız önce tamamını çıkartın. EXE'yi klasördeki DLL ve `libvlc` dizininden ayırmayın.

1. **Kaynak ekle → M3U URL** seçin, kaynak adı ve kendi playlist adresinizi girin.
2. Liste indirilir, kategoriler oluşturulur ve son başarılı sonuç kaydedilir.
3. Kanal satırına tıklayın. Yıldız düğmesi favoriyi değiştirir.
4. **Ayarlar** içinde kaynakları düzenleyin, yeniden adlandırın, yenileyin veya silin.
5. XMLTV adresini Ayarlar'a yazıp **Kaydet ve rehberi güncelle** seçin. Xtream hesaplarının XMLTV adresi otomatik oluşturulur; aynı düğmeyle güncellenir.

**Dosya aç** `.m3u` ve `.m3u8` kanal listelerini alır. Xtream için Kaynak ekle menüsünde **Xtream Codes**, sunucu, kullanıcı adı ve parola alanlarını kullanın.

## Geliştirme gereksinimleri

- Windows 11 x64
- .NET 10 SDK (geliştirme sırasında 10.0.403 kullanıldı)
- İlk paket geri yüklemesinde NuGet erişimi
- İsteğe bağlı Visual Studio 2022, .NET masaüstü geliştirme iş yükü

## Derleme ve çalıştırma

Kaynak kodu paketindeki enestv.sln dosyasının bulunduğu dizinden:

```powershell
dotnet restore enestv.sln
dotnet build enestv.sln -c Release
dotnet test EnesTV.Tests -c Release
dotnet run --project EnesTV.App -c Release
```

Bağımsız Windows dağıtımı:

```powershell
dotnet publish EnesTV.App -c Release -r win-x64 --self-contained true -o dist/enestv-win-x64
```

`Directory.Build.props` x64 hedefini ve compiler uyarılarının hata sayılmasını ayarlar. Trim ve single-file kullanılmaz; LibVLC'nin değiştirilebilir DLL/plugin dosyaları korunur. Üçüncü taraf bildirimlerini dağıtıma ekleyin.

## Mimari

```text
EnesTV.Core/
  Models/       Kanal, kaynak, program ve ayar kayıtları
  Services/     HttpClient, M3U, Xtream, XMLTV, filtre, DPAPI, dönen log
  Data/         SQLite sorguları, işlemler, EPG cache, şema sürümleme
EnesTV.App/
  ViewModels/   MVVM durumu ve asenkron komutlar
  Views/        Kaynak, ayarlar ve TV rehberi pencereleri; stiller
  Services/     Sınırlı eşzamanlı/asenkron logo önbelleği
  Playback/     LibVLC yaşam döngüsü ve bağlantı durumları
EnesTV.Tests/   Parser, normalizasyon, filtre, saat, kalıcılık testleri
EnesTV.Smoke/   Yerel HTTP sunucusu, sentetik medya ve entegrasyon testleri
```

SQLite `PRAGMA user_version=3` ile sürümlenir; sürüm 1/2 kayıtları mevcut veriler silinmeden yükseltilir; ilk kurulum bir transaction içinde yapılır. Gelecek migration'lar `LibraryStore.InitializeAsync` içinde sırayla eklenir. Bilinmeyen ileri şema sürümleri üzerine yazılmaz. Her bağlantı işlem sonunda kapanır; bulk işlemler UI thread'inden ayrıdır.

## Özellikler ve uygulama davranışı

- Birden fazla yerel/uzak M3U ve Xtream kaynağı; ekle, düzenle, ad değiştir, sil, manuel yenile, son güncelleme zamanı.
- `tvg-id`, `tvg-name`, `tvg-logo`, `group-title`; eksik kategori `Uncategorized`; bozuk metadata'ya dayanıklı ayrıştırma; tam tekrarları eleme.
- UTF-8/BOM, UTF-16 BOM; UTF-8 çözülemezse Windows-1254 geri dönüşü.
- Ad ve kategori üzerinde büyük/küçük harf duyarsız, 180 ms gecikmeli arama; sanallaştırılan ve geri dönüştürülen kanal satırları.
- Logolar yalnızca görünür satırlarda yüklenir; 4 eşzamanlı indirme, en fazla 200 bellek ve 500 disk girdisi, 7 gün cache ömrü, 4 MB dosya sınırı. Bozuk logo için TV simgesi.
- Oynat/duraklat, durdur, ses düzeyi, sessiz, tam ekran, görüntü oranı, mevcut ses ve altyazı izleri.
- F11 tam ekran, Esc tam ekrandan çıkış; uygun odakta Space oynat/duraklat.
- Kanal değişiminde eski işlem iptal edilir, player durdurulur, eski Media serbest bırakılır. Yerel VLC çağrıları tek sıra üzerinden worker thread'inde yürür.
- Idle, Loading, Playing, Paused, Buffering, Reconnecting ve Error durumları. Yeniden bağlanma en fazla 3 denemedir (1, 2, 4 saniye bekleme); yeni kanal veya manuel yeniden bağlanma bütçeyi yeniler. İlerlemeyen stream için yaklaşık 25–30 saniyelik kontrol vardır.
- Favoriler, son oynatılan 20 kanal, ayarlar, kaynaklar ve kanal metadata'sı SQLite'ta kalır. Ağ hatası kayıtlı listeyi silmez. Açılışta ağ gerekmeden yerel liste yüklenir.
- XMLTV/XMLTV gzip; `tvg-id` eşleştirmesi, saat dilimi ofsetleri ve yerel saat gösterimi; şimdi/sıradaki program, saat, açıklama ve kanal/gün bazlı TV Guide.
- EPG sadece ilgili kanal kimlikleri için, yaklaşık son 1 gün/gelecek 8 gün aralığında transaction içinde cache'lenir. Tüm XML belge ağacını belleğe yüklemez. Şimdi/sıradaki bilgiler dakika başında yenilenir.
- Koyu ve açık tema, varsayılan ses düzeyi, otomatik yeniden bağlanma, XMLTV ve kaynak/hesap yönetimi.

## Veriler ve gizlilik

Varsayılan veri klasörü `%LOCALAPPDATA%\enestv`:

- `library.db`: kaynaklar, kanallar, favoriler, geçmiş, ayarlar ve EPG.
- `logos/`: indirilen kanal logoları.
- `logs/enestv.log`: 1 MB'da dönen log ve en fazla üç eski dosya.

Kaynak adresi, Xtream kullanıcı adı/parolası, kanal URL'leri/metadata'sı ve ayarlar Windows DPAPI CurrentUser ile şifrelenir. SQLite'ta açık parola tutulmaz. Kaynak adları, favori kimlikleri ve EPG program metinleri şifrelenmez. Loglara URL, kimlik bilgisi veya exception mesajı yazılmaz; sabit olay kodları ve exception türleri kaydedilir. Windows hesapları arasında şifreli veritabanı taşınamaz. Kullanıcının sağladığı orijinal playlist dosyası değiştirilmez.

HTTP işlemleri tek uzun ömürlü HttpClient kullanır; redirect, bağlantı zaman aşımı, iptal ve geçici HTTP hatalarında sınırlı geri çekilme desteklenir. İndirme 2 dakika ve boyut sınırlarıyla korunur. Geçici indirme dosyaları işlem sonunda silinir. Şifresiz HTTP seçilirse aktarım HTTPS korumasına sahip olmaz.

Testler için `ENESTV_DATA_DIR` ortam değişkeni veri konumunu değiştirebilir; normal kullanımda gerekmez.

## Test ve kabul doğrulaması

```powershell
dotnet test EnesTV.Tests -c Release
dotnet run --project EnesTV.Smoke -c Release -- --verify ./test-results/integration
dotnet run --project EnesTV.Smoke -c Release -- --ui ./test-results/ui
```

Her test çalıştırmasında yeni/boş bir sonuç klasörü kullanın. Smoke testleri yalnızca loopback üzerinde yerel sunucu açar, kendi animasyonlu AVI'sini/sessiz WAV'ını üretir ve dış IPTV kaynağı kullanmaz. UI testi gerçek WPF pencerelerini açar; arama, favori, program bilgisi, tam ekran ve pencere boyutunu test eder. UI görselleri WPF render çıktısıdır; native VLC yüzeyi bu görüntülerde siyah görünür. Gerçek video decoder ayrıca LibVLC snapshot ile doğrulanır.

Manuel 10.000 kanal testi için:

```powershell
dotnet run --project EnesTV.Smoke -c Release -- --serve ./test-fixtures
```

Komut 127.0.0.1:18765 üzerinde sunucu başlatır ve `test-fixtures/test-10000.m3u` üretir. Ana uygulamaya `http://127.0.0.1:18765/playlist.m3u` eklenebilir. Bu adres yalnızca geliştirici testidir; hazır uygulamaya dahil edilmez. Ctrl+C sunucuyu kapatır. Çalışma sonuçları `VERIFICATION.md` içinde özetlenmiştir.

## Başlıca bağımlılıklar

- [LibVLCSharp.WPF 3.10.1](https://www.nuget.org/packages/LibVLCSharp.WPF/3.10.1)
- [VideoLAN.LibVLC.Windows 3.0.23.1](https://www.nuget.org/packages/VideoLAN.LibVLC.Windows/3.0.23.1)
- [Microsoft.Data.Sqlite 8.0.30](https://www.nuget.org/packages/Microsoft.Data.Sqlite/8.0.30)
- System.Security.Cryptography.ProtectedData 8.0.0 ve System.Text.Encoding.CodePages 8.0.0
- Testlerde xUnit ve Microsoft.NET.Test.Sdk

MVVM için ek framework bağımlılığı kullanılmadı.

## Bilinen sınırlar

- DRM, portal/MAC girişi, catch-up, kayıt ve timeshift bu sürümde yoktur. Film/dizi katalogları Xtream hesaplarıyla desteklenir; genel M3U listelerinden otomatik dizi kataloğu çıkarılmaz.
- Xtream canlı kanallar, film/dizi katalogları ve XMLTV EPG uygulanmıştır; sağlayıcıya özgü standart dışı API varyasyonları veya özel HTTP header/cookie isteyen stream'ler desteklenmez.
- Doğrudan HLS manifestleri kanal URL'si olarak LibVLC'ye verilir; dosya içe aktarma IPTV kanal listeleri içindir. HLS segment manifesti kanal kataloğu değildir.
- XMLTV ofseti belirtilmezse UTC kabul edilir; geçersiz veya eksik bitiş saatli programlar atlanır. Rehber indirme manuel tetiklenir.
- Son 20 kanalın kaydı oynatma başladığında yapılır; sadece başarısız bağlantı denemeleri geçmişe eklenmez.
- Decoder/ses/altyazı özellikleri kaynağın sağladığı izlerle sınırlıdır. Canlı kaynağın duraklatmaya izin verme biçimi sağlayıcıya bağlıdır.
- DPAPI ile şifreli kaynaklar başka bir Windows hesabında açılamaz. Kurulum EXE'si kod imzası içermez; ayrıca klasör dağıtımı üretilebilir.
- Gerçek bir kullanıcının sağlayıcı hesabıyla uzun süreli test yapılmadı. Ağ/sağlayıcı, ekran kartı ve codec çeşitliliği için gerçek kullanım doğrulaması gerekir.

