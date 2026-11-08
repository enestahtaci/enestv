using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using EnesTV.App.ViewModels;
using EnesTV.App.Playback;
using EnesTV.App.Views;
using EnesTV.Core.Models;
namespace EnesTV.App;
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private bool _closed, _closing, _fullscreen, _updatingTracks;
    private readonly FullscreenWindow _fullscreenWindow;
    private readonly Dictionary<UIElement, Visibility> _normalVisibility = [];
    public MainWindow()
    {
        InitializeComponent(); DataContext = _vm;
        InitializeNavigationUi(); InitializeVodUi();
        _fullscreenWindow = new FullscreenWindow(this);
        _updatingTracks = true;
        AudioTracks.ItemsSource = new[] { new TrackOption(-2, "Ses izi yok") }; AudioTracks.SelectedIndex = 0;
        SubtitleTracks.ItemsSource = new[] { new TrackOption(-2, "Altyazı yok") }; SubtitleTracks.SelectedIndex = 0;
        _updatingTracks = false;
        Loaded += async (_, _) => { try { await _vm.InitializeAsync(); App.Log.Write("app.ready"); } catch (Exception ex) { _vm.Error(ex); } };
        Closing += OnClosing;
        _vm.Playback.TracksChanged += () => { if (!_vm.IsVod) QueueRefreshTracks(); };
        _vm.ActiveTracksChanged += QueueRefreshTracks;
    }
    private void QueueRefreshTracks() { if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => RefreshTracks()); }
    // Errors belong to the visible mode's status line.
    private void ReportError(Exception ex) { if (_vm.IsVod && _vm.Vod is { } vod) vod.Error(ex); else _vm.Error(ex); }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closed) return; e.Cancel = true; if (_closing) return; _closing = true; IsEnabled = false;
        // Disappear at once; give libvlc a bounded time to release the stream before the process exits.
        if (GetWindow(VideoOverlay) is { } overlay && overlay != this) overlay.Hide();
        Hide();
        try
        {
            Video.MediaPlayer = null;
            var dispose = _vm.DisposeAsync().AsTask();
            if (await Task.WhenAny(dispose, Task.Delay(TimeSpan.FromSeconds(10))) != dispose) App.Log.Write("app.shutdown.slow");
            else await dispose;
        }
        catch (Exception ex) { App.Log.Write("app.shutdown.failed", ex); }
        finally { _closed = true; Close(); }
    }
    private void AddSource_Click(object sender, RoutedEventArgs e) => new SourceDialog(_vm) { Owner = this }.ShowDialog();
    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "M3U playlists|*.m3u;*.m3u8|Tüm dosyalar|*.*" };
        if (picker.ShowDialog(this) != true) return;
        try { await _vm.AddSourceAsync(System.IO.Path.GetFileNameWithoutExtension(picker.FileName), SourceKind.M3uFile, new(picker.FileName)); } catch (Exception ex) { _vm.Error(ex); }
    }
    private void Settings_Click(object sender, RoutedEventArgs e) => new SettingsWindow(_vm) { Owner = this }.ShowDialog();
    private void Guide_Click(object sender, RoutedEventArgs e) => new GuideWindow(_vm) { Owner = this }.ShowDialog();
    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();
    private void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen;
        if (_fullscreen)
        {
            _normalVisibility.Clear();
            foreach (UIElement child in RootGrid.Children.Cast<UIElement>().Where(x => x != PlayerPanel)
                .Concat(PlayerPanel.Children.Cast<UIElement>().Where(x => x != VideoFrame)))
            {
                _normalVisibility[child] = child.Visibility;
                child.SetCurrentValue(VisibilityProperty, Visibility.Collapsed);
            }
        }
        else foreach (var (child, visibility) in _normalVisibility) child.SetCurrentValue(VisibilityProperty, visibility);
        RootGrid.Margin = new Thickness(_fullscreen ? 0 : 24);
        VideoFrame.CornerRadius = new CornerRadius(_fullscreen ? 0 : 14);
        Grid.SetRow(PlayerPanel, _fullscreen ? 0 : 1); Grid.SetColumn(PlayerPanel, _fullscreen ? 0 : 2); Grid.SetRowSpan(PlayerPanel, _fullscreen ? 3 : 1); Grid.SetColumnSpan(PlayerPanel, _fullscreen ? 3 : 1);
        ApplyPlayerRows();
        if (_fullscreen) _fullscreenWindow.Enter(); else _fullscreenWindow.Exit();
        _numberTimer.Stop(); _channelDigits = ""; NumberBadge.Visibility = Visibility.Collapsed;
        if (_fullscreen) ShowFullscreenControls();
        else { _controlsTimer.Stop(); FullscreenControls.Visibility = Visibility.Collapsed; VodOverlayControls.Visibility = Visibility.Collapsed; VideoOverlay.Cursor = Cursors.Arrow; }
        FocusPlayer();
    }
    // One keyboard target per mode: the overlay window in fullscreen, the main window otherwise.
    private void FocusPlayer()
    {
        if (_fullscreen) { Keyboard.Focus(VideoOverlay); return; }
        Activate(); RootGrid.Focusable = true; Keyboard.Focus(RootGrid);
    }
    private async void Aspect_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_vm == null || !IsLoaded) return;
        try { await _vm.ActivePlayback.SetAspectAsync(Aspect.SelectedIndex == 0 ? null : ((ComboBoxItem)Aspect.SelectedItem).Content.ToString()); } catch (Exception ex) { ReportError(ex); }
    }
    private async void Audio_Changed(object sender, SelectionChangedEventArgs e)
    { if (!_updatingTracks && AudioTracks.SelectedItem is TrackOption { Id: >= -1 } track) { try { await _vm.ActivePlayback.SetAudioAsync(track.Id); } catch (Exception ex) { ReportError(ex); } } }
    private async void Subtitle_Changed(object sender, SelectionChangedEventArgs e)
    { if (!_updatingTracks && SubtitleTracks.SelectedItem is TrackOption { Id: >= -1 } track) { try { await _vm.ActivePlayback.SetSubtitleAsync(track.Id); } catch (Exception ex) { ReportError(ex); } } }
    private async void RefreshTracks()
    {
        if (_closing) return;
        try
        {
            var tracks = await _vm.ActivePlayback.GetTracksAsync(); if (_closing || tracks == null) return;
            _updatingTracks = true;
            var audio = tracks.Audio.Length == 0 ? new[] { new TrackOption(-2, "Ses izi yok") } : tracks.Audio;
            var subtitles = tracks.Subtitles.Length == 0 ? new[] { new TrackOption(-2, "Altyazı yok") } : tracks.Subtitles;
            AudioTracks.ItemsSource = audio; SubtitleTracks.ItemsSource = subtitles;
            AudioTracks.SelectedItem = audio.FirstOrDefault(t => t.Id == tracks.AudioId) ?? audio[0];
            SubtitleTracks.SelectedItem = subtitles.FirstOrDefault(t => t.Id == tracks.SubtitleId) ?? subtitles[0];
        }
        catch (Exception ex) { ReportError(ex); }
        finally { _updatingTracks = false; }
    }
}
