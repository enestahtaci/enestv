namespace EnesTV.Core.Models;

// Contents of an enestv backup (docs/CORE-SPEC.md, "Yedek"). Always stored encrypted with the user's passphrase:
// sources carry credentials, and Xtream channel URLs embed the username and password.
public sealed record LibraryBackup(
    int Version,
    DateTimeOffset Created,
    string AppVersion,
    IReadOnlyList<BackupSource> Sources,
    IReadOnlyList<Channel> Channels,
    IReadOnlyList<string> Favorites,
    IReadOnlyList<string> Recent,
    UserSettings Settings,
    LibraryLayout Layout,
    IReadOnlyList<BackupProgress> Progress)
{
    public const int CurrentVersion = 1;
}

public sealed record BackupSource(string Id, string Name, SourceKind Kind, string Location, string Username, string Password, DateTimeOffset? Updated);

public sealed record BackupProgress(string SourceId, string Key, long PositionMs, long DurationMs, bool Completed, DateTimeOffset Updated);
