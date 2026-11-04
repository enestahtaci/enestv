using EnesTV.Core.Data;
using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

public sealed class PlaylistService(NetworkClient network, LibraryStore store, SafeLog log)
{
    public async Task<IReadOnlyList<Channel>> RefreshAsync(Source source, CancellationToken ct)
    {
        log.Write("playlist.fetch");
        var temporary = Path.GetTempFileName();
        try
        {
            IReadOnlyList<Channel> channels;
            if (source.Kind == SourceKind.Xtream)
                channels = await new XtreamService(network).LoadAsync(source, ct);
            else if (source.Kind == SourceKind.M3uFile)
                channels = await M3uParser.ParseFileAsync(source.Secret.Location, source.Id, new Uri(Path.GetFullPath(source.Secret.Location)), ct);
            else
            {
                await network.DownloadAsync(source.Secret.Location, temporary, ct);
                channels = await M3uParser.ParseFileAsync(temporary, source.Id, NetworkClient.HttpUri(source.Secret.Location), ct);
            }
            if (channels.Count == 0)
            {
                if (source.Kind != SourceKind.Xtream) throw new UserInputException("Kaynakta oynatılabilir kanal bulunamadı. Bir kanal listesi seçin.");
                // An authenticated account may provide only VOD. Keep existing live channels
                // if the provider temporarily returns an empty array during a refresh.
                var library = await store.LoadAsync(ct);
                channels = library.Channels.Where(x => x.SourceId == source.Id).ToArray();
            }
            await store.SaveSourceAsync(source with { Updated = DateTimeOffset.UtcNow }, channels, ct);
            log.Write("playlist.parsed"); return channels;
        }
        finally { File.Delete(temporary); }
    }
}
