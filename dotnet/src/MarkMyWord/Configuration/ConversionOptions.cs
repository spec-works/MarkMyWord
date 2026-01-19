namespace MarkMyWord.Configuration;

/// <summary>
/// Options for converting markdown to Word documents.
/// </summary>
public class ConversionOptions
{
    /// <summary>
    /// Style configuration for the document.
    /// </summary>
    public StyleConfiguration Styles { get; set; } = new();

    /// <summary>
    /// Enable Markdig advanced extensions (tables, task lists, etc.).
    /// </summary>
    public bool EnableAdvancedExtensions { get; set; } = false;

    /// <summary>
    /// Enable table support.
    /// </summary>
    public bool EnableTables { get; set; } = true;

    /// <summary>
    /// Enable task list support.
    /// </summary>
    public bool EnableTaskLists { get; set; } = true;

    /// <summary>
    /// Enable syntax highlighting for code blocks.
    /// </summary>
    public bool EnableSyntaxHighlighting { get; set; } = true;

    /// <summary>
    /// Document title metadata.
    /// </summary>
    public string? DocumentTitle { get; set; }

    /// <summary>
    /// Document author metadata.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Document subject metadata.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Strategy for handling images in the document.
    /// </summary>
    public ImageHandlingStrategy ImageStrategy { get; set; } = ImageHandlingStrategy.Embed;

    /// <summary>
    /// Maximum image width in inches.
    /// </summary>
    public int MaxImageWidthInches { get; set; } = 6;

    /// <summary>
    /// Enable rendering of Mermaid diagrams as embedded SVG images.
    /// </summary>
    public bool EnableMermaidDiagrams { get; set; } = true;

    /// <summary>
    /// Maximum width for Mermaid diagrams in inches.
    /// </summary>
    public double MaxDiagramWidthInches { get; set; } = 6.5;

    /// <summary>
    /// Maximum height for Mermaid diagrams in inches.
    /// </summary>
    public double MaxDiagramHeightInches { get; set; } = 8.0;
}

/// <summary>
/// Strategy for handling images in markdown.
/// </summary>
public enum ImageHandlingStrategy
{
    /// <summary>
    /// Embed images in the document.
    /// </summary>
    Embed,

    /// <summary>
    /// Keep images as hyperlinks.
    /// </summary>
    Link,

    /// <summary>
    /// Skip images entirely.
    /// </summary>
    Skip
}
