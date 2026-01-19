namespace MarkMyWord.SyntaxHighlighting;

/// <summary>
/// Syntax highlighter for HTTP requests and responses with media-type-aware body highlighting.
/// Supports HTTP/1.1 and HTTP/2 message formats.
/// </summary>
public class HttpHighlighter : ISyntaxHighlighter
{
    private readonly ColorCodeHighlighter _colorCodeHighlighter;

    // HTTP methods (RFC 9110)
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS", "TRACE", "CONNECT"
    };

    // Common media type to language mapping
    private static readonly Dictionary<string, string> MediaTypeToLanguage = new()
    {
        { "application/json", "json" },
        { "application/xml", "xml" },
        { "text/xml", "xml" },
        { "text/html", "html" },
        { "text/plain", "plain" },
        { "application/javascript", "javascript" },
        { "text/javascript", "javascript" },
        { "application/typescript", "typescript" },
        { "application/x-www-form-urlencoded", "plain" }
    };

    public HttpHighlighter()
    {
        _colorCodeHighlighter = new ColorCodeHighlighter();
    }

    public bool SupportsLanguage(string language)
    {
        return language?.Equals("http", StringComparison.OrdinalIgnoreCase) == true ||
               language?.Equals("https", StringComparison.OrdinalIgnoreCase) == true ||
               language?.Equals("request", StringComparison.OrdinalIgnoreCase) == true ||
               language?.Equals("response", StringComparison.OrdinalIgnoreCase) == true;
    }

    public IEnumerable<SyntaxToken> Highlight(string code, string language)
    {
        if (string.IsNullOrEmpty(code))
            yield break;

        // Parse the HTTP message structure
        var message = ParseHttpMessage(code);

        if (message == null)
        {
            // Not a valid HTTP message, return as plain text
            yield return new SyntaxToken(code, TokenType.Default);
            yield break;
        }

        // Emit tokens for the HTTP message
        foreach (var token in GenerateTokens(message))
            yield return token;
    }

    private HttpMessage? ParseHttpMessage(string code)
    {
        var lines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (lines.Length == 0)
            return null;

        // Check first line to determine if request or response
        var firstLine = lines[0].Trim();

        HttpMessage message;
        if (IsRequestLine(firstLine))
        {
            message = ParseRequestLine(firstLine);
        }
        else if (IsStatusLine(firstLine))
        {
            message = ParseStatusLine(firstLine);
        }
        else
        {
            return null; // Not a valid HTTP message
        }

        // Parse headers and body
        int lineIndex = 1;
        var headers = new List<HttpHeader>();

        // Parse headers until blank line
        while (lineIndex < lines.Length)
        {
            var line = lines[lineIndex];

            // Blank line indicates end of headers
            if (string.IsNullOrWhiteSpace(line))
            {
                lineIndex++;
                break;
            }

            // Parse header (Name: Value)
            var colonIndex = line.IndexOf(':');
            if (colonIndex > 0)
            {
                var name = line.Substring(0, colonIndex);
                var value = colonIndex + 1 < line.Length
                    ? line.Substring(colonIndex + 1).TrimStart()
                    : string.Empty;
                headers.Add(new HttpHeader(name, value));
            }

            lineIndex++;
        }

        message.Headers = headers;

        // Everything after blank line is body
        if (lineIndex < lines.Length)
        {
            message.Body = string.Join(Environment.NewLine, lines.Skip(lineIndex));
        }

        // Detect Content-Type
        var contentTypeHeader = headers.FirstOrDefault(h =>
            h.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase));

        if (contentTypeHeader != null)
        {
            message.ContentType = ParseContentType(contentTypeHeader.Value);
        }

        return message;
    }

    private bool IsRequestLine(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 &&
               HttpMethods.Contains(parts[0]) &&
               parts[2].StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsStatusLine(string line)
    {
        // HTTP/1.1 200 OK
        // Position 9 is where the status code starts (after "HTTP/1.1 ")
        return line.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase) &&
               line.Length > 9 &&
               char.IsDigit(line[9]); // Status code starts at position 9
    }

    private HttpMessage ParseRequestLine(string line)
    {
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

        return new HttpMessage
        {
            IsRequest = true,
            Method = parts.Length > 0 ? parts[0] : string.Empty,
            Url = parts.Length > 1 ? parts[1] : string.Empty,
            Version = parts.Length > 2 ? parts[2] : string.Empty
        };
    }

    private HttpMessage ParseStatusLine(string line)
    {
        var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);

        return new HttpMessage
        {
            IsRequest = false,
            Version = parts.Length > 0 ? parts[0] : string.Empty,
            StatusCode = parts.Length > 1 ? parts[1] : string.Empty,
            ReasonPhrase = parts.Length > 2 ? parts[2] : string.Empty
        };
    }

    private string? ParseContentType(string contentTypeValue)
    {
        // Content-Type: application/json; charset=utf-8
        // Extract just the media type (before semicolon)
        var semicolonIndex = contentTypeValue.IndexOf(';');
        var mediaType = semicolonIndex > 0
            ? contentTypeValue.Substring(0, semicolonIndex).Trim()
            : contentTypeValue.Trim();

        return mediaType.ToLowerInvariant();
    }

    private IEnumerable<SyntaxToken> GenerateTokens(HttpMessage message)
    {
        // Generate tokens for request/status line
        if (message.IsRequest)
        {
            // Method (Keyword - blue)
            yield return new SyntaxToken(message.Method, TokenType.Keyword);
            yield return new SyntaxToken(" ", TokenType.Default);

            // URL (String - orange)
            yield return new SyntaxToken(message.Url, TokenType.String);
            yield return new SyntaxToken(" ", TokenType.Default);

            // Version (Type - cyan)
            yield return new SyntaxToken(message.Version, TokenType.Type);
            yield return new SyntaxToken(Environment.NewLine, TokenType.Default);
        }
        else
        {
            // Version (Type - cyan)
            yield return new SyntaxToken(message.Version, TokenType.Type);
            yield return new SyntaxToken(" ", TokenType.Default);

            // Status Code (Number - green)
            yield return new SyntaxToken(message.StatusCode, TokenType.Number);
            yield return new SyntaxToken(" ", TokenType.Default);

            // Reason Phrase (Default - dark gray)
            yield return new SyntaxToken(message.ReasonPhrase, TokenType.Default);
            yield return new SyntaxToken(Environment.NewLine, TokenType.Default);
        }

        // Generate tokens for headers
        foreach (var header in message.Headers)
        {
            // Header name (Property - light blue)
            yield return new SyntaxToken(header.Name, TokenType.Property);
            yield return new SyntaxToken(":", TokenType.Operator);
            yield return new SyntaxToken(" ", TokenType.Default);

            // Header value (Default - dark gray)
            yield return new SyntaxToken(header.Value, TokenType.Default);
            yield return new SyntaxToken(Environment.NewLine, TokenType.Default);
        }

        // Blank line separator
        if (!string.IsNullOrEmpty(message.Body))
        {
            yield return new SyntaxToken(Environment.NewLine, TokenType.Default);
        }

        // Generate tokens for body (with nested highlighting if applicable)
        if (!string.IsNullOrEmpty(message.Body))
        {
            string? bodyLanguage = null;

            if (message.ContentType != null &&
                MediaTypeToLanguage.TryGetValue(message.ContentType, out var lang))
            {
                bodyLanguage = lang;
            }

            // Delegate to appropriate highlighter if available
            if (bodyLanguage != null &&
                !bodyLanguage.Equals("plain", StringComparison.OrdinalIgnoreCase) &&
                _colorCodeHighlighter.SupportsLanguage(bodyLanguage))
            {
                var bodyTokens = _colorCodeHighlighter.Highlight(message.Body, bodyLanguage);
                foreach (var token in bodyTokens)
                    yield return token;
            }
            else
            {
                // Plain text body
                yield return new SyntaxToken(message.Body, TokenType.Default);
            }
        }
    }

    // Data structures for HTTP message representation
    private class HttpMessage
    {
        public bool IsRequest { get; set; }

        // Request fields
        public string Method { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        // Response fields
        public string StatusCode { get; set; } = string.Empty;
        public string ReasonPhrase { get; set; } = string.Empty;

        // Common fields
        public string Version { get; set; } = string.Empty;
        public List<HttpHeader> Headers { get; set; } = new();
        public string? Body { get; set; }
        public string? ContentType { get; set; }
    }

    private record HttpHeader(string Name, string Value);
}
