using EnesTV.Core.Models;
using EnesTV.Core.Services;
namespace EnesTV.App.ViewModels;
public sealed partial class MainViewModel
{
    public UserSettings Settings { get; private set; } = new();
    private string? _appliedTheme;
    public AsyncCommand FavoriteCommand { get; private set; } = null!;
    partial void InitializeExtensions()
    {
        InitializeNavigation();
        FavoriteCommand = new(async p =>
        {
            if (p is not ChannelItem item) return;
            var list = Section == "Favorites" ? Layout.FavoriteLists.FirstOrDefault(x => x.Id == SelectedFavoriteList?.Id) : null;
            if (list != null)
            {
                var members = new HashSet<string>(list.Channels);
                if (!members.Remove(item.Channel.Id)) members.Add(item.Channel.Id);
                await SaveOrganizationAsync(Layout with { FavoriteLists = Layout.FavoriteLists.Select(x => x.Id == list.Id ? x with { Channels = members } : x).ToList() });
                return;
            }
            await App.Store.SetFavoriteAsync(item.Channel.Id, !item.Favorite, _lifetime.Token);
            item.Favorite = !item.Favorite; UpdateQuickAccess();
            if (Section == "Favorites") await FilterAsync(false);
        }, Error);
        Playback.Started += id => Dispatch(() => Observe(RecordRecentAsync(id)));
    }
    private async Task RecordRecentAsync(string id)
    {
        if (_recent.FirstOrDefault() == id || !_all.Any(x => x.Channel.Id == id)) return;
        await App.Store.AddRecentAsync(id, _lifetime.Token);
        _recent = new[] { id }.Concat(_recent.Where(x => x != id)).Take(20).ToArray(); UpdateQuickAccess();
        if (Section == "Recent") await FilterAsync(false);
    }
    partial void ApplySettings(UserSettings settings)
    {
        Settings = settings; Raise(nameof(Settings)); Volume = settings.DefaultVolume; Playback.AutoReconnect = settings.AutoReconnect;
        if (_appliedTheme != settings.Theme) { EnesTV.App.Views.Theme.Apply(settings.Theme); _appliedTheme = settings.Theme; }
    }
    public async Task SaveSettingsAsync(UserSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.EpgUrl)) NetworkClient.HttpUri(settings.EpgUrl);
        await App.Store.SaveSettingsAsync(settings, _lifetime.Token); ApplySettings(settings); Status = "Ayarlar kaydedildi";
    }
    public async Task RenameSourceAsync(Source source, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new UserInputException("Kaynak adı boş bırakılamaz.");
        await App.Store.RenameSourceAsync(source.Id, name.Trim(), _lifetime.Token); await LoadLibraryAsync();
    }
    public async Task DeleteSourceAsync(Source source)
    {
        if (Busy) throw new UserInputException("Önce devam eden yüklemeyi bitirin veya iptal edin.");
        if (PlayingItem?.Channel.SourceId == source.Id) { await Playback.StopAsync(); PlayingItem = null; Selected = null; }
        await App.Store.DeleteSourceAsync(source.Id, _lifetime.Token); await LoadLibraryAsync();
    }
    public async Task UpdateSourceAsync(Source source, string name, SourceSecret secret)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new UserInputException("Kaynak adı boş bırakılamaz.");
        if (source.Kind == SourceKind.Xtream) secret = secret with { Location = XtreamService.NormalizeServer(secret.Location) };
        await RefreshAsync(source with { Name = name.Trim(), Secret = secret });
    }
}
