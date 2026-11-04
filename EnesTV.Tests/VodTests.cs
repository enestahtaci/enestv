using System.Text.Json;
using EnesTV.Core.Data;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EnesTV.Tests;

public sealed class VodTests
{
    [Fact] public void CatalogAcceptsNumericIdsMissingMetadataAndDeduplicates()
    {
        using var cats = JsonDocument.Parse("[{\"category_id\":1,\"category_name\":\"Drama\"}]");
        using var rows = JsonDocument.Parse("[{\"stream_id\":12,\"name\":\"Film\",\"category_id\":\"1\",\"container_extension\":\"mkv\"},{\"stream_id\":12},{\"stream_id\":13,\"name\":null},{}]");
        var items = XtreamService.ParseCatalog("account", VodKind.Movie, cats.RootElement, rows.RootElement);
        Assert.Equal(2, items.Count); Assert.Equal("Drama", items[0].Category); Assert.Equal("mkv", items[0].Extension); Assert.Equal("mp4", items[1].Extension);
        Assert.NotEqual(items[0].Key, (items[0] with { SourceId = "other" }).Key);
    }
    [Theory]
    [InlineData("{\"episodes\":{\"2\":[{\"id\":20,\"episode_num\":2,\"title\":\"B\"}],\"1\":[{\"id\":10,\"episode_num\":1,\"title\":\"A\"}]}}")]
    [InlineData("{\"episodes\":[{\"id\":20,\"season\":2,\"episode_num\":2,\"title\":\"B\"},{\"id\":10,\"season\":1,\"episode_num\":1,\"title\":\"A\"}]}")]
    public void SeriesUsesEpisodesForSeasonListAndSortsNumerically(string json)
    {
        using var doc = JsonDocument.Parse(json); var item = new VodItem("s", "account", VodKind.Series, "Series", "Drama");
        var details = XtreamService.ParseDetails(item, doc.RootElement);
        Assert.Equal(2, details.Episodes.Count); Assert.Equal("10", details.Episodes[0].Id); Assert.Equal(2, details.Episodes[1].Season);
    }
    [Fact] public void MovieDetailsAndStreamUrlUseProviderExtensionAndEscapeCredentials()
    {
        using var doc = JsonDocument.Parse("{\"info\":{\"plot\":\"Plot\",\"movie_image\":\"https://example.invalid/poster\"},\"movie_data\":{\"container_extension\":\"mkv\"}}");
        var details = XtreamService.ParseDetails(new("8", "s", VodKind.Movie, "Film", "Other"), doc.RootElement);
        Assert.Equal("Plot", details.Item.Plot); Assert.Equal("mkv", details.Item.Extension);
        Assert.Equal("https://example.invalid/sub/movie/user%2Fname/p%26ss/8.mkv", XtreamService.VodStreamUrl(new("https://example.invalid/sub", "user/name", "p&ss"), "8", "mkv", false));
        Assert.EndsWith("/series/u/p/8.mp4", XtreamService.VodStreamUrl(new("https://example.invalid", "u", "p"), "8", "../bad", true));
    }
    [Fact] public async Task Schema2UpgradeKeepsLayoutAndEncryptsCatalogAndResumeProgress()
    {
        var root = Path.Combine(Path.GetTempPath(), "enestv-vod-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "library.db"); var store = new LibraryStore(path); await store.InitializeAsync();
            var source = new Source("s", "Account", SourceKind.Xtream, new("https://example.invalid", "u", "p"));
            await store.SaveSourceAsync(source, []); await store.SaveLayoutAsync(new() { HiddenCategories = ["Hidden"] });
            using (var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            { db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "DROP TABLE vod_catalog; DROP TABLE watch_progress; PRAGMA user_version=2"; cmd.ExecuteNonQuery(); }
            await store.InitializeAsync();
            var item = new VodItem("1", "s", VodKind.Movie, "Private title", "Cinema");
            await store.SaveVodCatalogAsync("s", VodKind.Movie, new([item], DateTimeOffset.UtcNow));
            await store.SaveProgressAsync("s", new(item.Key, 45000, 120000, false, DateTimeOffset.UtcNow));
            var reopened = new LibraryStore(path); await reopened.InitializeAsync();
            Assert.Contains("Hidden", (await reopened.LoadLayoutAsync()).HiddenCategories);
            Assert.Equal("Private title", (await reopened.LoadVodCatalogAsync("s", VodKind.Movie))!.Items[0].Name);
            var p = (await reopened.LoadProgressAsync("s"))[item.Key]; Assert.Equal(45000, p.ResumeMs);
            Assert.Equal(0, (p with { Completed = true }).ResumeMs);
            Assert.DoesNotContain("Private title", System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path)));
            await reopened.DeleteSourceAsync("s"); Assert.Null(await reopened.LoadVodCatalogAsync("s", VodKind.Movie)); Assert.Empty(await reopened.LoadProgressAsync("s"));
        }
        finally { Directory.Delete(root, true); }
    }
}
