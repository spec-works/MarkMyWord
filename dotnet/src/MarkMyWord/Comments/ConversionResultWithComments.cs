using Sidemark;

namespace MarkMyWord.Comments;

/// <summary>
/// Result of a Word-to-Markdown conversion that includes extracted Sidemark comments.
/// </summary>
public class ConversionResultWithComments
{
    /// <summary>
    /// The converted markdown text.
    /// </summary>
    public required string Markdown { get; init; }

    /// <summary>
    /// The MRSF sidecar document containing extracted comments, or null if no comments were found.
    /// </summary>
    public MrsfDocument? SidemarkDocument { get; init; }

    /// <summary>
    /// Whether the source Word document contained any comments.
    /// </summary>
    public bool HasComments => SidemarkDocument?.Comments.Count > 0;
}
