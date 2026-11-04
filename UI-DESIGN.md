# enestv arayüz düzeni — 1.3.0

Amaç: kullanıcının kaynağını ekleyip neye basacağını aramadan izlemeye başlayabilmesi. Kömür rengi zemin ve yeşil vurgu korunur; açık tema aynı düzeni kullanır.

| Alan | Sorun | Uygulanan düzen |
| --- | --- | --- |
| Gezinme | Türkçe/İngilizce karışımı, bazı cihazlarda bozuk simgeler | Türkçe bölüm adları; kütüphane, keşfet ve yönetim ayrımı; ölçeklenebilir vektör simgeler; belirgin seçili bölüm |
| Kanal listesi | Gereksiz tekrar, küçük metinler, her boş durumda aynı mesaj | Kanal adı ve mevcut program öne çıkar; arama, favoriler, geçmiş, yükleme ve ilk açılış için ayrı açıklamalar; filtreyi temizle ve kaynak ekle eylemleri |
| Oynatıcı | Çok sayıda eşit ağırlıklı düğme; küçük pencerede az video alanı | Belirgin oynat/duraklat düğmesi, duruma göre simge, ortak ses çubuğu; görüntü/ses/altyazı seçenekleri açılır bölümde |
| Hatalar | Yayın hatası video dışındaki küçük yazıda kalıyor | Video üzerinde açıklama ve yeniden dene düğmesi; normal ve tam ekran oynatıcıda görünür |
| Film/dizi | Boş siyah ekran, filmlerde kullanılmayan bölüm sütunu | Başlangıç açıklaması, hesap/arama/yükleme durumları; film detayları tam genişlikte; bölüm seçenekleri yalnızca dizilerde |
| Ayarlar | Birbirine karışan form alanları | Görünüm ve oynatma, TV rehberi, kaynaklar ve hesaplar kartları |
| Kaynak ekleme | Kaynak türlerinin farkı belirsiz; uzun hesap formu kısa ekrana sığmayabilir | Türe göre açıklama; kaydırılabilir alanlar ve sabit kaydet/vazgeç düğmeleri |
| Ortak kontroller | Sistemden gelen uyumsuz giriş, kutucuk ve kaydırıcı görünümü | Ortak köşe, boşluk, renk, odak, seçim ve devre dışı durumları; koyu/açık tema |

## Korunan davranışlar

- Sanallaştırılmış listeler ve görünür satırlarda logo yükleme korunur.
- M3U ve Xtream işleyişi, şifreli yerel kayıtlar ve kullanıcı verileri değişmez.
- F11/düğme ile tam ekran bütün monitörü kaplar. Kanal ve EPG panelleri gizlenir. Oynatma kontrolleri kullanılmadığında kaybolur.
- Kısayollar metin alanlarına yazarken kanal değiştirmez. Klavye odağı görsel olarak belirtilir.
- Yayın kaynakları kullanıcıya aittir; kurulum hesap, kanal listesi veya test videosu içermez.

## Doğrulama yaklaşımı

Sadece derleme başarısı yeterli değildir. Küçük pencere, koyu/açık tema, boş liste, aramada sonuç bulunamaması, kısa ekranda hesap formu, oynatma hatası ve tam ekran yerleşimi ayrı kontrol edilir. Testler sentetik yerel kaynaklarla gerçek LibVLC üzerinde çalışır. Farklı sağlayıcılar, Windows ekran ölçekleri ve monitör kombinasyonları ayrıca saha kontrolü gerektirir.
