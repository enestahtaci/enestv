using EnesTV.Core.Data;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Xunit;
namespace EnesTV.Tests;
public sealed class PersistenceTests
{
    [Fact] public async Task SourcesFavoritesRecentSettingsSurviveRestartAndRefresh()
    {
        var root = Path.Combine(Path.GetTempPath(), "enestv-test-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "library.db"); var store = new LibraryStore(path, TestSecretProtector.Instance); await store.InitializeAsync();
            var source = new Source("s", "My source", SourceKind.M3uUrl, new("https://example.invalid/list?password=secret"), DateTimeOffset.UtcNow);
            var channels = Enumerable.Range(0, 25).Select(i => new Channel("c" + i, "s", "Channel " + i, "https://example.invalid/" + i)).ToArray();
            await store.SaveSourceAsync(source, channels); await store.SetFavoriteAsync("c2", true);
            foreach (var c in channels) await store.AddRecentAsync(c.Id);
            await store.SaveSettingsAsync(new("Light", 42, false, "https://example.invalid/epg"));
            await store.SaveSourceAsync(source with { Name = "Updated" }, channels);
            var reopened = new LibraryStore(path, TestSecretProtector.Instance); await reopened.InitializeAsync(); var library = await reopened.LoadAsync();
            Assert.Equal(25, library.Channels.Count); Assert.Contains("c2", library.Favorites); Assert.Equal(20, library.Recent.Count); Assert.Equal("c24", library.Recent[0]); Assert.Equal(42, library.Settings.DefaultVolume); Assert.Equal("Updated", library.Sources[0].Name);
            Assert.DoesNotContain("password=secret", System.Text.Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path)));
            await reopened.DeleteSourceAsync("s"); var empty = await reopened.LoadAsync(); Assert.Empty(empty.Channels); Assert.Empty(empty.Favorites); Assert.Empty(empty.Recent);
        }
        finally { Directory.Delete(root, true); }
    }
    // The DPAPI implementation and its compatibility with existing libraries are checked by the Windows smoke run.
    [Fact] public void ProtectorRoundTrip()
    {
        var protector = TestSecretProtector.Instance;
        var encrypted = protector.Protect("test-password"); Assert.Equal("test-password", protector.Unprotect(encrypted)); Assert.NotEqual("test-password", System.Text.Encoding.UTF8.GetString(encrypted));
    }
}
