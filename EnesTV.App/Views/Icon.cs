using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EnesTV.App.Views;

// Vector paths stay sharp at Windows display scales and never depend on emoji fonts.
public sealed class Icon : Control
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(string), typeof(Icon), new FrameworkPropertyMetadata("Play", FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    private static readonly IReadOnlyDictionary<string, Geometry> Paths = new Dictionary<string, string>
    {
        ["Play"] = "M8,5 L19,12 8,19 Z",
        ["Pause"] = "M8,5 L8,19 M16,5 L16,19",
        ["Stop"] = "M6,6 L18,6 18,18 6,18 Z",
        ["Previous"] = "M6,5 L6,19 M18,5 L9,12 18,19 Z",
        ["Next"] = "M18,5 L18,19 M6,5 L15,12 6,19 Z",
        ["Back"] = "M10,5 L3,12 10,19 M3,12 L15,12 Q21,12 21,20",
        ["Refresh"] = "M20,9 A8,8 0 1 0 20,16 M20,3 L20,9 14,9",
        ["Fullscreen"] = "M9,3 L3,3 3,9 M15,3 L21,3 21,9 M3,15 L3,21 9,21 M15,21 L21,21 21,15",
        ["Tv"] = "M3,5 L21,5 21,18 3,18 Z M8,22 L16,22 M12,18 L12,22",
        ["Star"] = "M12,3 L15,9 22,10 17,15 18,22 12,18 6,22 7,15 2,10 9,9 Z",
        ["Clock"] = "M21,12 A9,9 0 1 1 3,12 A9,9 0 1 1 21,12 M12,6 L12,12 16,15",
        ["Grid"] = "M3,3 L9,3 9,9 3,9 Z M15,3 L21,3 21,9 15,9 Z M3,15 L9,15 9,21 3,21 Z M15,15 L21,15 21,21 15,21 Z",
        ["Series"] = "M3,7 L18,7 18,21 3,21 Z M7,3 L22,3 22,17 M8,11 L14,14 8,17 Z",
        ["List"] = "M3,6 L4,6 M8,6 L21,6 M3,12 L4,12 M8,12 L21,12 M3,18 L4,18 M8,18 L21,18",
        ["Keyboard"] = "M2,5 L22,5 22,19 2,19 Z M6,9 L7,9 M11,9 L12,9 M16,9 L17,9 M6,13 L7,13 M11,13 L12,13 M16,13 L17,13 M7,16 L17,16",
        ["Guide"] = "M3,4 L21,4 21,21 3,21 Z M3,9 L21,9 M8,9 L8,21 M12,13 L18,13 M12,17 L18,17",
        ["Settings"] = "M3,6 L21,6 M3,12 L21,12 M3,18 L21,18 M8,3 L8,9 M16,9 L16,15 M9,15 L9,21",
        ["Search"] = "M17,10 A7,7 0 1 1 3,10 A7,7 0 1 1 17,10 M15,15 L22,22",
        ["Plus"] = "M12,4 L12,20 M4,12 L20,12",
        ["Volume"] = "M3,9 L7,9 12,4 12,20 7,15 3,15 Z M16,8 Q20,12 16,16 M19,4 Q26,12 19,20"
    }.ToDictionary(x => x.Key, x => { var geometry = Geometry.Parse(x.Value); geometry.Freeze(); return geometry; });

    public Icon() { Width = 18; Height = 18; Focusable = false; IsHitTestVisible = false; }
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (!Paths.TryGetValue(Kind, out var path)) return;
        var pen = new Pen(Foreground, 1.65) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        context.PushTransform(new ScaleTransform(ActualWidth / 24, ActualHeight / 24));
        context.DrawGeometry(null, pen, path);
        context.Pop();
    }
}
