using System.Windows;
using System.Windows.Controls;
using EnesTV.App.Services;
namespace EnesTV.App.Views;
public sealed class LazyLogo : Image
{
    public static LogoCache? Cache { get; set; }
    private CancellationTokenSource? _pending;
    public static readonly DependencyProperty UrlProperty = DependencyProperty.Register(nameof(Url), typeof(string), typeof(LazyLogo), new PropertyMetadata("", OnUrlChanged));
    public string Url { get => (string)GetValue(UrlProperty); set => SetValue(UrlProperty, value); }
    public LazyLogo() { Loaded += (_, _) => Load(); Unloaded += (_, _) => { _pending?.Cancel(); Source = null; }; }
    private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((LazyLogo)d).Load();
    private async void Load()
    {
        _pending?.Cancel(); _pending?.Dispose(); _pending = null; Source = null;
        if (!IsLoaded || Cache == null || string.IsNullOrWhiteSpace(Url)) return;
        _pending = new(); var ct = _pending.Token;
        try { var image = await Cache.GetAsync(Url, ct); if (!ct.IsCancellationRequested && IsLoaded) Source = image; }
        catch (OperationCanceledException) { }
    }
}
