using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace MarkMyWord.Tests.BlockElements;

/// <summary>
/// Tests for Word Online–compatible table rendering.
/// Validates that generated tables include TableGrid, explicit column widths,
/// border colors, cell margins, TableLook, and column alignment — all required
/// for consistent rendering in Word for the web.
/// </summary>
public class TableWebCompatibilityTests
{
    #region TableGrid Tests

    [Fact]
    public void Table_ShouldContainTableGrid()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var tableGrid = table.Elements<TableGrid>().FirstOrDefault();
        tableGrid.Should().NotBeNull("Word Online requires TableGrid for column layout");
    }

    [Fact]
    public void TableGrid_ShouldHaveOneGridColumnPerColumn()
    {
        var markdown = "| A | B | C |\n|---|---|---|\n| 1 | 2 | 3 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var gridColumns = table.Elements<TableGrid>().First().Elements<GridColumn>().ToList();
        gridColumns.Should().HaveCount(3, "one GridColumn per table column");
    }

    [Fact]
    public void GridColumn_ShouldHaveExplicitWidth()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var gridColumns = table.Elements<TableGrid>().First().Elements<GridColumn>().ToList();
        foreach (var col in gridColumns)
        {
            col.Width.Should().NotBeNull("each GridColumn must have an explicit width");
            int.Parse(col.Width!.Value!).Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void GridColumnWidths_ShouldBeEquallyDistributed()
    {
        var markdown = "| A | B | C | D |\n|---|---|---|---|\n| 1 | 2 | 3 | 4 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var widths = table.Elements<TableGrid>().First()
            .Elements<GridColumn>()
            .Select(c => int.Parse(c.Width!.Value!))
            .ToList();

        widths.Should().HaveCount(4);
        widths.Distinct().Should().HaveCount(1, "all columns should have equal width");
    }

    #endregion

    #region Table Width and Layout Tests

    [Fact]
    public void TableWidth_ShouldUseDxaUnits()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        var tableProps = table.Elements<TableProperties>().First();

        var tableWidth = tableProps.TableWidth;
        tableWidth.Should().NotBeNull();
        tableWidth!.Type!.Value.Should().Be(TableWidthUnitValues.Dxa,
            "DXA units are more reliable in Word Online than percentage");
    }

    [Fact]
    public void TableLayout_ShouldBeFixed()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        var tableProps = table.Elements<TableProperties>().First();

        var layout = tableProps.TableLayout;
        layout.Should().NotBeNull("fixed layout ensures consistent column widths in Word Online");
        layout!.Type!.Value.Should().Be(TableLayoutValues.Fixed);
    }

    #endregion

    #region Cell Width Tests

    [Fact]
    public void CellWidth_ShouldUseDxaUnits()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var cells = table.Descendants<TableCell>().ToList();
        foreach (var cell in cells)
        {
            var cellWidth = cell.TableCellProperties?.TableCellWidth;
            cellWidth.Should().NotBeNull("each cell needs explicit width for Word Online");
            cellWidth!.Type!.Value.Should().Be(TableWidthUnitValues.Dxa);
            int.Parse(cellWidth.Width!.Value!).Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region Border Tests

    [Fact]
    public void Borders_ShouldHaveExplicitColors()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        var borders = table.Elements<TableProperties>().First().TableBorders;

        borders.Should().NotBeNull();

        borders!.TopBorder.Should().NotBeNull("top border should be defined");
        borders.TopBorder!.Color.Should().NotBeNull("top border color should be set");
        borders.TopBorder.Color!.Value.Should().NotBeNullOrEmpty("border color prevents rendering differences");

        borders.BottomBorder.Should().NotBeNull("bottom border should be defined");
        borders.BottomBorder!.Color.Should().NotBeNull("bottom border color should be set");
        borders.BottomBorder.Color!.Value.Should().NotBeNullOrEmpty();

        borders.LeftBorder.Should().NotBeNull("left border should be defined");
        borders.LeftBorder!.Color.Should().NotBeNull("left border color should be set");
        borders.LeftBorder.Color!.Value.Should().NotBeNullOrEmpty();

        borders.RightBorder.Should().NotBeNull("right border should be defined");
        borders.RightBorder!.Color.Should().NotBeNull("right border color should be set");
        borders.RightBorder.Color!.Value.Should().NotBeNullOrEmpty();

        borders.InsideHorizontalBorder.Should().NotBeNull("inside horizontal border should be defined");
        borders.InsideHorizontalBorder!.Color.Should().NotBeNull("inside horizontal border color should be set");
        borders.InsideHorizontalBorder.Color!.Value.Should().NotBeNullOrEmpty();

        borders.InsideVerticalBorder.Should().NotBeNull("inside vertical border should be defined");
        borders.InsideVerticalBorder!.Color.Should().NotBeNull("inside vertical border color should be set");
        borders.InsideVerticalBorder.Color!.Value.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region TableLook Tests

    [Fact]
    public void Table_ShouldHaveTableLook()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        var tableProps = table.Elements<TableProperties>().First();

        var tableLook = tableProps.TableLook;
        tableLook.Should().NotBeNull("TableLook tells Word Online how to apply conditional formatting");
        tableLook!.FirstRow!.Value.Should().BeTrue("first row should be treated as header");
    }

    #endregion

    #region Cell Margins Tests

    [Fact]
    public void Table_ShouldHaveDefaultCellMargins()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();
        var tableProps = table.Elements<TableProperties>().First();

        var margins = tableProps.TableCellMarginDefault;
        margins.Should().NotBeNull("cell margins prevent content from touching borders");
    }

    #endregion

    #region Header Row Tests

    [Fact]
    public void HeaderRow_ShouldHaveTableHeader()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var headerRow = table.Elements<TableRow>().First();
        var headerProp = headerRow.TableRowProperties?.Elements<TableHeader>().FirstOrDefault();
        headerProp.Should().NotBeNull("header rows should repeat across page breaks");
    }

    [Fact]
    public void DataRow_ShouldNotHaveTableHeader()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var dataRow = table.Elements<TableRow>().Last();
        var headerProp = dataRow.TableRowProperties?.Elements<TableHeader>().FirstOrDefault();
        headerProp.Should().BeNull("data rows should not be marked as headers");
    }

    #endregion

    #region Column Alignment Tests

    [Fact]
    public void LeftAlignedColumn_ShouldHaveLeftJustification()
    {
        var markdown = "| Left |\n|:-----|\n| text |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var dataCell = table.Elements<TableRow>().Last().Elements<TableCell>().First();
        var para = dataCell.Elements<Paragraph>().First();
        var justification = para.ParagraphProperties?.Justification?.Val?.Value;
        justification.Should().Be(JustificationValues.Left);
    }

    [Fact]
    public void CenterAlignedColumn_ShouldHaveCenterJustification()
    {
        var markdown = "| Center |\n|:------:|\n| text   |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var dataCell = table.Elements<TableRow>().Last().Elements<TableCell>().First();
        var para = dataCell.Elements<Paragraph>().First();
        var justification = para.ParagraphProperties?.Justification?.Val?.Value;
        justification.Should().Be(JustificationValues.Center);
    }

    [Fact]
    public void RightAlignedColumn_ShouldHaveRightJustification()
    {
        var markdown = "| Right |\n|------:|\n| text  |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var dataCell = table.Elements<TableRow>().Last().Elements<TableCell>().First();
        var para = dataCell.Elements<Paragraph>().First();
        var justification = para.ParagraphProperties?.Justification?.Val?.Value;
        justification.Should().Be(JustificationValues.Right);
    }

    [Fact]
    public void MixedAlignmentColumns_ShouldApplyCorrectly()
    {
        var markdown = "| Left | Center | Right |\n|:-----|:------:|------:|\n| L    | C      | R     |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var dataRow = table.Elements<TableRow>().Last();
        var cells = dataRow.Elements<TableCell>().ToList();

        cells[0].Elements<Paragraph>().First().ParagraphProperties?.Justification?.Val?.Value
            .Should().Be(JustificationValues.Left);
        cells[1].Elements<Paragraph>().First().ParagraphProperties?.Justification?.Val?.Value
            .Should().Be(JustificationValues.Center);
        cells[2].Elements<Paragraph>().First().ParagraphProperties?.Justification?.Val?.Value
            .Should().Be(JustificationValues.Right);
    }

    #endregion

    #region Paragraph Spacing Tests

    [Fact]
    public void CellParagraph_ShouldHaveZeroAfterSpacing()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var cell = table.Descendants<TableCell>().First();
        var spacing = cell.Elements<Paragraph>().First().ParagraphProperties?.SpacingBetweenLines;
        spacing.Should().NotBeNull();
        spacing!.After!.Value.Should().Be("0", "cells should not have extra spacing after text");
    }

    #endregion

    #region End-to-End Integration Tests

    [Fact]
    public void ExistingTableTests_ShouldStillPass_SimpleTable()
    {
        var markdown = @"
| Header 1 | Header 2 |
|----------|----------|
| Cell 1   | Cell 2   |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        var tables = body.Descendants<Table>().ToList();
        tables.Should().HaveCount(1);

        var table = tables.First();
        var rows = table.Descendants<TableRow>().ToList();
        rows.Should().HaveCount(2);

        var cells = rows[0].Descendants<TableCell>().ToList();
        cells.Should().HaveCount(2);
    }

    [Fact]
    public void LargeTable_ShouldRenderWithCorrectStructure()
    {
        var markdown = "| A | B | C | D | E | F |\n|---|---|---|---|---|---|\n| 1 | 2 | 3 | 4 | 5 | 6 |\n| 7 | 8 | 9 | 10 | 11 | 12 |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        // Verify grid
        var gridColumns = table.Elements<TableGrid>().First().Elements<GridColumn>().ToList();
        gridColumns.Should().HaveCount(6);

        // Verify rows
        var rows = table.Elements<TableRow>().ToList();
        rows.Should().HaveCount(3);

        // Verify every row has 6 cells
        foreach (var row in rows)
        {
            row.Elements<TableCell>().Should().HaveCount(6);
        }
    }

    [Fact]
    public void HeaderCells_ShouldStillHaveShading()
    {
        var markdown = "| H1 | H2 |\n|----|----|\n| A  | B  |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var headerRow = table.Elements<TableRow>().First();
        var headerCell = headerRow.Elements<TableCell>().First();

        var shading = headerCell.Descendants<Shading>().FirstOrDefault();
        shading.Should().NotBeNull();
        shading!.Fill!.Value.Should().Be("D3D3D3");
    }

    [Fact]
    public void HeaderCells_ShouldStillHaveBoldText()
    {
        var markdown = "| H1 | H2 |\n|----|----|\n| A  | B  |";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var table = doc.MainDocumentPart!.Document.Body!.Descendants<Table>().First();

        var headerRow = table.Elements<TableRow>().First();
        var headerCell = headerRow.Elements<TableCell>().First();

        headerCell.Descendants<Bold>().Should().NotBeEmpty();
    }

    #endregion
}
