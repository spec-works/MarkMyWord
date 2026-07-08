using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace MarkMyWord.Configuration;

/// <summary>
/// Extracts and parses YAML frontmatter from a Markdig document.
/// </summary>
public static class FrontmatterExtractor
{
    private static readonly string[] DateFieldNames =
    [
        "date", "createdDate", "created_date", "createdTimestamp", "created_timestamp",
        "created", "publishDate", "publish_date", "published", "publishedDate",
        "published_date", "lastModified", "last_modified", "modifiedDate", "modified_date",
        "updated", "updatedDate", "updated_date"
    ];

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
        var authors = new List<string>();
        var lines = yamlBlock.Lines;
        string? currentListKey = null;

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines.Lines[i].Slice.ToString();
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
            {
                currentListKey = null;
                continue;
            }

            // Check for YAML list item (e.g., "  - John Doe")
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- ") && currentListKey != null)
            {
                var itemValue = StripQuotes(trimmed[2..].Trim());
                if (!string.IsNullOrEmpty(itemValue))
                    authors.Add(itemValue);
                continue;
            }

            var colonIndex = line.IndexOf(':');
            if (colonIndex <= 0)
            {
                currentListKey = null;
                continue;
            }

            var key = line[..colonIndex].Trim();
            var value = line[(colonIndex + 1)..].Trim();
            value = StripQuotes(value);

            // If value is empty, this might be a list key (e.g., "authors:")
            if (string.IsNullOrEmpty(value) &&
                key.Equals("authors", StringComparison.OrdinalIgnoreCase))
            {
                currentListKey = key;
                continue;
            }

            currentListKey = null;

            // Single "author" field
            if (key.Equals("author", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
            {
                authors.Add(value);
            }

            if (!string.IsNullOrEmpty(value))
                fields[key] = value;
        }

        return new FrontmatterData(fields, authors);
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }
        return value;
    }

    /// <summary>
    /// Returns the list of recognized date field names (case-insensitive matching).
    /// </summary>
    internal static IReadOnlyList<string> GetDateFieldNames() => DateFieldNames;
}

/// <summary>
/// Represents parsed frontmatter data from a markdown document.
/// </summary>
public class FrontmatterData
{
    private static readonly string[] DateFieldNames = FrontmatterExtractor.GetDateFieldNames().ToArray();

    /// <summary>
    /// All frontmatter fields as key-value pairs (case-insensitive keys).
    /// </summary>
    public IReadOnlyDictionary<string, string> Fields { get; }

    /// <summary>
    /// Authors extracted from the frontmatter ("author" for single, "authors" list for multiple).
    /// </summary>
    public IReadOnlyList<string> Authors { get; }

    /// <summary>
    /// The document title from the frontmatter, if present.
    /// </summary>
    public string? Title => Fields.TryGetValue("title", out var title) ? title : null;

    /// <summary>
    /// The document date, resolved from the first matching date field name.
    /// </summary>
    public string? Date
    {
        get
        {
            foreach (var name in DateFieldNames)
            {
                if (Fields.TryGetValue(name, out var value))
                    return value;
            }
            return null;
        }
    }

    public FrontmatterData(Dictionary<string, string> fields, List<string> authors)
    {
        Fields = fields;
        Authors = authors;
    }
}
