namespace EnesTV.Core.Models;

public sealed record ChannelLayout(string Name = "", int Order = int.MaxValue, bool Hidden = false);
public sealed record FavoriteList(string Id, string Name, HashSet<string> Channels)
{
    public override string ToString() => Name;
}
public sealed record LibraryLayout
{
    public Dictionary<string, ChannelLayout> Channels { get; init; } = [];
    public HashSet<string> HiddenCategories { get; init; } = [];
    public List<FavoriteList> FavoriteLists { get; init; } = [];
    public Dictionary<string, string> Shortcuts { get; init; } = [];
}
