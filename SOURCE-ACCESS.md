# Kaynak kodu ve yazılı kaynak teklifi

Kaynak deposu: https://github.com/enestahtaci/enestv
Release ekleri: https://github.com/enestahtaci/enestv/releases
Her Release'e aşağıdaki doğrulanmış resmi kaynak arşivleri ve bu dosyaların hash'leri eklenir. enestv'nin aynı sürüm kaynak arşivi de birlikte sunulur.

| Arşiv | Resmi indirme adresi | SHA-256 |
| --- | --- | --- |
| VLC 3.0.23 | https://download.videolan.org/pub/videolan/vlc/3.0.23/vlc-3.0.23.tar.xz | e891cae6aa3ccda69bf94173d5105cbc55c7a7d9b1d21b9b21666e69eff3e7e0 |
| FFmpeg 4.4.5 | https://ffmpeg.org/releases/ffmpeg-4.4.5.tar.xz | f9514e0d3515aee5a271283df71636e1d1ff7274b15853bcd84e144be416ab07 |

VLC hash'i resmi `.sha256` dosyasıyla karşılaştırıldı. FFmpeg arşivinin SHA-512 değeri VLC `contrib/src/ffmpeg/SHA512SUMS` değeriyle eşleşti ve ayrık imzası doğrulandı.
FFmpeg imza dosyası: https://ffmpeg.org/releases/ffmpeg-4.4.5.tar.xz.asc . Yayın anahtarı fingerprint'i: `FCF986EA15E6E293A5644F10B4322F04D67658D8`.
VLC checksum dosyası: https://download.videolan.org/pub/videolan/vlc/3.0.23/vlc-3.0.23.tar.xz.sha256 .

FFmpeg 4.4.5 için 14 yama ve build tarifi VLC 3.0.23 arşivinin `contrib/src/ffmpeg/` dizinindedir. Windows x64 tarifinde GPL seçimiyle `--enable-gpl --enable-postproc`; statik bağlantı için `--enable-static --disable-shared` kullanılır. Gerekli diğer kaynak ve değişiklikler aşağıdaki teklif kapsamındadır.

## Yazılı kaynak kodu teklifi

GPLv2 §3(b) ve LGPLv2.1 §6(c) kapsamında, bu teklifin eşlik ettiği enestv dağıtımlarında yer alan aşağıdaki bileşenlerin tam karşılık gelen, makine tarafından okunabilir kaynak kodunu talep eden herkese sağlamayı teklif ediyoruz:

- VLC/LibVLC 3.0.23 ve beraberindeki GPL/LGPL modülleri; mevcut paketteki x264 encoder, Lua, Dolby surround ve headphone mixer dahil.
- FFmpeg 4.4.5 ve dağıtıma statik bağlanan ilgili üçüncü taraf kütüphaneleri; libgsm, OpenJPEG ve LAME dahil.
- Dağıtılan VLC modüllerindeki Game Music Emu ve Orc çalışma zamanı kodu ile ilgili winpthreads çalışma zamanı kaynakları.
- libbluray native bileşeni ve dağıtılan libbluray 1.4.0 JAR dosyaları.
- Bu bileşenlerde kullanılan vendor değişiklikleri, yamalar ve derleme/kurulum betikleri; LGPLv2.1 §6(a) kapsamında gerektiğinde değiştirilmiş kütüphaneyle yeniden bağlantı kurmak için gerekli uygulama kaynak veya nesne dosyaları.

Teklif, eşlik ettiği her binary dağıtım tarihinden itibaren en az üç yıl geçerlidir. Talep için https://github.com/enestahtaci/enestv/issues adresinden veya enestahtaci03@gmail.com üzerinden sürüm ve bileşen adını belirtin; hesap parolası veya yayın adresi göndermeyin.
Kaynaklar elektronik olarak ücretsiz, istenirse yazılım değişiminde yaygın kullanılan fiziksel bir ortamda yalnızca kaynak dağıtımını fiilen gerçekleştirme maliyetini aşmayan bedelle sağlanır. İlgili GPL/LGPL lisansları, telif bildirimleri ve yeniden dağıtım hakları korunur. Teklifin yerine getirilmesinden enestv dağıtıcısı sorumludur; upstream'e başvurma zorunluluğu yoktur.

