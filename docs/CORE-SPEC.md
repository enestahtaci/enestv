# enestv çekirdek davranış belgesi

Bu belge enestv çekirdeğinin (kaynak ekleme, ayrıştırma, EPG, kayıt) **platformdan bağımsız** kurallarını anlatır.
Windows'taki C# çekirdeği (`EnesTV.Core`) bu kuralların referans uygulamasıdır; Android ve iOS çekirdekleri aynı
kuralları uygular. Kuralların ölçülebilir hâli `fixtures/` klasöründedir ([fixtures/README.md](../fixtures/README.md)):
belge ile fixture çelişirse fixture geçerlidir, belge düzeltilir.

Bir kural bilerek değiştirildiğinde sırasıyla: kod → `*.expected.json` → bu belge → diğer platformlar.

## 1. Kimlikler

| Ne | Biçim | Kod |
| --- | --- | --- |
| Kanal anahtarı | `SHA-256(UTF-8(sourceId + "\n" + adres + "\n" + ad))`, büyük harf onaltılık (64 karakter) | `Channel.Key` |
| — M3U kanalı | `adres` = kanalın mutlak URL'si | `M3uParser` |
| — Xtream kanalı | `adres` = **`stream_id`** (URL değil; şifre değişse de anahtar korunur) | `XtreamService.ParseLiveStreams` |
| Film/dizi anahtarı | `{sourceId}:{Movie\|Series}:{id}` | `VodItem.Key` |
| Bölüm anahtarı | `{sourceId}:Series:{diziId}:Episode:{bölümId}` | `XtreamService.EpisodeKey` |
| EPG kaynağı | `SHA-256(UTF-8(url))`, büyük harf onaltılık | `EpgService` |

Favoriler, son izlenenler, kanal düzeni ve izleme konumları bu anahtarlara bağlıdır; anahtar biçimi değişirse
kullanıcı verisi kopar.

## 2. M3U

Fixture'lar: `m3u/metadata-commas-duplicates`, `m3u/malformed-extinf`, `m3u/features`, `m3u/windows-1254`.

**Satırlar.** Her satırın başındaki/sonundaki boşluk ve BOM (`U+FEFF`) atılır; boş satırlar atlanır.

**`#EXTINF:` satırı** (büyük/küçük harf duyarsız):
- Ad, tırnak dışındaki **ilk virgülden** sonraki metindir. Tırnak takibi `"` ve `'` için yapılır; böylece
  `group-title="News, Live"` içindeki virgül adı bölmez.
- Öznitelikler virgülden önceki kısımdan okunur (virgül yoksa satırın tamamından): `ad=değer`, değer
  `"..."`, `'...'` ya da boşluk/virgül içermeyen düz metin olabilir. Öznitelik adları büyük/küçük harf duyarsızdır.
- Okunan öznitelikler: `tvg-id`, `tvg-name`, `tvg-logo`, `group-title`.

**`#EXTGRP:` satırı** kategoriyi (`group-title`) belirler. Diğer `#` satırları yok sayılır.

**Adres satırı.** Mutlak URL ya da (taban adres varsa) ona göre göreli URL. İzin verilen şemalar:
`http`, `https`, `rtsp`, `rtmp`, `udp`, `rtp`, `file`. Başka şema veya geçersiz adres kanalı oluşturmaz.
Her adres satırından sonra (kanal eklensin ya da eklenmesin) ad ve öznitelikler sıfırlanır.

**Kanal alanları:**
- Ad: `#EXTINF` adı → yoksa `tvg-name` → yoksa `İsimsiz kanal`.
- Kategori: `group-title`; boş veya yoksa `Uncategorized`.
- Logo: `tvg-logo`; taban adres varsa ona göre mutlak hâle getirilir.
- Aynı anahtarlı ikinci kanal atılır (ilk kalır).

