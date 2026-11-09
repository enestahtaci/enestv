using System.Text.Json;
using EnesTV.Core.Models;
using Microsoft.Data.Sqlite;

namespace EnesTV.Core.Data;

// Backup and restore of the whole library (docs/CORE-SPEC.md, "Yedek"). Not included: the movie/series catalog cache
// and the TV guide, which are downloaded again.
public sealed partial class LibraryStore
{
    public Task<LibraryBackup> ExportAsync(string appVersion, CancellationToken ct = default) => Run(db =>
    {
        var library = Load(db, ct);
        var progress = new List<BackupProgress>();
        using (var cmd = db.CreateCommand())
        {
            cmd.CommandText = "SELECT source_id,value FROM watch_progress";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                if (JsonSerializer.Deserialize<WatchProgress>(_protector.Unprotect((byte[])reader[1])) is { } p)
                    progress.Add(new(reader.GetString(0), p.Key, p.PositionMs, p.DurationMs, p.Completed, p.Updated));
        }
        return new LibraryBackup(LibraryBackup.CurrentVersion, DateTimeOffset.UtcNow, appVersion,
            library.Sources.Select(s => new BackupSource(s.Id, s.Name, s.Kind, s.Secret.Location, s.Secret.Username, s.Secret.Password, s.Updated)).ToArray(),
            library.Channels, library.Favorites.Order(StringComparer.Ordinal).ToArray(), library.Recent, library.Settings, LoadLayout(db), progress);
    }, ct);

    /// <summary>Consistent copy of the database file (SQLite online backup), e.g. a safety copy before a restore.</summary>
    public Task CopyToAsync(string destination, CancellationToken ct = default) => Run(db =>
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        using var target = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = destination, Pooling = false }.ToString());
        target.Open(); db.BackupDatabase(target); return true;
    }, ct);

    /// <summary>Replaces sources, channels, favourites, recent, settings, layout and progress with the backup, in one transaction.</summary>
    public Task ReplaceAllAsync(LibraryBackup backup, CancellationToken ct = default) => Run(db =>
    {
        using var tx = db.BeginTransaction();
        // Deleting sources cascades to channels, favourites, recent, catalog cache and progress.
        Execute(db, "DELETE FROM sources; DELETE FROM settings; DELETE FROM layout;");
        foreach (var s in backup.Sources)
            Execute(db, "INSERT INTO sources VALUES($id,$name,$kind,$secret,$updated)", ("$id", s.Id), ("$name", s.Name), ("$kind", (int)s.Kind),
                ("$secret", _protector.Protect(JsonSerializer.Serialize(new SourceSecret(s.Location, s.Username, s.Password)))), ("$updated", (object?)s.Updated?.ToString("O") ?? DBNull.Value));
        var sources = backup.Sources.Select(s => s.Id).ToHashSet();
        using (var insert = db.CreateCommand())
        {
            insert.CommandText = "INSERT OR IGNORE INTO channels VALUES($id,$source,$data)";
            insert.Parameters.Add("$id", SqliteType.Text); insert.Parameters.Add("$source", SqliteType.Text); insert.Parameters.Add("$data", SqliteType.Blob); insert.Prepare();
            foreach (var channel in backup.Channels.Where(c => sources.Contains(c.SourceId)))
            {
                ct.ThrowIfCancellationRequested();
                insert.Parameters["$id"].Value = channel.Id; insert.Parameters["$source"].Value = channel.SourceId; insert.Parameters["$data"].Value = _protector.Protect(JsonSerializer.Serialize(channel));
                insert.ExecuteNonQuery();
            }
        }
        // Favourites and recent only for channels that exist; recent keeps its order (newest first), at most 20.
        foreach (var id in backup.Favorites) Execute(db, "INSERT OR IGNORE INTO favorites SELECT id FROM channels WHERE id=$id", ("$id", id));
        var now = DateTime.UtcNow.Ticks;
        foreach (var (id, index) in backup.Recent.Take(20).Select((id, i) => (id, i)))
            Execute(db, "INSERT OR IGNORE INTO recent SELECT id,$watched FROM channels WHERE id=$id", ("$id", id), ("$watched", now - index));
        Execute(db, "INSERT INTO settings VALUES(1,$value)", ("$value", _protector.Protect(JsonSerializer.Serialize(backup.Settings))));
        Execute(db, "INSERT INTO layout VALUES(1,$value)", ("$value", _protector.Protect(JsonSerializer.Serialize(backup.Layout))));
        foreach (var p in backup.Progress.Where(p => sources.Contains(p.SourceId)))
            Execute(db, "INSERT OR REPLACE INTO watch_progress VALUES($s,$k,$v)", ("$s", p.SourceId), ("$k", p.Key),
                ("$v", _protector.Protect(JsonSerializer.Serialize(new WatchProgress(p.Key, p.PositionMs, p.DurationMs, p.Completed, p.Updated)))));
        tx.Commit(); return true;
    }, ct);
}
