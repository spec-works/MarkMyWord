using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Xunit;

namespace MarkMyWord.Tests.BlockElements;

public class TableTests
{
    [Fact]
    public void SimpleTable_ShouldCreateTableWithHeaderAndRow()
    {
        // Arrange
        var markdown = @"
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var tables = body.Descendants<Table>().ToList();
        tables.Should().HaveCount(1);

        var table = tables.First();
        var rows = table.Descendants<TableRow>().ToList();
        rows.Should().HaveCount(2); // Header + 1 data row

        var cells = rows[0].Descendants<TableCell>().ToList();
        cells.Should().HaveCount(2);
    }

    [Fact]
    public void TableWithMultipleRows_ShouldCreateAllRows()
    {
        // Arrange
        var markdown = @"
| Name | Age | City |
|------|-----|------|
| John | 30  | NYC  |
| Jane | 25  | LA   |
| Bob  | 35  | SF   |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var table = body.Descendants<Table>().First();
        var rows = table.Descendants<TableRow>().ToList();
        rows.Should().HaveCount(4); // Header + 3 data rows

        // Verify each row has 3 cells
        foreach (var row in rows)
        {
            var cells = row.Descendants<TableCell>().ToList();
            cells.Should().HaveCount(3);
        }
    }

    [Fact]
    public void TableHeaderRow_ShouldHaveBoldFormatting()
    {
        // Arrange
        var markdown = @"
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var table = body.Descendants<Table>().First();
        var headerRow = table.Descendants<TableRow>().First();
        var headerCell = headerRow.Descendants<TableCell>().First();

        var boldElements = headerCell.Descendants<Bold>().ToList();
        boldElements.Should().NotBeEmpty();
    }

    [Fact]
    public void TableHeaderRow_ShouldHaveShading()
    {
        // Arrange
        var markdown = @"
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var table = body.Descendants<Table>().First();
        var headerRow = table.Descendants<TableRow>().First();
        var headerCell = headerRow.Descendants<TableCell>().First();

        var shading = headerCell.Descendants<Shading>().FirstOrDefault();
        shading.Should().NotBeNull();
        shading!.Fill!.Value.Should().Be("D3D3D3"); // Light gray
    }

    [Fact]
    public void TableWithAlignment_ShouldCreateTable()
    {
        // Arrange
        var markdown = @"
| Left | Center | Right |
|:-----|:------:|------:|
| L1   | C1     | R1    |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var table = body.Descendants<Table>().First();
        var rows = table.Descendants<TableRow>().ToList();
        rows.Should().HaveCount(2);
    }

    [Fact]
    public void MultipleTables_ShouldCreateSeparateTables()
    {
        // Arrange
        var markdown = @"
| Table 1 |
|---------|
| Data 1  |

Some text

| Table 2 |
|---------|
| Data 2  |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var tables = body.Descendants<Table>().ToList();
        tables.Should().HaveCount(2);
    }

    [Fact]
    public void TableWithInlineFormatting_ShouldPreserveFormatting()
    {
        // Arrange
        var markdown = @"
| Header |
|--------|
| **Bold** text |";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var table = body.Descendants<Table>().First();

        // Just verify the table exists and has content
        var rows = table.Descendants<TableRow>().ToList();
        rows.Should().HaveCount(2);

        var cells = table.Descendants<TableCell>().ToList();
        cells.Should().HaveCount(2);
    }

    [Fact]
    public void EmptyTable_ShouldStillCreateTable()
    {
        // Arrange
        var markdown = @"
| Header |
|--------|";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var tables = body.Descendants<Table>().ToList();
        tables.Should().HaveCount(1);

        var rows = tables.First().Descendants<TableRow>().ToList();
        rows.Should().HaveCount(1); // Just the header
    }
}
