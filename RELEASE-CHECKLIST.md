# GitHub Release kontrol listesi

- [ ] Katkıcı GPL-3.0-or-later onayları proje sahibi tarafından alındı.
- [ ] LICENSE, tam üçüncü taraf lisans/telif/NOTICE dosyaları hazır.
- [ ] SOURCE-ACCESS.md native kaynak/yama/build eşleştirmeleri kapandı; eksik kaynakla binary yayınlanmıyor.
- [ ] Planlanan depo https://github.com/enestahtaci/enestv ve Release https://github.com/enestahtaci/enestv/releases bağlantıları dokümanlarda tutarlı; yayın sırasında erişimleri ayrıca doğrulandı.
- [ ] Temiz deponun kökü belirlendi; .gitignore o kökte. work, verification, DB/log/cache ve hesaplar dışarıda.
- [ ] verification/build.txt kaldırıldıktan sonra enestv kaynak ZIP'i yeniden oluşturuldu; eski ZIP yayımlanmıyor.
- [ ] Yeni ZIP'in dosya listesinde verification/build.txt bulunmadığı doğrulandı ve ZIP/SHA256SUMS değerleri yeniden hesaplandı.
- [ ] Kişisel yol ve gerçek URL/token taraması tamam.
- [ ] Release hazırlığında birlikte ele alınacak tag v1.3.x biçiminde; x gerçek patch numarası. csproj Version ve .iss AppVersion aynı sürüm.
- [ ] Önceden paylaşılan binary aynı tag altında sessizce değiştirilmedi; yeni içerik için yeni patch sürümü değerlendirildi.
- [ ] .NET 8 destek sonu 10 Kasım 2026 açıklandı; .NET 10 ayrı iş.
- [ ] Ayrı yetkilendirilecek build/test aşamasında temiz staging, M3U/Xtream, gerçek kaynaklarla canlı TV, film, dizi, HLS/m3u8, altyazı, ses parçası, reconnect, EPG, fullscreen ve DLL değiştirme yolu doğrulandı.
- [ ] Bu 1.3.x sürümünde eklenti çıkarma yapılmaz; 10. adım v1.4 iç incelemesine ertelenmiştir. Eklentiler ve kaynak teklifi kapsamı korunur.
- [ ] Installer'da LICENSE, THIRD-PARTY-NOTICES.md, SOURCE-ACCESS.md ve licenses/ mevcut.
- [ ] Kaynak arşivi tag ile eşleşiyor; bin/obj ve kişisel çıktı içermiyor.
- [ ] Her Release ekinin gerçek byte'larından SHA-256 hesaplandı; upstream imzalar ayrıca doğrulandı.

## Yerel Git ve yayın sırası — şimdi çalıştırılmaz

1. Önce 1–9 adımları ve gerekli kaynak doğrulamaları tamamlanır; kullanıcı ayrıca onay verir.
2. Sonrasında yalnız proje kökünde `git init -b main` yapılır. GitHub deposu oluşturulmaz; push, gh oturumu veya hesap ayarı işlemi yapılmaz.
3. `git status` ve `git ls-files --others --exclude-standard` çıktılarıyla commit adayı dosyaların tam listesi kullanıcıya gösterilir. Gizli dosya içerikleri yazdırılmaz.
4. Listede DB/SQLite yan dosyaları, log, .env, work/, verification/, bin/, obj/, dist/, setup çıktıları ve kişisel yol içeren dosya bulunmamalı. .gitignore tek başına içerik taraması yerine geçmez. Eksik dışlamalar varsa önce diff ve onay gerekir.
5. Dosya listesi için kullanıcının AYRI onayından sonra yalnız gözden geçirilmiş dosyalar ilk commit'e alınır. Mesaj gerçek sürümle `enestv 1.3.x ilk açık kaynak sürüm` biçimindedir; x yer tutucusu literal kullanılmaz.
6. Tag Release hazırlığında ayrı ele alınır. GitHub'da yayınlamayı kullanıcı GitHub Desktop'tan yapar.

## Release ekleri

- enestv-Setup-<aynı-sürüm>-x64.exe
- enestv-source-<aynı-sürüm>.zip
- SOURCE-ACCESS.md'de doğrulanan VLC, FFmpeg, LibVLCSharp, libbluray ve kalan copyleft kaynak arşivleri
- Gerekli yamalar, kesin kaynak revizyonları ve binary build yapılandırmaları/reçeteleri
- Varsa doğrulanmış upstream imza/checksum dosyaları
- SHA256SUMS.txt: yukarıdaki tüm ekleri kapsar

Kaynak listesi native eşleştirme tamamlanana kadar nihai değildir.

## Release açıklaması şablonu

# enestv v<1.3.x>

Windows 11 x64 için ücretsiz açık kaynak IPTV oynatıcı.

[Kaynak kodu](https://github.com/enestahtaci/enestv) · [Tüm sürümler ve kaynak ekleri](https://github.com/enestahtaci/enestv/releases)

Değişiklikler: <bu sürümdeki doğrulanmış değişiklikler>

Kurulum: Setup EXE'yi indirin. Açık enestv'yi kapatıp kurun. .NET ve VLC bileşenleri pakettedir.

Lisans: GPL-3.0-or-later. Copyright (c) 2026 enestv katkıcıları.
Üçüncü taraf lisansları ayrı korunur. Kaynak arşivleri ve gerekli build malzemeleri bu Release'in eklerindedir; SOURCE-ACCESS.md eşleştirmeyi açıklar.

SHA-256: SHA256SUMS.txt ile indirdiğiniz dosyayı karşılaştırın.

İçerik: enestv yayın, playlist, film/dizi, EPG veya hesap sağlamaz. Kendi erişim hakkınız olan kaynakları ekleyin. Yayıncılar veya VideoLAN ile bağlantılı değildir.

HTTP: HTTP kaynağında hesap ve yayın istekleri şifrelenmeden iletilebilir; HTTPS tercih edin.

Bilinen sınırlamalar: <doğrulanmış sınırlamalar>. Bu sürüm .NET 8 kullanır; destek sonu 10 Kasım 2026. .NET 10 ayrı iştir.

Doğrulama: <gerçekte yapılan testler ve kapsam; yapılmayanlar geçti diye yazılmaz>.
