using System.Windows;
using System.Windows.Controls;
using EnesTV.App.ViewModels;
using EnesTV.Core.Models;
namespace EnesTV.App.Views;
public partial class SettingsWindow : Window
{
    private readonly MainViewModel _vm;
    public SettingsWindow(MainViewModel vm)
    {
        InitializeComponent(); _vm = vm; DataContext = vm;
        ThemeChoice.SelectedIndex = vm.Settings.Theme == "Light" ? 1 : 0; DefaultVolume.Value = vm.Settings.DefaultVolume; AutoReconnect.IsChecked = vm.Settings.AutoReconnect; EpgUrl.Text = vm.Settings.EpgUrl;
    }
    private UserSettings ReadSettings() => new(ThemeChoice.SelectedIndex == 1 ? "Light" : "Dark", (int)DefaultVolume.Value, AutoReconnect.IsChecked == true, EpgUrl.Text.Trim());
    private async void Save_Click(object sender, RoutedEventArgs e) { try { await _vm.SaveSettingsAsync(ReadSettings()); } catch (Exception ex) { _vm.Error(ex); } }
    private async void Epg_Click(object sender, RoutedEventArgs e) { try { await _vm.SaveSettingsAsync(ReadSettings()); await _vm.RefreshEpgAsync(); } catch (Exception ex) { _vm.Error(ex); } }
    private void Add_Click(object sender, RoutedEventArgs e) => new SourceDialog(_vm) { Owner = this }.ShowDialog();
    private void Edit_Click(object sender, RoutedEventArgs e) { if (_vm.SelectedSource != null) new SourceDialog(_vm, _vm.SelectedSource) { Owner = this }.ShowDialog(); }
    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var source = _vm.SelectedSource; if (source == null) return;
        if (MessageBox.Show(this, $"“{source.Name}” kaynağı, kanalları ve bu kaynağın favori/geçmiş kayıtları silinsin mi?", "Kaynağı sil", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try { await _vm.DeleteSourceAsync(source); } catch (Exception ex) { _vm.Error(ex); }
    }
    private async void Rename_Click(object sender, RoutedEventArgs e)
    {
        var source = _vm.SelectedSource; if (source == null) return;
        var panel = new StackPanel { Margin = new Thickness(24) }; var name = new TextBox { Text = source.Name }; panel.Children.Add(name); var save = new Button { Content = "Kaydet", IsDefault = true, Margin = new Thickness(0, 15, 0, 0) }; panel.Children.Add(save);
        var dialog = new Window { Title = "Kaynak adı", Owner = this, Width = 420, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel }; save.Click += (_, _) => dialog.DialogResult = true;
        if (dialog.ShowDialog() == true) { try { await _vm.RenameSourceAsync(source, name.Text); } catch (Exception ex) { _vm.Error(ex); } }
    }
}
