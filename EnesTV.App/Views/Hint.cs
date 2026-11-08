using System.Windows;

namespace EnesTV.App.Views;

// Placeholder text for TextBox: <TextBox views:Hint.Text="Kanal ara…"/>. Shown by the shared TextBox template while empty.
public static class Hint
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(Hint), new FrameworkPropertyMetadata(""));
    public static string GetText(DependencyObject target) => (string)target.GetValue(TextProperty);
    public static void SetText(DependencyObject target, string value) => target.SetValue(TextProperty, value);
}
