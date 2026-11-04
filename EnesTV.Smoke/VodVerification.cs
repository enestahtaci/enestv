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
using EnesTV.Core.Services;

namespace EnesTV.Smoke;

internal static class VodVerification
{
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static async Task Until(Func<bool> predicate, string message, int seconds = 20)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        while (!predicate()) { if (DateTime.UtcNow > until) throw new TimeoutException(message); await Task.Delay(100); }
    }
    public static Task RunAsync(string root, FixtureServer server)
    {
        var url = server.Url;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            Environment.SetEnvironmentVariable("ENESTV_DATA_DIR", Path.Combine(root, "vod-library"));
            var app = new EnesTV.App.App(); app.InitializeComponent();
            var start = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            start.Tick += async (_, _) =>
            {
                if (app.MainWindow?.DataContext is not MainViewModel main) return; start.Stop();
                VodWindow? window = null;
                try
                {
                    await Task.Delay(400);
                    await main.AddSourceAsync("Xtream fixture", SourceKind.Xtream, new(url, "test", "test"));
                    window = new VodWindow(main.Sources, main.Settings with { AutoReconnect = false }, VodKind.Movie) { Owner = app.MainWindow }; window.Show();
                    var vm = window.ViewModel;
                    await Until(() => !vm.Busy && vm.Items.Count == 10000, "Movie catalog");
                    Check(vm.Categories.Contains("Yerel test"), "Xtream movie categories and 10000 films");
                    server.FailVodCatalog = true; await vm.LoadCatalogAsync(true);
                    Check(vm.Items.Count == 10000 && vm.Status.Contains("Kayıtlı katalog"), "Failed refresh preserves cached catalog"); server.FailVodCatalog = false;
                    var list = (ListBox)window.FindName("CatalogList");
                    Check(Count<ListBoxItem>(list) < 50, "Movie catalog virtualizes 10000 items");
                    vm.Search = "09999"; await Until(() => vm.Items.Count == 1, "Movie search"); Check(vm.Items[0].Id == "9999", "Movie search");
                    vm.Search = "no result 58ad"; await Until(() => vm.IsEmpty, "Empty movie search");
                    Check(vm.EmptyTitle == "Sonuç bulunamadı", "VOD search has actionable empty state");
                    Capture(window, Path.Combine(root, "vod-empty-search.png"));
                    vm.Search = ""; await Until(() => vm.Items.Count == 10000, "Reset search");
                    vm.Selected = vm.Items[0]; await Until(() => !vm.DetailBusy && vm.CanPlay && vm.Detail?.Plot.Length > 0, "Movie details");
                    Check(vm.Detail!.Poster.Length > 0, "Movie details and poster");
                    Capture(window, Path.Combine(root, "movie-ready.png"));
                    Capture((FrameworkElement)window.FindName("VideoOverlay"), Path.Combine(root, "vod-ready-overlay.png"));
                    await vm.PlaySelectionAsync(false); await Until(() => vm.Playback.Player.IsPlaying && vm.CanSeek && vm.Duration > 80, "Movie playback");
                    Check(vm.Duration > 80, "Actual LibVLC VOD playback and duration");
                    var snapshot = Path.Combine(root, "vod-decoded.png");
                    await Until(() => vm.Playback.Player.TakeSnapshot(0, snapshot, 320, 180), "VOD video output ready");
                    await Until(() => File.Exists(snapshot) && new FileInfo(snapshot).Length > 1000, "VOD decoded frame");
                    Check(true, "VOD video frame decoded");
                    var seek = (Slider)window.FindName("Seek");
                    seek.Focus(); seek.Value = 15;
                    seek.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(seek), Environment.TickCount, Key.Right) { RoutedEvent = Keyboard.PreviewKeyUpEvent });
                    await Until(() => vm.Position >= 14 && vm.Playback.Player.Time >= 14000, "Seek slider key event");
                    Check(true, "Bound seek slider applies playback position");
                    await vm.SeekAsync(30); await Until(() => vm.Position > 28, "Seek");
                    await vm.Playback.TogglePauseAsync(); await Task.Delay(300); Check(vm.Playback.Player.State == LibVLCSharp.Shared.VLCState.Paused, "VOD pause");
                    await vm.SaveProgressAsync(); var movieKey = vm.Detail.Key;
                    var progress = await EnesTV.App.App.Store.LoadProgressAsync(main.Sources[0].Id);
                    Check(progress[movieKey].PositionMs >= 28000, "Resume position saved in SQLite");
                    Capture(window, Path.Combine(root, "movies.png"));
                    Theme.Apply("Light"); await Task.Delay(150); Capture(window, Path.Combine(root, "movies-light.png")); Theme.Apply("Dark");
                    ((Button)window.FindName("FullscreenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(300);
                    var video = (FrameworkElement)window.FindName("Video"); var layout = (Grid)window.FindName("Root");
                    Check(Math.Abs(video.ActualWidth - layout.ActualWidth) < 1 && Math.Abs(video.ActualHeight - layout.ActualHeight) < 1, "VOD fullscreen uses entire viewport");
                    Capture((FrameworkElement)window.FindName("VideoOverlay"), Path.Combine(root, "vod-fullscreen-overlay.png"));
                    window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
                    window.Close(); await Until(() => !window.IsVisible, "Close and save");
                    window = new VodWindow(main.Sources, main.Settings with { AutoReconnect = false }, VodKind.Movie) { Owner = app.MainWindow }; window.Show(); vm = window.ViewModel;
                    await Until(() => !vm.Busy && vm.Items.Count > 0, "Cached catalog"); vm.Selected = vm.Items[0]; await Until(() => vm.CanPlay, "Movie ready after reopen");
                    Check(vm.ResumeLabel.Contains("Kaldığınız yer"), "Resume offered after reopening window");
                    await vm.PlaySelectionAsync(false); await Until(() => vm.Playback.Player.IsPlaying && vm.Position >= 28 && vm.CanSeek, "Resume playback position");
                    Check(vm.Position >= 28, "LibVLC resumes at saved timestamp");
                    await vm.PlaySelectionAsync(true); await Until(() => vm.Playback.Player.IsPlaying && vm.Duration > 80, "Restart"); await Task.Delay(1000);
                    Check(vm.Position < 10, "Restart plays from beginning");
                    await vm.SeekAsync(vm.Duration - 2); await Until(() => vm.PlaybackText == "İçerik tamamlandı", "Natural completion"); await Task.Delay(800);
                    progress = await EnesTV.App.App.Store.LoadProgressAsync(main.Sources[0].Id);
                    Check(progress[movieKey].Completed && !vm.Playback.Player.IsPlaying, "VOD EOF marks watched and does not reconnect");
                    vm.Kind = VodKind.Series; await Until(() => !vm.Busy && vm.Items.Count == 1, "Series catalog");
                    vm.Selected = vm.Items[0]; await Until(() => !vm.DetailBusy && vm.Seasons.Count == 2, "Series details");
                    Check(vm.Episodes.Count == 2 && vm.Episodes[0].Episode.Id == "101", "Seasons and episodes sorted from API response");
                    vm.Season = 2; Check(vm.Episodes.Count == 1 && vm.SelectedEpisode?.Episode.Id == "201", "Season selection changes episode list");
                    vm.Season = 1; await vm.PlaySelectionAsync(false); await Until(() => vm.Playback.Player.IsPlaying && vm.CanSeek, "Episode playback");
                    Check(vm.PlayingName.Contains("İlk bölüm") && vm.HasNextEpisode, "Episode playback and next episode available");
                    Capture(window, Path.Combine(root, "series.png"));
                    window.Width = 1100; window.Height = 760; await Task.Delay(200); Capture(window, Path.Combine(root, "vod-small.png"));
                    var detailsPanel = (FrameworkElement)window.FindName("DetailsPanel");
                    Check(detailsPanel.TranslatePoint(new Point(0, detailsPanel.ActualHeight), window).Y <= window.ActualHeight, "VOD details fit small window");
                    var watchButton = (Button)window.FindName("WatchButton");
                    var buttonBottom = watchButton.TranslatePoint(new Point(0, watchButton.ActualHeight), detailsPanel).Y;
                    Check(watchButton.IsVisible && buttonBottom <= detailsPanel.ActualHeight, "Watch button remains visible without scrolling details");
                    window.Width = 1450; window.Height = 940;
                    await vm.PlayNextEpisodeAsync(); Check(vm.PlayingName.Contains("İkinci bölüm"), "Next episode within season");
                    await vm.PlayNextEpisodeAsync(); Check(vm.PlayingName.Contains("İkinci sezon") && !vm.HasNextEpisode, "Next episode across seasons and end of series");
                    await Until(() => vm.CanSeek && vm.Playback.Player.IsPlaying, "Last episode ready"); await vm.SeekAsync(22); await Until(() => vm.Position > 20, "Episode seek"); await vm.SaveProgressAsync();
                    vm.Kind = VodKind.Movie; vm.Kind = VodKind.Series; await Until(() => !vm.Busy && vm.Items.Count == 1 && vm.Items[0].Kind == VodKind.Series, "Rapid catalog switch");
                    Check(true, "Latest catalog selection wins");
                    vm.Selected = vm.Items[0]; await Until(() => vm.CanPlay && vm.SelectedEpisode?.Episode.Id == "201", "Resume episode selection");
                    Check(vm.Season == 2 && vm.ResumeLabel.Contains("Kaldığınız yer"), "Series reselects last watched season and episode");
                    vm.Kind = VodKind.Movie; await Until(() => !vm.Busy && vm.Items.Count > 0, "Movie reload for broken stream");
                    vm.Selected = vm.Items[0] with { Id = "missing" }; await Until(() => vm.CanPlay, "Broken movie selection");
                    await vm.PlaySelectionAsync(false); await Until(() => vm.PlaybackText.Contains("kesildi") || vm.PlaybackText.Contains("açılamadı"), "Broken movie error", 35);
                    Check(window.IsVisible, "Broken movie shows error without closing app");
                    var errorCard = (Border)window.FindName("PlaybackErrorCard");
                    Check(vm.PlaybackError && errorCard.IsVisible, "Playback error is visible over the native player with retry action");
                    Capture((FrameworkElement)window.FindName("VideoOverlay"), Path.Combine(root, "vod-error-overlay.png"));
                    window.Close(); await Until(() => !window.IsVisible, "VOD shutdown");
                    await main.PlayChannelAsync(main.Channels[0]); await Until(() => main.Playback.Player.IsPlaying, "Live before navigation");
                    ((Button)app.MainWindow.FindName("MoviesNav")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    await Until(() => Application.Current.Windows.OfType<VodWindow>().Any(), "Main movies button");
                    var fromNavigation = Application.Current.Windows.OfType<VodWindow>().Single();
                    Check(!main.Playback.Player.IsPlaying && fromNavigation.ViewModel.IsMovie, "Main Films button opens catalog and stops live stream");
                    fromNavigation.Close(); await Until(() => !fromNavigation.IsVisible, "Return to live navigation");
                    var empty = new VodWindow([], main.Settings, VodKind.Movie) { Owner = app.MainWindow }; empty.Show(); await Task.Delay(300);
                    Check(empty.ViewModel.Status.Contains("Xtream hesabı"), "No account has actionable empty state"); Capture(empty, Path.Combine(root, "vod-no-account.png")); empty.Close(); await Until(() => !empty.IsVisible, "Empty window shutdown");
                    server.EmptyLiveCatalog = true; await main.RefreshAsync(main.Sources[0]);
                    Check(main.Channels.Count == 1, "Empty live response preserves cached channels"); server.EmptyLiveCatalog = false;
                    await main.AddSourceAsync("VOD-only account", SourceKind.Xtream, new(url, "vodonly", "test"));
                    Check(main.Sources.Any(x => x.Name == "VOD-only account"), "Authenticated VOD-only account can be added without live channels");
                    await File.WriteAllTextAsync(Path.Combine(root, "vod-results.txt"), "Movie and series catalog, details, search, virtualization, seek, pause, saved resume, restart, EOF, seasons, next episode, account empty state and rapid switching passed.");
                    app.MainWindow.Close(); done.TrySetResult();
                }
                catch (Exception ex) { window?.Close(); app.MainWindow.Close(); done.TrySetException(ex); }
            };
            start.Start(); app.Run();
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return done.Task;
    }
    private static int Count<T>(DependencyObject parent) where T : DependencyObject { var n = parent is T ? 1 : 0; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) n += Count<T>(VisualTreeHelper.GetChild(parent, i)); return n; }
    private static void Capture(FrameworkElement element, string path) { element.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element); var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); png.Save(file); }
}
