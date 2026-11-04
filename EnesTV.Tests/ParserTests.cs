using EnesTV.Core.Services;
using Xunit;

namespace EnesTV.Tests;

public class ParserTests
{
    [Fact] public void ReadsMetadataCommasAndDuplicates()
    {
        const string text = "#EXTM3U\n#EXTINF:-1 tvg-id=\"one\" tvg-name=\"One\" tvg-logo=\"logo.png\" group-title=\"News, Live\",One HD\nhttps://example.invalid/1\n#EXTINF:-1,One HD\nhttps://example.invalid/1";
        var channels = M3uParser.Parse(new StringReader(text), "s", new Uri("https://example.invalid/list"));
        var c = Assert.Single(channels); Assert.Equal("one", c.TvgId); Assert.Equal("News, Live", c.Category); Assert.Equal("https://example.invalid/logo.png", c.Logo);
    }
    [Fact] public void MalformedMetadataDoesNotLoseFollowingChannels()
    {
        var channels = M3uParser.Parse(new StringReader("#EXTINF:garbage tvg-name=\"Broken\nhttps://example.invalid/1\n#EXTINF:-1,Good\nhttps://example.invalid/2"), "s");
        Assert.Equal(2, channels.Count); Assert.Equal("Good", channels[1].Name); Assert.Equal("Uncategorized", channels[1].Category);
    }
    [Fact] public void HandlesTenThousandChannels()
    {
        var channels = M3uParser.Parse(new StringReader(Fixtures.LargePlaylist()), "large"); Assert.Equal(10000, channels.Count);
        Assert.True(ChannelFilter.Matches(channels[20], "CHANNEL 20")); Assert.True(ChannelFilter.Matches(channels[20], "GROUP")); Assert.False(ChannelFilter.Matches(channels[20], "absent"));
    }
    [Fact] public void CancellationIsObserved()
    {
        using var cts = new CancellationTokenSource(); cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => M3uParser.Parse(new StringReader(Fixtures.LargePlaylist()), "s", ct: cts.Token));
    }
}
public static class Fixtures
{
    public static string LargePlaylist(int count = 10000) => "#EXTM3U\n" + string.Join('\n', Enumerable.Range(0, count).Select(i => $"#EXTINF:-1 tvg-id=\"test{i}\" group-title=\"Group {i % 20}\",Channel {i}\nhttp://127.0.0.1:18765/stream/{i}"));
}
