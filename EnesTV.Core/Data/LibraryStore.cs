using System.Text.Json;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Microsoft.Data.Sqlite;

namespace EnesTV.Core.Data;

// Data at rest is encrypted through the platform's ISecretProtector (Windows: DPAPI).
public sealed partial class LibraryStore(string path, ISecretProtector protector)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ISecretProtector _protector = protector;
    private SqliteConnection Open()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Pooling = false }.ToString());
        db.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;";
        cmd.ExecuteNonQuery();
        return db;
    }
    private async Task<T> Run<T>(Func<SqliteConnection, T> operation, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try { return await Task.Run(() => { ct.ThrowIfCancellationRequested(); using var db = Open(); return operation(db); }, ct).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }
    private static void Execute(SqliteConnection db, string sql, params (string, object)[] args)
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = sql;
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
        cmd.ExecuteNonQuery();
    }
    public Task InitializeAsync(CancellationToken ct = default) => Run(db =>
    {
        using var version = db.CreateCommand(); version.CommandText = "PRAGMA user_version";
        var current = Convert.ToInt32(version.ExecuteScalar());
        if (current > 3) throw new InvalidOperationException("Unsupported database schema");
        if (current == 0)
        {
            using var tx = db.BeginTransaction();
            Execute(db, """
                CREATE TABLE sources(id TEXT PRIMARY KEY,name TEXT NOT NULL,kind INTEGER NOT NULL,secret BLOB NOT NULL,updated TEXT);
                CREATE TABLE channels(id TEXT PRIMARY KEY,source_id TEXT NOT NULL REFERENCES sources(id) ON DELETE CASCADE,metadata BLOB NOT NULL);
                CREATE INDEX channels_source ON channels(source_id);
                CREATE TABLE favorites(channel_id TEXT PRIMARY KEY REFERENCES channels(id) ON DELETE CASCADE);
                CREATE TABLE recent(channel_id TEXT PRIMARY KEY REFERENCES channels(id) ON DELETE CASCADE,watched INTEGER NOT NULL);
                CREATE TABLE settings(id INTEGER PRIMARY KEY CHECK(id=1),value BLOB NOT NULL);
                CREATE TABLE epg(channel_id TEXT NOT NULL,title TEXT NOT NULL,description TEXT NOT NULL,start INTEGER NOT NULL,end INTEGER NOT NULL,feed TEXT NOT NULL);
                CREATE INDEX epg_lookup ON epg(channel_id,start,end);
                CREATE TABLE epg_metadata(feed TEXT PRIMARY KEY,updated TEXT NOT NULL);
                PRAGMA user_version=1;
                """);
            tx.Commit();
        }
        if (current < 2)
        {
            using var tx = db.BeginTransaction();
            Execute(db, "CREATE TABLE layout(id INTEGER PRIMARY KEY CHECK(id=1),value BLOB NOT NULL); PRAGMA user_version=2;");
            tx.Commit();
        }
        if (current < 3)
        {
            using var tx = db.BeginTransaction();
            Execute(db, """
                CREATE TABLE IF NOT EXISTS vod_catalog(source_id TEXT NOT NULL REFERENCES sources(id) ON DELETE CASCADE,kind INTEGER NOT NULL,value BLOB NOT NULL,PRIMARY KEY(source_id,kind));
                CREATE TABLE IF NOT EXISTS watch_progress(source_id TEXT NOT NULL REFERENCES sources(id) ON DELETE CASCADE,item_key TEXT NOT NULL,value BLOB NOT NULL,PRIMARY KEY(source_id,item_key));
                PRAGMA user_version=3;
                """);
            tx.Commit();
        }
        return true;
    }, ct);
    public Task<Library> LoadAsync(CancellationToken ct = default) => Run(db =>
    {
        var sources = new List<Source>(); var channels = new List<Channel>(); var fav = new HashSet<string>(); var recent = new List<string>();
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT id,name,kind,secret,updated FROM sources";
        using (var r = cmd.ExecuteReader()) while (r.Read()) sources.Add(new(r.GetString(0), r.GetString(1), (SourceKind)r.GetInt32(2), JsonSerializer.Deserialize<SourceSecret>(_protector.Unprotect((byte[])r[3]))!, r.IsDBNull(4) ? null : DateTimeOffset.Parse(r.GetString(4))));
        cmd.CommandText = "SELECT metadata FROM channels";
        using (var r = cmd.ExecuteReader()) while (r.Read()) { ct.ThrowIfCancellationRequested(); channels.Add(JsonSerializer.Deserialize<Channel>(_protector.Unprotect((byte[])r[0]))!); }
        cmd.CommandText = "SELECT channel_id FROM favorites";
        using (var r = cmd.ExecuteReader()) while (r.Read()) fav.Add(r.GetString(0));
        cmd.CommandText = "SELECT channel_id FROM recent ORDER BY watched DESC LIMIT 20";
        using (var r = cmd.ExecuteReader()) while (r.Read()) recent.Add(r.GetString(0));
        cmd.CommandText = "SELECT value FROM settings WHERE id=1";
        var value = cmd.ExecuteScalar();
        var settings = value is byte[] bytes ? JsonSerializer.Deserialize<UserSettings>(_protector.Unprotect(bytes))! : new UserSettings();
        return new Library(sources, channels, fav, recent, settings);
    }, ct);
    public Task SaveSourceAsync(Source source, IReadOnlyList<Channel> channels, CancellationToken ct = default) => Run(db =>
    {
        using var tx = db.BeginTransaction();
        Execute(db, "INSERT INTO sources VALUES($id,$name,$kind,$secret,$updated) ON CONFLICT(id) DO UPDATE SET name=$name,kind=$kind,secret=$secret,updated=$updated", ("$id", source.Id), ("$name", source.Name), ("$kind", (int)source.Kind), ("$secret", _protector.Protect(JsonSerializer.Serialize(source.Secret))), ("$updated", (object?)source.Updated?.ToString("O") ?? DBNull.Value));
        Execute(db, "CREATE TEMP TABLE incoming(id TEXT PRIMARY KEY)");
        using var insert = db.CreateCommand(); insert.CommandText = "INSERT INTO channels VALUES($id,$source,$data) ON CONFLICT(id) DO UPDATE SET metadata=$data; INSERT OR IGNORE INTO incoming VALUES($id);";
        insert.Parameters.Add("$id", SqliteType.Text); insert.Parameters.AddWithValue("$source", source.Id); insert.Parameters.Add("$data", SqliteType.Blob); insert.Prepare();
        foreach (var channel in channels)
        {
            ct.ThrowIfCancellationRequested(); insert.Parameters["$id"].Value = channel.Id; insert.Parameters["$data"].Value = _protector.Protect(JsonSerializer.Serialize(channel)); insert.ExecuteNonQuery();
        }
        Execute(db, "DELETE FROM channels WHERE source_id=$id AND id NOT IN(SELECT id FROM incoming)", ("$id", source.Id));
        tx.Commit(); return true;
    }, ct);
    public Task RenameSourceAsync(string id, string name, CancellationToken ct = default) => Run(db => { Execute(db, "UPDATE sources SET name=$name WHERE id=$id", ("$name", name), ("$id", id)); return true; }, ct);
    public Task DeleteSourceAsync(string id, CancellationToken ct = default) => Run(db => { Execute(db, "DELETE FROM sources WHERE id=$id", ("$id", id)); return true; }, ct);
    public Task SetFavoriteAsync(string id, bool favorite, CancellationToken ct = default) => Run(db => { Execute(db, favorite ? "INSERT OR IGNORE INTO favorites VALUES($id)" : "DELETE FROM favorites WHERE channel_id=$id", ("$id", id)); return true; }, ct);
    public Task AddRecentAsync(string id, CancellationToken ct = default) => Run(db =>
    {
        using var tx = db.BeginTransaction();
        Execute(db, "INSERT INTO recent VALUES($id,$now) ON CONFLICT(channel_id) DO UPDATE SET watched=$now", ("$id", id), ("$now", DateTime.UtcNow.Ticks));
        Execute(db, "DELETE FROM recent WHERE channel_id NOT IN(SELECT channel_id FROM recent ORDER BY watched DESC LIMIT 20)"); tx.Commit(); return true;
    }, ct);
    public Task SaveSettingsAsync(UserSettings settings, CancellationToken ct = default) => Run(db => { Execute(db, "INSERT INTO settings VALUES(1,$value) ON CONFLICT(id) DO UPDATE SET value=$value", ("$value", _protector.Protect(JsonSerializer.Serialize(settings)))); return true; }, ct);
}
