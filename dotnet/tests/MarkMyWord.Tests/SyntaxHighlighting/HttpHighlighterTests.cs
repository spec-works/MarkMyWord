using FluentAssertions;
using MarkMyWord.SyntaxHighlighting;

namespace MarkMyWord.Tests.SyntaxHighlighting;

public class HttpHighlighterTests
{
    [Fact]
    public void SupportsLanguage_Http_ShouldReturnTrue()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act
        var result = highlighter.SupportsLanguage("http");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsLanguage_Https_ShouldReturnTrue()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act
        var result = highlighter.SupportsLanguage("https");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsLanguage_Request_ShouldReturnTrue()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act
        var result = highlighter.SupportsLanguage("request");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsLanguage_Response_ShouldReturnTrue()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act
        var result = highlighter.SupportsLanguage("response");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsLanguage_CaseInsensitive_ShouldReturnTrue()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act & Assert
        highlighter.SupportsLanguage("HTTP").Should().BeTrue();
        highlighter.SupportsLanguage("HTTPS").Should().BeTrue();
        highlighter.SupportsLanguage("REQUEST").Should().BeTrue();
        highlighter.SupportsLanguage("RESPONSE").Should().BeTrue();
    }

    [Fact]
    public void SupportsLanguage_UnsupportedLanguage_ShouldReturnFalse()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act
        var result = highlighter.SupportsLanguage("python");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HttpRequest_BasicGet_ShouldHighlightCorrectly()
    {
        // Arrange
        var code = @"GET /api/users HTTP/1.1
Host: example.com";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Check method is Keyword
        tokens[0].Should().Be(new SyntaxToken("GET", TokenType.Keyword));

        // Check URL is String
        tokens[2].Should().Be(new SyntaxToken("/api/users", TokenType.String));

        // Check version is Type
        tokens[4].Should().Be(new SyntaxToken("HTTP/1.1", TokenType.Type));

        // Check header name is Property
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "Host");
    }

    [Fact]
    public void HttpRequest_Post_ShouldHighlightMethodAsKeyword()
    {
        // Arrange
        var code = @"POST /api/users HTTP/1.1
Host: example.com";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens[0].Should().Be(new SyntaxToken("POST", TokenType.Keyword));
    }

    [Fact]
    public void HttpResponse_WithStatusCode_ShouldHighlightCorrectly()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK
Content-Type: text/plain";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Check version is Type
        tokens[0].Should().Be(new SyntaxToken("HTTP/1.1", TokenType.Type));

        // Check status code is Number
        tokens[2].Should().Be(new SyntaxToken("200", TokenType.Number));

        // Check reason phrase is Default
        tokens[4].Should().Be(new SyntaxToken("OK", TokenType.Default));

        // Check header name is Property
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "Content-Type");
    }

    [Fact]
    public void HttpResponse_WithJsonBody_ShouldHighlightBodyAsJson()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK
Content-Type: application/json

{""name"": ""John"", ""age"": 30}";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Should have Property tokens from JSON body (property names with quotes)
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "\"name\"");
        propertyTokens.Should().Contain(t => t.Text == "\"age\"");

        // Should have String tokens from JSON body (string values)
        var stringTokens = tokens.Where(t => t.Type == TokenType.String).ToList();
        stringTokens.Should().Contain(t => t.Text == "\"John\"");

        // Should have Number tokens from JSON body
        var numberTokens = tokens.Where(t => t.Type == TokenType.Number).ToList();
        numberTokens.Should().Contain(t => t.Text == "30");
    }

    [Fact]
    public void HttpRequest_WithJsonBody_ShouldHighlightBodyAsJson()
    {
        // Arrange
        var code = @"POST /api/users HTTP/1.1
Content-Type: application/json

{""name"": ""Jane"", ""email"": ""jane@example.com""}";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Should have Property tokens from JSON body (property names with quotes)
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "\"name\"");
        propertyTokens.Should().Contain(t => t.Text == "\"email\"");
    }

    [Fact]
    public void HttpRequest_MultipleHeaders_ShouldHighlightAllHeaders()
    {
        // Arrange
        var code = @"GET /api/users HTTP/1.1
Host: example.com
Authorization: Bearer token123
Accept: application/json
User-Agent: TestClient/1.0";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        var headerTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        headerTokens.Should().HaveCount(4);
        headerTokens.Should().Contain(t => t.Text == "Host");
        headerTokens.Should().Contain(t => t.Text == "Authorization");
        headerTokens.Should().Contain(t => t.Text == "Accept");
        headerTokens.Should().Contain(t => t.Text == "User-Agent");
    }

    [Fact]
    public void HttpResponse_NoContentType_ShouldRenderBodyAsPlainText()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK

Plain text body";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Body should be Default token type (plain text)
        var bodyToken = tokens.Last();
        bodyToken.Type.Should().Be(TokenType.Default);
        bodyToken.Text.Should().Be("Plain text body");
    }

    [Fact]
    public void HttpResponse_UnknownMediaType_ShouldRenderBodyAsPlainText()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK
Content-Type: application/octet-stream

