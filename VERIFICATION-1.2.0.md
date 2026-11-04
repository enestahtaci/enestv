# enestv 1.2.0 doğrulama

Tarih: 4 Kasım 2026. Windows x64; .NET SDK 8.0.425.

## Sonuç

- Release derleme ve bağımsız Windows x64 publish başarılı; 0 derleme hatası/uyarısı.
- xUnit: 28/28 başarılı. Xtream katalog ayrıştırma, sayısal/metinsel kimlikler, eksik metadata, sezon/bölüm sırası, URL kodlaması, şema 2 → 3 yükseltmesi, şifreli katalog ve izleme konumu kalıcılığı dahil.
- Film/dizi WPF + LibVLC entegrasyonu: 29 kontrol başarılı.
- Mevcut canlı TV native entegrasyonu: 17/17 başarılı (yeniden bağlanma, hızlı kanal değişimi, durdur/oynat yarışı, ses/altyazı altyapısı, ses düzeyi, duraklat/devam dahil).
- Mevcut WPF canlı TV arayüz kontrolleri başarılı: arama, 10.000 kanal sanallaştırması, favoriler, EPG, pencere/tam ekran, kanal düzeni ve özelleştirilen klavye kısayolları.

## Film/dizi kabul senaryoları

- Xtream hesabından 10.000 film ve kategoriler çekildi; görünür olmayan katalog satırları oluşturulmadı (50'den az ListBoxItem).
- Film araması, afiş ve açıklama; film oynatımı, gerçek decoder PNG karesi, süre keşfi, süre çubuğunun klavye olayıyla sarma ve duraklatma doğrulandı.
- Kayıtlı konum SQLite'tan yeni pencere örneğinde okundu; gerçek LibVLC oynatımı aynı zamandan devam etti. Baştan başlatma ayrı test edildi.
- İçerik bitişi izlenmiş olarak kaydedildi ve canlı yayın gibi yeniden bağlanıp başa dönmedi.
- Sezonlar API'nin episodes alanından oluşturuldu; bölüm oynatma, sezon içi ve sezonlar arası sonraki bölüm, son izlenen sezon/bölümü otomatik seçme doğrulandı.
- Başarısız katalog yenilemesi son başarılı kataloğu korudu. Bozuk film URL'si hata gösterdi, uygulama çalışmaya devam etti.
- Ana menüde Filmler düğmesi katalog penceresini açtı ve canlı oynatmayı durdurdu; eşzamanlı iki yayın açılmadı.
- Xtream hesabı olmayan durumda açıklayıcı mesaj gösterildi. Geçerli fakat canlı kanalı olmayan hesap eklenebildi. Boş canlı liste yanıtı mevcut kanalları silmedi.
- 1100×760 küçük pencere ve tam ekran kontrol edildi. İzle düğmesi küçük pencerede açıklamadan bağımsız görünür tutuldu. Film, dizi ve tam ekran overlay render'ları görsel olarak incelendi.

## Kapsam

Testler yerel TCP sunucusunun sentetik Xtream/EPG/M3U cevapları ve yerel üretilen AVI video ile yapıldı. Kullanıcının gerçek sağlayıcısına giriş yapılmadı, kişisel veritabanı değiştirilmedi. WPF harness'i gerçek pencerelerin olaylarını ve komutlarını yürütür; harici fiziksel fare/klavye testi olarak raporlanmaz. WPF render görüntüsünde native VLC yüzeyi görünmez; gerçek video karesi ayrı decoder snapshot ile doğrulandı.

İlk testlerde klavye odağı ve ilk decoder karesinin hazır olma zamanı için test önkoşulları eksikti; pencere odağını ve video çıkışının hazır olmasını doğrulayan sınırlı beklemelerle düzeltildi. Son koşular başarılıdır.

Bu sürüm film/dizi kataloglarını Xtream API üzerinden sunar; genel M3U'dan otomatik dizi kataloğu çıkarmak, DRM, indirme ve otomatik sonraki bölüm oynatma kapsam dışıdır. Sarma ve devam, sağlayıcının oynatılabilir/aranabilir medya akışı sunmasına bağlıdır. Bölüm ayrıntıları ağdan alınır; katalog önbelleği çevrimdışı izleme sağlamaz.

API uyumluluğu için [Hypnotix Xtream istemcisinin](https://github.com/linuxmint/hypnotix/blob/master/usr/lib/hypnotix/xtream.py) film/dizi uç noktaları ve episode yanıt yapısı incelendi; enestv implementasyonu kendi C# servisidir.
