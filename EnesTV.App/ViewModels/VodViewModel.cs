using System.Windows;
using EnesTV.App.Playback;
using EnesTV.Core.Models;
using EnesTV.Core.Services;

namespace EnesTV.App.ViewModels;

public sealed class EpisodeRow(VodEpisode episode, bool watched)
{
    public VodEpisode Episode { get; } = episode;
    public string Caption => (watched ? "✓ " : "") + Episode.Caption;
}

public sealed class VodViewModel : Observable, IAsyncDisposable
{
    private readonly NetworkClient _network = new();
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _catalogRequest, _detailRequest, _filterRequest;
    private readonly SemaphoreSlim _playGate = new(1, 1), _saveGate = new(1, 1);
    private readonly Dictionary<string, WatchProgress> _progress = [];
    private IReadOnlyList<VodItem> _all = [];
    private IReadOnlyList<VodEpisode> _episodes = [];
    private Source? _source;
    private VodKind _kind;
    private string _search = "", _category = "Tüm kategoriler", _status = "Xtream hesabı seçin", _playbackText = "İzlemek için bir içerik seçin";
    private bool _busy, _detailBusy, _disposed, _tickBusy, _transitioning;
    private VodItem? _selected, _detail;
    private IReadOnlyList<VodItem> _items = [];
    private IReadOnlyList<string> _categories = ["Tüm kategoriler"];
    private IReadOnlyList<int> _seasons = [];
    private IReadOnlyList<EpisodeRow> _episodeRows = [];
    private EpisodeRow? _selectedEpisode;
    private int _season, _volume;
    private bool _muted;
    private double _position, _duration;
    private bool _canSeek;
    private DateTimeOffset _lastSaved;
    private sealed record Playing(Source Source, VodItem Item, VodEpisode? Episode, IReadOnlyList<VodEpisode> Episodes)
    {
        public string Key => Episode == null ? Item.Key : XtreamService.EpisodeKey(Source.Id, Item.Id, Episode.Id);
        public string Name => Episode == null ? Item.Name : Item.Name + " · " + Episode.Caption;
    }
    private Playing? _playing;
    public PlaybackService Playback { get; } = new(App.Log, onDemand: true);
    public IReadOnlyList<Source> Sources { get; }
    public Source? Source { get => _source; set { if (Set(ref _source, value)) Observe(LoadCatalogAsync(false)); } }
    public VodKind Kind { get => _kind; set { if (Set(ref _kind, value)) { Raise(nameof(IsSeries)); Raise(nameof(IsMovie)); Observe(LoadCatalogAsync(false)); } } }
    public bool IsSeries => Kind == VodKind.Series;
    public bool IsMovie => Kind == VodKind.Movie;
    public bool Busy { get => _busy; private set { Set(ref _busy, value); UpdateEmptyState(); Raise(nameof(CountText)); } }
    public bool DetailBusy { get => _detailBusy; private set { Set(ref _detailBusy, value); Raise(nameof(CanPlay)); } }
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string PlaybackText { get => _playbackText; private set => Set(ref _playbackText, value); }
    public string Search { get => _search; set { if (Set(ref _search, value)) { UpdateEmptyState(); Observe(FilterAsync()); } } }
    public string Category { get => _category; set { if (Set(ref _category, value)) { UpdateEmptyState(); Observe(FilterAsync()); } } }
    public IReadOnlyList<string> Categories { get => _categories; private set => Set(ref _categories, value); }
    public IReadOnlyList<VodItem> Items { get => _items; private set { Set(ref _items, value); Raise(nameof(CountText)); UpdateEmptyState(); } }
    public bool IsEmpty => Items.Count == 0;
    public string EmptyTitle => Busy ? "Katalog yükleniyor" : Source == null ? "Xtream hesabı gerekli" : !string.IsNullOrWhiteSpace(Search) || Category != "Tüm kategoriler" ? "Sonuç bulunamadı" : "Katalog boş";
    public string EmptyDescription => Busy ? "İçerikler hazırlanıyor. İşlemi yukarıdan iptal edebilirsiniz." : Source == null ? "Canlı TV'ye dönüp Kaynak ekle üzerinden Xtream hesabınızı ekleyin. Film ve dizi kataloğu bu hesapla açılır." : !string.IsNullOrWhiteSpace(Search) || Category != "Tüm kategoriler" ? "Aramayı değiştirin veya tüm kategorileri seçin." : "Kataloğu yenileyin veya başka bir hesap seçin. İçerikler hesabınızın erişimine bağlıdır.";
    private void UpdateEmptyState() { Raise(nameof(IsEmpty)); Raise(nameof(EmptyTitle)); Raise(nameof(EmptyDescription)); }
    private PlaybackState _playbackState;
    public string PauseIcon => _playbackState is PlaybackState.Playing or PlaybackState.Buffering ? "Pause" : "Play";
    public bool ShowPlayerHint => _playbackState == PlaybackState.Idle;
    public bool PlaybackError => _playbackState == PlaybackState.Error;
    public string CountText => Busy && Items.Count == 0 ? "Katalog yükleniyor…" : $"{Items.Count:N0} {(IsSeries ? "dizi" : "film")}";
    public VodItem? Selected { get => _selected; set { if (Set(ref _selected, value)) Observe(LoadDetailsAsync()); } }
    public VodItem? Detail { get => _detail; private set { Set(ref _detail, value); Raise(nameof(CanPlay)); Raise(nameof(ResumeLabel)); } }
    public IReadOnlyList<int> Seasons { get => _seasons; private set => Set(ref _seasons, value); }
    public int Season { get => _season; set { if (Set(ref _season, value)) UpdateEpisodes(); } }
    public IReadOnlyList<EpisodeRow> Episodes { get => _episodeRows; private set => Set(ref _episodeRows, value); }
    public EpisodeRow? SelectedEpisode { get => _selectedEpisode; set { if (Set(ref _selectedEpisode, value)) { Raise(nameof(CanPlay)); Raise(nameof(ResumeLabel)); Raise(nameof(EpisodePlot)); } } }
    public string EpisodePlot => SelectedEpisode?.Episode.Plot ?? "";
    public bool CanPlay => !DetailBusy && Detail != null && (!IsSeries || SelectedEpisode != null);
    private string? SelectionKey => Detail == null ? null : !IsSeries ? Detail.Key : SelectedEpisode == null ? null : XtreamService.EpisodeKey(Detail.SourceId, Detail.Id, SelectedEpisode.Episode.Id);
    public string ResumeLabel => SelectionKey is { } key && _progress.TryGetValue(key, out var p) ? p.Completed ? "İzlendi · yeniden oynatabilirsiniz" : p.ResumeMs > 0 ? $"Kaldığınız yer: {FormatTime(p.ResumeMs / 1000d)}" : "" : "";
    public string PlayingName => _playing?.Name ?? "İzlemek için bir film veya bölüm seçin";
    public bool HasNextEpisode => NextEpisode() != null;
    public int Volume { get => _volume; set { if (Set(ref _volume, Math.Clamp(value, 0, 100))) Observe(Playback.SetVolumeAsync(_volume)); } }
    public bool Muted { get => _muted; set { if (Set(ref _muted, value)) Observe(Playback.SetMuteAsync(value)); } }
    public double Position { get => _position; set { if (Set(ref _position, value)) Raise(nameof(TimeText)); } }
    public double Duration { get => _duration; private set { Set(ref _duration, value); Raise(nameof(TimeText)); } }
    public bool CanSeek { get => _canSeek; private set => Set(ref _canSeek, value); }
    public bool Scrubbing { get; set; }
    public string TimeText => $"{FormatTime(Position)} / {FormatTime(Duration)}";
    public AsyncCommand RefreshCommand { get; }
    public AsyncCommand PlayCommand { get; }
    public AsyncCommand RestartCommand { get; }
    public AsyncCommand PauseCommand { get; }
    public AsyncCommand StopCommand { get; }
    public AsyncCommand NextEpisodeCommand { get; }
    public AsyncCommand CancelCommand { get; }

