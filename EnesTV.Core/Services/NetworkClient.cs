using System.Net;

namespace EnesTV.Core.Services;

public sealed class AuthenticationException : Exception;
public sealed class UserInputException(string message) : Exception(message);

public sealed class NetworkClient : IDisposable
{
    private readonly HttpClient _client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = true, MaxAutomaticRedirections = 8,
        AutomaticDecompression = DecompressionMethods.All,
        ConnectTimeout = TimeSpan.FromSeconds(12), PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    }) { Timeout = Timeout.InfiniteTimeSpan };

    public NetworkClient() => _client.DefaultRequestHeaders.UserAgent.ParseAdd("enestv/1.0");
    public static Uri HttpUri(string input)
    {
        if (!Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new UserInputException("Geçerli bir http:// veya https:// adresi girin.");
        return uri;
    }
    public async Task DownloadAsync(string url, string destination, CancellationToken ct, long maxBytes = 256L * 1024 * 1024)
    {
        var uri = HttpUri(url);
        for (var attempt = 0; ; attempt++)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromMinutes(2));
            try
            {
                using var response = await _client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new AuthenticationException();
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentLength > maxBytes) throw new UserInputException("Kaynak izin verilen boyuttan büyük.");
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
                await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);
                var buffer = new byte[65536]; long total = 0; int n;
                while ((n = await stream.ReadAsync(buffer, timeout.Token).ConfigureAwait(false)) != 0)
                {
                    total += n; if (total > maxBytes) throw new UserInputException("Kaynak izin verilen boyuttan büyük.");
                    await output.WriteAsync(buffer.AsMemory(0, n), timeout.Token).ConfigureAwait(false);
                }
                return;
            }
            catch (HttpRequestException ex) when (attempt < 2 && (ex.StatusCode is null || (int)ex.StatusCode >= 500 || ex.StatusCode == HttpStatusCode.RequestTimeout || (int)ex.StatusCode == 429))
            { await Task.Delay(TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt)), ct).ConfigureAwait(false); }
        }
    }
    public void Dispose() => _client.Dispose();
}
