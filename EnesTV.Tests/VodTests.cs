using System.Text.Json;
using EnesTV.Core.Data;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EnesTV.Tests;

public sealed class VodTests
{
    // Catalog, details (all episode layouts) and stream URL rules are covered by FixtureTests.
    [Fact] public async Task Schema2UpgradeKeepsLayoutAndEncryptsCatalogAndResumeProgress()
    {
        var root = Path.Combine(Path.GetTempPath(), "enestv-vod-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "library.db"); var store = new LibraryStore(path, TestSecretProtector.Instance); await store.InitializeAsync();
            var source = new Source("s", "Account", SourceKind.Xtream, new("https://example.invalid", "u", "p"));
            await store.SaveSourceAsync(source, []); await store.SaveLayoutAsync(new() { HiddenCategories = ["Hidden"] });
            using (var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            { db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "DROP TABLE vod_catalog; DROP TABLE watch_progress; PRAGMA user_version=2"; cmd.ExecuteNonQuery(); }
            await store.InitializeAsync();
            var item = new VodItem("1", "s", VodKind.Movie, "Private title", "Cinema");
            await store.SaveVodCatalogAsync("s", VodKind.Movie, new([item], DateTimeOffset.UtcNow));
            await store.SaveProgressAsync("s", new(item.Key, 45000, 120000, false, DateTimeOffset.UtcNow));
            var reopened = new LibraryStore(path, TestSecretProtector.Instance); await reopened.InitializeAsync();
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