Binary data here";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Body should be Default token type (plain text)
        var bodyToken = tokens.Last();
        bodyToken.Type.Should().Be(TokenType.Default);
        bodyToken.Text.Should().Be("Binary data here");
    }

    [Fact]
    public void InvalidHttpMessage_ShouldReturnPlainText()
    {
        // Arrange
        var code = "This is not an HTTP message";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().HaveCount(1);
        tokens[0].Should().Be(new SyntaxToken(code, TokenType.Default));
    }

    [Fact]
    public void HttpRequest_NoBody_ShouldNotEmitBodyTokens()
    {
        // Arrange
        var code = @"GET /api/users HTTP/1.1
Host: example.com";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Should not have extra blank line separator at the end
        // (only newlines after request line and headers)
        var newlineCount = tokens.Count(t => t.Text == Environment.NewLine);
        newlineCount.Should().Be(2); // One after request line, one after header
    }

    [Fact]
    public void HttpResponse_NoBody_ShouldNotEmitBodyTokens()
    {
        // Arrange
        var code = @"HTTP/1.1 204 No Content
Content-Length: 0";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Should not have extra blank line separator
        var newlineCount = tokens.Count(t => t.Text == Environment.NewLine);
        newlineCount.Should().Be(2); // One after status line, one after header
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    [InlineData("CONNECT")]
    public void HttpRequest_VariousMethods_ShouldHighlightMethodAsKeyword(string method)
    {
        // Arrange
        var code = $"{method} /api/resource HTTP/1.1\nHost: example.com";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens[0].Should().Be(new SyntaxToken(method, TokenType.Keyword));
    }

    [Theory]
    [InlineData("200", "OK")]
    [InlineData("201", "Created")]
    [InlineData("404", "Not Found")]
    [InlineData("500", "Internal Server Error")]
    public void HttpResponse_VariousStatusCodes_ShouldHighlightCodeAsNumber(string statusCode, string reasonPhrase)
    {
        // Arrange
        var code = $"HTTP/1.1 {statusCode} {reasonPhrase}\nContent-Type: text/plain";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens[2].Should().Be(new SyntaxToken(statusCode, TokenType.Number));
        tokens[4].Should().Be(new SyntaxToken(reasonPhrase, TokenType.Default));
    }

    [Fact]
    public void HttpResponse_ContentTypeWithCharset_ShouldExtractMediaType()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{""test"": true}";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Body should be highlighted as JSON (property tokens present with quotes)
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "\"test\"");

        // Should have Keyword token for true
        var keywordTokens = tokens.Where(t => t.Type == TokenType.Keyword).ToList();
        keywordTokens.Should().Contain(t => t.Text == "true");
    }

    [Fact]
    public void HttpRequest_EmptyCode_ShouldReturnEmpty()
    {
        // Arrange
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight("", "http").ToList();

        // Assert
        tokens.Should().BeEmpty();
    }

    [Fact]
    public void HttpRequest_WithComplexUrl_ShouldHighlightUrlAsString()
    {
        // Arrange
        var code = @"GET /api/users?page=1&limit=10 HTTP/1.1
Host: api.example.com";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens[2].Should().Be(new SyntaxToken("/api/users?page=1&limit=10", TokenType.String));
    }

    [Fact]
    public void HttpResponse_WithPlainTextContentType_ShouldRenderBodyAsPlainText()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK
Content-Type: text/plain

This is plain text content.";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Body should be Default token type
        var bodyToken = tokens.Last();
        bodyToken.Type.Should().Be(TokenType.Default);
        bodyToken.Text.Should().Be("This is plain text content.");
    }

    [Fact]
    public void HttpRequest_ColonInHeaderValue_ShouldParseCorrectly()
    {
        // Arrange
        var code = @"GET /api/users HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Header name should be Property
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "Authorization");

        // Header value should include the full JWT token with colons
        var authValueToken = tokens.SkipWhile(t => t.Text != "Authorization")
                                   .Skip(3) // Skip "Authorization", ":", " "
                                   .First();
        authValueToken.Text.Should().Contain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
    }

    [Fact]
    public void HttpResponse_MultilineJsonBody_ShouldHighlightCorrectly()
    {
        // Arrange
        var code = @"HTTP/1.1 200 OK
Content-Type: application/json

{
  ""id"": 123,
  ""status"": ""success"",
  ""data"": {
    ""name"": ""Alice"",
    ""active"": true
  }
}";
        var highlighter = new HttpHighlighter();

        // Act
        var tokens = highlighter.Highlight(code, "http").ToList();

        // Assert
        tokens.Should().NotBeEmpty();

        // Should have Property tokens from JSON body (property names with quotes)
        var propertyTokens = tokens.Where(t => t.Type == TokenType.Property).ToList();
        propertyTokens.Should().Contain(t => t.Text == "\"id\"");
        propertyTokens.Should().Contain(t => t.Text == "\"status\"");
        propertyTokens.Should().Contain(t => t.Text == "\"data\"");
        propertyTokens.Should().Contain(t => t.Text == "\"name\"");
        propertyTokens.Should().Contain(t => t.Text == "\"active\"");

        // Should have Number token
        var numberTokens = tokens.Where(t => t.Type == TokenType.Number).ToList();
        numberTokens.Should().Contain(t => t.Text == "123");

        // Should have Keyword tokens for true
        var keywordTokens = tokens.Where(t => t.Type == TokenType.Keyword).ToList();
        keywordTokens.Should().Contain(t => t.Text == "true");
    }
}
