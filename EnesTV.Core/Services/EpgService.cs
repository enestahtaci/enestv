using System.Security.Cryptography;
using System.Text;
using EnesTV.Core.Data;
namespace EnesTV.Core.Services;
public sealed class EpgService(NetworkClient network, LibraryStore store)
{
    public async Task<int> RefreshAsync(string url, ISet<string> channelIds, CancellationToken ct)
    {
        var temporary = Path.GetTempFileName();
        try
        {
            await network.DownloadAsync(url, temporary, ct, 512L * 1024 * 1024);
            var feed = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
            return await store.ImportEpgAsync(temporary, feed, channelIds, ct);
        }
        finally { File.Delete(temporary); }
    }
}