**Taban adres.** URL kaynağında listenin adresi; dosya kaynağında dosyanın tam yolu (`file://`).

**Karakter kodlaması** (dosyadan okuma): önce katı UTF-8 (BOM varsa ona uyulur); geçersiz UTF-8 baytında
dosya baştan **Windows-1254** (Türkçe) olarak okunur.

**Sıfır kanal.** M3U kaynağında hata (`Kaynakta oynatılabilir kanal bulunamadı`). Xtream için bkz. 3.3.

## 3. Xtream Codes

### 3.1 Sunucu adresi (`normalize-server.json`)
- Şema yoksa `https://` eklenir. Yalnızca `http` ve `https` kabul edilir.
- Kullanıcı bilgisi (`user:pass@`), sorgu (`?…`) veya parça (`#…`) varsa kullanıcı hatası.
- Sondaki `/` ve sondaki `/player_api.php` atılır; port ve alt yol korunur.

### 3.2 API ve kimlik doğrulama
- API: `{sunucu}/player_api.php?username={u}&password={p}[&action={eylem}]`; kullanıcı adı ve parola URL kaçışlıdır.
- Giriş: yanıtta `user_info.auth == "1"` olmalı ve `user_info.status` boş ya da `Active` (harf duyarsız) olmalı.
- Herhangi bir yanıtta `user_info.auth == "0"` veya HTTP 401/403 → kimlik doğrulama hatası (ağ hatasından ayrı mesaj).
- JSON değerleri metin olarak okunur: sayı `12` ve metin `"12"` aynıdır; `null` ve eksik alan boş metindir.

### 3.3 Canlı kanallar (`live.expected.json`)
- Eylemler: `get_live_categories`, `get_live_streams`. Akış listesi dizi değilse kullanıcı hatası.
- `stream_id` boşsa öğe atlanır. Ad boşsa `Kanal {id}`.
- Adres: `{sunucu}/live/{u}/{p}/{id}.ts` (her parça URL kaçışlı).
- `tvgId` = `epg_channel_id`, `tvgName` = ad, logo = `stream_icon`, kategori = `category_id` eşleşmesi, yoksa `Uncategorized`.
- Aynı anahtarlı ikinci kanal atılır.
- Yenilemede liste boş gelirse kayıtlı canlı kanallar korunur (yalnızca film/dizi sunan hesaplar ve geçici boş yanıtlar).
- EPG adresi: `{sunucu}/xmltv.php?username={u}&password={p}`.

### 3.4 Film/dizi kataloğu (`vod-catalog.expected.json`)
- Filmler: `get_vod_categories` + `get_vod_streams`, kimlik `stream_id`. Diziler: `get_series_categories` + `get_series`, kimlik `series_id`.
- Kimliği boş veya tekrar eden öğe atlanır (ilk kalır).
- Ad → `İsimsiz içerik`; kategori eşleşmezse `Diğer`; afiş `cover` → `stream_icon`; yıl `year` → `releaseDate` → `release_date`.

### 3.5 Ayrıntılar (`series-details-*`, `movie-details`)
- Eylem: `get_vod_info&vod_id={id}` veya `get_series_info&series_id={id}`; yanıt nesne değilse kullanıcı hatası.
- Katalog öğesinin üzerine yazılır (boş değerler eskisini korur): özet `info.plot` → `info.description`;
  afiş `info.cover` → `info.movie_image`; yıl `info.releasedate` → `releaseDate` → `release_date`; puan `info.rating`;
  film uzantısı `movie_data.container_extension`.
