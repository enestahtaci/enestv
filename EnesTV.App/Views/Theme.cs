using System.Windows;
using System.Windows.Media;
namespace EnesTV.App.Views;
public static class Theme
{
    public static void Apply(string theme)
    {
        var light = theme == "Light";
        Set("Page", light ? "#EEF1F5" : "#101216"); Set("Panel", light ? "#FFFFFF" : "#191D24");
        Set("Surface", light ? "#E6EAF0" : "#242A34"); Set("Text", light ? "#14202E" : "#F3F5F7");
        Set("Muted", light ? "#506076" : "#A3ADBC"); Set("Line", light ? "#D2D9E4" : "#303845");
        Set("Accent", light ? "#236200" : "#B3F36B"); Set("AccentSurface", light ? "#D2EFB9" : "#2C3B20");
        Set("Warning", light ? "#983800" : "#FFB899"); Set("Raised", light ? "#D3DAE4" : "#343C49");
        foreach (Window window in Application.Current.Windows) WindowAppearance.Apply(window);
    }
    private static void Set(string key, string color) => Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
