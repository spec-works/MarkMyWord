namespace MarkMyWord.Diff;

/// <summary>
/// Represents a unified diff document.
/// </summary>
public class DiffDocument
{
    /// <summary>
    /// The original filename (from --- line).
    /// </summary>
    public string? OriginalFile { get; set; }

    /// <summary>
    /// The new filename (from +++ line).
    /// </summary>
    public string? NewFile { get; set; }

    /// <summary>
    /// The list of diff hunks.
    /// </summary>
    public List<DiffHunk> Hunks { get; set; } = new();
}

/// <summary>
/// Represents a hunk (section of changes) in a diff.
/// </summary>
public class DiffHunk
{
    /// <summary>
    /// Starting line number in the original file (1-based).
    /// </summary>
    public int OldStart { get; set; }

    /// <summary>
    /// Number of lines from the original file.
    /// </summary>
    public int OldCount { get; set; }

    /// <summary>
    /// Starting line number in the new file (1-based).
    /// </summary>
    public int NewStart { get; set; }

    /// <summary>
    /// Number of lines in the new file.
    /// </summary>
    public int NewCount { get; set; }

    /// <summary>
    /// The lines in this hunk.
    /// </summary>
    public List<DiffLine> Lines { get; set; } = new();

    /// <summary>
    /// Gets the header line for this hunk (e.g., "@@ -1,4 +1,5 @@").
    /// </summary>
    public string GetHeader()
    {
        return $"@@ -{OldStart},{OldCount} +{NewStart},{NewCount} @@";
    }
}

/// <summary>
/// Represents a single line in a diff hunk.
/// </summary>
public class DiffLine
{
    /// <summary>
    /// The type of line (context, addition, deletion).
    /// </summary>
    public DiffLineType Type { get; set; }

    /// <summary>
    /// The content of the line (without the leading +, -, or space).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The line number in the original file (for context and deletion lines, 1-based).
    /// </summary>
    public int? OldLineNumber { get; set; }

    /// <summary>
    /// The line number in the new file (for context and addition lines, 1-based).
    /// </summary>
    public int? NewLineNumber { get; set; }
}

/// <summary>
/// The type of a diff line.
/// </summary>
public enum DiffLineType
{
    /// <summary>
    /// A context line (starts with space) - exists in both files.
    /// </summary>
    Context,

    /// <summary>
    /// An added line (starts with +) - only in new file.
    /// </summary>
    Addition,

    /// <summary>
    /// A deleted line (starts with -) - only in old file.
    /// </summary>
    Deletion
}
