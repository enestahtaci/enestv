using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using EnesTV.App.ViewModels;
using EnesTV.App.Views;
using EnesTV.Core.Models;

namespace EnesTV.Smoke;

internal static class NavigationChecks
{
    private static void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private struct NativePoint { public int X, Y; }
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out NativeRect rect);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    private static void KeyPress(Window window, Key key) => window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
    public static async Task FullscreenAsync(Window main, MainViewModel vm, string root)
    {
        var controls = (Border)main.FindName("FullscreenControls"); var overlay = (Grid)main.FindName("VideoOverlay");
        Check(controls.Visibility == Visibility.Visible, "Fullscreen controls initially visible");
        Check(controls.DataContext == vm, "Native VLC overlay inherits view model");
        Capture(overlay, Path.Combine(root, "fullscreen-overlay.png"));
        // The overlay receives real mouse input, so drive the real cursor: park it in a corner (away from the
        // controls), wait for auto-hide, then move it and expect the controls back.
        // Use the overlay window's Win32 rectangle: it shares SetCursorPos' coordinate space even on mixed-DPI
        // multi-monitor setups, where WPF's PointToScreen does not.
        // Windows may start the test window behind whatever the user is working in (focus-stealing prevention);
        // keep it on top while the real cursor is driven so the overlay is actually under the pointer.
        main.Topmost = true;
        GetCursorPos(out var original);
        GetWindowRect(new System.Windows.Interop.WindowInteropHelper(Window.GetWindow(overlay)).Handle, out var bounds);
        SetCursorPos(bounds.Left + 10, bounds.Top + 10);
        var hideDeadline = DateTime.UtcNow.AddSeconds(8);
        while (controls.Visibility != Visibility.Collapsed && DateTime.UtcNow < hideDeadline) await Task.Delay(100);
        Check(controls.Visibility == Visibility.Collapsed, "Fullscreen controls auto-hide after inactivity");
        var center = new NativePoint { X = (bounds.Left + bounds.Right) / 2, Y = bounds.Top + (bounds.Bottom - bounds.Top) / 3 };
        SetCursorPos(center.X, center.Y);
        var showDeadline = DateTime.UtcNow.AddSeconds(2);
        while (controls.Visibility != Visibility.Visible && DateTime.UtcNow < showDeadline) await Task.Delay(50);
        GetWindowThreadProcessId(WindowFromPoint(center), out var ownerPid);
        SetCursorPos(original.X, original.Y); main.Topmost = false;
        Check(controls.Visibility == Visibility.Visible, $"Mouse movement reveals fullscreen controls (window under cursor belongs to this app: {ownerPid == Environment.ProcessId})");
        main.Activate(); Keyboard.Focus(overlay);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (Keyboard.Modifiers != ModifierKeys.None && DateTime.UtcNow < deadline) await Task.Delay(100);
        Check(Keyboard.FocusedElement == overlay && Keyboard.Modifiers == ModifierKeys.None, "Overlay keyboard focus ready");
        var mute = vm.Muted;
        overlay.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(overlay), Environment.TickCount, Key.M) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
        Check(vm.Muted != mute, "Keyboard shortcut works through native video overlay");
        vm.Muted = mute;
    }
    public static async Task RunAsync(Window main, MainViewModel vm, string root)
    {
        vm.Search = ""; vm.Category = "Tüm kategoriler"; vm.Section = "Live TV"; await Task.Delay(300);
        var first = vm.Channels[0]; var second = vm.Channels[1]; var third = vm.Channels[2];
        await vm.PlayChannelAsync(first); await vm.StepChannelAsync(1);
        Check(vm.PlayingItem?.Channel.Id == second.Channel.Id, "Next channel follows visible list");
        await vm.ReturnToLastChannelAsync(); Check(vm.PlayingItem?.Channel.Id == first.Channel.Id, "Return to last channel");
        await vm.StepChannelAsync(-1); Check(vm.PlayingItem?.Channel.Id == vm.Channels[^1].Channel.Id, "Previous channel wraps list");
        var hiddenCategory = vm.AllChannels.First(x => x.Category != first.Category && x.Category != second.Category && x.Category != third.Category).Category;
        var layout = new LibraryLayout
        {
            Channels = new() { [second.Channel.Id] = new("Özel spor kanalı", 0), [first.Channel.Id] = new("", 1), [third.Channel.Id] = new("", 2, true) },
            HiddenCategories = [hiddenCategory],
            FavoriteLists = [new("custom-a", "Spor", [first.Channel.Id, second.Channel.Id]), new("custom-b", "Aile", [second.Channel.Id])]
        };
        await vm.SaveOrganizationAsync(layout);
        Check(vm.Channels[0].Name == "Özel spor kanalı" && vm.Channels[0].Number == 1, "Alias and order change channel numbering");
        Check(vm.Channels.All(x => x.Category != hiddenCategory && x.Channel.Id != third.Channel.Id), "Hidden channels and categories excluded");
        vm.Search = "Özel spor"; await Task.Delay(300); Check(vm.Channels.Count == 1, "Search uses custom display name");
        vm.Search = ""; vm.Section = "Favorites"; vm.SelectedFavoriteList = vm.FavoriteLists.Single(x => x.Id == "custom-b"); await Task.Delay(300);
        Check(vm.Channels.Count == 1 && vm.Channels[0].Star == "★", "Custom favorite list filter and star");
        await vm.PlayNumberAsync(2); Check(vm.PlayingItem?.Channel.Id == first.Channel.Id, "Number selection is stable across filtered lists");
        Check(!await vm.PlayNumberAsync(999999), "Unknown channel number is harmless");
        vm.Section = "Live TV"; await Task.Delay(300);
        await vm.SaveShortcutsAsync(new() { ["next"] = "G" });
        var focus = (Grid)main.FindName("RootGrid"); main.Activate(); Keyboard.Focus(focus);
        var focusDeadline = DateTime.UtcNow.AddSeconds(10);
        while (Keyboard.Modifiers != ModifierKeys.None && DateTime.UtcNow < focusDeadline) await Task.Delay(100);
        Check(Keyboard.FocusedElement == focus && Keyboard.Modifiers == ModifierKeys.None, "Keyboard test focus and modifiers ready");
        await vm.PlayNumberAsync(1); KeyPress(main, Key.G); await Task.Delay(400);
        Check(vm.PlayingItem?.Channel.Id == first.Channel.Id, $"Customized channel shortcut (focus={Keyboard.FocusedElement?.GetType().Name}, modifiers={Keyboard.Modifiers}, current={vm.PlayingItem?.Name})");
        var search = (TextBox)main.FindName("SearchBox"); Keyboard.Focus(search); KeyPress(main, Key.G); await Task.Delay(100);
        Check(vm.PlayingItem?.Channel.Id == first.Channel.Id, "Typing in search does not switch channels");
        Keyboard.Focus(focus); KeyPress(main, Key.D1); KeyPress(main, Key.Enter); await Task.Delay(400);
        Check(vm.PlayingItem?.Channel.Id == second.Channel.Id, "Digit and Enter channel selection");
        KeyPress(main, Key.D2); await Task.Delay(1600); Check(vm.PlayingItem?.Channel.Id == first.Channel.Id, "Digit timeout channel selection");
        KeyPress(main, Key.D1); KeyPress(main, Key.G); await Task.Delay(1600);
        Check(vm.PlayingItem?.Channel.Id != second.Channel.Id, "New channel action cancels pending number selection");
        await vm.RefreshAsync(vm.Sources[0]);
        Check(vm.Channels[0].Name == "Özel spor kanalı" && vm.Layout.FavoriteLists.Count == 2, "Refresh preserves layout and named lists");
        var editor = new OrganizationWindow(vm) { Owner = main }; editor.Show(); await Task.Delay(250);
        var grid = (DataGrid)editor.FindName("RowsGrid");
        Check(grid.Items.Count == 10000 && Count<DataGridRow>(grid) < 60, "10000-channel editor virtualizes rows");
        var row = (OrganizationWindow.ChannelRow)grid.Items[0]; row.Name = "Düzenleyicide değişti";
        grid.SelectedItem = row;
        Click(editor, "↓ Aşağı"); Check(((OrganizationWindow.ChannelRow)grid.Items[1]).Id == row.Id, "Editor moves channel down");
        Click(editor, "↑ Yukarı"); Check(((OrganizationWindow.ChannelRow)grid.Items[0]).Id == row.Id, "Editor moves channel up");
        FillNameDialog(editor, "Yeni liste", "Test listesi");
        var lists = (ComboBox)editor.FindName("Lists");
        Check(lists.Items.Count == 3 && ((FavoriteList)lists.SelectedItem).Name == "Test listesi", "Create named favorite list through dialog");
        Click(editor, "Seçilenleri ekle"); Check(((FavoriteList)lists.SelectedItem).Channels.Contains(row.Id), "Add selected channel to list");
        Click(editor, "Seçilenleri çıkar"); Check(!((FavoriteList)lists.SelectedItem).Channels.Contains(row.Id), "Remove selected channel from list");
        FillNameDialog(editor, "Adı değiştir", "Yeni ad"); Check(((FavoriteList)lists.SelectedItem).Name == "Yeni ad", "Rename favorite list");
        Click(editor, "Sil"); Check(lists.Items.Count == 2, "Delete custom list without deleting channels");
        await Task.Delay(150);
        Capture(editor, Path.Combine(root, "channel-organization.png"));
        ((Button)editor.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(500);
        Check(!editor.IsVisible && vm.Channels[0].Name == "Düzenleyicide değişti", "Editor save applies display name");
        var shortcuts = new ShortcutsWindow(vm) { Owner = main }; shortcuts.Show(); await Task.Delay(150);
        Check(((SolidColorBrush)shortcuts.Background).Color == ((SolidColorBrush)Application.Current.FindResource("Page")).Color, "Shortcut editor follows theme");
        Capture(shortcuts, Path.Combine(root, "shortcuts.png")); shortcuts.Close();
        var persisted = await EnesTV.App.App.Store.LoadLayoutAsync();
        Check(persisted.Channels[second.Channel.Id].Name == "Düzenleyicide değişti" && persisted.Shortcuts["next"] == "G", "Layout and shortcuts persist together");
        await vm.SaveOrganizationAsync(new()); await vm.SaveShortcutsAsync([]); vm.SelectedFavoriteList = vm.FavoriteLists[0];
    }
    private static int Count<T>(DependencyObject element) where T : DependencyObject
    { var n = element is T ? 1 : 0; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++) n += Count<T>(VisualTreeHelper.GetChild(element, i)); return n; }
    private static IEnumerable<T> Find<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) foreach (var item in Find<T>(VisualTreeHelper.GetChild(parent, i))) yield return item;
    }
    private static void Click(Window window, string content) => Find<Button>(window).Single(x => x.Content?.ToString() == content).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    private static void FillNameDialog(Window editor, string button, string name)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.Current.Windows.Cast<Window>().FirstOrDefault(x => x.Owner == editor);
            if (dialog == null) return;
            timer.Stop(); Find<TextBox>(dialog).Single().Text = name; Click(dialog, "Tamam");
        };
        timer.Start(); try { Click(editor, button); } finally { timer.Stop(); }
    }
    private static void Capture(FrameworkElement element, string path)
    {
        element.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); png.Save(file);
    }
}
