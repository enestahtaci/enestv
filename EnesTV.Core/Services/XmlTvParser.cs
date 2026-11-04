using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using EnesTV.Core.Models;
namespace EnesTV.Core.Services;
public static class XmlTvParser
{
    public static DateTimeOffset? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var parts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var digits = parts[0]; if (digits.Length is not (8 or 10 or 12 or 14)) return null;
        if (!DateTime.TryParseExact(digits.PadRight(14, '0'), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)) return null;
        var offset = TimeSpan.Zero;
        if (parts.Length > 1 && parts[1] is not ("UTC" or "GMT" or "Z"))
        {
            var zone = parts[1];
            if (zone.Length != 5 || zone[0] is not ('+' or '-') || !int.TryParse(zone[1..3], out var hours) || !int.TryParse(zone[3..5], out var minutes) || hours > 14 || minutes > 59 || (hours == 14 && minutes != 0)) return null;
            offset = new TimeSpan(hours, minutes, 0) * (zone[0] == '-' ? -1 : 1);
        }
        return new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Unspecified), offset);
    }
    public static IEnumerable<Programme> ReadFile(string path, ISet<string> channelIds, CancellationToken ct)
    {
        using var file = File.OpenRead(path);
        var first = file.ReadByte(); var second = file.ReadByte(); file.Position = 0;
        using Stream stream = first == 0x1f && second == 0x8b ? new GZipStream(file, CompressionMode.Decompress) : file;
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null, MaxCharactersInDocument = 1024L * 1024 * 1024, IgnoreComments = true, IgnoreWhitespace = true });
        var after = DateTimeOffset.UtcNow.AddDays(-1); var before = DateTimeOffset.UtcNow.AddDays(8);
        while (reader.Read())
        {
            ct.ThrowIfCancellationRequested();
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "programme") continue;
            var id = reader.GetAttribute("channel") ?? "";
            var start = ParseTime(reader.GetAttribute("start")); var end = ParseTime(reader.GetAttribute("stop"));
            if (!channelIds.Contains(id) || start == null || end == null || end <= start || end < after || start > before) continue;
            using var subtree = reader.ReadSubtree();
            var element = XElement.Load(subtree);
            yield return new(id, element.Element("title")?.Value ?? "İsimsiz program", element.Element("desc")?.Value ?? "", start.Value, end.Value);
        }
    }
}
