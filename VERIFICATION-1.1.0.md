# enestv 1.1.0 doğrulama

Tarih: 30 Ekim 2026. Windows x64, .NET SDK 8.0.425.

- Release build ve bağımsız Windows x64 publish başarılı; derlemede 0 hata, 0 uyarı.
- xUnit: 23/23 başarılı. Sürüm 1 veritabanını sürüm 2'ye yükseltme, mevcut hesap/favori/geçmişi koruma, özel düzeni yenilemeden sonra koruma ve şifreli kalıcılık kontrol edildi.
- Gerçek WPF/LibVLC UI testi: mevcut arama, favori, EPG, oynatma, tema, tam ekran ve normal/büyütülmüş pencereye dönüş kontrolleri geçti.
- Yeni davranışlar için 31 UI kontrolü geçti: VLC üzerindeki çubuğun görünmesi, 3 saniyede gizlenmesi ve fareyle dönmesi; overlay üzerinde kısayol; önceki/sonraki/son kanal; sayısal seçimin Enter ve zamanlayıcıyla çalışması; yeni komutun bekleyen sayıyı iptal etmesi; gizli kategori/kanal, özel ad/sıra/numara, özelleştirilen kısayol ve arama alanında yazma koruması.
- Düzenleme penceresinin gerçek düğmeleriyle kanal taşıma, liste oluşturma/ad değiştirme/silme ve üye ekleme/çıkarma yürütüldü. Kaydet ve yeniden yükle kontrolleri geçti.
- 10.000 kanallı ana listede yalnızca 7 satır oluşturuldu. Düzenleme ekranında 10.000 öğe bulunmasına rağmen oluşturulan DataGridRow sayısı 60'ın altında kaldı.
- Koyu tema, 1120×730 ana pencere, kanal düzeni, kısayollar ve tam ekran overlay render görüntüleri incelendi. Liste adlarının görünümü, kısayol penceresinin teması ve dar pencerede sidebar taşması düzeltildi.

Testler sentetik yerel AVI, M3U ve XMLTV ile çalıştırıldı. Kullanıcının hesabı veya kişisel veritabanı değiştirilmedi. WPF test harness'i gerçek pencerelerde olay ve komutları yürütür; harici fiziksel fare/klavye testi yapılmış sayılmaz. Native VLC video yüzeyi WPF render çıktısında yer almaz.

## Kullanım sınırları

- Kanal düzeni aynı kanal kimlikleri için korunur; sağlayıcı kimliklerini değiştirdiğinde yeniden eşleştirme bu sürümün kapsamı değildir.
- Numaralar görünür kanalların düzeninden türetilir; filtre değiştirmek numaraları değiştirmez, kanal gizlemek/sıralamak değiştirir.
- Önceki/sonraki mevcut filtrede dolaşır. Son kanal tuşu oturum içindeki önceki seçime döner; kalıcı Recent listesi ayrıca son 20 başarılı oynatmayı saklar.
- Şema sürümü 2'ye yükselen veri dosyası eski uygulama sürümüyle açılamaz. Kurulumun üzerine yükseltme desteklenir.
