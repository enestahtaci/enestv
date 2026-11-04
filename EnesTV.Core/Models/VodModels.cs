namespace EnesTV.Core.Models;

public enum VodKind { Movie, Series }
public sealed record VodItem(string Id, string SourceId, VodKind Kind, string Name, string Category,
    string Poster = "", string Plot = "", string Year = "", string Rating = "", string Extension = "mp4")
{
    public string Key => $"{SourceId}:{Kind}:{Id}";
    public string Summary => string.Join(" · ", new[] { Year, Rating.Length > 0 ? "Puan: " + Rating : "", Category }.Where(x => x.Length > 0));
}
public sealed record VodEpisode(string Id, int Season, int Number, string Name, string Extension = "mp4", string Plot = "", string Poster = "")
{
    public string Caption => $"S{Season:00} B{Number:00} · {Name}";
}
public sealed record VodDetails(VodItem Item, IReadOnlyList<VodEpisode> Episodes);
public sealed record VodCatalog(IReadOnlyList<VodItem> Items, DateTimeOffset Updated);
public sealed record WatchProgress(string Key, long PositionMs, long DurationMs, bool Completed, DateTimeOffset Updated)
{
    public long ResumeMs => Completed || PositionMs < 5000 ? 0 : PositionMs;
}
