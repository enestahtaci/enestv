# enestv 1.3.0 doğrulama

4 Kasım 2026 · Windows x64 · .NET SDK 8.0.425.

## Sonuçlar

- Release derleme başarılı, sıfır hata/uyarı. Bağımsız Windows x64 publish başarılı.
- xUnit: 28/28 geçti.
- Canlı TV arayüz akışı geçti: 10.000 kanallı liste, arama, favoriler, EPG, kaynak/ayar/rehber pencereleri, tam ekran ve normal/büyütülmüş pencereye dönüş, koyu/açık Windows başlık çubuğu.
- 10.000 kanalda 8 liste satırı oluşturuldu. Yerel testte UI zamanlayıcısının p95 aralığı 33,65 ms, en uzun aralığı 51,73 ms oldu. Bunlar bu makinedeki sentetik test ölçümleridir.
- Kanal düzeni, özel favori listeleri, numarayla geçiş, özelleştirilmiş kısayollar, metin alanında yazma ve tam ekran kontrol gizleme kontrolleri geçti.
- Yeni durum kontrolleri geçti: ilk açılışta kaynak ekleme, sonuçsuz arama, filtre temizleme, 620 piksel yüksekliğe sınırlı Xtream formunda görünür kaydet düğmesi, 1120×730 canlı TV ekranında 220 pikselden büyük video yüksekliği ve açılır oynatma seçeneklerinin ekrana sığması.
- Film/dizi entegrasyonu: 31 kontrol geçti. 10.000 film, sanallaştırma, arama/boş sonuç, afiş/açıklama, gerçek VLC video karesi, sarma, duraklatma, kayıtlı konumdan devam, baştan oynatma, bitişte yeniden bağlanmama, sezon/bölüm, sonraki bölüm, hızlı seçim, önbellek, hesap bulunmaması ve canlı kanalı olmayan hesap dahil.
- Film/dizi minimum penceresinde detay alanı ve izle düğmesi görünür kaldı. Yayın hatası, native video yüzeyi üzerindeki yeniden deneme kartında gösterildi.
- Koyu/açık canlı TV, film, dizi, küçük pencere, kaynak/ayarlar, tam ekran overlay ve hata ekranları WPF render görüntülerinden incelendi.

## Tekrarlama

```powershell
dotnet test EnesTV.Tests -c Release
dotnet run --project EnesTV.Smoke -c Release -- --ui ./test-results/ui-new
dotnet run --project EnesTV.Smoke -c Release -- --vod ./test-results/vod-new
```

Her akışa yeni bir çıktı klasörü verin. Arayüz testleri kendi pencerelerinde klavye odağı gerektirir. Tam ekran testinde başka pencereye geçmiş olma durumunu önlemek için odak ve modifier önkoşulları doğrulanır.

## Kapsam ve sınırlar

Testler yerel sentetik M3U/Xtream/XMLTV sunucusu ve üretilmiş AVI video kullanır; kullanıcının gerçek hesabı veya kişisel veritabanı kullanılmaz. Test videoları, geçici veritabanları ve günlükler kurulum paketine alınmaz. WPF render görüntüsünde native VLC video yüzeyi siyah görünür; gerçek çözülmüş video karesi ayrıca VLC snapshot ile doğrulanmıştır. Testler uygulama olayları/komutlarıyla çalışır; fiziksel fareyle tüm ekranların gezildiği iddia edilmez. Tüm sağlayıcı, monitör, DPI ve Windows kurulumlarının hatasız olduğu anlamına gelmez.
