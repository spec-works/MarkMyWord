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

    /// <summary>
    /// Color scheme for syntax highlighting (hex colors without #).
    /// </summary>
    public SyntaxColorScheme? SyntaxColorScheme { get; set; }
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

/// <summary>
/// Color scheme for syntax highlighting.
/// All colors are hex format without # prefix (e.g., "569CD6" for blue).
/// </summary>
public class SyntaxColorScheme
{
    /// <summary>
    /// Color for language keywords (default: VS Code blue).
    /// </summary>
    public string KeywordColor { get; set; } = "569CD6";

    /// <summary>
    /// Color for string literals (default: VS Code orange).
    /// </summary>
    public string StringColor { get; set; } = "CE9178";

    /// <summary>
    /// Color for numeric literals (default: darker green for better contrast).
    /// </summary>
    public string NumberColor { get; set; } = "098658";

    /// <summary>
    /// Color for comments (default: VS Code green).
    /// </summary>
    public string CommentColor { get; set; } = "6A9955";

    /// <summary>
    /// Color for operators (default: darker gray for better contrast).
    /// </summary>
    public string OperatorColor { get; set; } = "4A4A4A";

    /// <summary>
    /// Color for type names (default: VS Code cyan).
    /// </summary>
    public string TypeColor { get; set; } = "4EC9B0";

    /// <summary>
    /// Color for function names (default: darker yellow/gold for better contrast).
    /// </summary>
    public string FunctionColor { get; set; } = "C4A000";

    /// <summary>
    /// Color for property names (default: darker blue for better contrast).
    /// </summary>
    public string PropertyColor { get; set; } = "4FC1FF";

    /// <summary>
    /// Color for identifiers (default: darker gray for better contrast).
    /// </summary>
    public string IdentifierColor { get; set; } = "383838";

    /// <summary>
    /// Default color for unclassified text (default: darker gray for better contrast).
    /// </summary>
    public string DefaultColor { get; set; } = "383838";

    /// <summary>
    /// Gets the color for a specific token type.
    /// </summary>
    /// <param name="type">The token type.</param>
    /// <returns>Hex color string without # prefix.</returns>
    public string GetColorForTokenType(MarkMyWord.SyntaxHighlighting.TokenType type)
    {
        return type switch
        {
            MarkMyWord.SyntaxHighlighting.TokenType.Keyword => KeywordColor,
            MarkMyWord.SyntaxHighlighting.TokenType.String => StringColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Number => NumberColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Comment => CommentColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Operator => OperatorColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Type => TypeColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Function => FunctionColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Property => PropertyColor,
            MarkMyWord.SyntaxHighlighting.TokenType.Identifier => IdentifierColor,
            _ => DefaultColor
        };
    }
}
