using System.Windows;
using System.Net.Http;
using EnesTV.App.Playback;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
namespace EnesTV.App.ViewModels;
public sealed class ChannelItem(Channel channel) : Observable
{
    public Channel Channel { get; } = channel;
    private string? _displayName;
    public string Name => _displayName ?? Channel.Name;
    private int _number;
    public int Number { get => _number; set => Set(ref _number, value); }
    public bool Hidden { get; set; }
    public int SortOrder { get; set; } = int.MaxValue;
    public void ApplyLayout(ChannelLayout layout) { _displayName = string.IsNullOrWhiteSpace(layout.Name) ? null : layout.Name; SortOrder = layout.Order; Hidden = layout.Hidden; Raise(nameof(Name)); }
    public string Category => Channel.Category;
    public string Logo => Channel.Logo;
    private bool _favorite;
    public bool Favorite { get => _favorite; set { if (Set(ref _favorite, value)) Raise(nameof(Star)); } }
    private bool _customListMember;
    public bool CustomListMember { get => _customListMember; set { if (Set(ref _customListMember, value)) Raise(nameof(Star)); } }
    public string Star => Favorite || CustomListMember ? "★" : "☆";
    private string _now = "Program bilgisi yok";
    public string Now { get => _now; set => Set(ref _now, value); }
}
public sealed partial class MainViewModel : Observable, IAsyncDisposable
{
    private readonly NetworkClient _network = new();
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _loading, _filtering;
    private List<ChannelItem> _all = [];
    private IReadOnlyList<string> _recent = [];
    private string _section = "Live TV", _search = "", _category = "Tüm kategoriler", _status = "Kaynak ekleyerek başlayın";
    private IReadOnlyList<ChannelItem> _channels = [];
    private IReadOnlyList<Source> _sources = [];
    private IReadOnlyList<string> _categories = ["Tüm kategoriler"];
    private ChannelItem? _selected;
    private Source? _selectedSource;
    private bool _busy;
    public PlaybackService Playback { get; } = new(App.Log);
    public string Status { get => _status; set => Set(ref _status, value); }
    public bool Busy { get => _busy; set { if (Set(ref _busy, value)) UpdateEmptyState(); } }
    public string Search { get => _search; set { if (Set(ref _search, value)) { UpdateEmptyState(); QueueFilter(); } } }
    public string Category { get => _category; set { if (Set(ref _category, value)) { UpdateEmptyState(); QueueFilter(); } } }
    public IReadOnlyList<string> Categories { get => _categories; private set => Set(ref _categories, value); }
    public IReadOnlyList<ChannelItem> Channels { get => _channels; private set { Set(ref _channels, value); Raise(nameof(ChannelCount)); } }
    public string ChannelCount => $"{Channels.Count:N0} kanal";
    public IReadOnlyList<Source> Sources { get => _sources; private set { Set(ref _sources, value); UpdateEmptyState(); } }
    public Source? SelectedSource { get => _selectedSource; set => Set(ref _selectedSource, value); }
    public ChannelItem? Selected { get => _selected; set => Set(ref _selected, value); }
    private ChannelItem? _playingItem;
    public ChannelItem? PlayingItem { get => _playingItem; private set { Set(ref _playingItem, value); Raise(nameof(CurrentName)); UpdateCurrentEpg(); } }
    public string CurrentName => PlayingItem?.Name ?? "Kanal seçilmedi";
    public string Section
    {
        get => _section;
        set
        {
            var wasVod = IsVodSection(_section);
            if (!Set(ref _section, value)) return;
            Raise(nameof(SectionTitle)); Raise(nameof(SectionDescription)); Raise(nameof(IsVod)); UpdateEmptyState();
            if (!IsVod) QueueFilter();
            if (wasVod || IsVod) Observe(SwitchModeAsync(wasVod));
        }
    }
    public string SectionTitle => Section switch { "Favorites" => "Favoriler", "Recent" => "Son izlenenler", "Categories" => "Kategoriler", "Movies" => "Filmler", "Series" => "Diziler", _ => "Canlı yayınlar" };
    public string SectionDescription => Section switch { "Favorites" => "Sevdiğin kanallar, elinin altında.", "Recent" => "Son izlediğin 20 kanal.", "Categories" => "İzlemek istediğin türe göz at.", "Movies" => "Film kütüphanen, kaldığın yerden.", "Series" => "Dizilerin, sezon sezon.", _ => "Kendi kaynakların, tek bir yerde." };
    public bool HasFilter => !string.IsNullOrWhiteSpace(Search) || Category != "Tüm kategoriler";
    public bool NeedsSource => Sources.Count == 0 && !Busy;
    public string EmptyTitle => Busy ? "Kanallar yükleniyor" : HasFilter ? "Sonuç bulunamadı" : NeedsSource ? "İlk kaynağını ekle" : Section switch { "Favorites" => "Favorilerin burada görünecek", "Recent" => "Henüz bir kanal izlemedin", _ => "Gösterilecek kanal yok" };
    public string EmptyDescription => Busy ? "Liste hazırlanırken biraz bekleyin. İşlemi alttan iptal edebilirsiniz." : HasFilter ? "Başka bir ad deneyin veya filtreleri temizleyin." : NeedsSource ? "M3U bağlantısı, dosya veya Xtream hesabıyla başlayın." : Section switch { "Favorites" => "Bir kanalın yanındaki yıldıza dokunarak bu listeye ekleyin.", "Recent" => "Canlı TV bölümünden bir kanal seçerek başlayın.", _ => "Kaynağı ayarlardan yenileyin veya gizlenen kanalları kontrol edin. Film ve diziler ayrı bölümlerdedir." };
    private void UpdateEmptyState() { Raise(nameof(HasFilter)); Raise(nameof(NeedsSource)); Raise(nameof(EmptyTitle)); Raise(nameof(EmptyDescription)); }
    public AsyncCommand ClearFilterCommand { get; }
    public AsyncCommand PlayCommand { get; }
    public AsyncCommand PauseCommand { get; }
    public AsyncCommand StopCommand { get; }
    public AsyncCommand ReconnectCommand { get; }
    public AsyncCommand NavigateCommand { get; }
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand CancelCommand { get; }
    private string _playbackText = "Hazır";
    public string PlaybackText { get => _playbackText; private set => Set(ref _playbackText, value); }
    private PlaybackState _playbackState;
    public string PauseIcon => _playbackState is PlaybackState.Playing or PlaybackState.Buffering ? "Pause" : "Play";
    public bool PlaybackError => _playbackState == PlaybackState.Error;
    private int _volume = 75;
    public int Volume { get => _volume; set { if (Set(ref _volume, value)) Observe(Playback.SetVolumeAsync(value)); } }
    private bool _muted;
    public bool Muted { get => _muted; set { if (Set(ref _muted, value)) Observe(Playback.SetMuteAsync(value)); } }
    public MainViewModel()
    {
        EnesTV.App.Views.LazyLogo.Cache = new EnesTV.App.Services.LogoCache(_network, System.IO.Path.Combine(App.DataRoot, "logos"));
        PlayCommand = new(async p => { if (p is ChannelItem item) await PlayChannelAsync(item); }, Error, true);
        PauseCommand = new(_ => Playback.TogglePauseAsync(), Error);
        StopCommand = new(async _ =>
        {
            if (PlayingItem != null) _previousChannelId = PlayingItem.Channel.Id;
            await Playback.StopAsync(); PlayingItem = null; // back to the quick-start screen
        }, Error);
        ReconnectCommand = new(_ => PlayingItem == null ? Task.CompletedTask : Playback.PlayAsync(PlayingItem.Channel), Error);
        NavigateCommand = new(p => { Section = p?.ToString() ?? "Live TV"; return Task.CompletedTask; }, Error);
        RefreshCommand = new(_ => SelectedSource == null ? Task.CompletedTask : RefreshAsync(SelectedSource), Error);
        CancelCommand = new(_ => { _loading?.Cancel(); return Task.CompletedTask; }, Error);
        ClearFilterCommand = new(_ => { Search = ""; Category = "Tüm kategoriler"; return Task.CompletedTask; }, Error);
        Playback.Changed += update => Dispatch(() => { PlaybackText = update.Message; _playbackState = update.State; Raise(nameof(PauseIcon)); Raise(nameof(PlaybackError)); });
        InitializeExtensions();
    }
    partial void InitializeExtensions();
    public async Task InitializeAsync() { await LoadLibraryAsync(); await LoadEpgAsync(); _ = EpgClockAsync(); }
    private async Task LoadLibraryAsync()
    {
        var library = await App.Store.LoadAsync(_lifetime.Token);
        Sources = library.Sources;
        _all = await Task.Run(() => library.Channels.Select(c => new ChannelItem(c) { Favorite = library.Favorites.Contains(c.Id) }).ToList(), _lifetime.Token);
        _recent = library.Recent;
        Layout = await App.Store.LoadLayoutAsync(_lifetime.Token);
        ApplyLayout();
        SelectedSource = Sources.FirstOrDefault(s => s.Id == SelectedSource?.Id) ?? Sources.FirstOrDefault();
        Status = $"{_all.Count:N0} kanal · {Sources.Count} kaynak · Yerel liste hazır";
        ApplySettings(library.Settings); await FilterAsync(false);
    }
    partial void ApplySettings(UserSettings settings);
    public async Task AddSourceAsync(string name, SourceKind kind, SourceSecret secret)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new UserInputException("Kaynağa bir ad verin.");
        if (kind == SourceKind.M3uUrl) NetworkClient.HttpUri(secret.Location);
        if (kind == SourceKind.Xtream) secret = secret with { Location = XtreamService.NormalizeServer(secret.Location) };
        await RefreshAsync(new Source(Guid.NewGuid().ToString("N"), name.Trim(), kind, secret));
    }
    public async Task RefreshAsync(Source source)
    {
        if (Busy) throw new UserInputException("Önce devam eden yüklemeyi bitirin veya iptal edin.");
        _loading?.Dispose(); _loading = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        Busy = true; Status = "Kaynak yükleniyor…";
        try
        {
            await new PlaylistService(_network, App.Store, App.Log).RefreshAsync(source, _loading.Token);
            await LoadLibraryAsync(); await LoadEpgAsync(); Status = _all.Count == 0 && source.Kind == SourceKind.Xtream ? "Hesap kaydedildi. Filmler veya Diziler bölümünü açabilirsiniz." : "Kanal listesi güncellendi";
        }
        catch (OperationCanceledException) when (_loading.IsCancellationRequested) { Status = "Yükleme iptal edildi; kayıtlı liste korundu."; }
        finally { Busy = false; }
    }
    private void QueueFilter() => Observe(FilterAsync(true));
    private async Task FilterAsync(bool debounce)
    {
        _filtering?.Cancel(); _filtering?.Dispose(); _filtering = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token); var ct = _filtering.Token;
        try
        {
            if (debounce) await Task.Delay(180, ct);
            var all = _all; var query = Search; var category = Category; var section = Section; var recent = _recent;
            var layout = Layout; var favoriteList = layout.FavoriteLists.FirstOrDefault(x => x.Id == SelectedFavoriteList?.Id);
            var result = await Task.Run(() =>
            {
                IEnumerable<ChannelItem> filtered = all.Where(x => !x.Hidden && !layout.HiddenCategories.Contains(x.Category) &&
                    (category == "Tüm kategoriler" || x.Category == category) &&
                    (string.IsNullOrWhiteSpace(query) || x.Name.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase) || x.Category.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(x => x.SortOrder);
                if (section == "Favorites") filtered = favoriteList == null ? filtered.Where(x => x.Favorite) : filtered.Where(x => favoriteList.Channels.Contains(x.Channel.Id));
                if (section == "Recent") { var rank = recent.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i); filtered = filtered.Where(x => rank.ContainsKey(x.Channel.Id)).OrderBy(x => rank[x.Channel.Id]); }
                ct.ThrowIfCancellationRequested(); return filtered.ToArray();
            }, ct);
            if (!ct.IsCancellationRequested)
            {
                foreach (var item in result) item.CustomListMember = section == "Favorites" && favoriteList != null;
                Channels = result;
            }
        }
        catch (OperationCanceledException) { }
    }
    public void Error(Exception ex)
    {
        App.Log.Write("operation.failed", ex);
        Status = ex switch
        {
            AuthenticationException => "Giriş reddedildi. Kullanıcı adı ve parolayı kontrol edin.",
            UserInputException => ex.Message,
            HttpRequestException => "Kaynağa ulaşılamadı. Bağlantınızı ve adresi kontrol edin; kayıtlı liste korundu.",
            OperationCanceledException => "Bağlantı zaman aşımına uğradı. Tekrar deneyin.",
            System.IO.FileNotFoundException => "Dosya bulunamadı. Kaynağın konumunu kontrol edin.",
            _ => "İşlem tamamlanamadı. Kayıtlı verileriniz korundu; lütfen tekrar deneyin."
        };
    }
    private async void Observe(Task task) { try { await task; } catch (OperationCanceledException) { } catch (Exception ex) { Error(ex); } }
    private static void Dispatch(Action action) { if (!Application.Current.Dispatcher.HasShutdownStarted) Application.Current.Dispatcher.BeginInvoke(action); }
    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel(); _loading?.Cancel(); _filtering?.Cancel();
        if (Vod != null) await Vod.DisposeAsync();
        await Playback.DisposeAsync(); _network.Dispose(); _loading?.Dispose(); _filtering?.Dispose(); _lifetime.Dispose();
    }
}
