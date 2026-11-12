# Ortak test verileri (fixtures)

Bu klasördeki dosyalar enestv çekirdeğinin **dilden bağımsız** test verileridir. Windows'taki C# çekirdeği
(`EnesTV.Tests/FixtureTests.cs`) bunları kullanır; Android (Kotlin) ve iOS (Swift) çekirdekleri de aynı girdilerden
aynı çıktıları üretmelidir. Kuralların açıklaması: [docs/CORE-SPEC.md](../docs/CORE-SPEC.md).

Dosyalar bayt bayt okunur (BOM, Windows-1254, LF). `.gitattributes` bu klasörde satır sonu dönüşümünü kapatır.

## Düzen

| Girdi | Beklenen çıktı | Nasıl çalıştırılır |
| --- | --- | --- |
| `m3u/*.m3u` | `m3u/*.expected.json` | `sourceId` ve `baseUri` beklenen dosyadadır. `windows-1254.m3u` dosya olarak açılır (UTF-8 hatasında 1254'e geri dönüş). |
| `m3u/urls.json` | aynı dosya (`absolute`, `relative` + `baseUri`; `expected`) | Mutlak adres normalleştirme (.NET `Uri.AbsoluteUri`, Windows). `null`: mutlak adres değil. Kanal anahtarı bu adresten üretilir. |
| `xtream/normalize-server.json` | aynı dosya (`valid` / `rejected`) | `rejected` girdileri kullanıcı hatası vermelidir. |
| `xtream/account.json` + `live-categories.json` + `live-streams.json` | `xtream/live.expected.json` | Kaynak kimliği `acc`. |
| `xtream/vod-categories.json` + `vod-streams.json` | `xtream/vod-catalog.expected.json` | Tür `Movie`, kaynak `acc`. |
| `xtream/series-details-*.json`, `movie-details.json` | `*.expected.json` (`input` ve `result`) | `input` öğesi ayrıntılarla birleştirilir. |
| `xtream/stream-urls.json` | `xtream/stream-urls.expected.json` | Sıra aynıdır. |
| `backup/sample.enestv` | `backup/sample.expected.json` | Parola `enestv-fixture-parola`; çözülen içerik (camelCase, sayım türleri metin) beklenenle aynı olmalı. Hızlı test için 10.000 tur. Windows çekirdeği yazdı. |
| `backup/sample-android.enestv` | `backup/sample.expected.json` | Aynı içerik ve parola; Android çekirdeği yazdı (`gradlew :core:test -PgenerateFixtures=true`). Her platform her iki dosyayı da açabilmeli. |
| `search/cases.json` | aynı dosya (`contains` / `equals`, `expected`) | Arama eşleşmesi; beklenen değerler kuralın kendisidir (CORE-SPEC: Arama). |
| `xmltv/times.json` | aynı dosya (`valid` / `invalid`) | `utc` alanı UTC zamanıdır; `invalid` girdileri boş (null) dönmelidir. |
| `xmltv/guide.xml` | `xmltv/guide.expected.json` | `{NOW}`, `{NOW+1H}`, `{NOW+2H}`, `{NOW-3D}`, `{NOW-2D}`, `{NOW+9D}`, `{NOW+9D+1H}` yer tutucuları test anındaki UTC zamana göre `yyyyMMddHHmmss +0000` biçiminde doldurulur. Aynı içerik gzip ile sıkıştırılmış olarak da okunmalıdır. |

## JSON alanları

- Kanal: `id`, `sourceId`, `name`, `url`, `tvgId`, `tvgName`, `logo`, `category`. `id`, kanal anahtarıdır (CORE-SPEC: Kimlikler).
- Film/dizi: `id`, `sourceId`, `kind` (`Movie` / `Series`), `name`, `category`, `poster`, `plot`, `year`, `rating`, `extension`, `key`.
- Bölüm: `id`, `season`, `number`, `name`, `extension`, `plot`, `poster`.
- Program: `channelId`, `title`, `description`.

Arayüz metinleri (ör. "Puan: 7.5", "S01 B01") çıktılara dahil değildir; bunlar platformun arayüz katmanına aittir.

## Beklenen çıktıları güncellemek

Beklenen çıktılar elle tahmin edilmez; çekirdeğin gerçek davranışından üretilip gözden geçirilir. Bir kural bilerek
değiştirildiğinde ilgili `*.expected.json` dosyası güncellenir ve değişiklik CORE-SPEC.md'ye yazılır. Böylece diğer
platformlar da aynı değişikliği uygular.
