using System.Text.Json;
using EnesTV.Core.Models;
using EnesTV.Core.Services;

namespace EnesTV.Core.Data;

public sealed partial class LibraryStore
{
    public Task<VodCatalog?> LoadVodCatalogAsync(string source, VodKind kind, CancellationToken ct = default) => Run(db =>
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT value FROM vod_catalog WHERE source_id=$s AND kind=$k";
        cmd.Parameters.AddWithValue("$s", source); cmd.Parameters.AddWithValue("$k", (int)kind);
        return cmd.ExecuteScalar() is byte[] data ? JsonSerializer.Deserialize<VodCatalog>(SecretProtection.Unprotect(data)) : null;
    }, ct);
    public Task SaveVodCatalogAsync(string source, VodKind kind, VodCatalog catalog, CancellationToken ct = default) => Run(db =>
    {
        Execute(db, "INSERT INTO vod_catalog VALUES($s,$k,$v) ON CONFLICT(source_id,kind) DO UPDATE SET value=$v",
            ("$s", source), ("$k", (int)kind), ("$v", SecretProtection.Protect(JsonSerializer.Serialize(catalog)))); return true;
    }, ct);
    public Task<IReadOnlyDictionary<string, WatchProgress>> LoadProgressAsync(string source, CancellationToken ct = default) => Run<IReadOnlyDictionary<string, WatchProgress>>(db =>
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT value FROM watch_progress WHERE source_id=$s"; cmd.Parameters.AddWithValue("$s", source);
        using var reader = cmd.ExecuteReader(); var result = new Dictionary<string, WatchProgress>();
        while (reader.Read()) { var value = JsonSerializer.Deserialize<WatchProgress>(SecretProtection.Unprotect((byte[])reader[0])); if (value != null) result[value.Key] = value; }
        return result;
    }, ct);
    public Task SaveProgressAsync(string source, WatchProgress progress, CancellationToken ct = default) => Run(db =>
    {
        Execute(db, "INSERT INTO watch_progress VALUES($s,$k,$v) ON CONFLICT(source_id,item_key) DO UPDATE SET value=$v",
            ("$s", source), ("$k", progress.Key), ("$v", SecretProtection.Protect(JsonSerializer.Serialize(progress)))); return true;
    }, ct);
}
