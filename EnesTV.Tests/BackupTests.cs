using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using EnesTV.Core.Data;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Xunit;

namespace EnesTV.Tests;

public sealed class BackupTests
{
    private const string Passphrase = "doğru-parola-123";
    private const int FastIterations = 10_000; // the app uses BackupFile.Iterations (600,000)

    private static async Task<T> WithStore<T>(Func<LibraryStore, Task<T>> action)
    {
        var root = Path.Combine(Path.GetTempPath(), "enestv-backup-" + Guid.NewGuid()); Directory.CreateDirectory(root);
        try { var store = new LibraryStore(Path.Combine(root, "library.db"), TestSecretProtector.Instance); await store.InitializeAsync(); return await action(store); }
        finally { Directory.Delete(root, true); }
    }
    private static async Task FillAsync(LibraryStore store)
    {
        var xtream = new Source("acc", "Hesap", SourceKind.Xtream, new("https://example.invalid", "kullanıcı", "gizli&parola"), DateTimeOffset.UtcNow);
        var m3u = new Source("m3u", "Liste", SourceKind.M3uUrl, new("https://example.invalid/list.m3u?token=abc"));
        var channels = Enumerable.Range(0, 30).Select(i => new Channel($"c{i}", "acc", $"Kanal {i}", $"https://example.invalid/live/kullan%C4%B1c%C4%B1/gizli%26parola/{i}.ts", Category: i % 2 == 0 ? "Spor" : "Haber")).ToArray();
        await store.SaveSourceAsync(xtream, channels);
        await store.SaveSourceAsync(m3u, [new("m0", "m3u", "ŞİMDİ TV", "https://example.invalid/m0")]);
        await store.SetFavoriteAsync("c3", true); await store.SetFavoriteAsync("m0", true);
        foreach (var id in new[] { "c1", "c2", "m0" }) { await store.AddRecentAsync(id); await Task.Delay(2); }
        await store.SaveSettingsAsync(new("Light", 42, false, "https://example.invalid/epg.xml"));
        await store.SaveLayoutAsync(new() { Channels = new() { ["c1"] = new("Özel ad", 0) }, HiddenCategories = ["Haber"], FavoriteLists = [new("l1", "Spor", ["c2", "c4"])], Shortcuts = new() { ["next"] = "G" } });
        await store.SaveProgressAsync("acc", new("acc:Movie:12", 45000, 120000, false, DateTimeOffset.UtcNow));
    }

