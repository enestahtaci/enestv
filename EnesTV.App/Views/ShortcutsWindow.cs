using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnesTV.App.Services;
using EnesTV.App.ViewModels;

namespace EnesTV.App.Views;

public sealed class ShortcutsWindow : Window
{
    public ShortcutsWindow(MainViewModel vm)
    {
        SetResourceReference(StyleProperty, typeof(Window));
        Title = "enestv · Klavye kısayolları"; Width = 560; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(24) }; Content = panel;
        panel.Children.Add(new TextBlock { Text = "Klavye kısayolları", FontSize = 25 });
        panel.Children.Add(new TextBlock { Text = "Bir kutuya tıkla ve istediğin tuşa veya tuş birleşimine bas.\nF11 / Esc ve rakamla kanal seçimi sabittir.\nRakamları yaz, Enter'a bas veya 1,2 saniye bekle.", Margin = new Thickness(0, 12, 0, 18) });
        var fields = new Dictionary<string, TextBox>();
        foreach (var (id, label, gesture) in PlaybackShortcuts.Defaults)
        {
            var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
            var field = new TextBox { Width = 180, IsReadOnly = true, Text = vm.Layout.Shortcuts.GetValueOrDefault(id) ?? gesture };
            field.PreviewKeyDown += (_, e) => { if (e.Key == Key.Tab) return; var key = e.Key == Key.System ? e.SystemKey : e.Key; if (PlaybackShortcuts.Allowed(key, Keyboard.Modifiers)) field.Text = PlaybackShortcuts.Format(key, Keyboard.Modifiers); e.Handled = true; };
            fields[id] = field; DockPanel.SetDock(field, Dock.Right); row.Children.Add(field);
            row.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center }); panel.Children.Add(row);
        }
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) }; panel.Children.Add(status);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) }; panel.Children.Add(buttons);
        var reset = new Button { Content = "Varsayılanlar" }; reset.Click += (_, _) => { foreach (var item in PlaybackShortcuts.Defaults) fields[item.Id].Text = item.Gesture; }; buttons.Children.Add(reset);
        var cancel = new Button { Content = "Vazgeç", IsCancel = true }; buttons.Children.Add(cancel);
        var save = new Button { Content = "Kaydet" }; buttons.Children.Add(save);
        var saving = false; Closing += (_, e) => { if (saving) e.Cancel = true; };
        save.Click += async (_, _) =>
        {
            var values = fields.ToDictionary(x => x.Key, x => x.Value.Text);
            if (values.Values.Distinct().Count() != values.Count) { status.Text = "Aynı kısayolu iki farklı işleme atayamazsınız."; return; }
            saving = true; IsEnabled = false;
            try { await vm.SaveShortcutsAsync(values); saving = false; Close(); }
            catch (Exception ex) { vm.Error(ex); status.Text = vm.Status; }
            finally { saving = false; IsEnabled = true; }
        };
    }
}
