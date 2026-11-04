using System.Windows;
using System.Windows.Controls;
using EnesTV.App.ViewModels;
using EnesTV.Core.Services;
namespace EnesTV.App.Views;
public partial class GuideWindow : Window
{
    private readonly MainViewModel _vm;
    private CancellationTokenSource? _query;
    public GuideWindow(MainViewModel vm)
    {
        InitializeComponent(); _vm = vm; DataContext = vm; Channels.ItemsSource = vm.AllChannels; Day.SelectedDate = DateTime.Today;
        Channels.SelectedItem = vm.AllChannels.FirstOrDefault(x => x.Channel.Id == vm.PlayingItem?.Channel.Id) ?? vm.AllChannels.FirstOrDefault();
        Closed += (_, _) => { _query?.Cancel(); _query?.Dispose(); };
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) { if (_vm != null) Channels.ItemsSource = _vm.AllChannels.Where(x => ChannelFilter.Matches(x.Channel, Search.Text)).ToArray(); }
    private void Day_Changed(object? sender, SelectionChangedEventArgs e) => LoadGuide();
    private void Channel_Changed(object sender, SelectionChangedEventArgs e) => LoadGuide();
    private async void LoadGuide()
    {
        if (_vm == null || Channels.SelectedItem is not ChannelItem channel || Day.SelectedDate == null) return;
        _query?.Cancel(); _query?.Dispose(); _query = new(); var ct = _query.Token;
        try
        {
            var programmes = await App.Store.GuideAsync(channel.Channel.TvgId, new DateTimeOffset(Day.SelectedDate.Value), ct);
            if (ct.IsCancellationRequested) return;
            Programmes.ItemsSource = programmes; Empty.Text = "Bu kanal ve gün için program bilgisi yok."; Empty.Visibility = programmes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _vm.Error(ex); }
    }
    private async void Refresh_Click(object sender, RoutedEventArgs e) { try { await _vm.RefreshEpgAsync(); LoadGuide(); } catch (Exception ex) { _vm.Error(ex); } }
}
