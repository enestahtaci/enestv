using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using EnesTV.App.ViewModels;
using EnesTV.Core.Services;
namespace EnesTV.App.Views;
public partial class GuideWindow : Window
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private readonly MainViewModel _vm;
    private CancellationTokenSource? _query;
    private DateTime _day = DateTime.Today;
    public GuideWindow(MainViewModel vm)
    {
        InitializeComponent(); _vm = vm; DataContext = vm; Channels.ItemsSource = vm.AllChannels;
        Channels.SelectedItem = vm.AllChannels.FirstOrDefault(x => x.Channel.Id == vm.PlayingItem?.Channel.Id) ?? vm.AllChannels.FirstOrDefault();
        ShowDay(DateTime.Today);
        Closed += (_, _) => { _query?.Cancel(); _query?.Dispose(); };
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (_vm != null) Channels.ItemsSource = _vm.AllChannels.Where(x => ChannelFilter.Matches(x.Channel, Search.Text)).ToArray(); }
    private void PreviousDay_Click(object sender, RoutedEventArgs e) => ShowDay(_day.AddDays(-1));
    private void NextDay_Click(object sender, RoutedEventArgs e) => ShowDay(_day.AddDays(1));
    private void Today_Click(object sender, RoutedEventArgs e) => ShowDay(DateTime.Today);
    private void ShowDay(DateTime day)
    {
        _day = day.Date;
        // e.g. "8 Kasım 2026, Pazar"; today and tomorrow get a short label in front.
        var label = _day.ToString("d MMMM yyyy, dddd", Turkish);
        var offset = (_day - DateTime.Today).Days;
        DayText.Text = offset switch { 0 => "Bugün · " + label, 1 => "Yarın · " + label, -1 => "Dün · " + label, _ => label };
        TodayButton.IsEnabled = offset != 0;
        LoadGuide();
    }
    private void Channel_Changed(object sender, SelectionChangedEventArgs e) => LoadGuide();
    private async void LoadGuide()
    {
        if (_vm == null || Channels.SelectedItem is not ChannelItem channel) return;
        _query?.Cancel(); _query?.Dispose(); _query = new(); var ct = _query.Token;
        try
        {
            var programmes = await App.Store.GuideAsync(channel.Channel.TvgId, new DateTimeOffset(_day), ct);
            if (ct.IsCancellationRequested) return;
            Programmes.ItemsSource = programmes; Empty.Text = "Bu kanal ve gün için program bilgisi yok."; Empty.Visibility = programmes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _vm.Error(ex); }
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) { try { await _vm.RefreshEpgAsync(); LoadGuide(); } catch (Exception ex) { _vm.Error(ex); } }
}
