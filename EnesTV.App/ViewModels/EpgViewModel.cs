using EnesTV.Core.Models;
using EnesTV.Core.Services;
namespace EnesTV.App.ViewModels;
public sealed partial class MainViewModel
{
    private Dictionary<string, Programme[]> _nowNext = [];
    private Programme? _currentProgram, _nextProgram;
    public Programme? CurrentProgram { get => _currentProgram; private set => Set(ref _currentProgram, value); }
    public Programme? NextProgram { get => _nextProgram; private set => Set(ref _nextProgram, value); }
    private string _epgStatus = "Rehber henüz yüklenmedi";
    public string EpgStatus { get => _epgStatus; private set => Set(ref _epgStatus, value); }
    public IReadOnlyList<ChannelItem> AllChannels => _all;
    private void UpdateCurrentEpg()
    {
        var programs = _nowNext.GetValueOrDefault(PlayingItem?.Channel.TvgId ?? "", []); var now = DateTimeOffset.UtcNow;
        CurrentProgram = programs.FirstOrDefault(x => x.Start <= now && x.End > now); NextProgram = programs.FirstOrDefault(x => x.Start > now);
    }
    private async Task LoadEpgAsync()
    {
        var programmes = await App.Store.NowNextAsync(_lifetime.Token);
        _nowNext = programmes.GroupBy(x => x.ChannelId).ToDictionary(x => x.Key, x => x.ToArray()); var now = DateTimeOffset.UtcNow;
        foreach (var item in _all)
        {
            var current = _nowNext.GetValueOrDefault(item.Channel.TvgId, []).FirstOrDefault(x => x.Start <= now && x.End > now);
            item.Now = current == null ? "Program bilgisi yok" : current.Title;
        }
        UpdateCurrentEpg(); var updated = await App.Store.EpgUpdatedAsync(_lifetime.Token);
        EpgStatus = updated == null ? "Rehber henüz yüklenmedi" : $"Rehber güncellemesi: {updated.Value.LocalDateTime:g}";
    }
    private async Task EpgClockAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        try { while (await timer.WaitForNextTickAsync(_lifetime.Token)) await LoadEpgAsync(); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { App.Log.Write("epg.clock.failed", ex); }
    }
    public async Task RefreshEpgAsync()
    {
        if (Busy) throw new UserInputException("Devam eden yüklemenin tamamlanmasını bekleyin.");
        var feeds = new List<string>();
        if (!string.IsNullOrWhiteSpace(Settings.EpgUrl)) feeds.Add(Settings.EpgUrl);
        feeds.AddRange(Sources.Where(s => s.Kind == SourceKind.Xtream).Select(s => XtreamService.EpgUrl(s.Secret)));
        if (feeds.Count == 0) throw new UserInputException("Ayarlardan bir XMLTV URL'si veya Xtream hesabı ekleyin.");
        _loading?.Dispose(); _loading = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token); Busy = true; Status = "TV rehberi güncelleniyor…";
        try
        {
            var ids = _all.Select(x => x.Channel.TvgId).Where(x => x.Length > 0).ToHashSet(); var total = 0; var failed = 0;
            foreach (var feed in feeds.Distinct())
            {
                try { total += await new EpgService(_network, App.Store).RefreshAsync(feed, ids, _loading.Token); }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { App.Log.Write("epg.fetch.failed", ex); failed++; }
            }
            await LoadEpgAsync(); Status = $"{total:N0} program kaydedildi" + (failed > 0 ? $" · {failed} kaynağa ulaşılamadı; önceki rehber korundu" : "");
        }
        catch (OperationCanceledException) when (_loading.IsCancellationRequested) { Status = "Rehber yüklemesi iptal edildi."; }
        finally { Busy = false; }
    }
}
