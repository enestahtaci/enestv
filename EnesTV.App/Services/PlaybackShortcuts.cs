using System.Windows.Input;

namespace EnesTV.App.Services;

public static class PlaybackShortcuts
{
    public static readonly (string Id, string Label, string Gesture)[] Defaults =
    [
        ("next", "Sonraki kanal", "PageDown"), ("previous", "Önceki kanal", "PageUp"),
        ("last", "Son izlenen kanala dön", "Back"), ("pause", "Oynat / duraklat", "Space"),
        ("mute", "Sessiz", "M"), ("volumeUp", "Sesi artır", "Up"), ("volumeDown", "Sesi azalt", "Down")
    ];
    public static string Format(Key key, ModifierKeys modifiers) =>
        (modifiers.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "") +
        (modifiers.HasFlag(ModifierKeys.Alt) ? "Alt+" : "") +
        (modifiers.HasFlag(ModifierKeys.Shift) ? "Shift+" : "") + key;
    public static bool Allowed(Key key, ModifierKeys modifiers)
    {
        if (modifiers.HasFlag(ModifierKeys.Windows) || key is Key.None or Key.System or Key.ImeProcessed or Key.DeadCharProcessed or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.Tab or Key.Escape or Key.F11 or Key.Enter) return false;
        if (key == Key.F4 && modifiers.HasFlag(ModifierKeys.Alt)) return false;
        return modifiers != ModifierKeys.None || !(key >= Key.D0 && key <= Key.D9 || key >= Key.NumPad0 && key <= Key.NumPad9);
    }
    public static string? Find(IReadOnlyDictionary<string, string> configured, Key key, ModifierKeys modifiers)
    {
        var gesture = Format(key, modifiers);
        return Defaults.FirstOrDefault(x => (configured.GetValueOrDefault(x.Id) ?? x.Gesture) == gesture).Id;
    }
}
