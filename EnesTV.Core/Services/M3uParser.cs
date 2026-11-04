using System.Text;
using System.Text.RegularExpressions;
using EnesTV.Core.Models;

namespace EnesTV.Core.Services;

public static partial class M3uParser
{
    [GeneratedRegex("([\\w-]+)\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s,]+))", RegexOptions.CultureInvariant)]
    private static partial Regex Attributes();
    public static Task<IReadOnlyList<Channel>> ParseFileAsync(string path, string sourceId, Uri? baseUri, CancellationToken ct) => Task.Run<IReadOnlyList<Channel>>(() =>
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        try { using var reader = new StreamReader(path, new UTF8Encoding(false, true), true); return Parse(reader, sourceId, baseUri, ct); }
        catch (DecoderFallbackException) { using var reader = new StreamReader(path, Encoding.GetEncoding(1254), true); return Parse(reader, sourceId, baseUri, ct); }
    }, ct);
    public static IReadOnlyList<Channel> Parse(TextReader reader, string sourceId, Uri? baseUri = null, CancellationToken ct = default)
    {
        var result = new List<Channel>(); var seen = new HashSet<string>();
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); string name = ""; string? line;
        while ((line = reader.ReadLine()) != null)
        {
            ct.ThrowIfCancellationRequested(); line = line.Trim().TrimStart('\uFEFF'); if (line.Length == 0) continue;
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                metadata.Clear(); name = "";
                var quote = '\0'; var comma = -1;
                for (var i = 8; i < line.Length; i++)
                {
                    var ch = line[i];
                    if (ch is '\"' or '\'') { if (quote == ch) quote = '\0'; else if (quote == '\0') quote = ch; }
                    if (ch == ',' && quote == '\0') { comma = i; break; }
                }
                if (comma >= 0) name = line[(comma + 1)..].Trim();
                foreach (Match match in Attributes().Matches(comma >= 0 ? line[..comma] : line)) metadata[match.Groups[1].Value] = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value;
                continue;
            }
            if (line.StartsWith("#EXTGRP:", StringComparison.OrdinalIgnoreCase)) { metadata["group-title"] = line[8..].Trim(); continue; }
            if (line.StartsWith('#')) continue;
            Uri? uri = null;
            if (Uri.TryCreate(line, UriKind.Absolute, out var absolute)) uri = absolute;
            else if (baseUri != null && Uri.TryCreate(baseUri, line, out var relative)) uri = relative;
            if (uri != null && uri.Scheme is "http" or "https" or "rtsp" or "rtmp" or "udp" or "rtp" or "file")
            {
                var display = name.Length > 0 ? name : metadata.GetValueOrDefault("tvg-name", "İsimsiz kanal");
                var key = Channel.Key(sourceId, uri.AbsoluteUri, display);
                var category = metadata.GetValueOrDefault("group-title", "Uncategorized"); if (string.IsNullOrWhiteSpace(category)) category = "Uncategorized";
                var logo = metadata.GetValueOrDefault("tvg-logo", "");
                if (baseUri != null && logo.Length > 0 && Uri.TryCreate(baseUri, logo, out var logoUri)) logo = logoUri.AbsoluteUri;
                if (seen.Add(key)) result.Add(new(key, sourceId, display, uri.AbsoluteUri, metadata.GetValueOrDefault("tvg-id", ""), metadata.GetValueOrDefault("tvg-name", ""), logo, category));
            }
            metadata.Clear(); name = "";
        }
        return result;
    }
}