    [Fact] public async Task RestoreReplacesTheWholeLibrary()
    {
        var exported = await WithStore(async store => { await FillAsync(store); return await store.ExportAsync("test"); });
        var file = BackupFile.Encrypt(exported, Passphrase, FastIterations);
        await WithStore(async target =>
        {
            // Existing data must disappear.
            await target.SaveSourceAsync(new("old", "Eski", SourceKind.M3uUrl, new("https://example.invalid/old")), [new("o1", "old", "Eski kanal", "https://example.invalid/o1")]);
            await target.SetFavoriteAsync("o1", true);
            await target.ReplaceAllAsync(BackupFile.Decrypt(file, Passphrase));
            var library = await target.LoadAsync();
            Assert.Equal(["acc", "m3u"], library.Sources.Select(s => s.Id).Order());
            Assert.Equal("gizli&parola", library.Sources.Single(s => s.Id == "acc").Secret.Password);
            Assert.Equal("https://example.invalid/list.m3u?token=abc", library.Sources.Single(s => s.Id == "m3u").Secret.Location);
            Assert.Equal(31, library.Channels.Count); Assert.DoesNotContain(library.Channels, c => c.Id == "o1");
            Assert.Equal(["c3", "m0"], library.Favorites.Order());
            Assert.Equal(["m0", "c2", "c1"], library.Recent);
            Assert.Equal(new UserSettings("Light", 42, false, "https://example.invalid/epg.xml"), library.Settings);
            var layout = await target.LoadLayoutAsync();
            Assert.Equal("Özel ad", layout.Channels["c1"].Name); Assert.Contains("Haber", layout.HiddenCategories);
            Assert.Equal(["c2", "c4"], layout.FavoriteLists.Single().Channels.Order()); Assert.Equal("G", layout.Shortcuts["next"]);
            Assert.Equal(45000, (await target.LoadProgressAsync("acc"))["acc:Movie:12"].ResumeMs);
            return true;
        });
    }
    [Fact] public async Task BackupFileNeverContainsPlainSecrets()
    {
        var exported = await WithStore(async store => { await FillAsync(store); return await store.ExportAsync("test"); });
        var text = Encoding.UTF8.GetString(BackupFile.Encrypt(exported, Passphrase, FastIterations));
        foreach (var secret in new[] { "gizli", "kullan", "token=abc", "example.invalid", "Kanal 1", "Özel ad" }) Assert.DoesNotContain(secret, text);
    }
    [Fact] public async Task WrongPassphraseTamperingAndForeignFilesAreRejected()
    {
        var exported = await WithStore(async store => { await FillAsync(store); return await store.ExportAsync("test"); });
        var file = BackupFile.Encrypt(exported, Passphrase, FastIterations);
        Assert.Contains("Parola yanlış", Assert.Throws<UserInputException>(() => BackupFile.Decrypt(file, "yanlış-parola-123")).Message);
        var envelope = JsonNode.Parse(file)!; var data = Convert.FromBase64String(envelope["data"]!.GetValue<string>()); data[5] ^= 1; envelope["data"] = Convert.ToBase64String(data);
        Assert.Contains("Parola yanlış", Assert.Throws<UserInputException>(() => BackupFile.Decrypt(Encoding.UTF8.GetBytes(envelope.ToJsonString()), Passphrase)).Message);
        Assert.Contains("enestv yedeği değil", Assert.Throws<UserInputException>(() => BackupFile.Decrypt("{\"hello\":1}"u8.ToArray(), Passphrase)).Message);
        Assert.Contains("enestv yedeği değil", Assert.Throws<UserInputException>(() => BackupFile.Decrypt("not json"u8.ToArray(), Passphrase)).Message);
        var newer = JsonNode.Parse(file)!; newer["version"] = 99;
        Assert.Contains("daha yeni", Assert.Throws<UserInputException>(() => BackupFile.Decrypt(Encoding.UTF8.GetBytes(newer.ToJsonString()), Passphrase)).Message);
        Assert.Throws<UserInputException>(() => BackupFile.Encrypt(exported, "kısa", FastIterations));
    }
    [Fact] public async Task PassphraseIsUnicodeNormalized()
    {
        var exported = await WithStore(async store => { await FillAsync(store); return await store.ExportAsync("test"); });
        var composed = "Şifre-Ğüç-2026"; var decomposed = composed.Normalize(NormalizationForm.FormD);
        Assert.NotEqual(composed, decomposed);
        Assert.Equal(exported.Sources.Count, BackupFile.Decrypt(BackupFile.Encrypt(exported, composed, FastIterations), decomposed).Sources.Count);
    }
    // Cross-platform fixtures: every platform must open both files with this passphrase. sample.enestv is written by
    // this (Windows) core, sample-android.enestv by the Android core (android/core); both hold sample.expected.json.
    [Theory]
    [InlineData("sample.enestv")]
    [InlineData("sample-android.enestv")]
    public void SampleFixtureDecrypts(string file)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "fixtures", "backup");
        var backup = BackupFile.Decrypt(File.ReadAllBytes(Path.Combine(root, file)), "enestv-fixture-parola");
        var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "sample.expected.json")));
        var actual = JsonSerializer.SerializeToNode(backup, BackupFile.PayloadJson);
        Assert.True(JsonNode.DeepEquals(expected, actual), actual?.ToJsonString());
    }
}
