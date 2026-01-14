using System.Text.RegularExpressions;

namespace MarkMyWord.SyntaxHighlighting;

/// <summary>
/// Custom syntax highlighter for Bash/Shell scripts.
/// </summary>
public class BashHighlighter : ISyntaxHighlighter
{
    private static readonly string[] Keywords =
    {
        "if", "then", "else", "elif", "fi", "case", "esac", "for", "while", "until",
        "do", "done", "in", "function", "select", "time", "until", "return", "break",
        "continue", "exit", "local", "readonly", "declare", "typeset", "export", "unset"
    };

    private static readonly string[] BuiltinCommands =
    {
        "echo", "printf", "read", "cd", "pwd", "pushd", "popd", "dirs",
        "let", "eval", "exec", "source", "test", "alias", "unalias",
        "bg", "fg", "jobs", "wait", "suspend", "kill", "trap",
        "true", "false", "shift", "getopts", "umask", "ulimit"
    };

    private static readonly HashSet<string> KeywordSet = new(Keywords, StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> BuiltinSet = new(BuiltinCommands, StringComparer.OrdinalIgnoreCase);

    public bool SupportsLanguage(string language)
    {
        return language?.Equals("bash", StringComparison.OrdinalIgnoreCase) == true ||
               language?.Equals("sh", StringComparison.OrdinalIgnoreCase) == true ||
               language?.Equals("shell", StringComparison.OrdinalIgnoreCase) == true;
    }

    public IEnumerable<SyntaxToken> Highlight(string code, string language)
    {
        if (string.IsNullOrEmpty(code))
            yield break;

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

            // Comments
            if (code[position] == '#')
            {
                int start = position;

                while (position < code.Length && code[position] != '\n' && code[position] != '\r')
                    position++;

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.Comment);
                continue;
            }

            // Double-quoted strings
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

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.String);
                continue;
            }

            // Single-quoted strings
            if (code[position] == '\'')
            {
                int start = position;
                position++;

                while (position < code.Length)
                {
                    if (code[position] == '\'')
                    {
                        position++;
                        break;
                    }

                    position++;
                }

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.String);
                continue;
            }

            // Variables ($var, ${var}, $1, etc.)
            if (code[position] == '$')
            {
                int start = position;
                position++;

                // Handle ${...} syntax
                if (position < code.Length && code[position] == '{')
                {
                    position++;
                    while (position < code.Length && code[position] != '}')
                        position++;
                    if (position < code.Length)
                        position++; // Include closing }
                }
                // Handle regular variables
                else
                {
                    while (position < code.Length && (char.IsLetterOrDigit(code[position]) || code[position] == '_'))
                        position++;
                }

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.Identifier);
                continue;
            }

            // Numbers
            if (char.IsDigit(code[position]) ||
                (code[position] == '-' && position + 1 < code.Length && char.IsDigit(code[position + 1])))
            {
                int start = position;

                if (code[position] == '-')
                    position++;

                while (position < code.Length && (char.IsDigit(code[position]) || code[position] == '.'))
                    position++;

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.Number);
                continue;
            }

            // Keywords, builtins, and identifiers
            if (char.IsLetter(code[position]) || code[position] == '_')
            {
                int start = position;

                while (position < code.Length && (char.IsLetterOrDigit(code[position]) || code[position] == '_' || code[position] == '-'))
                    position++;

                string word = code.Substring(start, position - start);

                TokenType tokenType;
                if (KeywordSet.Contains(word))
                    tokenType = TokenType.Keyword;
                else if (BuiltinSet.Contains(word))
                    tokenType = TokenType.Function;
                else
                    tokenType = TokenType.Identifier;

                yield return new SyntaxToken(word, tokenType);
                continue;
            }

            // Operators and special characters
            if ("|&;<>()[]{}!".Contains(code[position]))
            {
                int start = position;
                position++;

                // Handle multi-character operators
                if (position < code.Length)
                {
                    char prev = code[position - 1];
                    char curr = code[position];

                    if ((prev == '|' && curr == '|') ||
                        (prev == '&' && curr == '&') ||
                        (prev == '>' && curr == '>') ||
                        (prev == '<' && curr == '<') ||
                        (prev == '2' && curr == '>'))
                    {
                        position++;
                    }
                }

                yield return new SyntaxToken(code.Substring(start, position - start), TokenType.Operator);
                continue;
            }

            // Unknown character - treat as default
            yield return new SyntaxToken(code[position].ToString(), TokenType.Default);
            position++;
        }
    }
}
