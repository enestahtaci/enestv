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

// Movies and series live inside the main window (left menu) and share its single video surface.
internal static class VodVerification
{
    private static void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); Console.WriteLine("PASS " + name); }
    private static async Task Until(Func<bool> predicate, string message, int seconds = 20)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        while (!predicate()) { if (DateTime.UtcNow > until) throw new TimeoutException(message); await Task.Delay(100); }
    }
    private static void Navigate(Window window, string button) { var nav = (Button)window.FindName(button); nav.Command.Execute(nav.CommandParameter); }
    private static void Escape(Window window) => window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(window), Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.PreviewKeyDownEvent });
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
                var window = app.MainWindow;
                try
                {
                    await Task.Delay(400);
                    var video = (LibVLCSharp.WPF.VideoView)window.FindName("Video");
                    await main.AddSourceAsync("Xtream fixture", SourceKind.Xtream, new(url, "test", "test"));
                    Navigate(window, "MoviesNav");
                    await Until(() => main.IsVod && main.Vod != null, "Movies section");
                    var vm = main.Vod!; vm.Playback.AutoReconnect = false;
                    // Only the main window and LibVLCSharp's overlay window exist: no separate catalog window.
                    Check(Application.Current.Windows.Cast<Window>().All(x => x == window || x.GetType().Name == "ForegroundWindow"), "Movies open inside the main window");
                    await Until(() => !vm.Busy && vm.Items.Count == 10000, "Movie catalog");
                    Check(vm.Categories.Contains("Yerel test"), "Xtream movie categories and 10000 films");
                    server.FailVodCatalog = true; await vm.LoadCatalogAsync(true);
                    Check(vm.Items.Count == 10000 && vm.Status.Contains("Kayıtlı katalog"), "Failed refresh preserves cached catalog"); server.FailVodCatalog = false;
                    var list = (ListBox)window.FindName("CatalogList");
                    Check(list.IsVisible && Count<ListBoxItem>(list) < 50, "Movie catalog virtualizes 10000 items");
                    Check(!((FrameworkElement)window.FindName("ChannelList")).IsVisible, "Catalog replaces the channel list");
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
                    Check(video.MediaPlayer == vm.Playback.Player, "Shared video surface shows the movie player");
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
                    ((Button)window.FindName("VodFullscreenButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(300);
                    var layout = (Grid)window.FindName("RootGrid");
                    Check(Math.Abs(video.ActualWidth - layout.ActualWidth) < 1 && Math.Abs(video.ActualHeight - layout.ActualHeight) < 1, "VOD fullscreen uses entire viewport");
                    Check(((FrameworkElement)window.FindName("VodOverlayControls")).Visibility == Visibility.Visible && ((FrameworkElement)window.FindName("FullscreenControls")).Visibility == Visibility.Collapsed, "Fullscreen shows movie controls, not live controls");
                    Capture((FrameworkElement)window.FindName("VideoOverlay"), Path.Combine(root, "vod-fullscreen-overlay.png"));
                    Escape(window); await Task.Delay(300);
                    Check(window.WindowStyle != WindowStyle.None && main.IsVod, "Esc in fullscreen exits fullscreen and stays in Movies");
                    main.Section = "Live TV"; await Until(() => !vm.Playback.Player.IsPlaying && video.MediaPlayer == main.Playback.Player, "Leave movies");
                    Check(true, "Leaving Movies stops the movie and restores the live player");
                    Navigate(window, "MoviesNav"); await Until(() => main.IsVod && main.Vod == vm && !vm.Busy && vm.Items.Count > 0, "Back to movies");
                    vm.Selected = vm.Items[0]; await Until(() => vm.CanPlay, "Movie ready after return");
                    Check(vm.ResumeLabel.Contains("Kaldığınız yer"), "Resume offered after returning to Movies");
                    await vm.PlaySelectionAsync(false); await Until(() => vm.Playback.Player.IsPlaying && vm.Position >= 28 && vm.CanSeek, "Resume playback position");
                    Check(vm.Position >= 28, "LibVLC resumes at saved timestamp");
                    await vm.PlaySelectionAsync(true); await Until(() => vm.Playback.Player.IsPlaying && vm.Duration > 80, "Restart"); await Task.Delay(1000);
                    Check(vm.Position < 10, "Restart plays from beginning");
                    await vm.SeekAsync(vm.Duration - 2); await Until(() => vm.PlaybackText == "İçerik tamamlandı", "Natural completion"); await Task.Delay(800);
                    progress = await EnesTV.App.App.Store.LoadProgressAsync(main.Sources[0].Id);
                    Check(progress[movieKey].Completed && !vm.Playback.Player.IsPlaying, "VOD EOF marks watched and does not reconnect");
                    Navigate(window, "SeriesNav"); await Until(() => vm.IsSeries && !vm.Busy && vm.Items.Count == 1, "Series catalog");
                    Check(main.Vod == vm && main.SectionTitle == "Diziler", "Series section reuses the catalog in the main window");
                    vm.Selected = vm.Items[0]; await Until(() => !vm.DetailBusy && vm.Seasons.Count == 2, "Series details");
                    Check(vm.Episodes.Count == 2 && vm.Episodes[0].Episode.Id == "101", "Seasons and episodes sorted from API response");
                    vm.Season = 2; Check(vm.Episodes.Count == 1 && vm.SelectedEpisode?.Episode.Id == "201", "Season selection changes episode list");
                    vm.Season = 1; await vm.PlaySelectionAsync(false); await Until(() => vm.Playback.Player.IsPlaying && vm.CanSeek, "Episode playback");
                    Check(vm.PlayingName.Contains("İlk bölüm") && vm.HasNextEpisode, "Episode playback and next episode available");
                    Capture(window, Path.Combine(root, "series.png"));
                    window.Width = 1120; window.Height = 730; await Task.Delay(300); Capture(window, Path.Combine(root, "vod-small.png"));
                    var detailsPanel = (FrameworkElement)window.FindName("DetailsPanel");
                    Check(detailsPanel.IsVisible && detailsPanel.TranslatePoint(new Point(0, detailsPanel.ActualHeight), window).Y <= window.ActualHeight, "VOD details fit small window");
                    var frame = (FrameworkElement)window.FindName("VideoFrame");
                    Check(frame.ActualHeight >= 150, "Small window keeps a usable video area in Series");
                    var watchButton = (Button)window.FindName("WatchButton");
                    var buttonBottom = watchButton.TranslatePoint(new Point(0, watchButton.ActualHeight), detailsPanel).Y;
                    Check(watchButton.IsVisible && buttonBottom <= detailsPanel.ActualHeight, "Watch button remains visible without scrolling details");
                    window.Width = 1480; window.Height = 900;
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
                    var errorCard = (Border)window.FindName("VodErrorCard");
                    Check(vm.PlaybackError && errorCard.IsVisible && !((FrameworkElement)window.FindName("PlaybackErrorCard")).IsVisible, "Playback error is visible over the native player with retry action");
                    Capture((FrameworkElement)window.FindName("VideoOverlay"), Path.Combine(root, "vod-error-overlay.png"));
                    main.Section = "Live TV"; await Until(() => video.MediaPlayer == main.Playback.Player, "Back to live");
                    await main.PlayChannelAsync(main.Channels[0]); await Until(() => main.Playback.Player.IsPlaying, "Live before navigation");
                    Navigate(window, "MoviesNav");
                    await Until(() => main.IsVod && !main.Playback.Player.IsPlaying && video.MediaPlayer == vm.Playback.Player, "Main movies button");
                    Check(vm.IsMovie && main.PlayingItem == null, "Main Films button opens catalog and stops live stream");
                    window.Activate(); var rootGrid = (Grid)window.FindName("RootGrid"); rootGrid.Focusable = true; Keyboard.Focus(rootGrid);
                    Escape(window);
                    await Until(() => main.Section == "Live TV" && video.MediaPlayer == main.Playback.Player, "Esc returns to live navigation");
                    Check(main.ResumeChannel?.Channel.Id == main.Channels[0].Channel.Id && ((FrameworkElement)window.FindName("ResumeButton")).IsVisible, "Live TV offers to continue the last channel");
                    var empty = new VodViewModel([], main.Settings, VodKind.Movie); await empty.InitializeAsync();
                    Check(empty.Status.Contains("Xtream hesabı"), "No account has actionable empty state"); await empty.DisposeAsync();
                    server.EmptyLiveCatalog = true; await main.RefreshAsync(main.Sources[0]);
                    Check(main.Channels.Count == 1, "Empty live response preserves cached channels"); server.EmptyLiveCatalog = false;
                    await main.AddSourceAsync("VOD-only account", SourceKind.Xtream, new(url, "vodonly", "test"));
                    Check(main.Sources.Any(x => x.Name == "VOD-only account"), "Authenticated VOD-only account can be added without live channels");
                    Navigate(window, "MoviesNav"); await Until(() => main.Vod != null && main.Vod != vm && main.Vod.Sources.Count == 2, "Catalog follows source changes");
                    Check(true, "Movies pick up a newly added Xtream account");
                    await File.WriteAllTextAsync(Path.Combine(root, "vod-results.txt"), "Movie and series catalog in the main window, shared video surface, details, search, virtualization, seek, pause, saved resume, restart, EOF, seasons, next episode, account empty state and rapid switching passed.");
                    window.Close(); done.TrySetResult();
                }
                catch (Exception ex) { window.Close(); done.TrySetException(ex); }
            };
            start.Start(); app.Run();
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); return done.Task;
    }
    private static int Count<T>(DependencyObject parent) where T : DependencyObject { var n = parent is T ? 1 : 0; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) n += Count<T>(VisualTreeHelper.GetChild(parent, i)); return n; }
    private static void Capture(FrameworkElement element, string path) { element.UpdateLayout(); var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(element); var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var file = File.Create(path); png.Save(file); }
}