    public VodViewModel(IReadOnlyList<Source> sources, UserSettings settings, VodKind kind)
    {
        Sources = sources.Where(x => x.Kind == SourceKind.Xtream).ToArray(); _kind = kind; _source = Sources.FirstOrDefault();
        Volume = settings.DefaultVolume; Playback.AutoReconnect = settings.AutoReconnect;
        RefreshCommand = new(_ => LoadCatalogAsync(true), Error, true);
        PlayCommand = new(_ => PlaySelectionAsync(false), Error);
        RestartCommand = new(_ => PlaySelectionAsync(true), Error);
        PauseCommand = new(_ => Playback.TogglePauseAsync(), Error);
        StopCommand = new(_ => StopAsync(), Error);
        NextEpisodeCommand = new(_ => PlayNextEpisodeAsync(), Error);
        CancelCommand = new(_ => { _catalogRequest?.Cancel(); _detailRequest?.Cancel(); return Task.CompletedTask; }, Error);
        Playback.Changed += update => Dispatch(() => { if (!_disposed) { PlaybackText = update.Message; _playbackState = update.State; Raise(nameof(PauseIcon)); Raise(nameof(ShowPlayerHint)); Raise(nameof(PlaybackError)); } });
        Playback.Completed += key => Dispatch(() => Observe(CompleteAsync(key)));
    }
    public Task InitializeAsync() => LoadCatalogAsync(false);
    private static string FormatTime(double seconds) { var t = TimeSpan.FromSeconds(Math.Max(0, double.IsFinite(seconds) ? seconds : 0)); return t.TotalHours >= 1 ? $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}"; }
    private static void Dispatch(Action action) { if (!Application.Current.Dispatcher.HasShutdownStarted) Application.Current.Dispatcher.BeginInvoke(action); }
    private async void Observe(Task task) { try { await task; } catch (OperationCanceledException) { } catch (Exception ex) { if (!_disposed) Error(ex); } }
    public void Error(Exception ex)
    {
        App.Log.Write("vod.operation.failed", ex);
        Status = ex is UserInputException ? ex.Message : ex is AuthenticationException ? "Hesaba erişim reddedildi. Kaynak ayarlarını kontrol edin." : "İşlem tamamlanamadı. Bağlantınızı veya sağlayıcının içerik erişimini kontrol edip yeniden deneyin.";
    }
    public async Task LoadCatalogAsync(bool refresh)
    {
        _catalogRequest?.Cancel(); _catalogRequest?.Dispose(); _catalogRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token); var ct = _catalogRequest.Token;
        _detailRequest?.Cancel(); _filterRequest?.Cancel(); _selected = null; Raise(nameof(Selected)); Detail = null; _episodes = []; Seasons = []; Episodes = []; SelectedEpisode = null;
        _all = []; Items = []; Categories = ["Tüm kategoriler"]; _category = "Tüm kategoriler"; Raise(nameof(Category));
        var source = Source; var kind = Kind;
        if (source == null) { Busy = false; Status = "Film ve diziler için ana ekrandan bir Xtream hesabı ekleyin. M3U listelerinde ayrı katalog bulunmaz."; return; }
        Busy = true; Status = "Katalog yükleniyor…";
        try
        {
            var progress = await App.Store.LoadProgressAsync(source.Id, ct); ct.ThrowIfCancellationRequested(); foreach (var pair in progress) _progress[pair.Key] = pair.Value;
            var cached = await App.Store.LoadVodCatalogAsync(source.Id, kind, ct); ct.ThrowIfCancellationRequested();
            if (cached != null) await ApplyCatalogAsync(cached, ct);
            if (cached == null || refresh || DateTimeOffset.UtcNow - cached.Updated > TimeSpan.FromHours(6))
            {
                var catalog = await new XtreamService(_network).LoadCatalogAsync(source, kind, ct);
                await App.Store.SaveVodCatalogAsync(source.Id, kind, catalog, ct); ct.ThrowIfCancellationRequested(); await ApplyCatalogAsync(catalog, ct);
                Status = catalog.Items.Count == 0 ? "Bu hesapta bu türde içerik bulunamadı. Diğer hesabı veya Filmler / Diziler sekmesini deneyin." : $"{catalog.Items.Count:N0} içerik · güncellendi";
            }
            else Status = $"{cached.Items.Count:N0} içerik · kayıtlı katalog ({cached.Updated.LocalDateTime:g})";
        }
        catch (OperationCanceledException) { if (!_lifetime.IsCancellationRequested && _catalogRequest.Token == ct) Status = "Yükleme iptal edildi; kayıtlı katalog korundu."; }
        catch (Exception ex) { if (!ct.IsCancellationRequested) { Error(ex); if (_all.Count > 0) Status += " Kayıtlı katalog gösteriliyor."; } }
        finally { if (_catalogRequest.Token == ct) Busy = false; }
    }
    private async Task ApplyCatalogAsync(VodCatalog catalog, CancellationToken ct)
    {
        var categories = await Task.Run(() => new[] { "Tüm kategoriler" }.Concat(catalog.Items.Select(x => x.Category).Distinct().Order()).ToArray(), ct);
        ct.ThrowIfCancellationRequested(); _all = catalog.Items; Categories = categories; await FilterAsync(false);
    }
    private async Task FilterAsync(bool delay = true)
    {
        _filterRequest?.Cancel(); _filterRequest?.Dispose(); _filterRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token); var ct = _filterRequest.Token;
        try
        {
            if (delay) await Task.Delay(180, ct);
            var all = _all; var search = Search.Trim(); var category = Category;
            var result = await Task.Run(() => all.Where(x => (category == "Tüm kategoriler" || x.Category == category) && TextSearch.Contains(x.Name, search)).ToArray(), ct);
            ct.ThrowIfCancellationRequested(); Items = result;
        }
        catch (OperationCanceledException) { }
    }
    public async Task LoadDetailsAsync()
    {
        _detailRequest?.Cancel(); _detailRequest?.Dispose(); _detailRequest = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token); var ct = _detailRequest.Token;
        var selected = Selected; var source = Source;
        _episodes = []; Seasons = []; Episodes = []; SelectedEpisode = null; Detail = selected;
        if (selected == null || source == null) { DetailBusy = false; return; }
        DetailBusy = true;
        try
        {
            var details = await new XtreamService(_network).LoadDetailsAsync(source, selected, ct); ct.ThrowIfCancellationRequested();
            Detail = details.Item; _episodes = details.Episodes; Seasons = _episodes.Select(x => x.Season).Distinct().ToArray();
            var latest = _episodes.Select((episode, index) => new { Episode = episode, Index = index, Progress = _progress.GetValueOrDefault(XtreamService.EpisodeKey(selected.SourceId, selected.Id, episode.Id)) })
                .Where(x => x.Progress != null).OrderByDescending(x => x.Progress!.Updated).FirstOrDefault();
            var resumeEpisode = latest == null ? _episodes.FirstOrDefault() : latest.Progress!.Completed && latest.Index + 1 < _episodes.Count ? _episodes[latest.Index + 1] : latest.Episode;
            _season = resumeEpisode?.Season ?? Seasons.FirstOrDefault(); Raise(nameof(Season)); UpdateEpisodes();
            if (resumeEpisode != null) SelectedEpisode = Episodes.FirstOrDefault(x => x.Episode.Id == resumeEpisode.Id);
            Status = selected.Kind == VodKind.Series && _episodes.Count == 0 ? "Bu dizi için oynatılabilir bölüm bulunamadı." : "İçerik hazır";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!ct.IsCancellationRequested) { Error(ex); if (selected.Kind == VodKind.Movie) Status += " Katalogdaki film bağlantısıyla oynatmayı deneyebilirsiniz."; } }
        finally { if (_detailRequest.Token == ct) DetailBusy = false; }
    }
    private void UpdateEpisodes()
    {
        var selectedId = SelectedEpisode?.Episode.Id;
        Episodes = _episodes.Where(x => x.Season == Season).Select(x => new EpisodeRow(x, Detail != null && _progress.GetValueOrDefault(XtreamService.EpisodeKey(Detail.SourceId, Detail.Id, x.Id))?.Completed == true)).ToArray();
        SelectedEpisode = Episodes.FirstOrDefault(x => x.Episode.Id == selectedId) ?? Episodes.FirstOrDefault();
    }
    public Task PlaySelectionAsync(bool restart)
    {
        if (!CanPlay || Source == null || Detail == null) return Task.CompletedTask;
        return PlayAsync(new(Source, Detail, IsSeries ? SelectedEpisode?.Episode : null, _episodes), restart);
    }
    private async Task PlayAsync(Playing playing, bool restart)
    {
        await _playGate.WaitAsync(_lifetime.Token); _transitioning = true;
        try
        {
            await SaveProgressAsync();
            var resume = restart ? 0 : _progress.GetValueOrDefault(playing.Key)?.ResumeMs ?? 0;
            _playing = playing; Position = resume / 1000d; Duration = 0; CanSeek = false; Scrubbing = false; _lastSaved = DateTimeOffset.UtcNow;
            if (playing.Episode != null && Detail?.Key == playing.Item.Key)
            {
                Season = playing.Episode.Season;
                SelectedEpisode = Episodes.FirstOrDefault(x => x.Episode.Id == playing.Episode.Id);
            }
            Raise(nameof(PlayingName)); Raise(nameof(HasNextEpisode));
            var url = XtreamService.VodStreamUrl(playing.Source.Secret, playing.Episode?.Id ?? playing.Item.Id, playing.Episode?.Extension ?? playing.Item.Extension, playing.Episode != null);
            await Playback.PlayAsync(new(playing.Key, playing.Source.Id, playing.Name, url), resume);
        }
        finally { _transitioning = false; _playGate.Release(); }
    }
    private VodEpisode? NextEpisode()
    {
        if (_playing?.Episode == null) return null;
        var index = _playing.Episodes.ToList().FindIndex(x => x.Id == _playing.Episode.Id);
        return index >= 0 && index + 1 < _playing.Episodes.Count ? _playing.Episodes[index + 1] : null;
    }
    public Task PlayNextEpisodeAsync() => _playing is { } playing && NextEpisode() is { } next ? PlayAsync(playing with { Episode = next }, false) : Task.CompletedTask;
    public async Task TickAsync()
    {
        if (_tickBusy || _transitioning || _disposed) return; _tickBusy = true;
        try
        {
            var t = await Playback.GetTimelineAsync();
            if (t == null || _playing?.Key != t.Id || _transitioning) return;
            if (t.DurationMs > 0) Duration = t.DurationMs / 1000d;
            CanSeek = t.CanSeek && Duration > 0;
            if (!Scrubbing && t.PositionMs >= 0) Position = t.PositionMs / 1000d;
            if (DateTimeOffset.UtcNow - _lastSaved >= TimeSpan.FromSeconds(5)) await SaveProgressAsync();
        }
        finally { _tickBusy = false; }
    }
    public async Task SeekAsync(double seconds) { await Playback.SeekAsync((long)(Math.Max(0, seconds) * 1000)); await TickAsync(); }
    public async Task SkipAsync(int seconds) { var t = await Playback.GetTimelineAsync(); if (t != null) await SeekAsync(t.PositionMs / 1000d + seconds); }
    public async Task SaveProgressAsync(bool completed = false, string? expectedKey = null)
    {
        await _saveGate.WaitAsync();
        try
        {
            var playing = _playing; if (playing == null || expectedKey != null && playing.Key != expectedKey) return;
            var t = await Playback.GetTimelineAsync(); if (t == null || t.Id != playing.Key || playing != _playing) return;
            if (!completed && (t.PositionMs <= 0 || t.State is LibVLCSharp.Shared.VLCState.Ended or LibVLCSharp.Shared.VLCState.Stopped or LibVLCSharp.Shared.VLCState.Error)) return;
            var old = _progress.GetValueOrDefault(playing.Key);
            var duration = Math.Max(Math.Max(t.DurationMs, old?.DurationMs ?? 0), (long)(Duration * 1000));
            var progress = new WatchProgress(playing.Key, completed ? duration : t.PositionMs, duration, completed, DateTimeOffset.UtcNow);
            await App.Store.SaveProgressAsync(playing.Source.Id, progress);
            _progress[progress.Key] = progress; _lastSaved = DateTimeOffset.UtcNow; Raise(nameof(ResumeLabel));
        }
        finally { _saveGate.Release(); }
    }
    private async Task CompleteAsync(string key)
    {
        if (_disposed || _playing?.Key != key) return;
        await SaveProgressAsync(true, key); if (_playing?.Key == key) { Position = Duration; CanSeek = false; UpdateEpisodes(); Raise(nameof(HasNextEpisode)); }
    }
    public async Task StopAsync()
    {
        await _playGate.WaitAsync(); _transitioning = true;
        try { await SaveProgressAsync(); await Playback.StopAsync(); CanSeek = false; }
        finally { _transitioning = false; _playGate.Release(); }
    }
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return; _disposed = true; _lifetime.Cancel(); _catalogRequest?.Cancel(); _detailRequest?.Cancel(); _filterRequest?.Cancel();
        await _playGate.WaitAsync();
        try { await SaveProgressAsync(); await Playback.DisposeAsync(); }
        finally { _playGate.Release(); _network.Dispose(); }
    }
}
