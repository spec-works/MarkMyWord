using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MarkMyWord;

internal static class MarkdownFrontmatter
{
    private static readonly Regex s_metadataLine = new(@"^\s*[A-Za-z0-9_.-]+\s*:", RegexOptions.Compiled);
    private static readonly Regex s_titleLine = new(@"^\s*title\s*:\s*(.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static (string Markdown, string? Title) Process(string markdown)
    {
        var lines = GetLines(markdown);
        if (lines.Count < 3)
            return (markdown, null);

        var opening = markdown[lines[0].Start..lines[0].ContentEnd].TrimStart('\uFEFF').Trim();
        if (opening != "---")
            return (markdown, null);

        var closingIndex = -1;
        for (var i = 1; i < lines.Count; i++)
        {
            var value = markdown[lines[i].Start..lines[i].ContentEnd].Trim();
            if (value is "---" or "...")
            {
                closingIndex = i;
                break;
            }
        }

        if (closingIndex < 0)
            return (markdown, null);

        if (!Enumerable.Range(1, closingIndex - 1)
            .Any(index => s_metadataLine.IsMatch(markdown[lines[index].Start..lines[index].ContentEnd])))
        {
            return (markdown, null);
        }

        string? title = null;
        for (var i = 1; i < closingIndex; i++)
        {
            var line = markdown[lines[i].Start..lines[i].ContentEnd];
            var match = s_titleLine.Match(line);
            if (match.Success)
            {
                title = ParseTitle(match.Groups[1].Value);
                break;
            }
        }

        var sanitized = new StringBuilder(markdown);
        for (var i = 0; i <= closingIndex; i++)
        {
            for (var position = lines[i].Start; position < lines[i].ContentEnd; position++)
                sanitized[position] = ' ';
        }

        return (sanitized.ToString(), title);
    }

    private static string? ParseTitle(string value)
    {
        value = value.Trim();
        if (value.Length == 0)
            return null;

        if (value[0] == '"')
        {
            try
            {
                return JsonSerializer.Deserialize<string>(value);
            }
            catch (JsonException)
            {
                return value.Trim('"');
            }
        }

        if (value[0] == '\'')
        {
            var end = value.LastIndexOf('\'');
            if (end > 0)
                return value[1..end].Replace("''", "'", StringComparison.Ordinal);
        }

        var commentIndex = value.IndexOf(" #", StringComparison.Ordinal);
        if (commentIndex >= 0)
            value = value[..commentIndex];

        return value.Trim();
    }

    private static List<(int Start, int ContentEnd)> GetLines(string markdown)
    {
        var lines = new List<(int Start, int ContentEnd)>();
        var start = 0;
        while (start < markdown.Length)
        {
            var end = markdown.IndexOfAny(['\r', '\n'], start);
            if (end < 0)
            {
                lines.Add((start, markdown.Length));
                break;
            }

            lines.Add((start, end));
            start = end + 1;
            if (markdown[end] == '\r' && start < markdown.Length && markdown[start] == '\n')
                start++;
        }

        if (markdown.Length == 0 || markdown[^1] is '\r' or '\n')
            lines.Add((markdown.Length, markdown.Length));

        return lines;
    }
}
