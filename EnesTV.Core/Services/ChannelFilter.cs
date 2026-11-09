using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

public static class ChannelFilter
{
    public static bool Matches(Channel channel, string query, string? category = null) =>
        (category == null || TextSearch.Equals(channel.Category, category)) &&
        (TextSearch.Contains(channel.Name, query) || TextSearch.Contains(channel.Category, query));
}
