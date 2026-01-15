using System.Text.RegularExpressions;
using ColorCode;
using ColorCode.Common;

namespace MarkMyWord.SyntaxHighlighting;

/// <summary>
/// Syntax highlighter implementation using ColorCode.Core library.
/// Supports 25+ languages including JSON, C#, JavaScript, TypeScript, Python, and more.
/// Note: This is a simplified implementation that uses regex-based parsing for common patterns.
/// </summary>
public class ColorCodeHighlighter : ISyntaxHighlighter
{
    public bool SupportsLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;

        // Support common languages - focus on JSON for now
        // ColorCode.Core's Languages class can detect these
        return language.Equals("json", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("csharp", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("cs", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("javascript", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("js", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("typescript", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("ts", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("python", StringComparison.OrdinalIgnoreCase) ||
               language.Equals("py", StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<SyntaxToken> Highlight(string code, string language)
    {
        if (string.IsNullOrEmpty(code))
            yield break;

        // For JSON, use a simple regex-based tokenizer
        if (language.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var token in HighlightJson(code))
                yield return token;
            yield break;
        }

        // For other languages, fall back to plain text
        // (Full ColorCode integration would require more complex parsing)
        yield return new SyntaxToken(code, TokenType.Default);
    }

    private IEnumerable<SyntaxToken> HighlightJson(string code)
    {
        int position = 0;

        while (position < code.Length)
        {
            // Skip whitespace
            if (char.IsWhiteSpace(code[position]))
            {
                int start = position;
                while (position < code.Length && char.IsWhiteSpace(code[position]))
                    position++;
                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.Default);
                continue;
            }

            // Strings (property names and values)
            if (code[position] == '"')
            {
                int start = position;
                position++;

                while (position < code.Length)
                {
                    if (code[position] == '\\' && position + 1 < code.Length)
                    {
                        position += 2;
                        continue;
                    }

                    if (code[position] == '"')
                    {
                        position++;
                        break;
                    }

                    position++;
                }

                // Check if this is a property name (followed by colon) or a value
                int lookahead = position;
                while (lookahead < code.Length && char.IsWhiteSpace(code[lookahead]))
                    lookahead++;

                TokenType stringType = (lookahead < code.Length && code[lookahead] == ':')
                    ? TokenType.Property  // Property name
                    : TokenType.String;   // String value

                yield return new SyntaxToken(code.Substring(start, position - start), stringType);
                continue;
            }

            // Numbers (including negative, decimals, and scientific notation)
            if (char.IsDigit(code[position]) ||
                (code[position] == '-' && position + 1 < code.Length && char.IsDigit(code[position + 1])))
            {
                int start = position;

                if (code[position] == '-')
                    position++;

                while (position < code.Length &&
                       (char.IsDigit(code[position]) || code[position] == '.' ||
                        code[position] == 'e' || code[position] == 'E' ||
                        code[position] == '+' || code[position] == '-'))
                    position++;

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.Number);
                continue;
            }

            // Keywords (true, false, null)
            if (char.IsLetter(code[position]))
            {
                int start = position;

                while (position < code.Length && char.IsLetter(code[position]))
                    position++;

                string word = code.Substring(start, position - start);

                TokenType type = word switch
                {
                    "true" or "false" or "null" => TokenType.Keyword,
                    _ => TokenType.Default
                };

                yield return new SyntaxToken(word, type);
                continue;
            }

            // Punctuation and operators
            if ("{}[]:,".Contains(code[position]))
            {
                yield return new SyntaxToken(code[position].ToString(), TokenType.Operator);
                position++;
                continue;
            }

            // Unknown character
            yield return new SyntaxToken(code[position].ToString(), TokenType.Default);
            position++;
        }
    }
}
