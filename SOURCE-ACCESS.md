# Source code and written source offer

Source repository: https://github.com/enestahtaci/enestv
Release attachments: https://github.com/enestahtaci/enestv/releases
Each Release includes the following verified official source archives and their hashes. The enestv source archive for the same version is also provided.

| Archive | Official download URL | SHA-256 |
| --- | --- | --- |
| VLC 3.0.23 | https://download.videolan.org/pub/videolan/vlc/3.0.23/vlc-3.0.23.tar.xz | e891cae6aa3ccda69bf94173d5105cbc55c7a7d9b1d21b9b21666e69eff3e7e0 |
| FFmpeg 4.4.5 | https://ffmpeg.org/releases/ffmpeg-4.4.5.tar.xz | f9514e0d3515aee5a271283df71636e1d1ff7274b15853bcd84e144be416ab07 |

The VLC hash was compared with the official `.sha256` file. The FFmpeg archive's SHA-512 matched VLC's `contrib/src/ffmpeg/SHA512SUMS`, and its detached signature was verified.
FFmpeg signature file: https://ffmpeg.org/releases/ffmpeg-4.4.5.tar.xz.asc . Release key fingerprint: `FCF986EA15E6E293A5644F10B4322F04D67658D8`.
VLC checksum file: https://download.videolan.org/pub/videolan/vlc/3.0.23/vlc-3.0.23.tar.xz.sha256 .

The 14 patches and build recipe for FFmpeg 4.4.5 are in the `contrib/src/ffmpeg/` directory of the VLC 3.0.23 archive. The Windows x64 recipe uses `--enable-gpl --enable-postproc` when GPL is selected, and `--enable-static --disable-shared` for static linking. Other required sources and modifications are covered by the offer below.

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

## Release attachments and licenses

The VLC and FFmpeg archives; the FFmpeg detached signature; official checksum files; the enestv source archive; the source offer in this document and THIRD-PARTY-NOTICES.md; and SHA256SUMS.txt calculated from all attachments are published.
Sources are provided under their applicable third-party licenses; enestv's GPL-3.0-or-later license does not change third-party licenses. The rights to use LGPL libraries with interface-compatible modified versions and to perform the necessary debugging are preserved.

## Additional runtime source archives

The following sources are included for the current Windows x64 modules. Checksums for different compression formats or vendor archives are not interchangeable.

| Component | Official source | SHA-256 | Verification |
| --- | --- | --- | --- |
| gme | https://github.com/libgme/game-music-emu/archive/refs/tags/0.6.3.tar.gz | 4c5a7614acaea44e5cb1423817d2889deb82674ddbc4e3e1291614304b86fca0 | Matched VLC contrib SHA-512 |
| orc | https://gstreamer.freedesktop.org/src/orc/orc-0.4.33.tar.xz | 844e6d7db8086f793f57618d3d4b68d29d99b16034e71430df3c21cfd3c3542a | Matched upstream's published SHA-256 |
| upnp | https://github.com/pupnp/pupnp/archive/refs/tags/release-1.14.13.tar.gz | 8c099da99b3506bef683d46415bc3158260ca5528b4cec211d880eb5dea61176 | Matched VLC contrib SHA-512 |
| pthreads | https://github.com/mingw-w64/mingw-w64/archive/refs/tags/v10.0.0.tar.gz | b0f94a32e91b011e53abc2d855139cc3f3cef87a65c04830345b0203c7d453cb | Calculated from the download; not a previously verified hash |

The GME source is version 0.6.3 from the contrib recipe; Orc is 0.4.33 and UPnP is 1.14.13. Orc and UPnP versions match the payload strings. The winpthreads portion of the mingw-w64 v10.0.0 archive is retained for runtime sources; the entire archive is not excluded as merely an SDK.

This Windows x64 payload uses FluidLite 1.2.2 instead of FluidSynth 2.1.8; the FluidLite source is included in the archive collection. libVorbis 1.3.6 is used instead of Tremor. Daala and x262 are not added to this payload's source list. This distinction does not remove x264 or Lua from scope.
