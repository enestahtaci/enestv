# enestv — doğrulama raporu

## 1.0.1 düzeltme doğrulaması

- Windows DWM başlık, yazı, çerçeve rengi ve dark-mode çağrıları koyu ve açık temalarda başarılı; yeni ayar penceresi de seçili temayı kullanıyor.
- Hem F11 hem tam ekran düğmesiyle giriş; Esc/F11 ile çıkış test edildi.
- Native pencere sınırları, görev çubuğu dahil monitör sınırlarıyla birebir eşleşti.
- Video genişlik/yüksekliği hem WPF içerik alanıyla hem geçerli DPI'daki native client piksel ölçüleriyle eşleşti.
- Tam ekranda kanal listesi, başlık, oynatma kontrolleri, kanal bilgileri ve EPG gizlendi; margin ve köşe yuvarlatmaları sıfırlandı.
- Normal ve önceden büyütülmüş pencere durumuna dönüş, önceki konum/boyut ve kenar boşluğu geri yüklemesi doğrulandı.
- Sonuç: `verification/fullscreen-1.0.1.json`. Test gerçek WPF penceresi ve native Windows ölçümleriyle çalışır; native video yüzeyi WPF render görüntüsünde görünmez.

Tarih: 12 Eylül 2026. Ortam: Windows x64, .NET SDK 8.0.425. Hazır dağıtım .NET / WindowsDesktop 8.0.31 ve LibVLC 3.0.23 içerir.

## Sonuç

- Release build: **0 uyarı, 0 hata**.
- xUnit: **22 / 22 başarılı**, atlanan test yok.
- Yerel ağ + SQLite + gerçek LibVLC entegrasyonu: **17 / 17 kontrol başarılı**.
- Gerçek WPF pencereleriyle uygulama içi UI testi başarılı.
- Koyu/açık tema, kaynak, ayarlar, TV Guide ve 1120×730 pencere düzeni render edilip görsel olarak incelendi.

## Kabul senaryoları

| Senaryo | Doğrulama |
| --- | --- |
| M3U URL → indir → parse → kategori → liste → video | Loopback HTTP yönlendirmesi, 10.000 kanal, 20 kategori, gerçek LibVLC decoder ve PNG video snapshot başarılı. WPF ViewModel akışı da çalıştırıldı. |
| Büyük liste/UI tepkisi | 10.000 kanalda yalnızca 8 ListBoxItem oluşturuldu. Son UI koşusunda Dispatcher ölçümü: p95 36,8 ms; en uzun aralık 230,3 ms. Donma/çökme gözlenmedi. |
| Bozuk stream | Yerel 404 stream, anlaşılır Error durumuyla sonuçlandı; süreç çalışmaya devam etti. |
| Hızlı kanal değişimi | 20 art arda seçimde son kanal oynadı. Stop ve yeni Play yarışında yeni seçim kazandı. |
| Yeniden açılışta veri | Yeni LibraryStore örneğiyle kaynak, favori, son 20 kanal ve ayarlar tekrar okundu. Aynı kaynağın refresh işleminde favoriler korundu. |
| Çevrimdışı liste | Cache okuma yalnızca SQLite'tan yapıldı; ağ çağrısı gerektirmedi. |
| Yanlış Xtream hesabı | Yerel API `auth=0` cevabı AuthenticationException olarak ayrıldı; kategori ve stream cevapları ayrıca test edildi. |
| EPG olmayan kanal | Programı bulunmayan test kanalı gerçek LibVLC ile oynadı. |
| EPG | Ofset dönüşümü unit testleri; 400 program import, 200 şimdi/sıradaki satırı, WPF ekranında mevcut program ve rehber. |
| Oynatıcı kontrolleri | Tam 3 yeniden bağlanma denemesi, ses izi keşfi/seçimi, ses düzeyi, sessiz, görüntü oranı, pause/resume, stop ve dispose test edildi. F11/Esc WPF routed key olaylarıyla doğrulandı. |

Yerel indirme + 10.000 kanal ayrıştırma + DPAPI ile şifreli metadata'nın transaction içinde kaydı son entegrasyon koşusunda **2.315 ms** sürdü. Bu tek makine ölçümüdür; sağlayıcı gecikmesini temsil etmez.

## Kapsam ve sınırlar

Testler uygulamanın kendi ürettiği AVI/WAV, M3U, XMLTV ve Xtream HTTP yanıtlarıyla yapıldı. Kullanıcının gerçek sağlayıcı hesabı verilmedi; dış bir IPTV sağlayıcısına bağlanılmadı. Uzun süreli gerçek yayın, sağlayıcıya özgü davranışlar, bütün codec/ekran kartları ve çoklu gerçek altyazı izleri ayrıca doğrulanmış sayılmaz.

UI doğrulaması WPF test harness'iyle ViewModel komutlarını ve gerçek pencere/olaylarını yürüttü. Harici fare/klavye sürme aracı onay zaman aşımına uğradığı için manuel tıklama testi yapılmış olarak raporlanmaz. WPF render görüntüleri native VLC yüzeyini içermez; görüntünün gerçekten çözüldüğü ayrı LibVLC snapshot testiyle doğrulandı.

Test çıktıları `verification/` içinde; tekrar çalıştırma komutları README'de bulunur.

## 1.0.2 kurulum ve ikon doğrulaması

- Release build: 0 uyarı, 0 hata. Gerçek WPF UI entegrasyon testi yeniden geçti; tema, F11/tam ekran, oynatma ve diyaloglar çalıştırıldı.
- Inno Setup 6.7.3 ile Windows 11 x64 kurulum paketi üretildi.
- Üretilen setup ayrı test dizinine sessiz modda kuruldu (çıkış 0). Kurulan enestv.exe başlatıldı, `app.ready` kaydı ve SQLite veritabanı doğrulandı; uygulama düzgün kapandı.
- Kaldırıcı çalıştırıldı (çıkış 0); uygulama ve kaldırma kaydı temizlendi, ayrı kullanıcı veri dizini korundu.
- PNG kaynak ikonundan 16–256 px çok boyutlu ICO oluşturuldu; uygulama EXE'sine, WPF pencere stiline ve setup'a bağlandı.
- Son sayfadaki `enes'e teşekkür et` kutusu yalnızca işaretleme durumunu değiştirir; olay işleyicisi veya veri/komut bağlantısı yoktur. Kurulum testi sessiz modda yapıldı; son sayfanın manuel görsel doğrulaması yapılmış sayılmaz.