## VideoLAN contrib kaynak referansları

- Resmi kaynak aynası: https://download.videolan.org/pub/contrib/ . FFmpeg 4.4.5 ayrıca https://download.videolan.org/pub/contrib/ffmpeg/ dizininde listelenir.
- Sürüme ait tarif ve yamalar, resmi VLC 3.0.23 arşivindeki `contrib/src/` altında bulunur; FFmpeg için `contrib/src/ffmpeg/rules.mak`, `SHA512SUMS` ve yama dosyaları kullanılır.
- https://download.videolan.org/pub/videolan/contrib/ dizini platforma göre hazır contrib paketleri de içerir; kaynak arşivi yerine kullanıldığı varsayılmaz.

Bu adresler referanstır. Kaynak teklifinin muhatabı enestv dağıtıcısıdır; alıcı yalnız upstream bağlantılarına yönlendirilmez.

## Release ekleri ve lisanslar

VLC ve FFmpeg arşivleri; FFmpeg ayrık imzası; resmi checksum dosyaları; enestv kaynak arşivi; bu belgedeki kaynak teklifi ve THIRD-PARTY-NOTICES.md; bütün eklerden hesaplanan SHA256SUMS.txt yayımlanır.
Kaynaklar ilgili üçüncü taraf lisansları altında sunulur; enestv'nin GPL-3.0-or-later lisansı üçüncü taraf lisanslarını değiştirmez. LGPL kütüphaneleri arayüz uyumlu değiştirilmiş sürümlerle kullanma ve gerekli hata ayıklama hakları korunur.

## Ek runtime kaynak arşivleri

Aşağıdaki kaynaklar mevcut Windows x64 modülleri için eklenir. Farklı sıkıştırma veya sağlayıcı arşivlerinin checksum'ları birbirinin yerine kullanılmaz.

| Bileşen | Resmi kaynak | SHA-256 | Kontrol |
| --- | --- | --- | --- |
| gme | https://github.com/libgme/game-music-emu/archive/refs/tags/0.6.3.tar.gz | 4c5a7614acaea44e5cb1423817d2889deb82674ddbc4e3e1291614304b86fca0 | VLC contrib SHA-512 eşleşti |
| orc | https://gstreamer.freedesktop.org/src/orc/orc-0.4.33.tar.xz | 844e6d7db8086f793f57618d3d4b68d29d99b16034e71430df3c21cfd3c3542a | Upstream yayımlanmış SHA-256 eşleşti |
| upnp | https://github.com/pupnp/pupnp/archive/refs/tags/release-1.14.13.tar.gz | 8c099da99b3506bef683d46415bc3158260ca5528b4cec211d880eb5dea61176 | VLC contrib SHA-512 eşleşti |
| pthreads | https://github.com/mingw-w64/mingw-w64/archive/refs/tags/v10.0.0.tar.gz | b0f94a32e91b011e53abc2d855139cc3f3cef87a65c04830345b0203c7d453cb | İndirilenden hesaplandı; önceden doğrulanmış hash değil |

GME kaynağı contrib tarifindeki 0.6.3, Orc 0.4.33 ve UPnP 1.14.13 sürümleridir. Orc ve UPnP sürümleri payload dizgeleriyle eşleşir. mingw-w64 v10.0.0 arşivinin winpthreads bölümü runtime kaynakları için saklanır; bütün arşiv yalnız SDK sayılarak dışlanmaz.

Bu Windows x64 payload'ında FluidSynth 2.1.8 yerine FluidLite 1.2.2 kullanılır; FluidLite kaynağı arşive dahildir. Tremor yerine libVorbis 1.3.6 kullanılır. Daala ve x262 bu payload'ın kaynak listesine eklenmez. Bu ayrım x264 veya Lua'yı kapsamdan çıkarmaz.
