using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace EnesTV.App.Views;

// Keep native Windows caption buttons and resizing, but use the app's palette.
public static class WindowAppearance
{
    public static readonly DependencyProperty MatchThemeProperty = DependencyProperty.RegisterAttached(
        "MatchTheme", typeof(bool), typeof(WindowAppearance), new PropertyMetadata(false, OnMatchThemeChanged));
    public static bool GetMatchTheme(DependencyObject target) => (bool)target.GetValue(MatchThemeProperty);
    public static void SetMatchTheme(DependencyObject target, bool value) => target.SetValue(MatchThemeProperty, value);
    private static void OnMatchThemeChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
    {
        if (target is not Window window) return;
        if ((bool)e.NewValue) { window.SourceInitialized += OnSourceInitialized; Apply(window); }
        else window.SourceInitialized -= OnSourceInitialized;
    }
    private static void OnSourceInitialized(object? sender, EventArgs e) { if (sender is Window window) Apply(window); }
    public static bool Apply(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return false;
        var page = ((SolidColorBrush)Application.Current.FindResource("Page")).Color;
        var text = ((SolidColorBrush)Application.Current.FindResource("Text")).Color;
        var dark = SetAttribute(handle, 20, page.R < 128 ? 1 : 0); // DWMWA_USE_IMMERSIVE_DARK_MODE
        var caption = SetAttribute(handle, 35, ColorRef(page));   // DWMWA_CAPTION_COLOR
        var foreground = SetAttribute(handle, 36, ColorRef(text)); // DWMWA_TEXT_COLOR
        var border = SetAttribute(handle, 34, ColorRef(page));    // DWMWA_BORDER_COLOR
        return dark == 0 && caption == 0 && foreground == 0 && border == 0;
    }
    internal static int SetAttribute(IntPtr handle, int attribute, int value) =>
        DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));
    private static int ColorRef(Color color) => color.R | color.G << 8 | color.B << 16;
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
