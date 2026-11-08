using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using EnesTV.App.Services;
using EnesTV.App.Views;

namespace EnesTV.App;

public partial class MainWindow
{
    private readonly DispatcherTimer _controlsTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private readonly DispatcherTimer _numberTimer = new() { Interval = TimeSpan.FromMilliseconds(1200) };
    private string _channelDigits = "";
    private Point? _lastPointer;
    private void InitializeNavigationUi()
    {
        _controlsTimer.Tick += (_, _) =>
        {
            var controls = ActiveFullscreenControls;
            if (controls.IsMouseOver || Mouse.Captured != null) return;
            _controlsTimer.Stop();
            if (!_fullscreen) return;
            if (controls.IsKeyboardFocusWithin) Keyboard.Focus(VideoOverlay);
            controls.Visibility = Visibility.Collapsed; VideoOverlay.Cursor = Cursors.None;
        };
        _numberTimer.Tick += async (_, _) => await CommitChannelNumberAsync();
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.PlayingItem)) CancelChannelNumber();
            if (e.PropertyName == nameof(_vm.IsVod))
            {
                CancelChannelNumber();
                FullscreenControls.Visibility = VodOverlayControls.Visibility = Visibility.Collapsed;
                ShowFullscreenControls();
                Aspect.SelectedIndex = 0; // the other mode's player starts with automatic aspect ratio
            }
        };
        Closed += (_, _) => { _controlsTimer.Stop(); _numberTimer.Stop(); };
        Deactivated += (_, _) => CancelChannelNumber();
    }
    private void CancelChannelNumber() { _numberTimer.Stop(); _channelDigits = ""; NumberBadge.Visibility = Visibility.Collapsed; }
    private void ShowFullscreenControls()
    {
        if (!_fullscreen) return;
        ActiveFullscreenControls.Visibility = Visibility.Visible; VideoOverlay.Cursor = Cursors.Arrow;
        _controlsTimer.Stop(); _controlsTimer.Start();
    }
    private void Overlay_MouseMove(object sender, MouseEventArgs e)
    {
        var point = e.GetPosition(VideoOverlay);
        if (_lastPointer == point) return;
        _lastPointer = point; ShowFullscreenControls();
    }
    private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        ShowFullscreenControls();
        if (e.OriginalSource == VideoOverlay) FocusPlayer();
    }
    private void Organization_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Busy) { _vm.Status = "Kanal düzenlemek için yüklemenin tamamlanmasını bekleyin."; return; }
        new OrganizationWindow(_vm) { Owner = this }.ShowDialog();
    }
    private void Shortcuts_Click(object sender, RoutedEventArgs e) => new ShortcutsWindow(_vm) { Owner = this }.ShowDialog();
    private async Task CommitChannelNumberAsync()
    {
        _numberTimer.Stop(); var digits = _channelDigits; _channelDigits = ""; NumberBadge.Visibility = Visibility.Collapsed;
        try { if (int.TryParse(digits, out var number)) await _vm.PlayNumberAsync(number); }
        catch (Exception ex) { _vm.Error(ex); }
    }
    private async void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || _closing) return;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.F11 || key == Key.Escape && _fullscreen)
        {
            if (!e.IsRepeat) ToggleFullscreen(); e.Handled = true; return;
        }
        if (_vm.IsVod) { await VodKeyDownAsync(e, key); return; }
        // Keep typing and native keyboard behavior intact in editors and selection controls.
        if (Keyboard.FocusedElement is TextBoxBase or PasswordBox or ComboBox or ComboBoxItem or Slider) return;
        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.None)
        {
            var digit = key >= Key.D0 && key <= Key.D9 ? key - Key.D0 : key >= Key.NumPad0 && key <= Key.NumPad9 ? key - Key.NumPad0 : -1;
            if (digit >= 0)
            {
                e.Handled = true; if (e.IsRepeat) return;
                if (_channelDigits.Length >= 6) _channelDigits = "";
                _channelDigits += digit; NumberText.Text = "Kanal " + _channelDigits; NumberBadge.Visibility = Visibility.Visible;
                _numberTimer.Stop(); _numberTimer.Start(); ShowFullscreenControls(); return;
            }
            if (key == Key.Enter && _channelDigits.Length > 0) { e.Handled = true; await CommitChannelNumberAsync(); return; }
            if (key == Key.Back && _channelDigits.Length > 0)
            {
                _channelDigits = _channelDigits[..^1]; NumberText.Text = "Kanal " + _channelDigits;
                if (_channelDigits.Length == 0) { _numberTimer.Stop(); NumberBadge.Visibility = Visibility.Collapsed; }
                e.Handled = true; return;
            }
            if (key == Key.Escape && _channelDigits.Length > 0) { _numberTimer.Stop(); _channelDigits = ""; NumberBadge.Visibility = Visibility.Collapsed; e.Handled = true; return; }
            if (key == Key.Space && Keyboard.FocusedElement is ButtonBase) return;
        }
        var action = PlaybackShortcuts.Find(_vm.Layout.Shortcuts, key, modifiers);
        if (action == null) return;
        e.Handled = true; if (e.IsRepeat && action is not ("volumeUp" or "volumeDown")) return;
        CancelChannelNumber();
        ShowFullscreenControls();
        try
        {
            switch (action)
            {
                case "next": await _vm.StepChannelAsync(1); break;
                case "previous": await _vm.StepChannelAsync(-1); break;
                case "last": await _vm.ReturnToLastChannelAsync(); break;
                case "pause": await _vm.Playback.TogglePauseAsync(); break;
                case "mute": _vm.Muted = !_vm.Muted; break;
                case "volumeUp": _vm.Volume = Math.Min(100, _vm.Volume + 5); break;
                case "volumeDown": _vm.Volume = Math.Max(0, _vm.Volume - 5); break;
            }
        }
        catch (Exception ex) { _vm.Error(ex); }
    }
}
