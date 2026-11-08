using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using EnesTV.App.ViewModels;

namespace EnesTV.App;

// Movies and series inside the main window: seek, skip and the movie keyboard map.
public partial class MainWindow
{
    private readonly DispatcherTimer _vodTick = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private bool _seeking;
    private VodViewModel? ActiveVod => _vm.IsVod ? _vm.Vod : null;
    private FrameworkElement ActiveFullscreenControls => _vm.IsVod ? VodOverlayControls : FullscreenControls;
    // Live: video takes all spare height, EPG card sizes to content. Movies/series: video and details share it 3:2
    // (details 170–255 px) so the watch button stays reachable in a small window. Fullscreen: video only.
    private void ApplyPlayerRows()
    {
        var details = _vm.IsVod && !_fullscreen;
        PlayerPanel.RowDefinitions[0].Height = new GridLength(details ? 3 : 1, GridUnitType.Star);
        var row = PlayerPanel.RowDefinitions[3];
        row.Height = details ? new GridLength(2, GridUnitType.Star) : GridLength.Auto;
        row.MinHeight = details ? 170 : 0; row.MaxHeight = details ? 265 : double.PositiveInfinity;
    }
    private void InitializeVodUi()
    {
        _vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(_vm.IsVod)) ApplyPlayerRows(); };
        _vodTick.Tick += async (_, _) => { if (ActiveVod is not { } vod) return; try { await vod.TickAsync(); } catch (Exception ex) { vod.Error(ex); } };
        _vodTick.Start();
        Closed += (_, _) => _vodTick.Stop();
    }
    private async void Rewind_Click(object sender, RoutedEventArgs e) { if (ActiveVod is { } vod) try { await vod.SkipAsync(-10); } catch (Exception ex) { vod.Error(ex); } }
    private async void Forward_Click(object sender, RoutedEventArgs e) { if (ActiveVod is { } vod) try { await vod.SkipAsync(10); } catch (Exception ex) { vod.Error(ex); } }
    private void Seek_Begin(object sender, MouseButtonEventArgs e) { if (ActiveVod is { } vod) vod.Scrubbing = true; }
    private async Task FinishSeekAsync()
    {
        if (_seeking || ActiveVod is not { } vod) return; _seeking = true;
        try { await vod.SeekAsync(vod.Position); }
        catch (Exception ex) { vod.Error(ex); }
        finally { vod.Scrubbing = false; _seeking = false; }
    }
    private async void Seek_End(object sender, MouseButtonEventArgs e) { if (ActiveVod?.Scrubbing == true) await FinishSeekAsync(); }
    private async void Seek_LostCapture(object sender, MouseEventArgs e) { if (ActiveVod?.Scrubbing == true) await FinishSeekAsync(); }
    private async void Seek_KeyUp(object sender, KeyEventArgs e) { if (e.Key is Key.Left or Key.Right or Key.Home or Key.End or Key.PageUp or Key.PageDown) await FinishSeekAsync(); }
    private async Task VodKeyDownAsync(KeyEventArgs e, Key key)
    {
        if (ActiveVod is not { } vod) return;
        // Esc returns to live TV, except while typing or choosing from a list (Esc closes the drop-down there).
        if (key == Key.Escape && Keyboard.FocusedElement is not (TextBoxBase or ComboBox or ComboBoxItem)) { e.Handled = true; if (!e.IsRepeat) _vm.Section = "Live TV"; return; }
        if (Keyboard.FocusedElement is TextBoxBase or ComboBox or ComboBoxItem or Slider or ButtonBase) return;
        if (Keyboard.Modifiers != ModifierKeys.None) return;
        try
        {
            if (key == Key.Space) { e.Handled = true; if (!e.IsRepeat) await vod.Playback.TogglePauseAsync(); }
            else if (key is Key.Left or Key.Right) { e.Handled = true; await vod.SkipAsync(key == Key.Left ? -10 : 10); }
            else if (key == Key.M) { e.Handled = true; if (!e.IsRepeat) vod.Muted = !vod.Muted; }
            else if (key is Key.Up or Key.Down) { e.Handled = true; vod.Volume += key == Key.Up ? 5 : -5; }
            if (e.Handled) ShowFullscreenControls();
        }
        catch (Exception ex) { vod.Error(ex); }
    }
}
