# Üçüncü taraf bileşenleri

enestv: GPL-3.0-or-later. Copyright (c) 2026 enestv katkıcıları.
Bu lisans üçüncü taraf bileşenlerin lisanslarını değiştirmez.
Bu envanter mevcut 1.3.0 payload'ını açıklar. Eklentiler bu sürümde korunur; çıkarma önerisi v1.4'e ertelenmiştir.
Üçüncü taraf bileşenlerin kendi lisans ve telif bildirimleri korunur; aşağıdaki kaynak teklifi bu dağıtımla birlikte sunulur.

## Çalışma zamanı

| Bileşen | Sürüm | Gerçek lisans kapsamı | Telif / kaynak |
| --- | --- | --- | --- |
| LibVLCSharp ve LibVLCSharp.WPF | 3.10.1 | LGPL-2.1-or-later | VideoLAN ve ilgili kaynak dosyalarında belirtilen katkıcılar. https://code.videolan.org/videolan/LibVLCSharp |
| VideoLAN.LibVLC.Windows | 3.0.23.1 | NuGet etiketi LGPL-2.1-or-later; payload karma | VideoLAN ve ilgili kaynak dosyalarının katkıcıları. https://code.videolan.org/videolan/libvlc-nuget |
| libvlc.dll / libvlccore.dll | VLC 3.0.23 dağıtımı | LGPL-2.1-or-later | Telif ve lisans bildirimleri VLC kaynak dosyalarında korunur; kaynak teklifi kapsamındadır |
| libdolby_surround_decoder_plugin.dll, libheadphone_channel_mixer_plugin.dll, libx26410b_plugin.dll, liblua_plugin.dll | VLC 3.0.23 dağıtımı | GPL-2.0-or-later | GPL ve telif bildirimleri ilgili kaynak dosyalarında korunur; kaynak teklifi kapsamındadır |
| libavcodec_plugin.dll içindeki FFmpeg | Gömülü sürüm 4.4.5 | GPL-2.0-or-later etkin derleme | GPL etkin FFmpeg; 4.4.5 arşiv hash'i ve imzası doğrulandı. 14 contrib yaması ve build tarifleri VLC arşivinde; ilgili değişiklikler ve statik kütüphaneler kaynak teklifine dahildir. https://ffmpeg.org/legal.html |
| Diğer VLC plugin DLL'leri | VLC 3.0.23 dağıtımı | İlgili kaynak dosyalarında belirtilen lisanslar; GPL/LGPL modülleri kaynak teklifine dahildir | VideoLAN ve ilgili modül katkıcıları; kaynak dosyalarındaki telif bildirimleri korunur |
| libbluray JAR dosyaları | Dosya adlarında 1.4.0 | İlgili libbluray kaynak dosyalarının lisansları | İlgili libbluray katkıcıları; native bileşen ve JAR kaynakları teklif kapsamındadır |
| dodeca_and_7channel_3DSL_HRTF.sofa | Paket 3.0.23.1 | LGPL-2.1-or-later | Copyright (C) 2017 3D Sound Labs |
| VLC HTTP HTML ve yardımcı kaynakları | VLC 3.0.23 | HTML GPL-2.0-or-later; diğer dosyalar kendi lisanslarıyla | Örnek: Copyright (C) 2005-2014 VLC authors and VideoLAN; dosyalardaki farklı başlıklar ayrıca korunur |
| jsTree | 1.0-rc3 | MIT OR GPL-2.0; MIT seçeneğiyle dağıtılır | Copyright (c) 2010 Ivan Bozhanov (vakata.com) |
| jQuery UI CSS ve tema kaynakları | 1.8.13 | MIT OR GPL-2.0; MIT seçeneğiyle dağıtılır | Copyright 2011, AUTHORS.txt (http://jqueryui.com/about) |
| Microsoft.Data.Sqlite ve Microsoft.Data.Sqlite.Core | 8.0.30 | MIT | Copyright Microsoft Corporation. All rights reserved. https://github.com/dotnet/efcore |
| SQLitePCLRaw.bundle_e_sqlite3, core, lib.e_sqlite3, provider.e_sqlite3 | 2.1.12 | Apache-2.0 | Copyright 2014-2024 SourceGear, LLC. https://github.com/ericsink/SQLitePCL.raw |
| e_sqlite3.dll / SQLite | Binary'de 3.53.3 sürüm dizgesi | Public domain | https://www.sqlite.org/copyright.html  |
| System.Security.Cryptography.ProtectedData; System.Text.Encoding.CodePages | 8.0.0 | MIT | Copyright Microsoft Corporation. All rights reserved |
| System.Memory | 4.5.3 | MIT ve paket third-party bildirimleri | Copyright (c) .NET Foundation and Contributors. All rights reserved |
| Microsoft.NETCore.App; Microsoft.WindowsDesktop.App | Payload'da 8.0.31 | MIT ve ilgili üçüncü taraf koşulları | licenses/dotnet-LICENSE.txt ve dotnet-THIRD-PARTY-NOTICES.txt |
| vcruntime140_cor3.dll | Dağıtılan .NET 8.0.31 runtime bileşeni | İlgili Microsoft runtime dağıtım koşulları | Microsoft; .NET paketinin ilgili lisans/telif bildirimleri korunur |
| Segoe UI | Windows sistem fontu | Windows font koşulları | Microsoft; font dosyası dağıtılmaz, yalnız adıyla kullanılır |

Dağıtıma dahil native kütüphanelerin ilgili kaynakları, değişiklikleri ve statik bağımlılıkları aşağıdaki kaynak teklifinin kapsamındadır.
DLL'ler ayrı dosyalar olarak dağıtılır. LGPL kütüphaneleri arayüz uyumlu değiştirilmiş sürümlerle kullanabilme, ilgili kaynakları edinme ve bu değişiklikleri hata ayıklama hakları korunmalıdır. 

Ek runtime kaynak kapsamı: libgme_plugin.dll için Game Music Emu, libschroedinger_plugin.dll için Orc, libupnp_plugin.dll için libupnp ve statik pthread çalışma zamanı kaynakları kaynak teklifine dahildir. Arşiv adresleri ve checksum'lar SOURCE-ACCESS.md içindedir. Windows MIDI modülünün FluidLite kaynakları da korunur.

## Yalnızca geliştirme

Test paketleri runtime envanteriyle karıştırılmaz; uygulama payload'ında oldukları varsayılmaz.

| Paket | Sürüm | Lisans / telif |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage, Microsoft.TestPlatform.ObjectModel, Microsoft.TestPlatform.TestHost | 17.11.1 | MIT; Microsoft Corporation |
| Newtonsoft.Json | 13.0.1 | MIT; Copyright James Newton-King 2008 |
| System.Reflection.Metadata | 1.6.0 | MIT; Microsoft Corporation ve paket third-party bildirimleri |
| xunit, xunit.assert, xunit.core, xunit.extensibility.core, xunit.extensibility.execution | 2.9.2 | Apache-2.0; Copyright (C) .NET Foundation |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0; Copyright (C) .NET Foundation |
| xunit.analyzers | 1.16.0 | Apache-2.0; Copyright (C) .NET Foundation |
| xunit.abstractions | 2.0.3 | Apache-2.0; Copyright (c) .NET Foundation and Contributors |

## Kurulum aracı

Inno Setup 6 serisi kurulum aracı.
Copyright (C) 1997-2026 Jordan Russell. All rights reserved.
Portions Copyright (C) 2000-2026 Martijn Laan. All rights reserved.
Özel Inno Setup lisansı: licenses/Inno-Setup-LICENSE.txt ve https://jrsoftware.org/files/is/license.txt .

## Kaynak kodu

enestv kaynak deposu: https://github.com/enestahtaci/enestv
Sürümler ve kaynak ekleri: https://github.com/enestahtaci/enestv/releases
Her Release'e VLC 3.0.23 ve FFmpeg 4.4.5 resmi kaynak arşivleri ve SHA-256 değerleri eklenir. FFmpeg için VLC contrib yamaları ve build tarifleri VLC arşivindedir. İndirilebilir dosyalar, hash'ler ve kaynak teklifi [SOURCE-ACCESS.md](SOURCE-ACCESS.md) içindedir.

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
