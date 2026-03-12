namespace MarkMyWord.Diff;

/// <summary>
/// Applies unified diff hunks to markdown content.
/// </summary>
public class MarkdownPatcher
{
    /// <summary>
    /// Applies a diff document to markdown content.
    /// </summary>
    /// <param name="originalMarkdown">The original markdown content.</param>
    /// <param name="diff">The diff to apply.</param>
    /// <param name="validateContext">Whether to validate context lines match.</param>
    /// <returns>The patched markdown content.</returns>
    public string ApplyDiff(string originalMarkdown, DiffDocument diff, bool validateContext = true)
    {
        if (string.IsNullOrEmpty(originalMarkdown))
            throw new ArgumentException("Original markdown cannot be null or empty.", nameof(originalMarkdown));

        if (diff == null)
            throw new ArgumentNullException(nameof(diff));

        var lines = originalMarkdown.Split('\n').ToList();

        // Apply hunks in reverse order to maintain line numbers
        foreach (var hunk in diff.Hunks.OrderByDescending(h => h.OldStart))
        {
            ApplyHunk(lines, hunk, validateContext);
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// Applies a single hunk to the markdown lines.
    /// </summary>
    private void ApplyHunk(List<string> lines, DiffHunk hunk, bool validateContext)
    {
        // Convert to 0-based index
        int currentLine = hunk.OldStart - 1;
        var newLines = new List<string>();

        foreach (var diffLine in hunk.Lines)
        {
            switch (diffLine.Type)
            {
                case DiffLineType.Context:
                    // Validate context line if requested
                    if (validateContext && currentLine < lines.Count)
                    {
                        var actualLine = lines[currentLine].TrimEnd('\r');
                        var expectedLine = diffLine.Content.TrimEnd('\r');

                        if (!LinesMatch(actualLine, expectedLine))
                        {
                            throw new InvalidOperationException(
                                $"Context mismatch at line {currentLine + 1}. " +
                                $"Expected: '{expectedLine}', Found: '{actualLine}'");
                        }
                    }

                    // Keep the line
                    if (currentLine < lines.Count)
                    {
                        newLines.Add(lines[currentLine]);
                        currentLine++;
                    }
                    break;

                case DiffLineType.Deletion:
                    // Validate deletion if requested
                    if (validateContext && currentLine < lines.Count)
                    {
                        var actualLine = lines[currentLine].TrimEnd('\r');
                        var expectedLine = diffLine.Content.TrimEnd('\r');

                        if (!LinesMatch(actualLine, expectedLine))
                        {
                            throw new InvalidOperationException(
                                $"Deletion mismatch at line {currentLine + 1}. " +
                                $"Expected to delete: '{expectedLine}', Found: '{actualLine}'");
                        }
                    }

                    // Skip the line (delete it)
                    currentLine++;
                    break;

                case DiffLineType.Addition:
                    // Add the new line
                    newLines.Add(diffLine.Content);
                    break;
            }
        }

        // Replace the affected lines
        var startIndex = hunk.OldStart - 1;
        var removeCount = hunk.OldCount;

        // Ensure we don't go out of bounds
        if (startIndex < 0)
            startIndex = 0;

        if (startIndex + removeCount > lines.Count)
            removeCount = lines.Count - startIndex;

        // Remove old lines and insert new ones
        if (removeCount > 0)
        {
            lines.RemoveRange(startIndex, removeCount);
        }

        if (newLines.Count > 0)
        {
            lines.InsertRange(startIndex, newLines);
        }
    }

    /// <summary>
    /// Checks if two lines match (with whitespace normalization).
    /// </summary>
    private bool LinesMatch(string line1, string line2)
    {
        // Exact match
        if (line1 == line2)
            return true;

        // Try trimming trailing whitespace
        if (line1.TrimEnd() == line2.TrimEnd())
            return true;

        return false;
    }
}
