using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

public static class ChannelFilter
{
    public static bool Matches(Channel channel, string query, string? category = null) =>
        (category == null || channel.Category.Equals(category, StringComparison.OrdinalIgnoreCase)) &&
        (string.IsNullOrWhiteSpace(query) || channel.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) || channel.Category.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase));
}
