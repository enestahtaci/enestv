using EnesTV.Core.Models;
using EnesTV.Core.Services;
using LibVLCSharp.Shared;

namespace EnesTV.App.Playback;

public enum PlaybackState { Idle, Loading, Playing, Paused, Buffering, Reconnecting, Error, Ended }
public sealed record PlaybackTimeline(string? Id, long PositionMs, long DurationMs, bool CanSeek, VLCState State);
public sealed record PlaybackUpdate(PlaybackState State, string Message);
public sealed record TrackOption(int Id, string Name)
{
    public override string ToString() => Name;
}
public sealed record TrackSnapshot(TrackOption[] Audio, TrackOption[] Subtitles, int AudioId, int SubtitleId);

public sealed class PlaybackService : IAsyncDisposable
{
    private readonly LibVLC _vlc = new("--no-video-title-show", "--quiet", "--no-snapshot-preview");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SafeLog _log;
    private Media? _media;
    private CancellationTokenSource _request = new();
    private Channel? _channel;
    private long _generation, _activeGeneration, _startSerial, _eventSequence;
    private volatile bool _acceptEvents, _disposed, _paused;
    private int _attempts, _reconnecting, _retryPending;
    private readonly bool _onDemand;
    private long _resumeMs;
    public MediaPlayer Player { get; }
    public bool AutoReconnect { get; set; } = true;
    public event Action<PlaybackUpdate>? Changed;
    public event Action? TracksChanged;
    public event Action<string>? Started;
    public event Action<string>? Completed;
    public PlaybackService(SafeLog log, bool onDemand = false)
    {
        _log = log; _onDemand = onDemand;
        // Keep mouse and keyboard input in WPF: VLC's video window runs on its own thread and must not take focus.
        Player = new MediaPlayer(_vlc) { EnableHardwareDecoding = true, Volume = 75, EnableMouseInput = false, EnableKeyInput = false };
        Player.Playing += (_, _) => { _paused = false; OnEvent(PlaybackState.Playing, _onDemand ? "Oynatılıyor" : "Canlı yayın", true); };
        Player.Paused += (_, _) => { _paused = true; OnEvent(PlaybackState.Paused, "Duraklatıldı"); };
        Player.Buffering += (_, e) => { if (!_paused) OnEvent(e.Cache < 100 ? PlaybackState.Buffering : PlaybackState.Playing, e.Cache < 100 ? "Yayın arabelleğe alınıyor…" : _onDemand ? "Oynatılıyor" : "Canlı yayın"); };
        Player.EncounteredError += (_, _) => ScheduleReconnect();
        Player.EndReached += (_, _) =>
        {
            if (!_onDemand) { ScheduleReconnect(); return; }
            if (!_acceptEvents || _disposed) return;
            var generation = Interlocked.Read(ref _activeGeneration); var id = _channel?.Id;
            _acceptEvents = false;
            _ = Task.Run(() => { if (generation != _generation || _disposed) return; Publish(PlaybackState.Ended, "İçerik tamamlandı"); if (id != null) Completed?.Invoke(id); });
        };
        Player.ESAdded += (_, _) => { if (_acceptEvents) _ = Task.Run(() => TracksChanged?.Invoke()); };
    }
    private void Publish(PlaybackState state, string message) => Changed?.Invoke(new(state, message));
    private void OnEvent(PlaybackState state, string message, bool started = false)
    {
        if (!_acceptEvents) return;
        var generation = Interlocked.Read(ref _activeGeneration);
        var sequence = Interlocked.Increment(ref _eventSequence);
        var channelId = _channel?.Id;
        _ = Task.Run(() =>
        {
            if (_disposed || generation != Interlocked.Read(ref _generation)) return;
            if (_acceptEvents && sequence == Interlocked.Read(ref _eventSequence)) Publish(state, message);
            if (started) { TracksChanged?.Invoke(); if (channelId != null) Started?.Invoke(channelId); }
        });
    }
    public async Task PlayAsync(Channel channel, long resumeMs = 0)
    {
        if (_disposed) return;
        var generation = Interlocked.Increment(ref _generation);
        var next = new CancellationTokenSource(); var old = Interlocked.Exchange(ref _request, next); old.Cancel(); old.Dispose();
        _channel = channel; _resumeMs = Math.Max(0, resumeMs); _attempts = 0; Interlocked.Exchange(ref _retryPending, 0);
        Publish(PlaybackState.Loading, "Yayına bağlanılıyor…");
        try { await StartAsync(channel, generation, next.Token); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _log.Write("playback.start.failed", ex); if (generation == _generation) Publish(PlaybackState.Error, "Yayın açılamadı. Kaynağı kontrol edip yeniden deneyin."); }
    }
    private Task StartAsync(Channel channel, long generation, CancellationToken ct) => Task.Run(async () =>
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            ct.ThrowIfCancellationRequested();
            if (generation != _generation || _disposed) return;
            _acceptEvents = false;
            Player.Stop(); Player.Media = null; _media?.Dispose(); _media = null;
            _paused = false;
            ct.ThrowIfCancellationRequested();
            _media = new Media(_vlc, new Uri(channel.Url));
            _media.AddOption(":network-caching=1000"); _media.AddOption(":http-reconnect=false");
            if (_onDemand && _resumeMs > 0) _media.AddOption(":start-time=" + (_resumeMs / 1000d).ToString(System.Globalization.CultureInfo.InvariantCulture));
            _activeGeneration = generation; _acceptEvents = true;
            if (!Player.Play(_media)) ScheduleReconnect();
            _ = WatchStartAsync(generation, Interlocked.Increment(ref _startSerial), ct);
        }
        finally { _gate.Release(); }
    }, ct);
    private async Task WatchStartAsync(long generation, long startSerial, CancellationToken ct)
    {
        try
        {
            var lastProgress = DateTime.UtcNow; long lastTime = -1;
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                if (generation != _generation || startSerial != _startSerial || _disposed || !_acceptEvents) return;
                var status = await WithPlayerAsync(p => (Playing: p.IsPlaying, State: p.State, Time: p.Time));
                if (_onDemand && generation == _generation && status.Playing && status.Time > 0) _resumeMs = status.Time;
                if (status.State == VLCState.Paused || status.Playing && (status.Time < 0 || status.Time != lastTime)) lastProgress = DateTime.UtcNow;
                lastTime = status.Time;
                if (DateTime.UtcNow - lastProgress > TimeSpan.FromSeconds(25)) { if (generation == _generation && startSerial == _startSerial) ScheduleReconnect(); return; }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _log.Write("playback.watchdog.failed", ex); }
    }
    private void ScheduleReconnect()
    {
        if (!_acceptEvents || _disposed) return;
        var generation = Interlocked.Read(ref _activeGeneration);
        Interlocked.Exchange(ref _retryPending, 1);
        _ = Task.Run(() => ReconnectAsync(generation));
    }
    private async Task ReconnectAsync(long generation)
    {
        if (generation != _generation || _disposed || Interlocked.CompareExchange(ref _reconnecting, 1, 0) != 0) return;
        try
        {
            Interlocked.Exchange(ref _retryPending, 0);
            var ct = _request.Token;
            if (!AutoReconnect || _attempts >= 3)
            {
                _acceptEvents = false;
                await WithPlayerAsync(p => { if (generation == _generation) p.Stop(); return true; });
                if (generation == _generation) { _log.Write("playback.error"); Publish(PlaybackState.Error, "Yayın kesildi veya açılamadı. Yeniden bağlanmayı deneyin."); }
                return;
            }
            _attempts++; _log.Write("playback.reconnect");
            Publish(PlaybackState.Reconnecting, $"Yeniden bağlanılıyor ({_attempts}/3)…");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, _attempts - 1)), ct);
            if (generation != _generation || _channel == null) return;
            if (_onDemand) { var timeline = await GetTimelineAsync(); if (timeline != null && timeline.PositionMs > 0) _resumeMs = timeline.PositionMs; }
            await StartAsync(_channel, generation, ct);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _log.Write("playback.reconnect.failed", ex); if (generation == _generation) Publish(PlaybackState.Error, "Yeniden bağlantı kurulamadı."); }
        finally
        {
            Interlocked.Exchange(ref _reconnecting, 0);
            if (Interlocked.Exchange(ref _retryPending, 0) != 0 && generation == _generation && _acceptEvents && !_disposed) _ = Task.Run(() => ReconnectAsync(generation));
        }
    }
    public async Task StopAsync()
    {
        var generation = Interlocked.Increment(ref _generation); _request.Cancel(); _acceptEvents = false; _channel = null;
        await WithPlayerAsync(p => { if (generation == _generation) { p.Stop(); p.Media = null; _media?.Dispose(); _media = null; } return true; });
        if (generation == _generation) Publish(PlaybackState.Idle, "Bir kanal seçin");
    }
    public Task TogglePauseAsync() => WithPlayerAsync(p => { p.Pause(); return true; });
    public Task SetVolumeAsync(int value) => WithPlayerAsync(p => { p.Volume = Math.Clamp(value, 0, 100); return true; });
    public Task SetMuteAsync(bool value) => WithPlayerAsync(p => { p.Mute = value; return true; });
    public Task SetAspectAsync(string? value) => WithPlayerAsync(p => { p.AspectRatio = value; return true; });
    public Task SetAudioAsync(int id) => WithPlayerAsync(p => p.SetAudioTrack(id));
    public Task SetSubtitleAsync(int id) => WithPlayerAsync(p => p.SetSpu(id));
    public Task<PlaybackTimeline> GetTimelineAsync() => WithPlayerAsync(p => new PlaybackTimeline(_channel?.Id, p.Time, p.Length, p.IsSeekable, p.State));
    public Task SeekAsync(long positionMs) => WithPlayerAsync(p =>
    {
        if (_onDemand && p.IsSeekable && p.Length > 0) p.Time = Math.Clamp(positionMs, 0, Math.Max(0, p.Length - 500));
        return true;
    });
    public Task<TrackSnapshot> GetTracksAsync() => WithPlayerAsync(p => new TrackSnapshot(p.AudioTrackDescription.Select(t => new TrackOption(t.Id, t.Name)).ToArray(), p.SpuDescription.Select(t => new TrackOption(t.Id, t.Name)).ToArray(), p.AudioTrack, p.Spu));
    private Task<T> WithPlayerAsync<T>(Func<MediaPlayer, T> action) => Task.Run(async () =>
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try { return _disposed ? default! : action(Player); }
        finally { _gate.Release(); }
    });
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await StopAsync(); await _gate.WaitAsync();
        try { _disposed = true; await Task.Run(() => { Player.Dispose(); _vlc.Dispose(); }); _request.Dispose(); }
        finally { _gate.Release(); }
    }
}
