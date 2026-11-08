using EnesTV.Core.Models;
using EnesTV.Core.Services;

namespace EnesTV.App.ViewModels;

public sealed partial class MainViewModel
{
    public LibraryLayout Layout { get; private set; } = new();
    public IReadOnlyList<FavoriteList> FavoriteLists { get; private set; } = [];
    private FavoriteList? _selectedFavoriteList;
    private string? _previousChannelId;
    private readonly SemaphoreSlim _layoutGate = new(1, 1);
    public FavoriteList? SelectedFavoriteList
    {
        get => _selectedFavoriteList;
        set { if (Set(ref _selectedFavoriteList, value)) QueueFilter(); }
    }
    public AsyncCommand NextChannelCommand { get; private set; } = null!;
    public AsyncCommand PreviousChannelCommand { get; private set; } = null!;
    public AsyncCommand LastChannelCommand { get; private set; } = null!;

    private void InitializeNavigation()
    {
        NextChannelCommand = new(_ => StepChannelAsync(1), Error, true);
        PreviousChannelCommand = new(_ => StepChannelAsync(-1), Error, true);
        LastChannelCommand = new(_ => ReturnToLastChannelAsync(), Error, true);
    }
    private void ApplyLayout()
    {
        foreach (var item in _all) item.ApplyLayout(Layout.Channels.GetValueOrDefault(item.Channel.Id) ?? new());
        var ordered = _all.Where(x => !x.Hidden && !Layout.HiddenCategories.Contains(x.Category)).OrderBy(x => x.SortOrder).ToArray();
        for (var i = 0; i < ordered.Length; i++) ordered[i].Number = i + 1;
        Categories = new[] { "Tüm kategoriler" }.Concat(ordered.Select(x => x.Category).Distinct().Order()).ToArray();
        if (!Categories.Contains(Category)) _category = "Tüm kategoriler";
        Raise(nameof(Category));
        var selectedId = _selectedFavoriteList?.Id;
        FavoriteLists = new[] { new FavoriteList("", "★ Genel favoriler", []) }.Concat(Layout.FavoriteLists).ToArray();
        _selectedFavoriteList = FavoriteLists.FirstOrDefault(x => x.Id == selectedId) ?? FavoriteLists[0];
        Raise(nameof(FavoriteLists)); Raise(nameof(SelectedFavoriteList)); Raise(nameof(Layout));
        if (PlayingItem != null) PlayingItem = _all.FirstOrDefault(x => x.Channel.Id == PlayingItem.Channel.Id) ?? PlayingItem;
        Raise(nameof(CurrentName));
        UpdateQuickAccess();
    }
    public async Task SaveOrganizationAsync(LibraryLayout edited)
    {
        await _layoutGate.WaitAsync(_lifetime.Token);
        try
        {
            // A dialog edits a snapshot. Preserve shortcut changes made independently.
            var next = edited with { Shortcuts = Layout.Shortcuts };
            await App.Store.SaveLayoutAsync(next, _lifetime.Token);
            Layout = next; ApplyLayout(); await FilterAsync(false);
            Status = "Kanal düzeni ve favori listeleri kaydedildi";
        }
        finally { _layoutGate.Release(); }
    }
    public async Task SaveShortcutsAsync(Dictionary<string, string> shortcuts)
    {
        await _layoutGate.WaitAsync(_lifetime.Token);
        try
        {
            var next = Layout with { Shortcuts = new(shortcuts) };
            await App.Store.SaveLayoutAsync(next, _lifetime.Token); Layout = next;
            Raise(nameof(Layout)); Status = "Kısayollar kaydedildi";
        }
        finally { _layoutGate.Release(); }
    }
    public async Task PlayChannelAsync(ChannelItem item)
    {
        if (item.Hidden || Layout.HiddenCategories.Contains(item.Category)) return;
        if (PlayingItem != null && PlayingItem.Channel.Id != item.Channel.Id) _previousChannelId = PlayingItem.Channel.Id;
        Selected = item; PlayingItem = item;
        await Playback.PlayAsync(item.Channel);
    }
    public Task StepChannelAsync(int direction)
    {
        var visible = Channels; if (visible.Count == 0) return Task.CompletedTask;
        var index = -1;
        for (var i = 0; i < visible.Count; i++) if (visible[i].Channel.Id == PlayingItem?.Channel.Id) { index = i; break; }
        var next = index < 0 ? direction > 0 ? 0 : visible.Count - 1 : (index + direction + visible.Count) % visible.Count;
        return PlayChannelAsync(visible[next]);
    }
    public Task ReturnToLastChannelAsync()
    {
        var item = _all.FirstOrDefault(x => x.Channel.Id == _previousChannelId && !x.Hidden && !Layout.HiddenCategories.Contains(x.Category));
        if (item == null) { Status = "Dönülebilecek önceki kanal yok"; return Task.CompletedTask; }
        return PlayChannelAsync(item);
    }
    public async Task<bool> PlayNumberAsync(int number)
    {
        var item = _all.FirstOrDefault(x => x.Number == number && !x.Hidden && !Layout.HiddenCategories.Contains(x.Category));
        if (item == null) { Status = $"{number} numaralı kanal yok"; return false; }
        await PlayChannelAsync(item); return true;
    }
}
