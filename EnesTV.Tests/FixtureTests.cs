using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Xunit;

namespace EnesTV.Tests;

// Shared, language-neutral fixtures (repo-root fixtures/, see fixtures/README.md and docs/CORE-SPEC.md).
// Every platform's core must turn the same inputs into the same *.expected.json outputs.
public sealed class FixtureTests
{
    private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "fixtures");
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static string P(string rel) => Path.Combine(Root, rel);
    private static JsonElement Doc(string rel) => JsonDocument.Parse(File.ReadAllText(P(rel))).RootElement;
    private static JsonNode Expected(string rel) => JsonNode.Parse(File.ReadAllText(P(rel)))!;
    private static void Same(JsonNode? expected, object actual)
    {
        var node = JsonSerializer.SerializeToNode(actual, Json);
        Assert.True(JsonNode.DeepEquals(expected, node), $"Expected:\n{expected?.ToJsonString(Json)}\nActual:\n{node?.ToJsonString(Json)}");
    }
    private static object Ch(Channel c) => new { c.Id, c.SourceId, c.Name, c.Url, c.TvgId, c.TvgName, c.Logo, c.Category };
    private static object Vi(VodItem v) => new { v.Id, v.SourceId, Kind = v.Kind.ToString(), v.Name, v.Category, v.Poster, v.Plot, v.Year, v.Rating, v.Extension, v.Key };
    private static object Ep(VodEpisode e) => new { e.Id, e.Season, e.Number, e.Name, e.Extension, e.Plot, e.Poster };

    [Theory]
    [InlineData("m3u/metadata-commas-duplicates")]
    [InlineData("m3u/malformed-extinf")]
    [InlineData("m3u/features")]
    public void M3uText(string name)
    {
        var expected = Expected(name + ".expected.json");
        var baseUri = expected["baseUri"]?.GetValue<string>();
        using var reader = new StreamReader(P(name + ".m3u"), new UTF8Encoding(false), true);
        var channels = M3uParser.Parse(reader, expected["sourceId"]!.GetValue<string>(), baseUri == null ? null : new Uri(baseUri));
        Same(expected["channels"], channels.Select(Ch));
    }
    [Fact] public async Task M3uWindows1254FileFallsBackFromUtf8()
    {
        var expected = Expected("m3u/windows-1254.expected.json");
        var channels = await M3uParser.ParseFileAsync(P("m3u/windows-1254.m3u"), "tr", null, default);
        Same(expected["channels"], channels.Select(Ch));
    }
    // Channel URLs (and so channel keys) are .NET Uri.AbsoluteUri; other platforms reimplement it. This also catches a
    // .NET upgrade that changes Uri normalization, which would silently change channel keys.
    [Fact] public void M3uUrlNormalization()
    {
        var doc = Doc("m3u/urls.json");
        foreach (var c in doc.GetProperty("absolute").EnumerateArray())
            Assert.Equal(c.GetProperty("expected").GetString(), Uri.TryCreate(c.GetProperty("input").GetString(), UriKind.Absolute, out var u) ? u.AbsoluteUri : null);
        var baseUri = new Uri(doc.GetProperty("baseUri").GetString()!);
        foreach (var c in doc.GetProperty("relative").EnumerateArray())
            Assert.Equal(c.GetProperty("expected").GetString(), Uri.TryCreate(baseUri, c.GetProperty("input").GetString(), out var u) ? u.AbsoluteUri : null);
    }
    [Fact] public void XtreamServerNormalization()
    {
        var cases = Doc("xtream/normalize-server.json");
        foreach (var c in cases.GetProperty("valid").EnumerateArray()) Assert.Equal(c.GetProperty("expected").GetString(), XtreamService.NormalizeServer(c.GetProperty("input").GetString()!));
        foreach (var c in cases.GetProperty("rejected").EnumerateArray()) Assert.Throws<UserInputException>(() => XtreamService.NormalizeServer(c.GetString()!));
    }
    private static SourceSecret Account()
    {
        var a = Doc("xtream/account.json");
        return new(a.GetProperty("server").GetString()!, a.GetProperty("username").GetString()!, a.GetProperty("password").GetString()!);
    }
    [Fact] public void XtreamLiveStreams() =>
        Same(Expected("xtream/live.expected.json")["channels"], XtreamService.ParseLiveStreams("acc", Account(), Doc("xtream/live-categories.json"), Doc("xtream/live-streams.json")).Select(Ch));
    [Fact] public void XtreamVodCatalog() =>
        Same(Expected("xtream/vod-catalog.expected.json")["items"], XtreamService.ParseCatalog("acc", VodKind.Movie, Doc("xtream/vod-categories.json"), Doc("xtream/vod-streams.json")).Select(Vi));
    [Theory]
    [InlineData("series-details-object", VodKind.Series)]
    [InlineData("series-details-array", VodKind.Series)]
    [InlineData("series-details-nested", VodKind.Series)]
    [InlineData("movie-details", VodKind.Movie)]
    public void XtreamDetails(string name, VodKind kind)
    {
        var item = kind == VodKind.Movie ? new VodItem("8", "acc", VodKind.Movie, "Film", "Other") : new VodItem("s1", "acc", VodKind.Series, "Dizi", "Drama");
        var expected = Expected($"xtream/{name}.expected.json");
        Same(expected["input"], Vi(item));
        var details = XtreamService.ParseDetails(item, Doc($"xtream/{name}.json"));
        Same(expected["result"], new { item = Vi(details.Item), episodes = details.Episodes.Select(Ep) });
    }
    [Fact] public void XtreamStreamUrls()
    {
        var urls = Doc("xtream/stream-urls.json").EnumerateArray().Select(c => XtreamService.VodStreamUrl(
            new(c.GetProperty("server").GetString()!, c.GetProperty("username").GetString()!, c.GetProperty("password").GetString()!),
            c.GetProperty("id").GetString()!, c.GetProperty("extension").GetString()!, c.GetProperty("episode").GetBoolean()));
        Same(Expected("xtream/stream-urls.expected.json"), urls);
    }
    [Fact] public void TurkishAwareSearch()
    {
        var cases = Doc("search/cases.json");
        foreach (var c in cases.GetProperty("contains").EnumerateArray())
        {
            var (text, query) = (c.GetProperty("text").GetString()!, c.GetProperty("query").GetString()!);
            Assert.True(c.GetProperty("expected").GetBoolean() == TextSearch.Contains(text, query), $"Contains(\"{text}\", \"{query}\")");
        }
        foreach (var c in cases.GetProperty("equals").EnumerateArray())
        {
            var (a, b) = (c.GetProperty("a").GetString()!, c.GetProperty("b").GetString()!);
            Assert.True(c.GetProperty("expected").GetBoolean() == TextSearch.Equals(a, b), $"Equals(\"{a}\", \"{b}\")");
        }
    }
    [Fact] public void XmlTvTimes()
    {
        var cases = Doc("xmltv/times.json");
        foreach (var c in cases.GetProperty("valid").EnumerateArray()) Assert.Equal(DateTimeOffset.Parse(c.GetProperty("utc").GetString()!), XmlTvParser.ParseTime(c.GetProperty("input").GetString()));
        foreach (var c in cases.GetProperty("invalid").EnumerateArray()) Assert.Null(XmlTvParser.ParseTime(c.GetString()));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void XmlTvGuide(bool gzip)
    {
        // {NOW…} tokens are replaced with times relative to the test run (the parser keeps yesterday .. +8 days).
        var now = DateTimeOffset.UtcNow; static string F(DateTimeOffset t) => t.ToString("yyyyMMddHHmmss +0000");
        var xml = File.ReadAllText(P("xmltv/guide.xml")).Replace("{NOW+9D+1H}", F(now.AddDays(9).AddHours(1))).Replace("{NOW+9D}", F(now.AddDays(9)))
            .Replace("{NOW-3D}", F(now.AddDays(-3))).Replace("{NOW-2D}", F(now.AddDays(-2))).Replace("{NOW+2H}", F(now.AddHours(2))).Replace("{NOW+1H}", F(now.AddHours(1))).Replace("{NOW}", F(now));
        var path = Path.GetTempFileName();
        try
        {
            if (gzip) { using var file = File.Create(path); using var zip = new System.IO.Compression.GZipStream(file, System.IO.Compression.CompressionLevel.Fastest); zip.Write(Encoding.UTF8.GetBytes(xml)); }
            else File.WriteAllText(path, xml);
            var expected = Expected("xmltv/guide.expected.json");
            var channels = expected["channels"]!.AsArray().Select(x => x!.GetValue<string>()).ToHashSet();
            Same(expected["programmes"], XmlTvParser.ReadFile(path, channels, default).Select(p => new { p.ChannelId, p.Title, p.Description }).ToArray());
        }
        finally { File.Delete(path); }
    }
}
