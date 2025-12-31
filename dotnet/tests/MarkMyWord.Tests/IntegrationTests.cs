using DocumentFormat.OpenXml.Packaging;
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
}
