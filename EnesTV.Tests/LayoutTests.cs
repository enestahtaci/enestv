using EnesTV.Core.Data;
using EnesTV.Core.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EnesTV.Tests;

public sealed class LayoutTests
{
    [Fact]
    public async Task UpgradeFromV1PreservesAccountFavoritesAndSavesIndependentLayout()
    {
        var root = Path.Combine(Path.GetTempPath(), "enestv-layout-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "library.db"); var store = new LibraryStore(path);
            await store.InitializeAsync();
            var source = new Source("s", "Fixture", SourceKind.M3uUrl, new("https://example.invalid/list"));
            Channel[] channels = [new("a", "s", "Original", "https://example.invalid/a"), new("b", "s", "Second", "https://example.invalid/b")];
            await store.SaveSourceAsync(source, channels); await store.SetFavoriteAsync("a", true); await store.AddRecentAsync("a");
            // Reproduce the schema shipped in 1.0.2, including existing user data.
            using (var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString()))
            {
                db.Open(); using var cmd = db.CreateCommand(); cmd.CommandText = "DROP TABLE layout; PRAGMA user_version=1"; cmd.ExecuteNonQuery();
            }
            var upgraded = new LibraryStore(path); await upgraded.InitializeAsync(); await upgraded.InitializeAsync();
            Assert.Empty((await upgraded.LoadLayoutAsync()).Channels);
            var layout = new LibraryLayout
            {
                Channels = new() { ["a"] = new("My private alias", 2, false), ["b"] = new("", 0, true) },
                HiddenCategories = ["Hidden group"],
                FavoriteLists = [new("sports", "Sports", ["a"]), new("family", "Family", ["a", "b"])],
                Shortcuts = new() { ["next"] = "Ctrl+Right" }
            };
            await upgraded.SaveLayoutAsync(layout);
            await upgraded.SaveSourceAsync(source, channels); // Refresh must not overwrite user layout.
            var reopened = new LibraryStore(path); await reopened.InitializeAsync();
            var library = await reopened.LoadAsync(); var restored = await reopened.LoadLayoutAsync();
            Assert.Single(library.Sources); Assert.Contains("a", library.Favorites); Assert.Equal("a", library.Recent[0]);
            Assert.Equal("Original", library.Channels.First(x => x.Id == "a").Name);
            Assert.Equal("My private alias", restored.Channels["a"].Name); Assert.True(restored.Channels["b"].Hidden);
            Assert.Contains("Hidden group", restored.HiddenCategories); Assert.Equal(2, restored.FavoriteLists.Count);
            Assert.Contains("b", restored.FavoriteLists[1].Channels); Assert.Equal("Ctrl+Right", restored.Shortcuts["next"]);
            Assert.DoesNotContain("My private alias", System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path)));
        }
        finally { Directory.Delete(root, true); }
    }
}
