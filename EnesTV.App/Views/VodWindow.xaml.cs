using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using EnesTV.App.Playback;
using EnesTV.App.ViewModels;
using EnesTV.Core.Models;

namespace EnesTV.App.Views;

public partial class VodWindow : Window
{
    public VodViewModel ViewModel { get; }
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private readonly DispatcherTimer _hide = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly FullscreenWindow _fullscreenWindow;
    private bool _fullscreen, _closing, _closed, _tracksUpdating, _seeking;
    private Point? _pointer;
    public VodWindow(IReadOnlyList<Source> sources, UserSettings settings, VodKind kind)
    {
        InitializeComponent(); ViewModel = new(sources, settings, kind); DataContext = ViewModel; _fullscreenWindow = new(this);
        _tracksUpdating = true;
        Audio.ItemsSource = new[] { new TrackOption(-2, "Ses izi yok") }; Audio.SelectedIndex = 0;
        Subtitles.ItemsSource = new[] { new TrackOption(-2, "Altyazı yok") }; Subtitles.SelectedIndex = 0;
        _tracksUpdating = false;
        _tick.Tick += async (_, _) => { try { await ViewModel.TickAsync(); } catch (Exception ex) { ViewModel.Error(ex); } };
        _hide.Tick += (_, _) => { if (OverlayControls.IsMouseOver || Mouse.Captured != null) return; _hide.Stop(); OverlayControls.Visibility = Visibility.Collapsed; VideoOverlay.Cursor = Cursors.None; };
        Loaded += async (_, _) => { _tick.Start(); await ViewModel.InitializeAsync(); };
        ViewModel.Playback.TracksChanged += () => { if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(RefreshTracks); };
        Closing += OnClosing;
    }
    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_closed) return; e.Cancel = true; if (_closing) return; _closing = true; _tick.Stop(); _hide.Stop(); IsEnabled = false;
        try { Video.MediaPlayer = null; await ViewModel.DisposeAsync(); }
        catch (Exception ex) { App.Log.Write("vod.shutdown.failed", ex); }
        finally { _closed = true; Close(); }
    }
    private void Back_Click(object sender, RoutedEventArgs e) => Close();
    private void Movies_Click(object sender, RoutedEventArgs e) => ViewModel.Kind = VodKind.Movie;
    private void Series_Click(object sender, RoutedEventArgs e) => ViewModel.Kind = VodKind.Series;
    private async void Rewind_Click(object sender, RoutedEventArgs e) { try { await ViewModel.SkipAsync(-10); } catch (Exception ex) { ViewModel.Error(ex); } }
    private async void Forward_Click(object sender, RoutedEventArgs e) { try { await ViewModel.SkipAsync(10); } catch (Exception ex) { ViewModel.Error(ex); } }
    private void Seek_Begin(object sender, MouseButtonEventArgs e) => ViewModel.Scrubbing = true;
    private async Task FinishSeekAsync()
    {
        if (_seeking) return; _seeking = true;
        try { var target = ViewModel.Position; await ViewModel.SeekAsync(target); }
        catch (Exception ex) { ViewModel.Error(ex); }
        finally { ViewModel.Scrubbing = false; _seeking = false; }
    }
    private async void Seek_End(object sender, MouseButtonEventArgs e) { if (ViewModel.Scrubbing) await FinishSeekAsync(); }
    private async void Seek_LostCapture(object sender, MouseEventArgs e) { if (ViewModel.Scrubbing) await FinishSeekAsync(); }
    private async void Seek_KeyUp(object sender, KeyEventArgs e) { if (e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown) await FinishSeekAsync(); }
    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();
    private void ToggleFullscreen()
    {
        _fullscreen = !_fullscreen; var visible = _fullscreen ? Visibility.Collapsed : Visibility.Visible;
        foreach (var element in new FrameworkElement[] { Header, CatalogPanel, PlayerControls, DetailsPanel, StatusPanel }) element.Visibility = visible;
        Root.Margin = new Thickness(_fullscreen ? 0 : 24); VideoFrame.CornerRadius = new CornerRadius(_fullscreen ? 0 : 12);
        ContentPanel.RowDefinitions[2].MinHeight = _fullscreen ? 0 : 150;
        ContentPanel.RowDefinitions[2].Height = _fullscreen ? new GridLength(0) : new GridLength(2, GridUnitType.Star);
        Grid.SetColumn(ContentPanel, _fullscreen ? 0 : 2); Grid.SetColumnSpan(ContentPanel, _fullscreen ? 3 : 1);
        Grid.SetRow(ContentPanel, _fullscreen ? 0 : 1); Grid.SetRowSpan(ContentPanel, _fullscreen ? 3 : 1);
        if (_fullscreen) { _fullscreenWindow.Enter(); ShowControls(); }
        else { _fullscreenWindow.Exit(); _hide.Stop(); OverlayControls.Visibility = Visibility.Collapsed; VideoOverlay.Cursor = Cursors.Arrow; }
        VideoOverlay.Focus();
    }
    private void ShowControls() { if (!_fullscreen) return; OverlayControls.Visibility = Visibility.Visible; VideoOverlay.Cursor = Cursors.Arrow; _hide.Stop(); _hide.Start(); }
    private void Overlay_MouseMove(object sender, MouseEventArgs e) { var p = e.GetPosition(VideoOverlay); if (_pointer != p) { _pointer = p; ShowControls(); } }
    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || _closing) return;
        if (e.Key == Key.F11 || e.Key == Key.Escape && _fullscreen) { e.Handled = true; if (!e.IsRepeat) ToggleFullscreen(); return; }
        if (Keyboard.FocusedElement is TextBoxBase or ComboBox or ComboBoxItem or Slider or ButtonBase) return;
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        try
        {
            if (e.Key == Key.Space) { e.Handled = true; if (!e.IsRepeat) await ViewModel.Playback.TogglePauseAsync(); }
            else if (e.Key is Key.Left or Key.Right) { e.Handled = true; await ViewModel.SkipAsync(e.Key == Key.Left ? -10 : 10); }
            else if (e.Key == Key.M) { e.Handled = true; if (!e.IsRepeat) ViewModel.Muted = !ViewModel.Muted; }
            else if (e.Key is Key.Up or Key.Down) { e.Handled = true; ViewModel.Volume += e.Key == Key.Up ? 5 : -5; }
            if (e.Handled) ShowControls();
        }
        catch (Exception ex) { ViewModel.Error(ex); }
    }
    private async void RefreshTracks()
    {
        if (_closing) return;
        try
        {
            var tracks = await ViewModel.Playback.GetTracksAsync(); if (tracks == null || _closing) return; _tracksUpdating = true;
            var audio = tracks.Audio.Length == 0 ? new[] { new TrackOption(-2, "Ses izi yok") } : tracks.Audio;
            var subtitles = tracks.Subtitles.Length == 0 ? new[] { new TrackOption(-2, "Altyazı yok") } : tracks.Subtitles;
            Audio.ItemsSource = audio; Subtitles.ItemsSource = subtitles;
            Audio.SelectedItem = audio.FirstOrDefault(x => x.Id == tracks.AudioId) ?? audio[0]; Subtitles.SelectedItem = subtitles.FirstOrDefault(x => x.Id == tracks.SubtitleId) ?? subtitles[0];
        }
        catch (Exception ex) { ViewModel.Error(ex); }
        finally { _tracksUpdating = false; }
    }
    private async void Audio_Changed(object sender, SelectionChangedEventArgs e) { if (!_tracksUpdating && Audio.SelectedItem is TrackOption { Id: >= -1 } t) { try { await ViewModel.Playback.SetAudioAsync(t.Id); } catch (Exception ex) { ViewModel.Error(ex); } } }
    private async void Subtitles_Changed(object sender, SelectionChangedEventArgs e) { if (!_tracksUpdating && Subtitles.SelectedItem is TrackOption { Id: >= -1 } t) { try { await ViewModel.Playback.SetSubtitleAsync(t.Id); } catch (Exception ex) { ViewModel.Error(ex); } } }
}
