using System.Globalization;
using System.Text.Json;
using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

public sealed partial class XtreamService
{
    public async Task<VodCatalog> LoadCatalogAsync(Source source, VodKind kind, CancellationToken ct)
    {
        if (source.Kind != SourceKind.Xtream) throw new UserInputException("Film ve dizi kataloğu için Xtream hesabı ekleyin.");
        var prefix = kind == VodKind.Movie ? "vod" : "series";
        using var categories = await GetJsonAsync(ApiUrl(source.Secret, $"get_{prefix}_categories"), ct);
        using var items = await GetJsonAsync(ApiUrl(source.Secret, kind == VodKind.Movie ? "get_vod_streams" : "get_series"), ct);
        return await Task.Run(() => new VodCatalog(ParseCatalog(source.Id, kind, categories.RootElement, items.RootElement, ct), DateTimeOffset.UtcNow), ct);
    }
    public static IReadOnlyList<VodItem> ParseCatalog(string sourceId, VodKind kind, JsonElement categories, JsonElement items, CancellationToken ct = default)
    {
        if (items.ValueKind != JsonValueKind.Array) throw new UserInputException("Sunucu geçerli bir içerik listesi döndürmedi. Hesabınızı ve içerik erişiminizi kontrol edin.");
        var groups = new Dictionary<string, string>();
        if (categories.ValueKind == JsonValueKind.Array) foreach (var c in categories.EnumerateArray()) groups[Value(c, "category_id")] = Value(c, "category_name");
        var result = new List<VodItem>(); var seen = new HashSet<string>();
        foreach (var item in items.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested(); var id = Value(item, kind == VodKind.Movie ? "stream_id" : "series_id"); if (id.Length == 0 || !seen.Add(id)) continue;
            result.Add(new(id, sourceId, kind, First(Value(item, "name"), "İsimsiz içerik"),
                groups.GetValueOrDefault(Value(item, "category_id"), "Diğer"),
                First(Value(item, "cover"), Value(item, "stream_icon")), Value(item, "plot"),
                First(Value(item, "year"), Value(item, "releaseDate"), Value(item, "release_date")),
                Value(item, "rating"), Extension(Value(item, "container_extension"))));
        }
        return result;
    }
    public async Task<VodDetails> LoadDetailsAsync(Source source, VodItem item, CancellationToken ct)
    {
        var action = item.Kind == VodKind.Movie ? "get_vod_info&vod_id=" : "get_series_info&series_id=";
        using var json = await GetJsonAsync(ApiUrl(source.Secret, action + Uri.EscapeDataString(item.Id)), ct);
        return await Task.Run(() => ParseDetails(item, json.RootElement), ct);
    }
    public static VodDetails ParseDetails(VodItem item, JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object) throw new UserInputException("İçerik ayrıntıları sunucudan alınamadı.");
        var info = Child(root, "info"); var movie = Child(root, "movie_data");
        var updated = item with
        {
            Plot = First(Value(info, "plot"), Value(info, "description"), item.Plot),
            Poster = First(Value(info, "cover"), Value(info, "movie_image"), item.Poster),
            Year = First(Value(info, "releasedate"), Value(info, "releaseDate"), Value(info, "release_date"), item.Year),
            Rating = First(Value(info, "rating"), item.Rating),
            Extension = Extension(First(Value(movie, "container_extension"), item.Extension))
        };
        var episodes = new List<VodEpisode>(); var seen = new HashSet<string>();
        void AddEpisodes(JsonElement list, int season)
        {
            if (list.ValueKind != JsonValueKind.Array) return;
            foreach (var entry in list.EnumerateArray())
            {
                var id = Value(entry, "id"); if (id.Length == 0 || !seen.Add(id)) continue;
                var details = Child(entry, "info");
                episodes.Add(new(id, Number(Value(entry, "season"), season), Number(Value(entry, "episode_num"), episodes.Count + 1),
                    First(Value(entry, "title"), Value(entry, "name"), "Bölüm"), Extension(Value(entry, "container_extension")),
                    First(Value(details, "plot"), Value(details, "description")), First(Value(details, "movie_image"), updated.Poster)));
            }
        }
        var data = Child(root, "episodes");
        if (data.ValueKind == JsonValueKind.Object) foreach (var group in data.EnumerateObject()) AddEpisodes(group.Value, Number(group.Name, 1));
        else if (data.ValueKind == JsonValueKind.Array)
        {
            if (data.GetArrayLength() > 0 && data[0].ValueKind == JsonValueKind.Array) { var season = 1; foreach (var group in data.EnumerateArray()) AddEpisodes(group, season++); }
            else AddEpisodes(data, 1);
        }
        return new(updated, episodes.OrderBy(x => x.Season).ThenBy(x => x.Number).ToArray());
    }
    public static string VodStreamUrl(SourceSecret secret, string id, string extension, bool episode)
        => $"{NormalizeServer(secret.Location)}/{(episode ? "series" : "movie")}/{Uri.EscapeDataString(secret.Username)}/{Uri.EscapeDataString(secret.Password)}/{Uri.EscapeDataString(id)}.{Extension(extension)}";
    public static string EpisodeKey(string source, string series, string episode) => $"{source}:Series:{series}:Episode:{episode}";
    private static string Extension(string value) => value.Length is > 0 and <= 8 && value.All(char.IsAsciiLetterOrDigit) ? value.ToLowerInvariant() : "mp4";
    private static string First(params string[] values) => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
    private static JsonElement Child(JsonElement e, string key) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(key, out var value) ? value : default;
    private static int Number(string value, int fallback) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n >= 0 ? n : fallback;
}
