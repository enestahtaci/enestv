using System.Diagnostics;
using System.IO;
using System.Text.Json;
using EnesTV.App.Playback;
using EnesTV.Core.Data;
using EnesTV.Core.Models;
using EnesTV.Core.Services;
using EnesTV.Smoke;

var output = Path.GetFullPath(args.ElementAtOrDefault(1) ?? Path.Combine(Path.GetTempPath(), "enestv-smoke-" + Guid.NewGuid())); Directory.CreateDirectory(output);
var video = Path.Combine(output, "test.avi"); TestMedia.Create(video);
await using var server = new FixtureServer(video, args.Contains("--serve") ? 18765 : 0);
if (args.Contains("--vod")) { await VodVerification.RunAsync(output, server).WaitAsync(TimeSpan.FromSeconds(150)); Console.WriteLine("VOD integration checks passed."); return; }
if (args.Contains("--ui")) { await UiVerification.RunAsync(output, server.Url).WaitAsync(TimeSpan.FromSeconds(90)); Console.WriteLine("UI integration checks passed."); return; }
if (args.Contains("--serve"))
{
    await File.WriteAllTextAsync(Path.Combine(output, "test-10000.m3u"), server.Playlist()); Console.WriteLine(server.Url); Console.WriteLine("Local fixtures ready. Ctrl+C to stop."); await Task.Delay(Timeout.Infinite); return;
}
var results = new List<object>();
void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); results.Add(new { test = name, passed = true }); Console.WriteLine("PASS " + name); }
var protector = new EnesTV.Platform.Windows.DpapiSecretProtector();
// Libraries written by enestv 1.0–1.4.1 used DPAPI directly with this entropy; they must still open.
var legacyEntropy = System.Text.Encoding.UTF8.GetBytes("enestv/source/v1");
var legacy = System.Security.Cryptography.ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes("test-password"), legacyEntropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);
var current = protector.Protect("test-password");
Check(protector.Unprotect(legacy) == "test-password" && System.Text.Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(current, legacyEntropy, System.Security.Cryptography.DataProtectionScope.CurrentUser)) == "test-password" && !System.Text.Encoding.UTF8.GetString(current).Contains("test-password"), "DPAPI protector is compatible with existing libraries");
var store = new LibraryStore(Path.Combine(output, "test.db"), protector); await store.InitializeAsync(); using var network = new NetworkClient(); var log = new SafeLog(Path.Combine(output, "logs"));
var source = new Source("test", "Local fixture", SourceKind.M3uUrl, new(server.Url + "/redirect"));
var timer = Stopwatch.StartNew(); var channels = await new PlaylistService(network, store, log).RefreshAsync(source, default); timer.Stop();
Check(channels.Count == 10000 && channels.Select(c => c.Category).Distinct().Count() == 20, "HTTP redirect → 10000 M3U channels → 20 categories → SQLite");
results.Add(new { metric = "10000 channel download/parse/encrypted save ms", value = timer.ElapsedMilliseconds });
await store.SetFavoriteAsync(channels[0].Id, true); await store.AddRecentAsync(channels[0].Id); await store.SaveSettingsAsync(new("Dark", 55, false, server.Url + "/epg.xml"));
var cached = await new LibraryStore(Path.Combine(output, "test.db"), protector).LoadAsync(); Check(cached.Channels.Count == 10000 && cached.Favorites.Contains(channels[0].Id) && cached.Recent.Count == 1 && cached.Settings.DefaultVolume == 55, "Offline cache and restart persistence");
var xtream = new XtreamService(network); var xtreamChannels = await xtream.LoadAsync(new("xt", "Local account", SourceKind.Xtream, new(server.Url, "test", "test")), default); Check(xtreamChannels.Count == 1, "Xtream authentication/categories/streams");
try { await xtream.LoadAsync(new("xt", "Wrong", SourceKind.Xtream, new(server.Url, "wrong", "wrong")), default); throw new InvalidOperationException("Expected auth error"); } catch (AuthenticationException) { Check(true, "Wrong Xtream credentials distinguished from network failures"); }
var epgCount = await new EpgService(network, store).RefreshAsync(server.Url + "/epg.xml", channels.Select(c => c.TvgId).ToHashSet(), default); var epg = await store.NowNextAsync(); Check(epgCount == 400 && epg.Count == 200, "XMLTV streaming import and now/next matching");
LibVLCSharp.Shared.Core.Initialize(); await using var playback = new PlaybackService(log) { AutoReconnect = false };
var playing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); playback.Changed += update => { if (update.State == PlaybackState.Playing) playing.TrySetResult(); };
await playback.PlayAsync(channels[0]); await playing.Task.WaitAsync(TimeSpan.FromSeconds(20));
await Task.Delay(1200); Check(playback.Player.IsPlaying, "LibVLC plays local HTTP video");
var snapshot = Path.Combine(output, "decoded-video.png"); Check(playback.Player.TakeSnapshot(0, snapshot, 320, 180), "LibVLC video frame snapshot");
await Task.Delay(500); Check(File.Exists(snapshot) && new FileInfo(snapshot).Length > 1000, "Actual video frame decoded");
var starts = new List<string>(); playback.Started += id => { lock (starts) starts.Add(id); };
var switches = Enumerable.Range(1, 20).Select(i => playback.PlayAsync(channels[i])).ToArray(); await Task.WhenAll(switches); await Task.Delay(1800);
lock (starts) Check(starts.Count > 0 && starts[^1] == channels[20].Id, "20 rapid switches: latest channel wins");
var error = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); playback.Changed += update => { if (update.State == PlaybackState.Error) error.TrySetResult(); };
await playback.PlayAsync(new("bad", "test", "Broken", server.Url + "/missing")); await error.Task.WaitAsync(TimeSpan.FromSeconds(35)); Check(true, "Broken stream returns Error without crashing");
await playback.StopAsync(); Check(!playback.Player.IsPlaying, "Stop releases active playback");
playback.AutoReconnect = true; var retryCount = 0; var exhausted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
playback.Changed += update => { if (update.State == PlaybackState.Reconnecting) Interlocked.Increment(ref retryCount); if (update.State == PlaybackState.Error) exhausted.TrySetResult(); };
await playback.PlayAsync(new("retry", "test", "Broken with reconnect", server.Url + "/missing")); await exhausted.Task.WaitAsync(TimeSpan.FromSeconds(45)); Check(retryCount == 3, "Auto reconnect stops after exactly 3 attempts");
await Task.WhenAll(playback.StopAsync(), playback.PlayAsync(channels[500])); await Task.Delay(1500); Check(playback.Player.IsPlaying, "Stop/play race: newer selection wins; channel without EPG plays");
var wave = Path.Combine(output, "test.wav"); TestMedia.CreateWave(wave); await playback.PlayAsync(new("audio", "test", "Silent PCM fixture", new Uri(wave).AbsoluteUri)); await Task.Delay(1200);
var audioTracks = await playback.GetTracksAsync(); Check(audioTracks.Audio.Any(t => t.Id >= 0), "Audio tracks detected"); await playback.SetAudioAsync(audioTracks.Audio.First(t => t.Id >= 0).Id);
await playback.SetVolumeAsync(33); await playback.SetMuteAsync(true); await playback.SetAspectAsync("16:9"); Check(playback.Player.Volume == 33 && playback.Player.Mute, "Volume/mute/aspect controls");
await playback.TogglePauseAsync(); await Task.Delay(250); Check(playback.Player.State == LibVLCSharp.Shared.VLCState.Paused, "Pause"); await playback.TogglePauseAsync(); await Task.Delay(250); Check(playback.Player.IsPlaying, "Resume");
await playback.StopAsync();
await File.WriteAllTextAsync(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true })); Console.WriteLine("All integration checks passed.");
