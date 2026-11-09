using System.Windows;
using System.Windows.Controls;
using EnesTV.Core.Services;

namespace EnesTV.App.Views;

// Asks for the backup passphrase; when creating a backup it is asked twice and must match.
public sealed class PassphraseDialog : Window
{
    private readonly PasswordBox _first = new(), _second = new();
    private readonly TextBlock _error = new() { Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("Warning"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 0), Visibility = Visibility.Collapsed };
    private readonly bool _confirm;
    public string Passphrase => _first.Password;

    public PassphraseDialog(string title, string message, bool confirm)
    {
        Style = (Style)Application.Current.FindResource(typeof(Window));
        Title = title; Width = 460; SizeToContent = SizeToContent.Height; ResizeMode = ResizeMode.NoResize; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        _confirm = confirm;
        var panel = new StackPanel { Margin = new Thickness(26) };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("Muted"), Margin = new Thickness(0, 8, 0, 16) });
        panel.Children.Add(new TextBlock { Text = "Parola", Margin = new Thickness(0, 0, 0, 6) });
        AutomationProperties(_first, "Yedek parolası"); panel.Children.Add(_first);
        if (confirm)
        {
            panel.Children.Add(new TextBlock { Text = "Parola (tekrar)", Margin = new Thickness(0, 12, 0, 6) });
            AutomationProperties(_second, "Yedek parolası tekrar"); panel.Children.Add(_second);
        }
        panel.Children.Add(_error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        var cancel = new Button { Content = "Vazgeç", IsCancel = true };
        var ok = new Button { Content = confirm ? "Yedeği oluştur" : "Devam", IsDefault = true, Style = (Style)Application.Current.FindResource("PrimaryButton") };
        ok.Click += (_, _) => Accept();
        buttons.Children.Add(cancel); buttons.Children.Add(ok); panel.Children.Add(buttons);
        Content = panel;
        Loaded += (_, _) => _first.Focus();
    }
    private static void AutomationProperties(PasswordBox box, string name) => System.Windows.Automation.AutomationProperties.SetName(box, name);
    private void Accept()
    {
        try
        {
            BackupFile.CheckPassphrase(_first.Password);
            if (_confirm && _first.Password != _second.Password) throw new UserInputException("Parolalar aynı değil.");
            DialogResult = true;
        }
        catch (UserInputException ex) { _error.Text = ex.Message; _error.Visibility = Visibility.Visible; }
    }
}
