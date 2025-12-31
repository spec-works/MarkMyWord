using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace MarkMyWord.Tests.BlockElements;

public class ListTests
{
    [Fact]
    public void UnorderedList_ShouldCreateBulletedList()
    {
        // Arrange
        var markdown = @"- Item 1
- Item 2
- Item 3";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCount(3);

        // Each paragraph should have numbering properties
        foreach (var para in paragraphs)
        {
            para.ParagraphProperties.Should().NotBeNull();
            para.ParagraphProperties!.NumberingProperties.Should().NotBeNull();
        }
    }

    [Fact]
    public void OrderedList_ShouldCreateNumberedList()
    {
        // Arrange
        var markdown = @"1. First item
2. Second item
3. Third item";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCount(3);

        // Each paragraph should have numbering properties
        foreach (var para in paragraphs)
        {
            para.ParagraphProperties.Should().NotBeNull();
            para.ParagraphProperties!.NumberingProperties.Should().NotBeNull();
        }
    }

    [Fact]
    public void NestedUnorderedList_ShouldIncreaseIndentation()
    {
        // Arrange
        var markdown = @"- Item 1
  - Nested 1.1
  - Nested 1.2
- Item 2";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCountGreaterOrEqualTo(4);

        // Verify numbering exists
        var numberedParas = paragraphs.Where(p =>
            p.ParagraphProperties?.NumberingProperties != null).ToList();
        numberedParas.Should().HaveCount(4);
    }

    [Fact]
    public void NestedOrderedList_ShouldIncreaseIndentation()
    {
        // Arrange
        var markdown = @"1. Item 1
   1. Nested 1.1
   2. Nested 1.2
2. Item 2";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        var numberedParas = paragraphs.Where(p =>
            p.ParagraphProperties?.NumberingProperties != null).ToList();

        numberedParas.Should().HaveCount(4);
    }

    [Fact]
    public void MixedList_OrderedAndUnordered_ShouldWork()
    {
        // Arrange
        var markdown = @"1. Ordered item
   - Unordered nested
   - Another unordered
2. Second ordered";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        var numberedParas = paragraphs.Where(p =>
            p.ParagraphProperties?.NumberingProperties != null).ToList();

        numberedParas.Should().HaveCount(4);
    }

    [Fact]
    public void ListWithInlineFormatting_ShouldPreserveFormatting()
    {
        // Arrange
        var markdown = @"- Item with **bold**
- Item with *italic*
- Item with `code`";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().ToList();

        // Should have runs with different formatting
        var boldRuns = runs.Where(r => r.RunProperties != null && r.RunProperties.Bold != null).ToList();
        var italicRuns = runs.Where(r => r.RunProperties != null && r.RunProperties.Italic != null).ToList();

        boldRuns.Should().NotBeEmpty();
        italicRuns.Should().NotBeEmpty();
    }

    [Fact]
    public void MultipleSeparateLists_ShouldCreateSeparateLists()
    {
        // Arrange
        var markdown = @"1. List 1 Item 1
2. List 1 Item 2

Regular paragraph

1. List 2 Item 1
2. List 2 Item 2";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        paragraphs.Should().HaveCountGreaterOrEqualTo(5);

        // Should have a paragraph without numbering (the regular paragraph)
        var nonListParas = paragraphs.Where(p =>
            p.ParagraphProperties == null || p.ParagraphProperties.NumberingProperties == null).ToList();
        nonListParas.Should().NotBeEmpty();
    }
}
