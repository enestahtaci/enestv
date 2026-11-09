using System.IO;
using System.Reflection;
using EnesTV.Core.Models;
using EnesTV.Core.Services;

namespace EnesTV.App.ViewModels;

// Backup and restore (docs/CORE-SPEC.md, "Yedek"): one passphrase-encrypted .enestv file with the whole library.
public sealed partial class MainViewModel
{
    private static string AppVersion => typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "";

    public async Task ExportBackupAsync(string path, string passphrase)
    {
        BackupFile.CheckPassphrase(passphrase);
        var backup = await App.Store.ExportAsync(AppVersion, _lifetime.Token);
        var bytes = await Task.Run(() => BackupFile.Encrypt(backup, passphrase), _lifetime.Token);
        // Write next to the target and move into place, so an interrupted write never leaves a half file.
        var temporary = path + ".tmp";
        await File.WriteAllBytesAsync(temporary, bytes, _lifetime.Token);
        File.Move(temporary, path, true);
        App.Log.Write("backup.export");
        Status = $"Yedek kaydedildi: {Path.GetFileName(path)} · {backup.Sources.Count} kaynak, {backup.Channels.Count:N0} kanal";
    }

    public async Task<LibraryBackup> ReadBackupAsync(string path, string passphrase)
    {
        if (new FileInfo(path).Length > BackupFile.MaxFileBytes) throw new UserInputException("Yedek dosyası çok büyük.");
        var bytes = await File.ReadAllBytesAsync(path, _lifetime.Token);
        return await Task.Run(() => BackupFile.Decrypt(bytes, passphrase), _lifetime.Token);
    }

    /// <summary>Replaces the whole library with the backup after taking a local safety copy of the current one.</summary>
    public async Task<string> RestoreBackupAsync(LibraryBackup backup)
    {
        if (Busy) throw new UserInputException("Önce devam eden yüklemeyi bitirin veya iptal edin.");
        if (IsVod) Section = "Live TV";
        var safety = Path.Combine(App.DataRoot, "backups", $"library-before-restore-{DateTime.Now:yyyyMMdd-HHmmss}.db");
        await App.Store.CopyToAsync(safety, _lifetime.Token);
        await BoundedAsync(Playback.StopAsync(), "restore.stop.slow");
        PlayingItem = null; _previousChannelId = null; Selected = null;
        await App.Store.ReplaceAllAsync(backup, _lifetime.Token);
        await LoadLibraryAsync(); await LoadEpgAsync();
        App.Log.Write("backup.restore");
        Status = $"Yedek geri yüklendi · {backup.Sources.Count} kaynak, {backup.Channels.Count:N0} kanal. Önceki kütüphanenin kopyası: backups klasörü.";
        return safety;
    }
}
