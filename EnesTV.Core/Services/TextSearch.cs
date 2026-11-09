namespace EnesTV.Core.Services;

/// <summary>
/// Case-insensitive search that understands Turkish letters (see docs/CORE-SPEC.md, "Arama").
/// Ordinal ignore-case does not pair İ/i or I/ı, so "şimdi" missed "ŞİMDİ TV". Culture-aware tr-TR lowering is not used
/// either: it turns "BEIN" into "beın", breaking English channel names. Instead every i-letter (I, ı, İ, i) is one letter,
/// and all other characters compare by their invariant lower case (Ç/ç, Ş/ş, Ğ/ğ, Ö/ö, Ü/ü already pair that way).
/// </summary>
public static class TextSearch
{
    public static char Fold(char c) => c is 'I' or 'ı' or 'İ' ? 'i' : char.ToLowerInvariant(c);

    /// <summary>True when <paramref name="query"/> (trimmed) occurs in <paramref name="text"/>; an empty query matches everything.</summary>
    public static bool Contains(string text, string? query)
    {
        var q = (query ?? "").AsSpan().Trim();
        if (q.IsEmpty) return true;
        for (var start = 0; start + q.Length <= text.Length; start++)
        {
            var i = 0;
            while (i < q.Length && Fold(text[start + i]) == Fold(q[i])) i++;
            if (i == q.Length) return true;
        }
        return false;
    }

    /// <summary>Whole-string equality under the same folding (e.g. favourite list names).</summary>
    public static bool Equals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++) if (Fold(a[i]) != Fold(b[i])) return false;
        return true;
    }
}
