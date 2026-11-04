using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;
using EnesTV.Core.Services;
namespace EnesTV.App.Services;
public sealed class LogoCache(NetworkClient network, string directory)
{
    private readonly SemaphoreSlim _downloads = new(4, 4);
    private readonly Dictionary<string, BitmapSource> _memory = [];
    private readonly Queue<string> _order = new();
    private readonly object _gate = new();
    public async Task<BitmapSource?> GetAsync(string url, CancellationToken ct)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) return null;
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        lock (_gate) { if (_memory.TryGetValue(key, out var cached)) return cached; }
        await _downloads.WaitAsync(ct);
        try
        {
            lock (_gate) { if (_memory.TryGetValue(key, out var cached)) return cached; }
            return await Task.Run(async () =>
            {
                Directory.CreateDirectory(directory); var path = Path.Combine(directory, key);
                if (!File.Exists(path) || File.GetLastWriteTimeUtc(path) < DateTime.UtcNow.AddDays(-7))
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(TimeSpan.FromSeconds(12));
                    var tmp = path + ".tmp-" + Guid.NewGuid().ToString("N");
                    try { await network.DownloadAsync(url, tmp, timeout.Token, 4 * 1024 * 1024); File.Move(tmp, path, true); }
                    finally { if (File.Exists(tmp)) File.Delete(tmp); }
                }
                ct.ThrowIfCancellationRequested();
                var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.DecodePixelWidth = 96;
                using (var input = File.OpenRead(path)) { image.StreamSource = input; image.EndInit(); }
                image.Freeze();
                lock (_gate)
                {
                    if (!_memory.ContainsKey(key)) { _memory[key] = image; _order.Enqueue(key); }
                    while (_order.Count > 200) _memory.Remove(_order.Dequeue());
                }
                foreach (var old in new DirectoryInfo(directory).EnumerateFiles().Where(f => !f.Name.Contains(".tmp-")).OrderByDescending(f => f.LastWriteTimeUtc).Skip(500))
                { try { old.Delete(); } catch (IOException) { } }
                return (BitmapSource)image;
            }, ct);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) { return null; }
        finally { _downloads.Release(); }
    }
}
