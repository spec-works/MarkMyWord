using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace MarkMyWord.Tests;

public class BasicConversionTests
{
    [Fact]
    public void HelloWorld_ShouldConvertSuccessfully()
    {
        // Arrange
        var markdown = "Hello World";

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
    }

    [Fact]
    public void SimpleParagraph_ShouldCreateParagraph()
    {
        // Arrange
        var markdown = "This is a simple paragraph.";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Heading1_ShouldCreateHeadingWithStyle()
    {
        // Arrange
        var markdown = "# My Heading";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCountGreaterThan(0);

        var firstParagraph = paragraphs[0];
        var styleId = firstParagraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        styleId.Should().Be("Heading1");
    }

    [Fact]
    public void BoldText_ShouldCreateBoldRun()
    {
        // Arrange
        var markdown = "This is **bold** text";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().ToList();
        runs.Should().HaveCountGreaterThan(0);

        // Find the bold run
        var boldRun = runs.FirstOrDefault(r => r.RunProperties?.Bold != null);
        boldRun.Should().NotBeNull();
    }

    [Fact]
    public void InlineCode_ShouldCreateCodeRun()
    {
        // Arrange
        var markdown = "This is `inline code` text";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().ToList();
        runs.Should().HaveCountGreaterThan(0);

        // Find the code run (should have specific font and shading)
        var codeRun = runs.FirstOrDefault(r =>
            r.RunProperties?.RunFonts?.Ascii?.Value == "Consolas" ||
            r.RunProperties?.Shading != null);
        codeRun.Should().NotBeNull();
    }

    [Fact]
    public void CodeBlock_ShouldCreateMultipleParagraphs()
    {
        // Arrange
        var markdown = @"```csharp
var x = 10;
Console.WriteLine(x);
```";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCount(2); // Exactly 2 lines of code (no language label, no trailing empty lines)
    }

    [Fact]
    public void Hyperlink_ShouldCreateHyperlinkElement()
    {
        // Arrange
        var markdown = "[Google](https://www.google.com)";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var hyperlinks = doc.MainDocumentPart!.Document.Body!.Descendants<Hyperlink>().ToList();
        hyperlinks.Should().HaveCount(1);
    }
}
