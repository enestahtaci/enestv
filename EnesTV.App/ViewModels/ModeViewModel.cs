using EnesTV.App.Playback;
using EnesTV.Core.Models;

namespace EnesTV.App.ViewModels;

// Live TV, Movies and Series share the main window and its single video surface.
// Each mode keeps its own PlaybackService; only the active one is bound to the VideoView and only one streams at a time.
public sealed partial class MainViewModel
{
    private readonly SemaphoreSlim _modeGate = new(1, 1);
    private IReadOnlyList<Source>? _vodSources;
    private VodViewModel? _vod;
    public VodViewModel? Vod { get => _vod; private set => Set(ref _vod, value); }
    public static bool IsVodSection(string section) => section is "Movies" or "Series";
    public bool IsVod => IsVodSection(Section);
    public PlaybackService ActivePlayback => IsVod && Vod != null ? Vod.Playback : Playback;
    public LibVLCSharp.Shared.MediaPlayer ActivePlayer => ActivePlayback.Player;
    /// <summary>Raised when the active player's tracks change or the active player itself changes.</summary>
    public event Action? ActiveTracksChanged;

    private async Task SwitchModeAsync(bool wasVod)
    {
        await _modeGate.WaitAsync(_lifetime.Token);
        try
        {
            if (IsVod)
            {
                var kind = Section == "Series" ? VodKind.Series : VodKind.Movie;
                if (!wasVod)
                {
                    // One active stream: avoid exceeding the provider's simultaneous connection limit.
                    if (PlayingItem != null) _previousChannelId = PlayingItem.Channel.Id;
                    await BoundedAsync(Playback.StopAsync(), "vod.open.stop.slow");
                    PlayingItem = null;
                }
                if (Vod == null || !ReferenceEquals(_vodSources, Sources))
                {
                    var old = Vod;
                    if (old != null) { Vod = null; Raise(nameof(ActivePlayer)); await BoundedAsync(old.DisposeAsync().AsTask(), "vod.recreate.slow", 10); }
                    _vodSources = Sources;
                    var vod = new VodViewModel(Sources, Settings, kind);
                    vod.Playback.TracksChanged += () => { if (IsVod && Vod == vod) ActiveTracksChanged?.Invoke(); };
                    Vod = vod;
                    Observe(vod.InitializeAsync());
                }
                else Vod.Kind = kind;
            }
            else if (wasVod && Vod != null) await BoundedAsync(Vod.StopAsync(), "vod.leave.stop.slow");
            Raise(nameof(ActivePlayer)); Raise(nameof(ActivePlayback));
            ActiveTracksChanged?.Invoke();
        }
        finally { _modeGate.Release(); }
    }
    // libvlc's Stop can stall on an unresponsive stream; never let it hold navigation hostage.
    private static async Task BoundedAsync(Task task, string slowEvent, int seconds = 3)
    {
        if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(seconds))) == task) { await task; return; }
        App.Log.Write(slowEvent);
        _ = task.ContinueWith(t => App.Log.Write(slowEvent + ".failed", t.Exception), TaskContinuationOptions.OnlyOnFaulted);
    }

    // Empty player screen: continue the last channel or jump to a recent/favourite one.
    private ChannelItem? _resumeChannel;
    public ChannelItem? ResumeChannel { get => _resumeChannel; private set => Set(ref _resumeChannel, value); }
    private IReadOnlyList<ChannelItem> _quickChannels = [];
    public IReadOnlyList<ChannelItem> QuickChannels { get => _quickChannels; private set => Set(ref _quickChannels, value); }
    private void UpdateQuickAccess()
    {
        bool Visible(ChannelItem x) => !x.Hidden && !Layout.HiddenCategories.Contains(x.Category);
        var byId = _all.GroupBy(x => x.Channel.Id).ToDictionary(x => x.Key, x => x.First());
        var ordered = _recent.Select(id => byId.GetValueOrDefault(id)).OfType<ChannelItem>()
            .Concat(_all.Where(x => x.Favorite)).Where(Visible).Distinct().Take(4).ToArray(); // "Continue" + three shortcuts
        ResumeChannel = ordered.FirstOrDefault();
        QuickChannels = ordered.Skip(1).ToArray();
    }
}
