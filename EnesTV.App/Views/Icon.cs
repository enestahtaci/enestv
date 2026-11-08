using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EnesTV.App.Views;

// Vector paths stay sharp at Windows display scales and never depend on emoji fonts.
// Icon artwork: Lucide 0.460.0 (https://lucide.dev), ISC License; see THIRD-PARTY-NOTICES.md and licenses/Lucide-ISC.txt.
// Each SVG icon was flattened into one WPF path (rect/circle/line/polyline/polygon converted to path commands).
public sealed class Icon : Control
{
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(string), typeof(Icon), new FrameworkPropertyMetadata("Play", FrameworkPropertyMetadataOptions.AffectsRender));
    public string Kind { get => (string)GetValue(KindProperty); set => SetValue(KindProperty, value); }
    private static readonly IReadOnlyDictionary<string, Geometry> Paths = new Dictionary<string, string>
    {
        ["Play"] = "M 6 3 L 20 12 L 6 21 L 6 3 Z", // play
        ["Pause"] = "M 15 4 H 17 A 1 1 0 0 1 18 5 V 19 A 1 1 0 0 1 17 20 H 15 A 1 1 0 0 1 14 19 V 5 A 1 1 0 0 1 15 4 Z M 7 4 H 9 A 1 1 0 0 1 10 5 V 19 A 1 1 0 0 1 9 20 H 7 A 1 1 0 0 1 6 19 V 5 A 1 1 0 0 1 7 4 Z", // pause
        ["Stop"] = "M 5 3 H 19 A 2 2 0 0 1 21 5 V 19 A 2 2 0 0 1 19 21 H 5 A 2 2 0 0 1 3 19 V 5 A 2 2 0 0 1 5 3 Z", // square
        ["Previous"] = "M 19 20 L 9 12 L 19 4 L 19 20 Z M 5 19 L 5 5", // skip-back
        ["Next"] = "M 5 4 L 15 12 L 5 20 L 5 4 Z M 19 5 L 19 19", // skip-forward
        ["Back"] = "M 3 12 a 9 9 0 1 0 9 -9 a 9.75 9.75 0 0 0 -6.74 2.74 L 3 8 M 3 3 v 5 h 5 M 12 7 v 5 l 4 2", // history
        ["Refresh"] = "M 3 12 a 9 9 0 0 1 9 -9 a 9.75 9.75 0 0 1 6.74 2.74 L 21 8 M 21 3 v 5 h -5 M 21 12 a 9 9 0 0 1 -9 9 a 9.75 9.75 0 0 1 -6.74 -2.74 L 3 16 M 8 16 H 3 v 5", // refresh-cw
        ["Fullscreen"] = "M 8 3 H 5 a 2 2 0 0 0 -2 2 v 3 M 21 8 V 5 a 2 2 0 0 0 -2 -2 h -3 M 3 16 v 3 a 2 2 0 0 0 2 2 h 3 M 16 21 h 3 a 2 2 0 0 0 2 -2 v -3", // maximize
        ["Tv"] = "M 4 7 H 20 A 2 2 0 0 1 22 9 V 20 A 2 2 0 0 1 20 22 H 4 A 2 2 0 0 1 2 20 V 9 A 2 2 0 0 1 4 7 Z M 17 2 L 12 7 L 7 2", // tv
        ["Star"] = "M 11.525 2.295 a 0.53 0.53 0 0 1 0.95 0 l 2.31 4.679 a 2.123 2.123 0 0 0 1.595 1.16 l 5.166 0.756 a 0.53 0.53 0 0 1 0.294 0.904 l -3.736 3.638 a 2.123 2.123 0 0 0 -0.611 1.878 l 0.882 5.14 a 0.53 0.53 0 0 1 -0.771 0.56 l -4.618 -2.428 a 2.122 2.122 0 0 0 -1.973 0 L 6.396 21.01 a 0.53 0.53 0 0 1 -0.77 -0.56 l 0.881 -5.139 a 2.122 2.122 0 0 0 -0.611 -1.879 L 2.16 9.795 a 0.53 0.53 0 0 1 0.294 -0.906 l 5.165 -0.755 a 2.122 2.122 0 0 0 1.597 -1.16 z", // star
        ["Clock"] = "M 2 12 A 10 10 0 1 0 22 12 A 10 10 0 1 0 2 12 Z M 12 6 L 12 12 L 16 14", // clock
        ["Grid"] = "M 4 3 H 9 A 1 1 0 0 1 10 4 V 9 A 1 1 0 0 1 9 10 H 4 A 1 1 0 0 1 3 9 V 4 A 1 1 0 0 1 4 3 Z M 15 3 H 20 A 1 1 0 0 1 21 4 V 9 A 1 1 0 0 1 20 10 H 15 A 1 1 0 0 1 14 9 V 4 A 1 1 0 0 1 15 3 Z M 15 14 H 20 A 1 1 0 0 1 21 15 V 20 A 1 1 0 0 1 20 21 H 15 A 1 1 0 0 1 14 20 V 15 A 1 1 0 0 1 15 14 Z M 4 14 H 9 A 1 1 0 0 1 10 15 V 20 A 1 1 0 0 1 9 21 H 4 A 1 1 0 0 1 3 20 V 15 A 1 1 0 0 1 4 14 Z", // layout-grid
        ["Film"] = "M 5 3 H 19 A 2 2 0 0 1 21 5 V 19 A 2 2 0 0 1 19 21 H 5 A 2 2 0 0 1 3 19 V 5 A 2 2 0 0 1 5 3 Z M 7 3 v 18 M 3 7.5 h 4 M 3 12 h 18 M 3 16.5 h 4 M 17 3 v 18 M 17 7.5 h 4 M 17 16.5 h 4", // film
        ["Series"] = "M 20.2 6 L 3 11 l -0.9 -2.4 c -0.3 -1.1 0.3 -2.2 1.3 -2.5 l 13.5 -4 c 1.1 -0.3 2.2 0.3 2.5 1.3 Z M 6.2 5.3 l 3.1 3.9 M 12.4 3.4 l 3.1 4 M 3 11 h 18 v 8 a 2 2 0 0 1 -2 2 H 5 a 2 2 0 0 1 -2 -2 Z", // clapperboard
        ["List"] = "M 3 12 h 0.01 M 3 18 h 0.01 M 3 6 h 0.01 M 8 12 h 13 M 8 18 h 13 M 8 6 h 13", // list
        ["Keyboard"] = "M 10 8 h 0.01 M 12 12 h 0.01 M 14 8 h 0.01 M 16 12 h 0.01 M 18 8 h 0.01 M 6 8 h 0.01 M 7 16 h 10 M 8 12 h 0.01 M 4 4 H 20 A 2 2 0 0 1 22 6 V 18 A 2 2 0 0 1 20 20 H 4 A 2 2 0 0 1 2 18 V 6 A 2 2 0 0 1 4 4 Z", // keyboard
        ["Guide"] = "M 8 2 v 4 M 16 2 v 4 M 5 4 H 19 A 2 2 0 0 1 21 6 V 20 A 2 2 0 0 1 19 22 H 5 A 2 2 0 0 1 3 20 V 6 A 2 2 0 0 1 5 4 Z M 3 10 h 18 M 8 14 h 0.01 M 12 14 h 0.01 M 16 14 h 0.01 M 8 18 h 0.01 M 12 18 h 0.01 M 16 18 h 0.01", // calendar-days
        ["Settings"] = "M 12.22 2 h -0.44 a 2 2 0 0 0 -2 2 v 0.18 a 2 2 0 0 1 -1 1.73 l -0.43 0.25 a 2 2 0 0 1 -2 0 l -0.15 -0.08 a 2 2 0 0 0 -2.73 0.73 l -0.22 0.38 a 2 2 0 0 0 0.73 2.73 l 0.15 0.1 a 2 2 0 0 1 1 1.72 v 0.51 a 2 2 0 0 1 -1 1.74 l -0.15 0.09 a 2 2 0 0 0 -0.73 2.73 l 0.22 0.38 a 2 2 0 0 0 2.73 0.73 l 0.15 -0.08 a 2 2 0 0 1 2 0 l 0.43 0.25 a 2 2 0 0 1 1 1.73 V 20 a 2 2 0 0 0 2 2 h 0.44 a 2 2 0 0 0 2 -2 v -0.18 a 2 2 0 0 1 1 -1.73 l 0.43 -0.25 a 2 2 0 0 1 2 0 l 0.15 0.08 a 2 2 0 0 0 2.73 -0.73 l 0.22 -0.39 a 2 2 0 0 0 -0.73 -2.73 l -0.15 -0.08 a 2 2 0 0 1 -1 -1.74 v -0.5 a 2 2 0 0 1 1 -1.74 l 0.15 -0.09 a 2 2 0 0 0 0.73 -2.73 l -0.22 -0.38 a 2 2 0 0 0 -2.73 -0.73 l -0.15 0.08 a 2 2 0 0 1 -2 0 l -0.43 -0.25 a 2 2 0 0 1 -1 -1.73 V 4 a 2 2 0 0 0 -2 -2 z M 9 12 A 3 3 0 1 0 15 12 A 3 3 0 1 0 9 12 Z", // settings
        ["Search"] = "M 3 11 A 8 8 0 1 0 19 11 A 8 8 0 1 0 3 11 Z M 21 21 l -4.3 -4.3", // search
        ["Plus"] = "M 5 12 h 14 M 12 5 v 14", // plus
        ["Heart"] = "M 19 14 c 1.49 -1.46 3 -3.21 3 -5.5 A 5.5 5.5 0 0 0 16.5 3 c -1.76 0 -3 0.5 -4.5 2 c -1.5 -1.5 -2.74 -2 -4.5 -2 A 5.5 5.5 0 0 0 2 8.5 c 0 2.3 1.5 4.05 3 5.5 l 7 7 Z", // heart
        ["Close"] = "M 18 6 L 6 18 M 6 6 l 12 12", // x
        ["ChevronLeft"] ="M 15 18 l -6 -6 l 6 -6", // chevron-left
        ["ChevronRight"] = "M 9 18 l 6 -6 l -6 -6", // chevron-right
        ["Volume"] = "M 11 4.702 a 0.705 0.705 0 0 0 -1.203 -0.498 L 6.413 7.587 A 1.4 1.4 0 0 1 5.416 8 H 3 a 1 1 0 0 0 -1 1 v 6 a 1 1 0 0 0 1 1 h 2.416 a 1.4 1.4 0 0 1 0.997 0.413 l 3.383 3.384 A 0.705 0.705 0 0 0 11 19.298 z M 16 9 a 5 5 0 0 1 0 6 M 19.364 18.364 a 9 9 0 0 0 0 -12.728" // volume-2
    }.ToDictionary(x => x.Key, x => { var geometry = Geometry.Parse(x.Value); geometry.Freeze(); return geometry; });

    public Icon() { Width = 18; Height = 18; Focusable = false; IsHitTestVisible = false; }
    protected override void OnRender(DrawingContext context)
    {
        base.OnRender(context);
        if (!Paths.TryGetValue(Kind, out var path)) return;
        // Lucide is drawn on a 24 px grid with a 2 px round stroke; the pen scales with the icon size.
        var pen = new Pen(Foreground, 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        context.PushTransform(new ScaleTransform(ActualWidth / 24, ActualHeight / 24));
        context.DrawGeometry(null, pen, path);
        context.Pop();
    }
}
