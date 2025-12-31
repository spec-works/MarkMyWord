using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using FluentAssertions;
using Xunit;

namespace MarkMyWord.Tests.InlineElements;

public class ImageTests
{
    [Fact]
    public void ImageWithAltText_ShouldCreateFallback()
    {
        // Arrange
        var markdown = "![Alt text](nonexistent.png)";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var runs = body.Descendants<Run>().ToList();
        runs.Should().NotBeEmpty();

        // Should contain fallback text
        var text = body.InnerText;
        text.Should().Contain("Image:");
    }

    [Fact]
    public void ImageWithTitle_ShouldUseTitleInFallback()
    {
        // Arrange
        var markdown = "![Alt text](nonexistent.png \"Image Title\")";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var text = body.InnerText;
        text.Should().Contain("Image Title");
    }

    [Fact]
    public void ImageWithoutUrl_ShouldCreateFallback()
    {
        // Arrange
        var markdown = "![Alt text]()";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var text = body.InnerText;
        text.Should().Contain("Image:");
    }

    [Fact]
    public void RegularLink_ShouldNotBeImage()
    {
        // Arrange
        var markdown = "[Link text](http://example.com)";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var hyperlinks = body.Descendants<Hyperlink>().ToList();
        hyperlinks.Should().HaveCount(1);

        var text = body.InnerText;
        text.Should().Be("Link text");
    }

    [Fact]
    public void MultipleImages_ShouldCreateMultipleFallbacks()
    {
        // Arrange
        var markdown = @"
![Image 1](img1.png)

Some text

![Image 2](img2.png)";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var paragraphs = body.Descendants<Paragraph>().ToList();
        paragraphs.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
