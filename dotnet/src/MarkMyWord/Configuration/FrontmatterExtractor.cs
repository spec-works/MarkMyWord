using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace MarkMyWord.Configuration;

/// <summary>
/// Extracts and parses YAML frontmatter from a Markdig document.
/// </summary>
public static class FrontmatterExtractor
{
    /// <summary>
    /// Extracts frontmatter data from a parsed Markdig document.
    /// Returns null if the document contains no YAML frontmatter block.
    /// </summary>
    public static FrontmatterData? Extract(MarkdownDocument document)
    {
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (yamlBlock == null)
            return null;

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = yamlBlock.Lines;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines.Lines[i].Slice.ToString();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                continue;

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var key = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();

            // Strip matching surrounding quotes
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            fields[key] = value;
        }

        return new FrontmatterData(fields);
    }
}

/// <summary>
/// Represents parsed frontmatter data from a markdown document.
/// </summary>
public class FrontmatterData
{
    /// <summary>
    /// All frontmatter fields as key-value pairs (case-insensitive keys).
    /// </summary>
    public IReadOnlyDictionary<string, string> Fields { get; }

    /// <summary>
    /// The document title from the frontmatter, if present.
    /// </summary>
    public string? Title => Fields.TryGetValue("title", out var title) ? title : null;

    public FrontmatterData(Dictionary<string, string> fields)
    {
        Fields = fields;
    }
}
