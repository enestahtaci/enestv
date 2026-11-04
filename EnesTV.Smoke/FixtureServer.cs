using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace EnesTV.Smoke;
public sealed class FixtureServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly byte[] _video;
    private readonly byte[] _logo;
    private readonly Task _loop;
    public string Url { get; }
    public bool FailVodCatalog { get; set; }
    public bool EmptyLiveCatalog { get; set; }
    public FixtureServer(string videoPath, int port = 0)
    {
        _video = File.ReadAllBytes(videoPath); _logo = File.ReadAllBytes(videoPath + ".png"); _listener = new(IPAddress.Loopback, port); _listener.Start(); Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}"; _loop = AcceptAsync();
    }
    public string Playlist(int count = 10000) => "#EXTM3U\n" + string.Join('\n', Enumerable.Range(0, count).Select(i => $"#EXTINF:-1 tvg-id=\"test{i}\" tvg-logo=\"{Url}/logo.png\" group-title=\"Group {i % 20:00}\",Test Channel {i:00000}\n{Url}/video.avi?channel={i}"));
    private async Task AcceptAsync()
    {
        try { while (!_cts.IsCancellationRequested) { var client = await _listener.AcceptTcpClientAsync(_cts.Token); _ = HandleAsync(client); } }
        catch (OperationCanceledException) { }
    }
    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                using var stream = client.GetStream(); using var reader = new StreamReader(stream, Encoding.ASCII, false, 4096, true);
                var line = await reader.ReadLineAsync(_cts.Token); if (line == null) return;
                var path = line.Split(' ')[1]; string? range = null;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(_cts.Token))) if (line.StartsWith("Range:", StringComparison.OrdinalIgnoreCase)) range = line[6..].Trim();
                byte[] content; var status = "200 OK"; var type = "text/plain";
                if (path.StartsWith("/movie/test/test/missing")) { content = Encoding.UTF8.GetBytes("Not found"); status = "404 Not Found"; }
                else if (path.StartsWith("/video.avi") || path.StartsWith("/live/") || path.StartsWith("/movie/") || path.StartsWith("/series/")) { content = _video; type = "video/x-msvideo"; }
                else if (path.StartsWith("/logo.png")) { content = _logo; type = "image/png"; }
                else if (path.StartsWith("/playlist")) content = Encoding.UTF8.GetBytes(Playlist());
                else if (path.StartsWith("/redirect")) { var redirect = Encoding.ASCII.GetBytes($"HTTP/1.1 302 Found\r\nLocation: {Url}/playlist.m3u\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"); await stream.WriteAsync(redirect, _cts.Token); return; }
                else if (path.StartsWith("/player_api.php"))
                {
                    type = "application/json";
                    var body = path.Contains("username=wrong") ? "{\"user_info\":{\"auth\":0}}" : path.Contains("get_live_categories") ? "[{\"category_id\":\"1\",\"category_name\":\"Test\"}]" : path.Contains("get_live_streams") ? "[{\"stream_id\":1,\"name\":\"Local test\",\"epg_channel_id\":\"test0\",\"category_id\":\"1\"}]" : "{\"user_info\":{\"auth\":1,\"status\":\"Active\"}}";
                    if (!path.Contains("username=wrong"))
                    {
                        if (path.Contains("get_vod_categories") || path.Contains("get_series_categories")) body = "[{\"category_id\":\"1\",\"category_name\":\"Yerel test\"}]";
                        else if (path.Contains("get_vod_streams")) body = JsonSerializer.Serialize(Enumerable.Range(1, 10000).Select(i => new { stream_id = i, name = $"Test Film {i:00000}", category_id = "1", stream_icon = Url + "/logo.png", container_extension = "avi", rating = "8.0" }));
                        else if (path.Contains("get_vod_info")) body = JsonSerializer.Serialize(new { info = new { plot = "Bu video yalnızca enestv testleri için yerel olarak üretildi.", movie_image = Url + "/logo.png", rating = "8.0" }, movie_data = new { container_extension = "avi" } });
                        else if (path.Contains("get_series_info")) body = JsonSerializer.Serialize(new { info = new { plot = "Sentetik sezon ve bölüm verileri.", cover = Url + "/logo.png" }, episodes = new Dictionary<string, object[]> { ["2"] = [new { id = "201", episode_num = 1, title = "İkinci sezon", container_extension = "avi" }], ["1"] = [new { id = "101", episode_num = 1, title = "İlk bölüm", container_extension = "avi" }, new { id = "102", episode_num = 2, title = "İkinci bölüm", container_extension = "avi" }] } });
                        else if (path.Contains("get_series")) body = JsonSerializer.Serialize(new[] { new { series_id = 7, name = "Test Dizisi", category_id = "1", cover = Url + "/logo.png", plot = "Yerel test dizisi" } });
                    }
                    if (FailVodCatalog && path.Contains("get_vod_streams")) body = "{\"error\":\"fixture failure\"}";
                    if ((EmptyLiveCatalog || path.Contains("username=vodonly")) && (path.Contains("get_live_categories") || path.Contains("get_live_streams"))) body = "[]";
                    content = Encoding.UTF8.GetBytes(body);
                }
                else if (path.StartsWith("/xmltv.php") || path.StartsWith("/epg.xml"))
                {
                    var now = DateTimeOffset.UtcNow;
                    var programs = Enumerable.Range(0, 100).SelectMany(i => Enumerable.Range(0, 4).Select(n => $"<programme channel='test{i}' start='{now.AddMinutes(-30 + n * 60):yyyyMMddHHmmss} +0000' stop='{now.AddMinutes(30 + n * 60):yyyyMMddHHmmss} +0000'><title>Local test programme {n + 1}</title><desc>Generated locally for enestv verification. No external broadcast source.</desc></programme>"));
                    content = Encoding.UTF8.GetBytes("<tv>" + string.Join("", programs) + "</tv>"); type = "application/xml";
                }
                else { content = Encoding.UTF8.GetBytes("Not found"); status = "404 Not Found"; }
                var start = 0; var end = content.Length - 1; var rangeHeader = "";
                if (range?.StartsWith("bytes=") == true && int.TryParse(range[6..].Split('-')[0], out var requested) && requested >= 0 && requested < content.Length)
                {
                    start = requested; var endText = range[6..].Split('-').ElementAtOrDefault(1); if (int.TryParse(endText, out var requestedEnd)) end = Math.Min(end, requestedEnd);
                    status = "206 Partial Content"; rangeHeader = $"Content-Range: bytes {start}-{end}/{content.Length}\r\n";
                }
                var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: {type}\r\nContent-Length: {end - start + 1}\r\nAccept-Ranges: bytes\r\n{rangeHeader}Connection: close\r\n\r\n");
                await stream.WriteAsync(header, _cts.Token); await stream.WriteAsync(content.AsMemory(start, end - start + 1), _cts.Token);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or SocketException) { }
        }
    }
    public async ValueTask DisposeAsync() { _cts.Cancel(); _listener.Stop(); await _loop; _cts.Dispose(); }
}
