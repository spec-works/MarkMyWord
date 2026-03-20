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

    [Fact]
    public void MultipleSeparateOrderedLists_ShouldResetNumbering()
    {
        // Arrange – two separate ordered lists separated by a paragraph
        var markdown = @"1. First
2. Second
3. Third

A break paragraph

1. Alpha
2. Beta";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var numbering = doc.MainDocumentPart!.NumberingDefinitionsPart!.Numbering;
        var listParas = doc.MainDocumentPart.Document.Body!
            .Elements<Paragraph>()
            .Where(p => p.ParagraphProperties?.NumberingProperties != null)
            .ToList();

        // The two lists must use different NumberingId values
        var numIds = listParas
            .Select(p => p.ParagraphProperties!.NumberingProperties!.NumberingId!.Val!.Value)
            .Distinct()
            .ToList();

        numIds.Should().HaveCount(2, "each separate list block should get its own numbering instance");

        // Each NumberingInstance should carry a LevelOverride that restarts at 1
        foreach (var numId in numIds)
        {
            var instance = numbering.Elements<NumberingInstance>()
                .First(ni => ni.NumberID!.Value == numId);

            var overrides = instance.Elements<LevelOverride>().ToList();
            overrides.Should().NotBeEmpty("every numbering instance should reset its start value");
            overrides.First().StartOverrideNumberingValue!.Val!.Value.Should().Be(1);
        }
    }

    [Fact]
    public void BulletList_ShouldUseBulletFormat_NotDecimal()
    {
        // Arrange – bullet list followed by an ordered list
        var markdown = @"- Bullet A
- Bullet B

1. Number 1
2. Number 2";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var numbering = doc.MainDocumentPart!.NumberingDefinitionsPart!.Numbering;
        var listParas = doc.MainDocumentPart.Document.Body!
            .Elements<Paragraph>()
            .Where(p => p.ParagraphProperties?.NumberingProperties != null)
            .ToList();

        // Bullet paragraphs (first two) and ordered paragraphs (last two)
        var bulletNumId = listParas[0].ParagraphProperties!.NumberingProperties!.NumberingId!.Val!.Value;
        var orderedNumId = listParas[2].ParagraphProperties!.NumberingProperties!.NumberingId!.Val!.Value;

        bulletNumId.Should().NotBe(orderedNumId, "bullets and numbers must use different numbering instances");

        // Resolve the abstract num for each and verify formats
        var bulletInstance = numbering.Elements<NumberingInstance>().First(ni => ni.NumberID!.Value == bulletNumId);
        var orderedInstance = numbering.Elements<NumberingInstance>().First(ni => ni.NumberID!.Value == orderedNumId);

        var bulletAbstractId = bulletInstance.AbstractNumId!.Val!.Value;
        var orderedAbstractId = orderedInstance.AbstractNumId!.Val!.Value;

        bulletAbstractId.Should().NotBe(orderedAbstractId, "bullets and numbers must reference different abstract definitions");

        var bulletAbstract = numbering.Elements<AbstractNum>().First(a => a.AbstractNumberId!.Value == bulletAbstractId);
        var orderedAbstract = numbering.Elements<AbstractNum>().First(a => a.AbstractNumberId!.Value == orderedAbstractId);

        bulletAbstract.Elements<Level>().First().NumberingFormat!.Val!.Value
            .Should().Be(NumberFormatValues.Bullet, "bullet list abstract num should use Bullet format");

        orderedAbstract.Elements<Level>().First().NumberingFormat!.Val!.Value
            .Should().Be(NumberFormatValues.Decimal, "ordered list abstract num should use Decimal format");
    }

    [Fact]
    public void OrderedListThenBulletList_ShouldUseBulletFormat_NotDecimal()
    {
        // Arrange – ordered list FIRST, then bullet list (triggers the ID collision bug)
        var markdown = @"1. Number 1
2. Number 2

- Bullet A
- Bullet B";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var numbering = doc.MainDocumentPart!.NumberingDefinitionsPart!.Numbering;
        var listParas = doc.MainDocumentPart.Document.Body!
            .Elements<Paragraph>()
            .Where(p => p.ParagraphProperties?.NumberingProperties != null)
            .ToList();

        var orderedNumId = listParas[0].ParagraphProperties!.NumberingProperties!.NumberingId!.Val!.Value;
        var bulletNumId = listParas[2].ParagraphProperties!.NumberingProperties!.NumberingId!.Val!.Value;

        // Resolve the abstract num for the bullet list and verify it uses Bullet format
        var bulletInstance = numbering.Elements<NumberingInstance>().First(ni => ni.NumberID!.Value == bulletNumId);
        var bulletAbstractId = bulletInstance.AbstractNumId!.Val!.Value;
        var bulletAbstract = numbering.Elements<AbstractNum>().First(a => a.AbstractNumberId!.Value == bulletAbstractId);

        bulletAbstract.Elements<Level>().First().NumberingFormat!.Val!.Value
            .Should().Be(NumberFormatValues.Bullet, "bullet list must use Bullet format even when ordered list is created first");
    }
}
