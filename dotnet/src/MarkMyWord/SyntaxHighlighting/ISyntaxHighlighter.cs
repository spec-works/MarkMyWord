namespace MarkMyWord.SyntaxHighlighting;

/// <summary>
/// Interface for syntax highlighters that tokenize code into colored segments.
/// </summary>
public interface ISyntaxHighlighter
{
    /// <summary>
    /// Highlights code by breaking it into syntax tokens.
    /// </summary>
    /// <param name="code">The source code to highlight.</param>
    /// <param name="language">The programming language identifier.</param>
    /// <returns>A sequence of syntax tokens with text and type information.</returns>
    IEnumerable<SyntaxToken> Highlight(string code, string language);

    /// <summary>
    /// Checks if this highlighter supports the specified language.
    /// </summary>
    /// <param name="language">The programming language identifier.</param>
    /// <returns>True if the language is supported, false otherwise.</returns>
    bool SupportsLanguage(string language);
}

/// <summary>
/// Represents a syntax token with its text content and classification type.
/// </summary>
/// <param name="Text">The text content of the token.</param>
/// <param name="Type">The classification type of the token.</param>
public record SyntaxToken(string Text, TokenType Type);

/// <summary>
/// Classification types for syntax tokens.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Language keywords (e.g., if, class, model, namespace).
    /// </summary>
    Keyword,

    /// <summary>
    /// String literals.
    /// </summary>
    String,

    /// <summary>
    /// Numeric literals.
    /// </summary>
    Number,

    /// <summary>
    /// Comments (line and block).
    /// </summary>
    Comment,

    /// <summary>
    /// Operators (e.g., +, -, =, :).
    /// </summary>
    Operator,

    /// <summary>
    /// Identifiers and variable names.
    /// </summary>
    Identifier,

    /// <summary>
    /// Type names (e.g., string, int32, custom types).
    /// </summary>
    Type,

    /// <summary>
    /// Function and method names.
    /// </summary>
    Function,

    /// <summary>
    /// Property and field names.
    /// </summary>
    Property,

    /// <summary>
    /// Default/unclassified text.
    /// </summary>
    Default
}
