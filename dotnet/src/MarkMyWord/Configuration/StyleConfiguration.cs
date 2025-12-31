namespace MarkMyWord.Configuration;

/// <summary>
/// Configuration for document styling.
/// </summary>
public class StyleConfiguration
{
    /// <summary>
    /// Default font name for body text.
    /// </summary>
    public string DefaultFontName { get; set; } = "Calibri";

    /// <summary>
    /// Default font size for body text (in points).
    /// </summary>
    public int DefaultFontSize { get; set; } = 11;

    /// <summary>
    /// Heading styles for levels 1-6.
    /// </summary>
    public HeadingStyle[] HeadingStyles { get; set; } = Array.Empty<HeadingStyle>();

    /// <summary>
    /// Font name for code blocks and inline code.
    /// </summary>
    public string CodeFontName { get; set; } = "Consolas";

    /// <summary>
    /// Font size for code blocks and inline code (in points).
    /// </summary>
    public int CodeFontSize { get; set; } = 9;

    /// <summary>
    /// Background color for code blocks and inline code (hex color without #).
    /// </summary>
    public string CodeBackgroundColor { get; set; } = "F5F5F5";

    /// <summary>
    /// Left border color for quote blocks (hex color without #).
    /// </summary>
    public string QuoteLeftBorderColor { get; set; } = "CCCCCC";

    /// <summary>
    /// Left border width for quote blocks (in eighths of a point).
    /// </summary>
    public int QuoteLeftBorderWidth { get; set; } = 4;

    /// <summary>
    /// Background color for quote blocks (hex color without #).
    /// </summary>
    public string QuoteBackgroundColor { get; set; } = "F9F9F9";

    /// <summary>
    /// List indentation in twips (1/1440 inch).
    /// </summary>
    public int ListIndentationTwips { get; set; } = 720; // 0.5 inch
}

/// <summary>
/// Style configuration for a heading level.
/// </summary>
public class HeadingStyle
{
    /// <summary>
    /// Heading level (1-6).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Font size in points.
    /// </summary>
    public int FontSize { get; set; }

    /// <summary>
    /// Whether the heading should be bold.
    /// </summary>
    public bool Bold { get; set; }

    /// <summary>
    /// Text color (hex color without #).
    /// </summary>
    public string Color { get; set; } = "000000";

    /// <summary>
    /// Spacing before the heading in twips (1/1440 inch).
    /// </summary>
    public int SpacingBeforeTwips { get; set; }

    /// <summary>
    /// Spacing after the heading in twips (1/1440 inch).
    /// </summary>
    public int SpacingAfterTwips { get; set; }
}
