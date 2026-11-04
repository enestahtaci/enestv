using System.Windows;
using System.Windows.Controls;
using EnesTV.App.ViewModels;
using EnesTV.Core.Models;
using Microsoft.Win32;
namespace EnesTV.App.Views;
public partial class SourceDialog : Window
{
    private readonly MainViewModel _vm;
    private readonly Source? _source;
    public SourceDialog(MainViewModel vm, Source? source = null)
    {
        InitializeComponent(); MaxHeight = Math.Max(300, SystemParameters.WorkArea.Height - 24); _vm = vm; _source = source; Kind.SelectedIndex = source == null ? 0 : (int)source.Kind;
        if (source != null) { SourceName.Text = source.Name; Location.Text = source.Secret.Location; Username.Text = source.Secret.Username; Password.Password = source.Secret.Password; Kind.IsEnabled = false; Save.Content = "Kaydet ve yenile"; }
        Closed += (_, _) => Password.Clear();
    }
    private SourceKind SelectedKind => Kind.SelectedIndex is >= 0 and <= 2 ? (SourceKind)Kind.SelectedIndex : SourceKind.M3uUrl;
    private void Kind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Credentials == null || KindHint == null) return;
        KindHint.Text = SelectedKind switch { SourceKind.Xtream => "Canlı TV, film ve diziler için sunucu adresi ve hesap bilgilerinizi girin.", SourceKind.M3uFile => "Bilgisayarınızdaki .m3u veya .m3u8 kanal listesini seçin.", _ => "M3U bağlantınızla canlı TV kanallarını içe aktarın." };
        Height = SelectedKind == SourceKind.Xtream ? 740 : 570;
        Credentials.Visibility = SelectedKind == SourceKind.Xtream ? Visibility.Visible : Visibility.Collapsed;
        Browse.Visibility = SelectedKind == SourceKind.M3uFile ? Visibility.Visible : Visibility.Collapsed;
        LocationLabel.Text = SelectedKind switch { SourceKind.Xtream => "Sunucu adresi", SourceKind.M3uFile => "Dosya konumu", _ => "M3U URL" };
    }
    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "M3U playlists|*.m3u;*.m3u8|Tüm dosyalar|*.*" };
        if (picker.ShowDialog(this) == true) { Location.Text = picker.FileName; if (SourceName.Text.Length == 0) SourceName.Text = System.IO.Path.GetFileNameWithoutExtension(picker.FileName); }
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_vm.Busy) { ErrorText.Text = "Önce devam eden yüklemeyi bitirin veya iptal edin."; return; }
        Save.IsEnabled = false; ErrorText.Text = "Kaynak yükleniyor…";
        try
        {
            var secret = new SourceSecret(Location.Text.Trim(), Username.Text.Trim(), Password.Password);
            if (_source == null) await _vm.AddSourceAsync(SourceName.Text, SelectedKind, secret);
            else await _vm.UpdateSourceAsync(_source, SourceName.Text, secret);
            if (IsVisible) DialogResult = true;
        }
        catch (Exception ex) { _vm.Error(ex); ErrorText.Text = _vm.Status; }
        finally { Save.IsEnabled = true; }
    }
}
