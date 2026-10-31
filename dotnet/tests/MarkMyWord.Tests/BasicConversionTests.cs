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

    [Fact]
    public void HyperlinksInsideTableCells_ShouldCreateHyperlinksAndRelationships()
    {
        var markdown = """
            # Repro

            A link in prose: [foo](https://example.com/foo)

            | col1 | col2 |
            |---|---|
            | plain | [bar](https://example.com/bar) |
            | [baz](https://example.com/baz) | text with [inline link](https://example.com/inline) inside |
            """;

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        var hyperlinks = doc.MainDocumentPart!.Document.Body!.Descendants<Hyperlink>().ToList();
        hyperlinks.Should().HaveCount(4);
        doc.MainDocumentPart.HyperlinkRelationships
            .Select(relationship => relationship.Uri.AbsoluteUri)
            .Should().BeEquivalentTo(
                "https://example.com/foo",
                "https://example.com/bar",
                "https://example.com/baz",
                "https://example.com/inline");
    }

    [Fact]
    public void Frontmatter_ShouldBeOmittedAndTitleUsedAsDocumentProperty()
    {
        var markdown = """
            ---
            title: "Release Notes"
            author: Example Author
            draft: true
            ---

            # Introduction

            Content remains in the document.
            """;

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;
        bodyText.Should().NotContain("author");
        bodyText.Should().NotContain("draft");
        bodyText.Should().Contain("Introduction");
        bodyText.Should().Contain("Content remains");
        doc.PackageProperties.Title.Should().Be("Release Notes");
    }

    [Fact]
    public void FrontmatterTitle_ShouldNotOverrideExplicitDocumentTitle()
    {
        var markdown = "---\ntitle: Frontmatter title\n---\n\nContent.";
        var options = new MarkMyWord.Configuration.ConversionOptions { DocumentTitle = "Explicit title" };

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown, options);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        doc.PackageProperties.Title.Should().Be("Explicit title");
    }

    [Fact]
    public void ConsecutiveLines_ShouldNotMergeIntoOneLine()
    {
        // Arrange — two lines in the same paragraph block, separated by a soft break
        var markdown = "**Authors:** Sebastien Levert\n**Date:** March 6, 2026";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        var breaks = paragraph.Descendants<Break>().ToList();
        breaks.Should().HaveCountGreaterThan(0, "a soft line break should produce a line break in Word");

        // Verify both text fragments are present
        var text = paragraph.InnerText;
        text.Should().Contain("Authors:");
        text.Should().Contain("Date:");
    }
}
