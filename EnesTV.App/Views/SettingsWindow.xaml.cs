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
    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.SaveFileDialog { FileName = $"enestv-yedek-{DateTime.Now:yyyy-MM-dd}.enestv", Filter = "enestv yedeği (*.enestv)|*.enestv", DefaultExt = ".enestv" };
        if (picker.ShowDialog(this) != true) return;
        var dialog = new PassphraseDialog("Yedek parolası", "Yedeği açmak için bu parola gerekecek. En az 8 karakter; unutursan yedek açılamaz.", confirm: true) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        await RunBusy(() => _vm.ExportBackupAsync(picker.FileName, dialog.Passphrase));
    }
    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Filter = "enestv yedeği (*.enestv)|*.enestv|Tüm dosyalar|*.*" };
        if (picker.ShowDialog(this) != true) return;
        var dialog = new PassphraseDialog("Yedeği aç", "Yedeği oluştururken belirlediğin parolayı gir.", confirm: false) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        LibraryBackup? backup = null;
        await RunBusy(async () => backup = await _vm.ReadBackupAsync(picker.FileName, dialog.Passphrase));
        if (backup == null) return;
        var question = $"Yedek tarihi: {backup.Created.LocalDateTime:g}\n{backup.Sources.Count} kaynak, {backup.Channels.Count:N0} kanal, {backup.Favorites.Count} favori.\n\n" +
                       "Mevcut kütüphanen bu yedekle tamamen değiştirilecek. Önce otomatik bir güvenlik kopyası alınır. Devam edilsin mi?";
        if (MessageBox.Show(this, question, "Yedekten geri yükle", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        await RunBusy(() => _vm.RestoreBackupAsync(backup));
    }
    // Key derivation takes a moment; keep the window responsive and show errors in the status line.
    private async Task RunBusy(Func<Task> action)
    {
        BackupButton.IsEnabled = RestoreButton.IsEnabled = false; Cursor = System.Windows.Input.Cursors.Wait;
        try { await action(); }
        catch (Exception ex) { _vm.Error(ex); }
        finally { BackupButton.IsEnabled = RestoreButton.IsEnabled = true; Cursor = null; }
    }
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
