using EnesTV.Core.Services;
using Xunit;
namespace EnesTV.Tests;
public sealed class EpgTests
{
    [Theory]
    [InlineData("20260912210000 +0300", "2026-09-12T18:00:00Z")]
    [InlineData("20260912120000 -0530", "2026-09-12T17:30:00Z")]
    [InlineData("20260912210000", "2026-09-12T21:00:00Z")]
    public void ConvertsOffsets(string xml, string utc) => Assert.Equal(DateTimeOffset.Parse(utc), XmlTvParser.ParseTime(xml));
    [Theory][InlineData("bad")][InlineData("20261399210000 +0300")][InlineData("20260912210000 +1460")]
    public void BadTimeIsSkipped(string input) => Assert.Null(XmlTvParser.ParseTime(input));
    [Fact] public void ReadsConsecutiveProgrammesAndIgnoresUnmatchedChannels()
    {
        var path = Path.GetTempFileName(); var start = DateTimeOffset.UtcNow;
        try
        {
            var from = start.ToString("yyyyMMddHHmmss +0000"); var end = start.AddHours(1).ToString("yyyyMMddHHmmss +0000");
            File.WriteAllText(path, $"<tv><programme channel='a' start='{from}' stop='{end}'><title>One</title></programme><programme channel='a' start='{from}' stop='{end}'><title>Two</title></programme><programme channel='b' start='{from}' stop='{end}'><title>Other</title></programme></tv>");
            var programmes = XmlTvParser.ReadFile(path, new HashSet<string> { "a" }, default).ToArray(); Assert.Equal(2, programmes.Length); Assert.Equal("Two", programmes[1].Title);
        }
        finally { File.Delete(path); }
    }
}
