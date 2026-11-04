using EnesTV.Core.Services;
using Xunit;
namespace EnesTV.Tests;
public sealed class XtreamTests
{
    [Theory]
    [InlineData("example.com", "https://example.com")]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("https://example.com/", "https://example.com")]
    [InlineData(" https://example.com:8443/sub/ ", "https://example.com:8443/sub")]
    [InlineData("https://example.com/player_api.php", "https://example.com")]
    public void NormalizesServer(string input, string expected) => Assert.Equal(expected, XtreamService.NormalizeServer(input));
    [Theory]
    [InlineData("file:///c:/test")][InlineData("https://user:pass@example.com")][InlineData("https://example.com?password=a")][InlineData("https://example.com#fragment")]
    public void RejectsUnsafeServer(string input) => Assert.Throws<UserInputException>(() => XtreamService.NormalizeServer(input));
}