- **Bölümler** üç biçimde gelebilir:
  1. Nesne: `{"1": [...], "2": [...]}` — anahtar sezon numarasıdır.
  2. Düz dizi: `[ {...}, ... ]` — sezon öğenin `season` alanından, yoksa 1.
  3. İç içe dizi: `[[...], [...]]` — sezon dizinin sırasıdır (1'den başlar).
- Bölüm: kimlik `id` (boş veya tekrar edense atlanır); sezon `season` alanı varsa o; numara `episode_num`, yoksa
  o ana kadar eklenen bölüm sayısı + 1; ad `title` → `name` → `Bölüm`; özet ve görsel `info.plot`/`info.description`
  ve `info.movie_image` → dizinin afişi.
- Sıralama: sezon, sonra bölüm numarası.

### 3.6 Oynatma adresi ve uzantı (`stream-urls`)
- `{sunucu}/{movie|series}/{u}/{p}/{id}.{uzantı}` (bölümler `series`).
- Uzantı yalnızca 1–8 ASCII harf/rakamsa kullanılır ve küçük harfe çevrilir; aksi hâlde `mp4` (ör. `../bad` → `mp4`).

## 4. XMLTV (EPG)

Fixture'lar: `xmltv/times.json`, `xmltv/guide.xml`.

**Zaman.** `yyyyMMdd[HH[mm[ss]]]` (8, 10, 12 veya 14 hane; eksik kısımlar 0) ve isteğe bağlı bölge:
`+HHMM`/`-HHMM` (saat ≤ 14, 14 ise dakika 00), `UTC`, `GMT`, `Z` veya yok (UTC). Başka her şey geçersizdir.

**Dosya.** Gzip (ilk iki bayt `1F 8B`) otomatik açılır. DTD yok sayılır, harici kaynak çözülmez, en fazla 1 milyar karakter.

**Programlar.** `programme` öğeleri akış hâlinde okunur. Atlananlar: kanal kimliği listede yoksa, başlangıç/bitiş
geçersizse, bitiş ≤ başlangıçsa, bitiş **şimdi − 1 gün**den önceyse, başlangıç **şimdi + 8 gün**den sonraysa.
Başlık yoksa `İsimsiz program`, açıklama yoksa boş. Eşleştirme kanalın `tvgId` değeriyle yapılır.

## 4a. Arama (`search/cases.json`)

- Bir metin, sorgunun baştaki/sondaki boşlukları atılmış hâlini **alt metin** olarak içeriyorsa eşleşir; boş sorgu
  her şeyle eşleşir.
- Büyük/küçük harf duyarsızdır ve **Türkçe i harflerini tek harf sayar**: `I`, `ı`, `İ`, `i` birbirinin yerine geçer.
  Diğer karakterler kültürden bağımsız küçük harfleriyle karşılaştırılır (`Ç/ç`, `Ş/ş`, `Ğ/ğ`, `Ö/ö`, `Ü/ü` eşleşir).
  Böylece hem `şimdi` → `ŞİMDİ TV` hem `bein` → `BEIN SPORTS` bulunur. (Kültüre göre küçültme kullanılmaz; tr-TR
  `BEIN`'i `beın` yapar ve İngilizce adları bozar.)
- Aksanlar kaldırılmaz: `cocuk` ≠ `çocuk`, `seker` ≠ `şeker`.
- Tam eşitlik (ör. favori listesi adının benzersizliği) aynı harf eşlemesiyle yapılır.
- Kod: `TextSearch.Contains` / `TextSearch.Equals`. Kanal listesi, film/dizi kataloğu, TV rehberi ve Kanal düzeni
  aramaları bunu kullanır.

## 5. Ağ

- Yalnızca `http`/`https`. En fazla 8 yönlendirme, sıkıştırma (gzip/deflate/brotli) açık, bağlantı zaman aşımı 12 sn,
  istek başına toplam 2 dk. User-Agent `enestv/1.0`.
- Yeniden deneme: ağ hatası, HTTP 5xx, 408 veya 429'da en fazla 3 deneme (0,5 sn ve 1 sn bekleme).
- Boyut sınırı: Xtream API yanıtı 128 MB, M3U 256 MB, XMLTV 512 MB. Aşılırsa kullanıcı hatası.
- Yanıtlar önce geçici dosyaya indirilir, sonra ayrıştırılır (büyük listelerde bellek koruması).

## 6. Kayıt

- SQLite, şema sürümü `PRAGMA user_version` (şu an 3). Sürüm 1 → 2 kanal düzenini, 2 → 3 film/dizi kataloğunu ve
  izleme konumlarını ekler; yükseltme veriyi korur.
- **Şifreli** saklananlar (platformun `ISecretProtector`'ı ile): kaynak bilgileri (adres, kullanıcı adı, parola),
  kanal listesi, ayarlar, kanal düzeni, film/dizi kataloğu, izleme konumları. EPG düz metindir.
- Son izlenenler en fazla 20 kanal (en yeni önce). Kaynak silinince kanalları, favorileri, son izlenenleri,
  kataloğu ve izleme konumları da silinir.
- Varsayılan ayarlar: tema `Dark`, ses 75, otomatik yeniden bağlanma açık, EPG adresi boş.
- Şifreleme anahtarları cihaza bağlıdır (Windows: DPAPI). Veri bir cihazdan diğerine veritabanı kopyalanarak
  taşınamaz; bunun için yedekle/geri yükle biçimi kullanılır (Faz 0.4).

## 7. İzleme konumu

- Devam noktası: içerik tamamlandıysa veya konum 5 saniyeden azsa 0, değilse kayıtlı konum (`WatchProgress.ResumeMs`).
- Uygulama konumu yaklaşık 5 saniyede bir ve durdurma, içerik değiştirme, kapatma sırasında kaydeder; içerik doğal
  olarak bitince "tamamlandı" işaretlenir ve yeniden bağlanma denenmez.

## 8. Şu an uygulama katmanında olan kurallar

Bunlar Windows uygulamasında (`EnesTV.App`) uygulanıyor; mobil/TV çekirdeğine taşınırken aynı davranış korunmalı:

- **Kanal numarası:** Gizli olmayan ve kategorisi gizlenmemiş kanallar, kanal düzenindeki sıraya göre 1'den başlayarak numaralanır.
- **Numarayla geçiş:** En fazla 6 hane; son tuştan 1,2 sn sonra veya Enter ile geçilir; Geri (Backspace) son haneyi siler,
  Esc iptal eder; pencere odağını kaybedince de iptal edilir.
- **Önceki/sonraki kanal:** Görünen listede döngüsel. **Son kanal:** bir önceki farklı kanal.
- **Arama:** Kanal adında (özel ad dahil) veya kategorisinde, 4a'daki kurala göre. Kanal düzeninde orijinal ad da aranır.
- **Aynı anda tek yayın:** Canlı TV ile film/dizi arasında geçerken diğeri durdurulur (sağlayıcı bağlantı sınırı).

## 9. Bilinen tuhaflıklar ve hatalar

Fixture'lar şu anki davranışı kaydeder. Düzeltilirse ilgili fixture ve bu bölüm güncellenir.

1. ~~Türkçe büyük/küçük harf araması~~ — **düzeltildi.** Önceden arama `OrdinalIgnoreCase` kullanıyordu ve
   `"şimdi"` `"ŞİMDİ TV"`yi bulamıyordu. Kural artık 4a'da; diğer platformlar doğrudan yeni kuralı uygular.
2. **Kapanmamış tırnak (tuhaflık).** `#EXTINF:garbage tvg-name="Broken` satırında ad bulunamaz ve `tvg-name` değeri
   tırnakla birlikte okunur: kanal adı `"Broken` olur (`malformed-extinf`). Sonraki kanallar etkilenmez.
3. **İç içe dizi biçiminde bölüm numarası (tuhaflık).** `episode_num` yoksa numara sezon içindeki sıra değil,
   dizinin o ana kadarki toplam bölüm sayısı + 1 olur; ikinci sezonun ilk bölümü "B02" görünür (`series-details-nested`).
