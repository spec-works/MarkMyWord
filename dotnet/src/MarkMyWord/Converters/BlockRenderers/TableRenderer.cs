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
/// </summary>
public class TableRenderer : OpenXmlObjectRenderer<MarkdigTable>
{
    protected override void Write(OpenXmlRenderer renderer, MarkdigTable table)
    {
        var wordTable = new WordTable();

        // Set table properties
        var tableProperties = new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
            ),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }
        );
        wordTable.AppendChild(tableProperties);

        // Process table rows
        foreach (var row in table)
        {
            if (row is MarkdigTableRow tableRow)
            {
                WriteTableRow(renderer, wordTable, tableRow);
            }
        }

        // Add table to document body
        renderer.DocumentBuilder.Body.AppendChild(wordTable);
    }

    private void WriteTableRow(OpenXmlRenderer renderer, WordTable wordTable, MarkdigTableRow tableRow)
    {
        var wordRow = new WordTableRow();

        // Check if this is a header row
        bool isHeaderRow = tableRow.IsHeader;

        foreach (var cell in tableRow)
        {
            if (cell is MarkdigTableCell tableCell)
            {
                WriteTableCell(renderer, wordRow, tableCell, isHeaderRow);
            }
        }

        wordTable.AppendChild(wordRow);
    }

    private void WriteTableCell(OpenXmlRenderer renderer, WordTableRow wordRow, MarkdigTableCell tableCell, bool isHeader)
    {
        var wordCell = new WordTableCell();

        // Set cell properties
        var cellProperties = new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Auto }
        );

        // Add shading for header cells
        if (isHeader)
        {
            cellProperties.AppendChild(new Shading
            {
                Val = ShadingPatternValues.Clear,
                Fill = "D3D3D3" // Light gray background for headers
            });
        }

        wordCell.AppendChild(cellProperties);

        // Create a paragraph for the cell content
        var paragraph = new Paragraph();

        // Apply bold formatting for header cells
        if (isHeader)
        {
            var paragraphProperties = new ParagraphProperties();
            paragraph.ParagraphProperties = paragraphProperties;
        }

        // Add the paragraph to the cell
        wordCell.AppendChild(paragraph);

        // Render cell content
        if (tableCell.Count > 0)
        {
            foreach (var block in tableCell)
            {
                // For simple cases, just render inline content
                if (block is Markdig.Syntax.ParagraphBlock paragraphBlock && paragraphBlock.Inline != null)
                {
                    // Render inline elements directly to the cell's paragraph
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
