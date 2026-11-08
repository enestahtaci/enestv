# Third-party components

enestv: GPL-3.0-or-later. Copyright (c) 2026 enestv katkıcıları.
This license does not change the licenses of third-party components.
This inventory describes the current 1.3.0 payload. Plugins are retained in this version; the removal proposal has been deferred to v1.4.
Third-party components' own license and copyright notices are preserved; the source offer below accompanies this distribution.

## Runtime

| Component | Version | Actual license scope | Copyright / source |
| --- | --- | --- | --- |
| LibVLCSharp and LibVLCSharp.WPF | 3.10.1 | LGPL-2.1-or-later | VideoLAN and contributors identified in the relevant source files. https://code.videolan.org/videolan/LibVLCSharp |
| VideoLAN.LibVLC.Windows | 3.0.23.1 | NuGet label: LGPL-2.1-or-later; mixed payload | VideoLAN and contributors to the relevant source files. https://code.videolan.org/videolan/libvlc-nuget |
| libvlc.dll / libvlccore.dll | VLC 3.0.23 distribution | LGPL-2.1-or-later | Copyright and license notices are preserved in the VLC source files; covered by the source offer |
| libdolby_surround_decoder_plugin.dll, libheadphone_channel_mixer_plugin.dll, libx26410b_plugin.dll, liblua_plugin.dll | VLC 3.0.23 distribution | GPL-2.0-or-later | GPL and copyright notices are preserved in the relevant source files; covered by the source offer |
| FFmpeg within libavcodec_plugin.dll | Embedded version 4.4.5 | GPL-2.0-or-later enabled build | GPL-enabled FFmpeg; the 4.4.5 archive hash and signature were verified. The 14 contrib patches and build recipes are in the VLC archive; relevant modifications and static libraries are included in the source offer. https://ffmpeg.org/legal.html |
| Other VLC plugin DLLs | VLC 3.0.23 distribution | Licenses specified in the relevant source files; GPL/LGPL modules are included in the source offer | VideoLAN and relevant module contributors; copyright notices in source files are preserved |
| libbluray JAR files | 1.4.0 in filenames | Licenses of the relevant libbluray source files | Relevant libbluray contributors; native component and JAR sources are covered by the offer |
| dodeca_and_7channel_3DSL_HRTF.sofa | Package 3.0.23.1 | LGPL-2.1-or-later | Copyright (C) 2017 3D Sound Labs |
| VLC HTTP HTML and supporting resources | VLC 3.0.23 | HTML: GPL-2.0-or-later; other files under their own licenses | Example: Copyright (C) 2005-2014 VLC authors and VideoLAN; differing headers in individual files are also preserved |
| jsTree | 1.0-rc3 | MIT OR GPL-2.0; distributed under the MIT option | Copyright (c) 2010 Ivan Bozhanov (vakata.com) |
| jQuery UI CSS and theme resources | 1.8.13 | MIT OR GPL-2.0; distributed under the MIT option | Copyright 2011, AUTHORS.txt (http://jqueryui.com/about) |
| Microsoft.Data.Sqlite and Microsoft.Data.Sqlite.Core | 8.0.30 | MIT | Copyright Microsoft Corporation. All rights reserved. https://github.com/dotnet/efcore |
| SQLitePCLRaw.bundle_e_sqlite3, core, lib.e_sqlite3, provider.e_sqlite3 | 2.1.12 | Apache-2.0 | Copyright 2014-2024 SourceGear, LLC. https://github.com/ericsink/SQLitePCL.raw |
| e_sqlite3.dll / SQLite | Version string 3.53.3 in the binary | Public domain | https://www.sqlite.org/copyright.html  |
| System.Security.Cryptography.ProtectedData; System.Text.Encoding.CodePages | 8.0.0 | MIT | Copyright Microsoft Corporation. All rights reserved |
| System.Memory | 4.5.3 | MIT and package third-party notices | Copyright (c) .NET Foundation and Contributors. All rights reserved |
| Microsoft.NETCore.App; Microsoft.WindowsDesktop.App | 8.0.31 in the payload | MIT and applicable third-party terms | licenses/dotnet-LICENSE.txt and dotnet-THIRD-PARTY-NOTICES.txt |
| vcruntime140_cor3.dll | Distributed .NET 8.0.31 runtime component | Applicable Microsoft runtime distribution terms | Microsoft; relevant license/copyright notices from the .NET package are preserved |
| Segoe UI | Windows system font | Windows font terms | Microsoft; the font file is not distributed and is only referenced by name |
| Lucide icons (25 icon paths, converted to WPF geometry in EnesTV.App/Views/Icon.cs) | lucide-static 0.460.0 | ISC | Copyright (c) for portions of Lucide are held by Cole Bemis 2013-2022 as part of Feather (MIT). All other copyright (c) for Lucide are held by Lucide Contributors 2022. licenses/Lucide-ISC.txt; https://lucide.dev |

The relevant sources, modifications and static dependencies of native libraries included in the distribution are covered by the source offer below.
DLLs are distributed as separate files. The rights to use LGPL libraries with interface-compatible modified versions, obtain the relevant sources and debug these modifications must be preserved.

Additional runtime source scope: Game Music Emu for libgme_plugin.dll, Orc for libschroedinger_plugin.dll, libupnp for libupnp_plugin.dll and static pthread runtime sources are included in the source offer. Archive URLs and checksums are in SOURCE-ACCESS.md. FluidLite sources for the Windows MIDI module are also preserved.

## Development only

Test packages are kept separate from the runtime inventory; they are not assumed to be in the application payload.

| Package | Version | License / copyright |
| --- | --- | --- |
| Microsoft.NET.Test.Sdk, Microsoft.CodeCoverage, Microsoft.TestPlatform.ObjectModel, Microsoft.TestPlatform.TestHost | 17.11.1 | MIT; Microsoft Corporation |
| Newtonsoft.Json | 13.0.1 | MIT; Copyright James Newton-King 2008 |
| System.Reflection.Metadata | 1.6.0 | MIT; Microsoft Corporation and package third-party notices |
| xunit, xunit.assert, xunit.core, xunit.extensibility.core, xunit.extensibility.execution | 2.9.2 | Apache-2.0; Copyright (C) .NET Foundation |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0; Copyright (C) .NET Foundation |
| xunit.analyzers | 1.16.0 | Apache-2.0; Copyright (C) .NET Foundation |
| xunit.abstractions | 2.0.3 | Apache-2.0; Copyright (c) .NET Foundation and Contributors |

## Installer tool

Inno Setup 6 series installer tool.
Copyright (C) 1997-2026 Jordan Russell. All rights reserved.
Portions Copyright (C) 2000-2026 Martijn Laan. All rights reserved.
Custom Inno Setup license: licenses/Inno-Setup-LICENSE.txt and https://jrsoftware.org/files/is/license.txt .

## Source code

enestv source repository: https://github.com/enestahtaci/enestv
Releases and source attachments: https://github.com/enestahtaci/enestv/releases
Each Release includes the official VLC 3.0.23 and FFmpeg 4.4.5 source archives and their SHA-256 values. VLC contrib patches and build recipes for FFmpeg are in the VLC archive. Downloads, hashes and the source offer are in [SOURCE-ACCESS.md](SOURCE-ACCESS.md).

## Written source code offer

Under GPLv2 §3(b) and LGPLv2.1 §6(c), we offer to provide anyone who requests it with the complete corresponding machine-readable source code for the following components included in enestv distributions accompanied by this offer:

- VLC/LibVLC 3.0.23 and its accompanying GPL/LGPL modules, including the x264 encoder, Lua, Dolby surround and headphone mixer in the current package.
- FFmpeg 4.4.5 and the relevant third-party libraries statically linked into the distribution, including libgsm, OpenJPEG and LAME.
- Game Music Emu and Orc runtime code in the distributed VLC modules, and the relevant winpthreads runtime sources.
- The libbluray native component and the distributed libbluray 1.4.0 JAR files.
- Vendor modifications, patches and build/installation scripts used in these components; application source or object files necessary, where required under LGPLv2.1 §6(a), to relink with a modified library.

The offer is valid for at least three years from the date of each binary distribution it accompanies. To request sources, specify the version and component name at https://github.com/enestahtaci/enestv/issues or via enestahtaci03@gmail.com; do not send account passwords or stream addresses.
Sources are provided electronically free of charge or, on request, on a physical medium customarily used for software interchange, for a charge no greater than the cost of physically performing source distribution. The applicable GPL/LGPL licenses, copyright notices and redistribution rights are preserved. The enestv distributor is responsible for fulfilling the offer; there is no requirement to contact upstream.

## VideoLAN contrib source references

- Official source mirror: https://download.videolan.org/pub/contrib/ . FFmpeg 4.4.5 is also listed at https://download.videolan.org/pub/contrib/ffmpeg/ .
- Version-specific recipes and patches are under `contrib/src/` in the official VLC 3.0.23 archive; for FFmpeg, `contrib/src/ffmpeg/rules.mak`, `SHA512SUMS` and the patch files are used.
- https://download.videolan.org/pub/videolan/contrib/ also contains prebuilt contrib packages by platform; these are not assumed to substitute for source archives.

These addresses are references. The source offer is made by the enestv distributor; recipients are not merely directed to upstream links.
