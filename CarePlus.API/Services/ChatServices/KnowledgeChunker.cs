using System.Text.RegularExpressions;

namespace CarePlus.API.Services.ChatServices;

public static partial class KnowledgeChunker
{
    private const int TargetCharacters = 1_000;
    private const int OverlapCharacters = 160;

    public static IReadOnlyList<string> Split(string content)
    {
        var normalized = NewLineRegex().Replace(content.Trim(), "\n");
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var chunks = new List<string>();
        var start = 0;
        while (start < normalized.Length)
        {
            var remaining = normalized.Length - start;
            if (remaining <= TargetCharacters)
            {
                chunks.Add(normalized[start..].Trim());
                break;
            }

            var tentativeEnd = Math.Min(start + TargetCharacters, normalized.Length);
            var window = normalized[start..tentativeEnd];
            var splitAt = Math.Max(window.LastIndexOf("\n\n", StringComparison.Ordinal), window.LastIndexOf(". ", StringComparison.Ordinal));
            if (splitAt < TargetCharacters / 2)
            {
                splitAt = window.LastIndexOf(' ');
            }
            if (splitAt < TargetCharacters / 2)
            {
                splitAt = window.Length;
            }

            var end = start + splitAt;
            var chunk = normalized[start..end].Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            start = Math.Max(end - OverlapCharacters, start + 1);
        }

        return chunks;
    }

    [GeneratedRegex(@"\r\n?|\n{3,}")]
    private static partial Regex NewLineRegex();
}

