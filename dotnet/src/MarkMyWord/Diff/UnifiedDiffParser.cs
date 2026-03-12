using System.Text.RegularExpressions;

namespace MarkMyWord.Diff;

/// <summary>
/// Parses unified diff format (git diff style).
/// </summary>
public class UnifiedDiffParser
{
    private static readonly Regex HunkHeaderRegex = new(@"^@@\s+-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@", RegexOptions.Compiled);

    /// <summary>
    /// Parses a unified diff from a file.
    /// </summary>
    /// <param name="diffPath">Path to the diff file.</param>
    /// <returns>The parsed diff document.</returns>
    public DiffDocument ParseFile(string diffPath)
    {
        if (string.IsNullOrEmpty(diffPath))
            throw new ArgumentException("Diff path cannot be null or empty.", nameof(diffPath));

        if (!File.Exists(diffPath))
            throw new FileNotFoundException("Diff file not found.", diffPath);

        var content = File.ReadAllText(diffPath);
        return Parse(content);
    }

    /// <summary>
    /// Parses a unified diff from a stream.
    /// </summary>
    /// <param name="diffStream">Stream containing the diff.</param>
    /// <returns>The parsed diff document.</returns>
    public DiffDocument ParseStream(Stream diffStream)
    {
        if (diffStream == null)
            throw new ArgumentNullException(nameof(diffStream));

        using var reader = new StreamReader(diffStream);
        var content = reader.ReadToEnd();
        return Parse(content);
    }

    /// <summary>
    /// Parses a unified diff from a string.
    /// </summary>
    /// <param name="diffContent">The diff content.</param>
    /// <returns>The parsed diff document.</returns>
    public DiffDocument Parse(string diffContent)
    {
        if (string.IsNullOrEmpty(diffContent))
            throw new ArgumentException("Diff content cannot be null or empty.", nameof(diffContent));

        var document = new DiffDocument();
        var lines = diffContent.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

        int i = 0;

        // Parse file headers
        while (i < lines.Length)
        {
            var line = lines[i];

            if (line.StartsWith("--- "))
            {
                document.OriginalFile = line.Substring(4).Trim();
                i++;
            }
            else if (line.StartsWith("+++ "))
            {
                document.NewFile = line.Substring(4).Trim();
                i++;
            }
            else if (line.StartsWith("@@"))
            {
                // Found first hunk, start parsing hunks
                break;
            }
            else
            {
                // Skip other header lines (diff --git, index, etc.)
                i++;
            }
        }

        // Parse hunks
        while (i < lines.Length)
        {
            var line = lines[i];

            if (line.StartsWith("@@"))
            {
                var hunk = ParseHunk(lines, ref i);
                if (hunk != null)
                {
                    document.Hunks.Add(hunk);
                }
            }
            else
            {
                i++;
            }
        }

        if (document.Hunks.Count == 0)
        {
            throw new InvalidOperationException("No hunks found in diff. The diff may be empty or invalid.");
        }

        return document;
    }

    /// <summary>
    /// Parses a single hunk starting at the current index.
    /// </summary>
    private DiffHunk? ParseHunk(string[] lines, ref int index)
    {
        if (index >= lines.Length)
            return null;

        var headerLine = lines[index];
        var match = HunkHeaderRegex.Match(headerLine);

        if (!match.Success)
        {
            throw new FormatException($"Invalid hunk header format: {headerLine}");
        }

        var hunk = new DiffHunk
        {
            OldStart = int.Parse(match.Groups[1].Value),
            OldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1,
            NewStart = int.Parse(match.Groups[3].Value),
            NewCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1
        };

        index++; // Move past hunk header

        int oldLineNum = hunk.OldStart;
        int newLineNum = hunk.NewStart;

        // Parse hunk lines
        while (index < lines.Length)
        {
            var line = lines[index];

            // Check if we've hit the next hunk or end of diff
            if (line.StartsWith("@@") || line.StartsWith("--- ") || line.StartsWith("+++ "))
            {
                break;
            }

            // Parse the line based on its prefix
            if (line.Length == 0)
            {
                // Empty line is treated as context
                hunk.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Context,
                    Content = string.Empty,
                    OldLineNumber = oldLineNum++,
                    NewLineNumber = newLineNum++
                });
            }
            else if (line[0] == ' ')
            {
                // Context line
                hunk.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Context,
                    Content = line.Length > 1 ? line.Substring(1) : string.Empty,
                    OldLineNumber = oldLineNum++,
                    NewLineNumber = newLineNum++
                });
            }
            else if (line[0] == '+')
            {
                // Addition
                hunk.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Addition,
                    Content = line.Length > 1 ? line.Substring(1) : string.Empty,
                    NewLineNumber = newLineNum++
                });
            }
            else if (line[0] == '-')
            {
                // Deletion
                hunk.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Deletion,
                    Content = line.Length > 1 ? line.Substring(1) : string.Empty,
                    OldLineNumber = oldLineNum++
                });
            }
            else if (line.StartsWith("\\ No newline at end of file"))
            {
                // Ignore this line
            }
            else
            {
                // Unknown line format - might be context without proper prefix
                // Treat as context
                hunk.Lines.Add(new DiffLine
                {
                    Type = DiffLineType.Context,
                    Content = line,
                    OldLineNumber = oldLineNum++,
                    NewLineNumber = newLineNum++
                });
            }

            index++;
        }

        return hunk;
    }
}
