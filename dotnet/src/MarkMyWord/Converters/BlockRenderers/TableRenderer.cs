using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using MarkdigTable = Markdig.Extensions.Tables.Table;
using MarkdigTableRow = Markdig.Extensions.Tables.TableRow;
using MarkdigTableCell = Markdig.Extensions.Tables.TableCell;
using WordTable = DocumentFormat.OpenXml.Wordprocessing.Table;
using WordTableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using WordTableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for table blocks.
/// Generates Word Online–compatible OpenXML with explicit TableGrid,
/// fixed column widths, border colors, cell margins, and TableLook.
/// </summary>
public class TableRenderer : OpenXmlObjectRenderer<MarkdigTable>
{
    // Standard page body width in DXA (twips): 8.5" − 1" left − 1" right = 6.5" × 1440 dxa/inch
    private const int PageBodyWidthDxa = 9360;

    private const string BorderColor = "BFBFBF";
    private const string HeaderFillColor = "D3D3D3";

    protected override void Write(OpenXmlRenderer renderer, MarkdigTable table)
    {
        int columnCount = GetColumnCount(table);
        if (columnCount == 0)
            return;

        int cellWidthDxa = PageBodyWidthDxa / columnCount;
        int tableWidthDxa = cellWidthDxa * columnCount;

        var wordTable = new WordTable();

        // Table properties — explicit layout and border colors for Word Online compatibility
        var tableProperties = new TableProperties(
            new TableWidth { Width = tableWidthDxa.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed },
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor, Space = 0 },
                new BottomBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor, Space = 0 },
                new LeftBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor, Space = 0 },
                new RightBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor, Space = 0 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor, Space = 0 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = BorderColor, Space = 0 }
            ),
            new TableCellMarginDefault(
                new TopMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                new StartMargin { Width = "80", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "40", Type = TableWidthUnitValues.Dxa },
                new EndMargin { Width = "80", Type = TableWidthUnitValues.Dxa }
            ),
            new TableLook
            {
                Val = "04A0",
                FirstRow = true,
                LastRow = false,
                FirstColumn = false,
                LastColumn = false,
                NoHorizontalBand = false,
                NoVerticalBand = true
            }
        );
        wordTable.AppendChild(tableProperties);

        // TableGrid — explicit column widths required by Word Online
        var tableGrid = new TableGrid();
        for (int i = 0; i < columnCount; i++)
        {
            tableGrid.AppendChild(new GridColumn { Width = cellWidthDxa.ToString() });
        }
        wordTable.AppendChild(tableGrid);

        // Column alignments from Markdig
        var alignments = table.ColumnDefinitions?
            .Select(cd => cd?.Alignment)
            .ToArray() ?? Array.Empty<TableColumnAlign?>();

        // Process table rows
        foreach (var row in table)
        {
            if (row is MarkdigTableRow tableRow)
            {
                WriteTableRow(renderer, wordTable, tableRow, cellWidthDxa, alignments);
            }
        }

        // Add table to document body
        renderer.DocumentBuilder.Body.AppendChild(wordTable);
    }

    /// <summary>
    /// Determines the column count from the first row's actual cell count.
    /// Falls back to ColumnDefinitions if no rows are present.
    /// </summary>
    private static int GetColumnCount(MarkdigTable table)
    {
        // Prefer actual cell count from the first row — most reliable source
        foreach (var row in table)
        {
            if (row is MarkdigTableRow tableRow)
                return tableRow.Count;
        }

        if (table.ColumnDefinitions?.Count > 0)
            return table.ColumnDefinitions.Count;

        return 0;
    }

    private void WriteTableRow(OpenXmlRenderer renderer, WordTable wordTable, MarkdigTableRow tableRow, int cellWidthDxa, TableColumnAlign?[] alignments)
    {
        var wordRow = new WordTableRow();

        bool isHeaderRow = tableRow.IsHeader;

        // Mark header rows so Word repeats them across page breaks
        if (isHeaderRow)
        {
            wordRow.AppendChild(new TableRowProperties(
                new TableHeader()
            ));
        }

        int colIndex = 0;
        foreach (var cell in tableRow)
        {
            if (cell is MarkdigTableCell tableCell)
            {
                var alignment = colIndex < alignments.Length ? alignments[colIndex] : null;
                WriteTableCell(renderer, wordRow, tableCell, isHeaderRow, cellWidthDxa, alignment);
                colIndex++;
            }
        }

        wordTable.AppendChild(wordRow);
    }

    private void WriteTableCell(OpenXmlRenderer renderer, WordTableRow wordRow, MarkdigTableCell tableCell, bool isHeader, int cellWidthDxa, TableColumnAlign? alignment)
    {
        var wordCell = new WordTableCell();

        // Explicit cell width in DXA for Word Online compatibility
        var cellProperties = new TableCellProperties(
            new TableCellWidth { Width = cellWidthDxa.ToString(), Type = TableWidthUnitValues.Dxa }
        );

        // Header cell shading
        if (isHeader)
        {
            cellProperties.AppendChild(new Shading
            {
                Val = ShadingPatternValues.Clear,
                Color = "auto",
                Fill = HeaderFillColor
            });
        }

        wordCell.AppendChild(cellProperties);

        // Create a paragraph for the cell content with alignment and tight spacing
        var paragraph = new Paragraph();
        var paragraphProperties = new ParagraphProperties();

        var justification = alignment switch
        {
            TableColumnAlign.Center => JustificationValues.Center,
            TableColumnAlign.Right => JustificationValues.Right,
            _ => JustificationValues.Left
        };
        paragraphProperties.AppendChild(new Justification { Val = justification });
        paragraphProperties.AppendChild(new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto });

        paragraph.ParagraphProperties = paragraphProperties;

        wordCell.AppendChild(paragraph);

        // Render cell content
        if (tableCell.Count > 0)
        {
            foreach (var block in tableCell)
            {
                if (block is Markdig.Syntax.ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
                {
                    RenderInlineContent(renderer, paragraph, paragraphBlock, isHeader);
                }
            }
        }

        wordRow.AppendChild(wordCell);
    }

    private void RenderInlineContent(OpenXmlRenderer renderer, Paragraph paragraph, Markdig.Syntax.ParagraphBlock paragraphBlock, bool isBold)
    {
        if (paragraphBlock.Inline == null)
            return;

        var inline = paragraphBlock.Inline.FirstChild;
        while (inline != null)
        {
            if (inline is Markdig.Syntax.Inlines.LiteralInline literal)
            {
                var run = new Run();
                var runProps = new RunProperties();

                if (isBold)
                {
                    runProps.AppendChild(new Bold());
                }

                if (runProps.HasChildren)
                {
                    run.RunProperties = runProps;
                }

                run.AppendChild(new Text(literal.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve });
                paragraph.AppendChild(run);
            }
            else if (inline is Markdig.Syntax.Inlines.EmphasisInline emphasis)
            {
                var run = new Run();
                var runProps = new RunProperties();

                if (isBold)
                {
                    runProps.AppendChild(new Bold());
                }

                // Check for bold or italic
                if (emphasis.DelimiterCount == 2)
                {
                    runProps.AppendChild(new Bold());
                }
                else if (emphasis.DelimiterCount == 1)
                {
                    runProps.AppendChild(new Italic());
                }

                if (runProps.HasChildren)
                {
                    run.RunProperties = runProps;
                }

                // Recursively render emphasis content
                if (emphasis.FirstChild != null)
                {
                    var emphInline = emphasis.FirstChild;
                    while (emphInline != null)
                    {
                        if (emphInline is Markdig.Syntax.Inlines.LiteralInline emphLiteral)
                        {
                            run.AppendChild(new Text(emphLiteral.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve });
                        }
                        emphInline = emphInline.NextSibling;
                    }
                }

                paragraph.AppendChild(run);
            }
            else if (inline is Markdig.Syntax.Inlines.CodeInline code)
            {
                var run = new Run();
                var runProps = new RunProperties();

                if (isBold)
                {
                    runProps.AppendChild(new Bold());
                }

                runProps.AppendChild(new RunFonts { Ascii = "Consolas", HighAnsi = "Consolas" });
                run.RunProperties = runProps;
                run.AppendChild(new Text(code.Content) { Space = SpaceProcessingModeValues.Preserve });
                paragraph.AppendChild(run);
            }
            else if (inline is Markdig.Syntax.Inlines.LineBreakInline)
            {
                var run = new Run();
                run.AppendChild(new Break());
                paragraph.AppendChild(run);
            }
            // For any other inline types, just skip them for now
            // (LinkInline would need special handling if needed in tables)

            inline = inline.NextSibling;
        }
    }
}
