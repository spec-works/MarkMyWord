namespace MarkMyWord.Configuration;

/// <summary>
/// Options for converting Word documents to Markdown.
/// </summary>
public class WordToMarkdownOptions
{
    /// <summary>
    /// Target markdown flavor for output.
    /// </summary>
    public MarkdownFlavor Flavor { get; set; } = MarkdownFlavor.GitHubFlavoredMarkdown;

    /// <summary>
    /// Extract images from the document and save them to disk.
    /// </summary>
    public bool ExtractImages { get; set; } = true;

    /// <summary>
    /// Directory path where extracted images will be saved.
    /// If null, images are saved relative to the output markdown file.
    /// </summary>
    public string? ImageOutputDirectory { get; set; }

    /// <summary>
    /// URL prefix for image links in the generated markdown.
    /// If null, uses relative file paths.
    /// </summary>
    public string? ImageUrlPrefix { get; set; }

    /// <summary>
    /// Preserve formatting details that may not have direct markdown equivalents.
    /// Useful for roundtripping back to Word.
    /// </summary>
    public bool PreserveFormattingMetadata { get; set; } = false;

    /// <summary>
    /// Optimize output for LLM grounding by removing unnecessary formatting.
    /// When true, focuses on semantic content over visual formatting.
    /// </summary>
    public bool OptimizeForLLM { get; set; } = true;

    /// <summary>
    /// Convert complex formatting (colors, fonts, etc.) to HTML when no markdown equivalent exists.
    /// Only applicable when Flavor is GitHubFlavoredMarkdown.
    /// </summary>
    public bool UseHtmlForComplexFormatting { get; set; } = false;

    /// <summary>
    /// Extract and include document metadata (title, author, subject) as YAML frontmatter.
    /// </summary>
    public bool IncludeMetadata { get; set; } = false;

    /// <summary>
    /// Line ending style for the output markdown.
    /// </summary>
    public LineEndingStyle LineEndings { get; set; } = LineEndingStyle.Environment;

    /// <summary>
    /// Extract comments from the Word document as markdown comments.
    /// </summary>
    public bool ExtractComments { get; set; } = false;

    /// <summary>
    /// Extract Word comments as an MRSF (Sidemark) sidecar document.
    /// When true, comments are extracted into a structured <see cref="Sidemark.MrsfDocument"/>
    /// with line-level targeting, enabling roundtripping between Word and Markdown.
    /// </summary>
    public bool ExtractCommentsAsSidemark { get; set; } = false;

    /// <summary>
    /// When <see cref="ExtractCommentsAsSidemark"/> is true and the conversion
    /// target is a file, automatically write the .review.yaml sidecar file
    /// alongside the output markdown file.
    /// </summary>
    public bool WriteSidemarkFile { get; set; } = true;
}

/// <summary>
/// Markdown flavor to use for output.
/// </summary>
public enum MarkdownFlavor
{
    /// <summary>
    /// Strict CommonMark (no extensions).
    /// Use this for maximum compatibility.
    /// </summary>
    CommonMark,

    /// <summary>
    /// GitHub Flavored Markdown (CommonMark + tables, strikethrough, task lists, etc.).
    /// Recommended for most use cases.
    /// </summary>
    GitHubFlavoredMarkdown
}

/// <summary>
/// Line ending style for markdown output.
/// </summary>
public enum LineEndingStyle
{
    /// <summary>
    /// Use the platform's default line endings (CRLF on Windows, LF on Unix).
    /// </summary>
    Environment,

    /// <summary>
    /// Use LF (Unix-style) line endings.
    /// </summary>
    LF,

    /// <summary>
    /// Use CRLF (Windows-style) line endings.
    /// </summary>
    CRLF
}
