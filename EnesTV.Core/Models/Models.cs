using System.Security.Cryptography;
using System.Text;

namespace EnesTV.Core.Models;

public enum SourceKind { M3uUrl, M3uFile, Xtream }
public sealed record SourceSecret(string Location, string Username = "", string Password = "");
public sealed record Source(string Id, string Name, SourceKind Kind, SourceSecret Secret, DateTimeOffset? Updated = null)
{
    public override string ToString() => Name;
    public string UpdatedLabel => Updated is { } time ? time.LocalDateTime.ToString("g") : "Henüz güncellenmedi";
    public string KindLabel => Kind switch { SourceKind.M3uUrl => "M3U URL", SourceKind.M3uFile => "Yerel M3U", _ => "Xtream Codes" };
}
public sealed record Channel(string Id, string SourceId, string Name, string Url, string TvgId = "", string TvgName = "", string Logo = "", string Category = "Uncategorized")
{
    public static string Key(string source, string url, string name) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source + "\n" + url + "\n" + name)));
}
public sealed record Programme(string ChannelId, string Title, string Description, DateTimeOffset Start, DateTimeOffset End)
{
    public string TimeRange => $"{Start.LocalDateTime:HH:mm} – {End.LocalDateTime:HH:mm}";
}
public sealed record UserSettings(string Theme = "Dark", int DefaultVolume = 75, bool AutoReconnect = true, string EpgUrl = "");
public sealed record Library(IReadOnlyList<Source> Sources, IReadOnlyList<Channel> Channels, HashSet<string> Favorites, IReadOnlyList<string> Recent, UserSettings Settings);
