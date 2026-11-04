using EnesTV.Core.Models;
using EnesTV.Core.Services;
using Microsoft.Data.Sqlite;
namespace EnesTV.Core.Data;
public sealed partial class LibraryStore
{
    public Task<int> ImportEpgAsync(string path, string feed, ISet<string> channelIds, CancellationToken ct) => Run(db =>
    {
        using var tx = db.BeginTransaction();
        Execute(db, "DELETE FROM epg WHERE feed=$feed OR end<$old", ("$feed", feed), ("$old", DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds()));
        using var cmd = db.CreateCommand(); cmd.CommandText = "INSERT INTO epg VALUES($id,$title,$desc,$start,$end,$feed)";
        cmd.Parameters.Add("$id", SqliteType.Text); cmd.Parameters.Add("$title", SqliteType.Text); cmd.Parameters.Add("$desc", SqliteType.Text); cmd.Parameters.Add("$start", SqliteType.Integer); cmd.Parameters.Add("$end", SqliteType.Integer); cmd.Parameters.AddWithValue("$feed", feed); cmd.Prepare();
        var count = 0;
        foreach (var p in XmlTvParser.ReadFile(path, channelIds, ct))
        {
            cmd.Parameters["$id"].Value = p.ChannelId; cmd.Parameters["$title"].Value = p.Title; cmd.Parameters["$desc"].Value = p.Description; cmd.Parameters["$start"].Value = p.Start.ToUnixTimeSeconds(); cmd.Parameters["$end"].Value = p.End.ToUnixTimeSeconds(); cmd.ExecuteNonQuery(); count++;
        }
        ct.ThrowIfCancellationRequested();
        Execute(db, "INSERT INTO epg_metadata VALUES($feed,$now) ON CONFLICT(feed) DO UPDATE SET updated=$now", ("$feed", feed), ("$now", DateTimeOffset.UtcNow.ToString("O")));
        tx.Commit(); return count;
    }, ct);
    public Task<IReadOnlyList<Programme>> NowNextAsync(CancellationToken ct = default) => Run<IReadOnlyList<Programme>>(db =>
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            WITH unique_programmes AS (SELECT channel_id,title,description,start,end FROM epg GROUP BY channel_id,start,end,title),
            ranked AS (SELECT *,ROW_NUMBER() OVER(PARTITION BY channel_id ORDER BY start) AS rn FROM unique_programmes WHERE end>$now)
            SELECT channel_id,title,description,start,end FROM ranked WHERE rn<=2 ORDER BY channel_id,start
            """;
        cmd.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds()); return ReadProgrammes(cmd, ct);
    }, ct);
    public Task<IReadOnlyList<Programme>> GuideAsync(string channelId, DateTimeOffset day, CancellationToken ct = default) => Run<IReadOnlyList<Programme>>(db =>
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT DISTINCT channel_id,title,description,start,end FROM epg WHERE channel_id=$id AND end>$from AND start<$until ORDER BY start LIMIT 500";
        cmd.Parameters.AddWithValue("$id", channelId); cmd.Parameters.AddWithValue("$from", day.ToUnixTimeSeconds()); cmd.Parameters.AddWithValue("$until", day.AddDays(1).ToUnixTimeSeconds()); return ReadProgrammes(cmd, ct);
    }, ct);
    public Task<DateTimeOffset?> EpgUpdatedAsync(CancellationToken ct = default) => Run<DateTimeOffset?>(db =>
    {
        using var cmd = db.CreateCommand(); cmd.CommandText = "SELECT MAX(updated) FROM epg_metadata"; return cmd.ExecuteScalar() is string value ? DateTimeOffset.Parse(value) : null;
    }, ct);
    private static List<Programme> ReadProgrammes(SqliteCommand cmd, CancellationToken ct)
    {
        var result = new List<Programme>(); using var reader = cmd.ExecuteReader();
        while (reader.Read()) { ct.ThrowIfCancellationRequested(); result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)), DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(4)))); }
        return result;
    }
}
