using EnesTV.Core.Services;
using Xunit;

namespace EnesTV.Tests;

public class ParserTests
{
    // Format rules (metadata, quotes, duplicates, malformed lines, encodings) are covered by FixtureTests.
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
