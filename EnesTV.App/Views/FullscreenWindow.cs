using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace EnesTV.App.Views;

// WM_GETMINMAXINFO uses physical monitor pixels, including the taskbar area.
internal sealed class FullscreenWindow
{
    private readonly Window _window;
    private bool _active;
    private WindowState _state;
    private WindowStyle _style;
    private ResizeMode _resizeMode;
    private Rect _restore;
    private double _minWidth, _minHeight;
    private NativeRect _monitor;

    public FullscreenWindow(Window window)
    {
        _window = window;
        window.SourceInitialized += (_, _) => HwndSource.FromHwnd(new WindowInteropHelper(window).Handle)?.AddHook(WindowMessage);
    }
    public void Enter()
    {
        if (_active) return;
        var handle = new WindowInteropHelper(_window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref info)) throw new InvalidOperationException("Monitor bounds unavailable");
        _monitor = info.Monitor;
        _state = _window.WindowState; _style = _window.WindowStyle; _resizeMode = _window.ResizeMode;
        _restore = _state == WindowState.Normal ? new Rect(_window.Left, _window.Top, _window.Width, _window.Height) : _window.RestoreBounds;
        _minWidth = _window.MinWidth; _minHeight = _window.MinHeight;
        _active = true;
        _window.WindowState = WindowState.Normal;
        _window.MinWidth = 0; _window.MinHeight = 0;
        _window.WindowStyle = WindowStyle.None; _window.ResizeMode = ResizeMode.NoResize;
        WindowAppearance.SetAttribute(handle, 33, 1); // DWMWCP_DONOTROUND
        _window.WindowState = WindowState.Maximized;
        _ = SetWindowPos(handle, IntPtr.Zero, _monitor.Left, _monitor.Top,
            _monitor.Right - _monitor.Left, _monitor.Bottom - _monitor.Top, 0x0020 | 0x0040);
    }
    public void Exit()
    {
        if (!_active) return;
        _active = false;
        _window.WindowState = WindowState.Normal;
        _window.WindowStyle = _style; _window.ResizeMode = _resizeMode;
        _window.MinWidth = _minWidth; _window.MinHeight = _minHeight;
        _window.Left = _restore.Left; _window.Top = _restore.Top; _window.Width = _restore.Width; _window.Height = _restore.Height;
        WindowAppearance.SetAttribute(new WindowInteropHelper(_window).Handle, 33, 0);
        _window.WindowState = _state;
        WindowAppearance.Apply(_window);
    }
    private IntPtr WindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_active && message == 0x0024)
        {
            var bounds = Marshal.PtrToStructure<MinMaxInfo>(lParam);
            bounds.MaxPosition = new NativePoint();
            bounds.MaxSize = new NativePoint { X = _monitor.Right - _monitor.Left, Y = _monitor.Bottom - _monitor.Top };
            bounds.MaxTrackSize = bounds.MaxSize;
            Marshal.StructureToPtr(bounds, lParam, false); handled = true;
        }
        return IntPtr.Zero;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct MinMaxInfo { public NativePoint Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
}
