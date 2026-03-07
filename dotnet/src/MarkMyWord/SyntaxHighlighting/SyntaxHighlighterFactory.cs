namespace MarkMyWord.SyntaxHighlighting;

/// <summary>
/// Factory for creating and selecting appropriate syntax highlighters based on language.
/// </summary>
public class SyntaxHighlighterFactory
{
    private readonly List<ISyntaxHighlighter> _highlighters;

    public SyntaxHighlighterFactory()
    {
        _highlighters = new List<ISyntaxHighlighter>
        {
            new HttpHighlighter(),      // HTTP request/response highlighting
            new TypeSpecHighlighter(),  // Check TypeSpec first (more specific)
            new BashHighlighter(),      // Bash/Shell highlighting
            new ColorCodeHighlighter()  // ColorCode supports many languages (JSON, etc.)
        };
    }

    /// <summary>
    /// Highlights code using the appropriate highlighter for the specified language.
    /// </summary>
    /// <param name="code">The source code to highlight.</param>
    /// <param name="language">The programming language identifier (case-insensitive).</param>
    /// <returns>A sequence of syntax tokens, or plain text tokens if no highlighter supports the language.</returns>
    public IEnumerable<SyntaxToken> Highlight(string code, string language)
    {
        if (string.IsNullOrEmpty(code))
            return Enumerable.Empty<SyntaxToken>();

        if (string.IsNullOrWhiteSpace(language))
            return CreateDefaultTokens(code);

        // Find the first highlighter that supports this language
        var highlighter = _highlighters.FirstOrDefault(h => h.SupportsLanguage(language));

        if (highlighter != null)
            return highlighter.Highlight(code, language);

        // Fallback: return code as a single default token (no highlighting)
        return CreateDefaultTokens(code);
    }

    /// <summary>
    /// Checks if any highlighter supports the specified language.
    /// </summary>
    /// <param name="language">The programming language identifier.</param>
    /// <returns>True if any highlighter supports the language, false otherwise.</returns>
    public bool IsLanguageSupported(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        return _highlighters.Any(h => h.SupportsLanguage(language));
    }

    private IEnumerable<SyntaxToken> CreateDefaultTokens(string code)
    {
        yield return new SyntaxToken(code, TokenType.Default);
    }
}
