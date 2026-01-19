using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace MarkMyWord.Tests;

public class IntegrationTests
{
    [Fact]
    public void ExampleMarkdown_ShouldConvertSuccessfully()
    {
        // Arrange
        var examplePath = Path.Combine("..", "..", "..", "..", "..", "samples", "example.md");
        if (!File.Exists(examplePath))
        {
            // Skip test if example file doesn't exist
            return;
        }

        var markdown = File.ReadAllText(examplePath);

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        docxBytes.Should().NotBeNull();
        docxBytes.Length.Should().BeGreaterThan(0);

        // Verify it's a valid Word document
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        doc.Should().NotBeNull();
        doc.MainDocumentPart.Should().NotBeNull();

        // Optionally save for manual inspection
        var outputPath = Path.Combine(Path.GetTempPath(), "markmyword-example.docx");
        File.WriteAllBytes(outputPath, docxBytes);
    }

    [Fact]
    public void ComplexDocument_WithAllFeatures_ShouldConvert()
    {
        // Arrange
        var markdown = @"# Main Title

## Introduction

This document contains **bold**, *italic*, and ***bold italic*** text.

### Code Examples

Here's some inline code: `var x = 10;`

And a code block:

```csharp
public class Test
{
    public void Method()
    {
        Console.WriteLine(""Hello"");
    }
}
```

### Links and Quotes

Check out [this link](https://example.com).

> This is a quote
> with multiple lines

---

## Conclusion

That's all folks!";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        docxBytes.Should().NotBeNull();
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        doc.MainDocumentPart.Should().NotBeNull();
    }

    [Fact]
    public void HttpCodeBlock_WithJsonBody_ShouldConvertWithSyntaxHighlighting()
    {
        // Arrange
        var markdown = @"# HTTP Request Example

Here's an HTTP request with JSON body:

```http
POST /api/users HTTP/1.1
Host: api.example.com
Content-Type: application/json
Authorization: Bearer token123

{
  ""name"": ""Alice"",
  ""email"": ""alice@example.com"",
  ""age"": 30
}
```

And here's an HTTP response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-cache

{
  ""id"": 123,
  ""status"": ""success"",
  ""created"": true
}
```";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        docxBytes.Should().NotBeNull();
        docxBytes.Length.Should().BeGreaterThan(0);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        doc.Should().NotBeNull();
        doc.MainDocumentPart.Should().NotBeNull();

        var runs = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().ToList();

        // Should have multiple runs (syntax highlighting creates separate runs for different token types)
        runs.Should().HaveCountGreaterThan(20);

        // Should have runs with different colors (indicating syntax highlighting is working)
        var coloredRuns = runs.Where(r => r.RunProperties?.Color?.Val?.Value != null).ToList();
        coloredRuns.Should().HaveCountGreaterThan(10, "syntax highlighting should create multiple colored runs");

        // Verify we have at least a few different colors being used
        var distinctColors = coloredRuns
            .Select(r => r.RunProperties?.Color?.Val?.Value)
            .Distinct()
            .ToList();
        distinctColors.Should().HaveCountGreaterThan(2, "should have multiple different colors for different syntax elements");

        // Optionally save for manual inspection
        var outputPath = Path.Combine(Path.GetTempPath(), "markmyword-http-test.docx");
        File.WriteAllBytes(outputPath, docxBytes);
    }
}
