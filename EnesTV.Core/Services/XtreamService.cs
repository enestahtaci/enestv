using System.Text.Json;
using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

public sealed partial class XtreamService(NetworkClient network)
{
    public static string NormalizeServer(string input)
    {
        var text = input.Trim();
        if (!text.Contains("://", StringComparison.Ordinal)) text = "https://" + text;
        var uri = NetworkClient.HttpUri(text);
        if (!string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)) throw new UserInputException("Sunucu adresine kullanıcı bilgisi, sorgu veya # eklemeyin.");
        var builder = new UriBuilder(uri) { Path = uri.AbsolutePath.TrimEnd('/') };
        if (builder.Path.EndsWith("/player_api.php", StringComparison.OrdinalIgnoreCase)) builder.Path = builder.Path[..^15];
        return builder.Uri.AbsoluteUri.TrimEnd('/');
    }
    private static string Query(SourceSecret secret) => $"username={Uri.EscapeDataString(secret.Username)}&password={Uri.EscapeDataString(secret.Password)}";
    public static string EpgUrl(SourceSecret secret) => $"{NormalizeServer(secret.Location)}/xmltv.php?{Query(secret)}";
    private static string ApiUrl(SourceSecret secret, string action = "") => $"{NormalizeServer(secret.Location)}/player_api.php?{Query(secret)}" + (action.Length == 0 ? "" : "&action=" + action);
    public async Task<IReadOnlyList<Channel>> LoadAsync(Source source, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(source.Secret.Username) || string.IsNullOrEmpty(source.Secret.Password)) throw new UserInputException("Kullanıcı adı ve parola gerekli.");
        using (var auth = await GetJsonAsync(ApiUrl(source.Secret), ct))
        {
            if (!auth.RootElement.TryGetProperty("user_info", out var info) || Value(info, "auth") != "1" || (Value(info, "status") is var status && status.Length > 0 && !status.Equals("Active", StringComparison.OrdinalIgnoreCase))) throw new AuthenticationException();
        }
        using var categories = await GetJsonAsync(ApiUrl(source.Secret, "get_live_categories"), ct);
        using var streams = await GetJsonAsync(ApiUrl(source.Secret, "get_live_streams"), ct);
        return await Task.Run<IReadOnlyList<Channel>>(() =>
        {
            var groups = new Dictionary<string, string>();
            if (categories.RootElement.ValueKind == JsonValueKind.Array) foreach (var c in categories.RootElement.EnumerateArray()) groups[Value(c, "category_id")] = Value(c, "category_name");
            if (streams.RootElement.ValueKind != JsonValueKind.Array) throw new UserInputException("Sunucu geçerli bir canlı kanal listesi döndürmedi.");
            var list = new List<Channel>(); var seen = new HashSet<string>(); var secret = source.Secret;
            foreach (var item in streams.RootElement.EnumerateArray())
            {
                ct.ThrowIfCancellationRequested(); var id = Value(item, "stream_id"); if (id.Length == 0) continue;
                var name = Value(item, "name"); if (name.Length == 0) name = "Kanal " + id;
                var url = $"{NormalizeServer(secret.Location)}/live/{Uri.EscapeDataString(secret.Username)}/{Uri.EscapeDataString(secret.Password)}/{Uri.EscapeDataString(id)}.ts";
                var key = Channel.Key(source.Id, id, name);
                if (seen.Add(key)) list.Add(new(key, source.Id, name, url, Value(item, "epg_channel_id"), name, Value(item, "stream_icon"), groups.GetValueOrDefault(Value(item, "category_id"), "Uncategorized")));
            }
            return list;
        }, ct);
    }
    private async Task<JsonDocument> GetJsonAsync(string url, CancellationToken ct)
    {
        var temporary = Path.GetTempFileName();
        try
        {
            await network.DownloadAsync(url, temporary, ct, 128L * 1024 * 1024);
            await using var stream = File.OpenRead(temporary);
            var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("user_info", out var info) && Value(info, "auth") == "0")
            { document.Dispose(); throw new AuthenticationException(); }
            return document;
        }
        finally { File.Delete(temporary); }
    }
    private static string Value(JsonElement e, string name) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) ? value.ToString() : "";
}
