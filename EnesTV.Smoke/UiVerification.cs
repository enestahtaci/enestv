using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Input;
using EnesTV.App.ViewModels;
using EnesTV.App.Views;
using EnesTV.Core.Models;
namespace EnesTV.Smoke;
public static class UiVerification
{
    public static Task RunAsync(string root, string url)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Environment.SetEnvironmentVariable("ENESTV_DATA_DIR", Path.Combine(root, "ui-library"));
            var app = new EnesTV.App.App(); app.InitializeComponent();
            var start = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            start.Tick += async (_, _) =>
            {
                if (app.MainWindow?.DataContext is not MainViewModel vm) return; start.Stop();
                try
                {
                    await Task.Delay(500);
                    var main = app.MainWindow;
                    WindowChecks.Caption(main, false);
                    if (!vm.NeedsSource || vm.Channels.Count != 0 || !((FrameworkElement)main.FindName("EmptyState")).IsVisible) throw new InvalidOperationException("First launch offers source onboarding");
                    Capture(main, Path.Combine(root, "enestv-empty.png"));
                    Capture((FrameworkElement)main.FindName("VideoOverlay"), Path.Combine(root, "enestv-empty-player.png"));
                    var beats = new List<double>(); var watch = Stopwatch.StartNew(); double previous = watch.Elapsed.TotalMilliseconds;
                    var heartbeat = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(16) };
                    heartbeat.Tick += (_, _) => { var now = watch.Elapsed.TotalMilliseconds; beats.Add(now - previous); previous = now; }; heartbeat.Start();
                    await vm.AddSourceAsync("Local verification", SourceKind.M3uUrl, new(url + "/playlist.m3u"));
                    await Task.Delay(500); heartbeat.Stop();
                    if (vm.Channels.Count != 10000) throw new InvalidOperationException("UI channel count");
                    var list = (ListBox)main.FindName("ChannelList");
                    var realized = Count<ListBoxItem>(list);
                    if (realized > 50) throw new InvalidOperationException("Virtualization disabled");
                    vm.Search = "Test Channel 09999"; await Task.Delay(500); if (vm.Channels.Count != 1) throw new InvalidOperationException("UI search");
                    vm.Search = "no matching channel 8a93"; await Task.Delay(350);
                    if (vm.Channels.Count != 0 || !vm.HasFilter || vm.EmptyTitle != "Sonuç bulunamadı") throw new InvalidOperationException("Empty search is distinct from first launch");
                    Capture(main, Path.Combine(root, "enestv-no-results.png"));
                    vm.ClearFilterCommand.Execute(null); await Task.Delay(350);
                    if (vm.HasFilter || vm.Channels.Count != 10000) throw new InvalidOperationException("Clear filter restores complete library");
                    vm.Search = ""; await Task.Delay(350);
                    vm.FavoriteCommand.Execute(vm.Channels[0]); await Task.Delay(300); vm.Section = "Favorites"; await Task.Delay(350); if (vm.Channels.Count != 1) throw new InvalidOperationException("UI favorites");
                    vm.Section = "Live TV"; await Task.Delay(300);
                    await vm.SaveSettingsAsync(new("Dark", 55, false, url + "/epg.xml")); await vm.RefreshEpgAsync();
                    vm.PlayCommand.Execute(vm.Channels[0]); await Task.Delay(2000);
                    if (!vm.Playback.Player.IsPlaying || vm.CurrentProgram == null) throw new InvalidOperationException("UI playback or EPG");
                    Capture(main, Path.Combine(root, "enestv-dark.png"));
                    var settings = new SettingsWindow(vm) { Owner = main }; settings.Show(); await Task.Delay(250); Capture(settings, Path.Combine(root, "enestv-settings.png")); settings.Close();
                    var guide = new GuideWindow(vm) { Owner = main }; guide.Show(); await Task.Delay(350); Capture(guide, Path.Combine(root, "enestv-guide.png")); guide.Close();
                    var source = new SourceDialog(vm) { Owner = main }; source.Show(); await Task.Delay(250); Capture(source, Path.Combine(root, "enestv-source.png")); source.Close();
                    var xtreamForm = new SourceDialog(vm) { Owner = main, MaxHeight = 620 }; xtreamForm.Show();
                    ((ComboBox)xtreamForm.FindName("Kind")).SelectedIndex = 2; await Task.Delay(200);
                    var sourceSave = (Button)xtreamForm.FindName("Save");
                    if (sourceSave.TranslatePoint(new Point(0, sourceSave.ActualHeight), xtreamForm).Y > xtreamForm.ActualHeight || !sourceSave.IsVisible) throw new InvalidOperationException("Xtream submit remains visible on short displays");
                    Capture(xtreamForm, Path.Combine(root, "enestv-xtream-small.png")); xtreamForm.Close();
                    var key = new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(main), Environment.TickCount, Key.F11) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
                    var normalBounds = new Rect(main.Left, main.Top, main.Width, main.Height);
                    main.RaiseEvent(key); await Task.Delay(400); WindowChecks.Fullscreen(main);
                    await NavigationChecks.FullscreenAsync(main, vm, root);
                    Capture(main, Path.Combine(root, "enestv-fullscreen.png"));
                    main.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(main), Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    await Task.Delay(250);
                    if (main.WindowState != WindowState.Normal || new Rect(main.Left, main.Top, main.Width, main.Height) != normalBounds || ((Grid)main.FindName("RootGrid")).Margin != new Thickness(24)) throw new InvalidOperationException("Restore normal window bounds and layout");
                    main.WindowState = WindowState.Maximized; await Task.Delay(200);
                    ((Button)main.FindName("FullscreenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await Task.Delay(300); WindowChecks.Fullscreen(main);
                    main.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(main), Environment.TickCount, Key.F11) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    await Task.Delay(200); if (main.WindowState != WindowState.Maximized || main.WindowStyle != WindowStyle.SingleBorderWindow) throw new InvalidOperationException("Restore maximized window");
                    main.WindowState = WindowState.Normal;
                    main.Width = 1120; main.Height = 730; await Task.Delay(200); Capture(main, Path.Combine(root, "enestv-small.png"));
                    var frame = (FrameworkElement)main.FindName("VideoFrame");
                    if (frame.ActualHeight < 220) throw new InvalidOperationException("Small window keeps useful video height");
                    var options = (Expander)main.FindName("PlaybackOptions"); options.IsExpanded = true; await Task.Delay(150);
                    var subtitles = (ComboBox)main.FindName("SubtitleTracks");
                    if (!subtitles.IsVisible || subtitles.TranslatePoint(new Point(subtitles.ActualWidth, 0), main).X > main.ActualWidth) throw new InvalidOperationException("Secondary playback options fit small window");
                    Capture(main, Path.Combine(root, "enestv-small-options.png")); options.IsExpanded = false;
                    main.Width = 1480; main.Height = 900;
                    await NavigationChecks.RunAsync(main, vm, root);
                    await vm.SaveSettingsAsync(vm.Settings with { Theme = "Light" }); await Task.Delay(150); Capture(main, Path.Combine(root, "enestv-light.png"));
                    WindowChecks.Caption(main, true);
                    var lightSettings = new SettingsWindow(vm) { Owner = main }; lightSettings.Show(); await Task.Delay(150); WindowChecks.Caption(lightSettings, true); lightSettings.Close();
                    await vm.SaveSettingsAsync(vm.Settings with { Theme = "Dark" }); WindowChecks.Caption(main, false);
                    var sorted = beats.Order().ToArray();
                    await File.WriteAllTextAsync(Path.Combine(root, "ui-results.json"), JsonSerializer.Serialize(new { channels = 10000, realizedListItems = realized, heartbeatSamples = beats.Count, maxUiGapMs = sorted.LastOrDefault(), p95UiGapMs = sorted.Length == 0 ? 0 : sorted[(int)(sorted.Length * 0.95)], search = true, favorites = true, playback = true, epg = true, settings = true, guide = true, sourceDialog = true, fullscreenMonitorPixels = true, fullscreenOnlyVideo = true, fullscreenButtonAndF11 = true, restoreNormalAndMaximized = true, nativeTitleBarDarkLight = true }, new JsonSerializerOptions { WriteIndented = true }));
                    main.Close(); done.TrySetResult();
                }
                catch (Exception ex) { done.TrySetException(ex); app.MainWindow.Close(); }
            };
            start.Start(); app.Run();
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return done.Task;
    }
    private static int Count<T>(DependencyObject parent) where T : DependencyObject
    { var count = parent is T ? 1 : 0; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) count += Count<T>(VisualTreeHelper.GetChild(parent, i)); return count; }
    private static void Capture(FrameworkElement window, string path)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(path); encoder.Save(stream);
    }
}
