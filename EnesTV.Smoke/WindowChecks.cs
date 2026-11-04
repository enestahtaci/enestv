using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using EnesTV.App.Views;

namespace EnesTV.Smoke;

internal static class WindowChecks
{
    public static void Caption(Window window, bool light)
    {
        var page = ((SolidColorBrush)Application.Current.FindResource("Page")).Color;
        Assert(page == (Color)ColorConverter.ConvertFromString(light ? "#EEF1F5" : "#101216"), "Caption palette follows active theme");
        // Caption colors are set-only DWM attributes; verify the native setter HRESULTs.
        Assert(WindowAppearance.Apply(window), "Windows accepted caption, text, border and dark-mode colors");
    }
    public static void Fullscreen(Window window)
    {
        window.UpdateLayout();
        var root = (Grid)window.FindName("RootGrid");
        var panel = (Grid)window.FindName("PlayerPanel");
        var frame = (Border)window.FindName("VideoFrame");
        var video = (FrameworkElement)window.FindName("Video");
        Assert(window.WindowStyle == WindowStyle.None && window.ResizeMode == ResizeMode.NoResize, "Fullscreen has no native frame");
        Assert(root.Margin == new Thickness(0) && frame.CornerRadius == new CornerRadius(0), "Fullscreen has no margins or rounded corners");
        Assert(root.Children.Cast<UIElement>().Where(x => x != panel).All(x => x.Visibility == Visibility.Collapsed), "Sidebar and header hidden");
        Assert(panel.Children.Cast<UIElement>().Where(x => x != frame).All(x => x.Visibility == Visibility.Collapsed), "Player controls, channel details and EPG hidden");
        Assert(Math.Abs(video.ActualWidth - root.ActualWidth) < 1 && Math.Abs(video.ActualHeight - root.ActualHeight) < 1, "Video uses entire WPF client area");
        var handle = new WindowInteropHelper(window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        Assert(GetMonitorInfo(MonitorFromWindow(handle, 2), ref info) && GetWindowRect(handle, out var bounds), "Native monitor geometry available");
        GetWindowRect(handle, out var actual);
        Assert(actual.Left == info.Monitor.Left && actual.Top == info.Monitor.Top && actual.Right == info.Monitor.Right && actual.Bottom == info.Monitor.Bottom, "Window covers monitor including taskbar area");
        GetClientRect(handle, out var client);
        var dpi = VisualTreeHelper.GetDpi(window);
        Assert(Math.Abs(video.ActualWidth * dpi.DpiScaleX - (client.Right - client.Left)) < 2 && Math.Abs(video.ActualHeight * dpi.DpiScaleY - (client.Bottom - client.Top)) < 2, "Video dimensions match native client pixels at current DPI");
    }
    private static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect bounds);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(IntPtr hwnd, out NativeRect bounds);
}
