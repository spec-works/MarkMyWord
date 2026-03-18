using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using MarkMyWord.Configuration;

namespace MarkMyWord.Tests;

/// <summary>
/// Compares output from the direct OpenXML pipeline vs the OTK pipeline
/// to identify structural and content differences.
/// </summary>
public class PipelineParityTests
{
    #region Helpers

    /// <summary>
    /// Extracts paragraph info (text + style) from a .docx byte array.
    /// </summary>
    private static List<(string Text, string Style)> ExtractParagraphs(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        return body.Elements<Paragraph>()
            .Select(p =>
            {
                var text = string.Join("", p.Descendants<Text>().Select(t => t.Text));
                var style = p.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "Normal";
                return (Text: text, Style: style);
            })
            .Where(p => !string.IsNullOrEmpty(p.Text)) // skip empty structural paragraphs
            .ToList();
    }

    /// <summary>
    /// Converts markdown through both pipelines and returns the paragraph lists.
    /// </summary>
    private static (List<(string Text, string Style)> Direct, List<(string Text, string Style)> Otk)
        ConvertBothPipelines(string markdown, ConversionOptions? options = null)
    {
        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown, options);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown, options);

        var direct = ExtractParagraphs(directBytes);
        var otk = ExtractParagraphs(otkBytes);

        return (direct, otk);
    }

    /// <summary>
    /// Extracts table data from a .docx byte array.
    /// Returns list of tables, each containing rows of cell text.
    /// </summary>
    private static List<List<List<string>>> ExtractTables(byte[] docxBytes)
    {
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart!.Document.Body!;

        return body.Descendants<Table>()
            .Select(table => table.Elements<TableRow>()
                .Select(row => row.Elements<TableCell>()
                    .Select(cell => string.Join("", cell.Descendants<Text>().Select(t => t.Text)))
                    .ToList())
                .ToList())
            .ToList();
    }

    #endregion

    #region Headings

    [Theory]
    [InlineData("# Heading 1", "Heading1")]
    [InlineData("## Heading 2", "Heading2")]
    [InlineData("### Heading 3", "Heading3")]
    [InlineData("#### Heading 4", "Heading4")]
    [InlineData("##### Heading 5", "Heading5")]
    [InlineData("###### Heading 6", "Heading6")]
    public void Heading_TextAndStyleMatch(string markdown, string expectedStyle)
    {
        var (direct, otk) = ConvertBothPipelines(markdown);

        var directHeading = direct.First(p => p.Style.Contains("Heading"));
        var otkHeading = otk.First(p => p.Style.Contains("Heading"));

        otkHeading.Text.Should().Be(directHeading.Text, "heading text should match");
        otkHeading.Style.Should().Be(directHeading.Style, "heading style should match");
    }

    [Fact]
    public void MultipleHeadings_SameCountAndOrder()
    {
        var markdown = "# Title\n\n## Section A\n\n## Section B\n\n### Subsection";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var directHeadings = direct.Where(p => p.Style.Contains("Heading")).ToList();
        var otkHeadings = otk.Where(p => p.Style.Contains("Heading")).ToList();

        otkHeadings.Should().HaveCount(directHeadings.Count, "heading count should match");

        for (int i = 0; i < directHeadings.Count; i++)
        {
            otkHeadings[i].Text.Should().Be(directHeadings[i].Text, $"heading {i} text");
            otkHeadings[i].Style.Should().Be(directHeadings[i].Style, $"heading {i} style");
        }
    }

    #endregion

    #region Paragraphs

    [Fact]
    public void SimpleParagraph_TextMatches()
    {
        var markdown = "This is a simple paragraph.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        otk.Should().ContainSingle(p => p.Text == "This is a simple paragraph.");
        direct.Should().ContainSingle(p => p.Text == "This is a simple paragraph.");
    }

    [Fact]
    public void MultipleParagraphs_SameCountAndText()
    {
        var markdown = "First paragraph.\n\nSecond paragraph.\n\nThird paragraph.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var directParas = direct.Where(p => p.Style == "Normal").ToList();
        var otkParas = otk.Where(p => p.Style == "Normal").ToList();

        otkParas.Should().HaveCount(directParas.Count, "paragraph count should match");

        for (int i = 0; i < directParas.Count; i++)
        {
            otkParas[i].Text.Should().Be(directParas[i].Text, $"paragraph {i} text");
        }
    }

    [Fact]
    public void HeadingFollowedByParagraph_BothPresent()
    {
        var markdown = "# Title\n\nBody text goes here.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        otk.Should().Contain(p => p.Style.Contains("Heading") && p.Text == "Title");
        otk.Should().Contain(p => p.Text == "Body text goes here.");

        direct.Should().Contain(p => p.Style.Contains("Heading") && p.Text == "Title");
        direct.Should().Contain(p => p.Text == "Body text goes here.");
    }

    #endregion

    #region Inline Formatting

    [Fact]
    public void BoldText_PresentInBothPipelines()
    {
        var markdown = "This has **bold** text.";

        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        // Both should contain the full text
        var directText = string.Join("", ExtractParagraphs(directBytes).Select(p => p.Text));
        var otkText = string.Join("", ExtractParagraphs(otkBytes).Select(p => p.Text));

        otkText.Should().Contain("bold");
        directText.Should().Contain("bold");

        // Both should have a bold run
        using var dms = new MemoryStream(directBytes);
        using var ddoc = WordprocessingDocument.Open(dms, false);
        var directBold = ddoc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Any(r => r.RunProperties?.Bold != null &&
                      r.Descendants<Text>().Any(t => t.Text.Contains("bold")));

        using var oms = new MemoryStream(otkBytes);
        using var odoc = WordprocessingDocument.Open(oms, false);
        var otkBold = odoc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Any(r => r.RunProperties?.Bold != null &&
                      r.Descendants<Text>().Any(t => t.Text.Contains("bold")));

        directBold.Should().BeTrue("direct pipeline should produce bold run");
        otkBold.Should().BeTrue("OTK pipeline should produce bold run");
    }

    [Fact]
    public void ItalicText_PresentInBothPipelines()
    {
        var markdown = "This has *italic* text.";

        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        using var dms = new MemoryStream(directBytes);
        using var ddoc = WordprocessingDocument.Open(dms, false);
        var directItalic = ddoc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Any(r => r.RunProperties?.Italic != null &&
                      r.Descendants<Text>().Any(t => t.Text.Contains("italic")));

        using var oms = new MemoryStream(otkBytes);
        using var odoc = WordprocessingDocument.Open(oms, false);
        var otkItalic = odoc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Any(r => r.RunProperties?.Italic != null &&
                      r.Descendants<Text>().Any(t => t.Text.Contains("italic")));

        directItalic.Should().BeTrue("direct pipeline should produce italic run");
        otkItalic.Should().BeTrue("OTK pipeline should produce italic run");
    }

    [Fact]
    public void InlineCode_UsesMonospaceInBoth()
    {
        var markdown = "Use `npm start` to run.";

        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        using var dms = new MemoryStream(directBytes);
        using var ddoc = WordprocessingDocument.Open(dms, false);
        var directCode = ddoc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Where(r => r.Descendants<Text>().Any(t => t.Text.Contains("npm start")))
            .Any(r => r.RunProperties?.RunFonts?.Ascii?.Value == "Consolas");

        using var oms = new MemoryStream(otkBytes);
        using var odoc = WordprocessingDocument.Open(oms, false);
        var otkCode = odoc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Where(r => r.Descendants<Text>().Any(t => t.Text.Contains("npm start")))
            .Any(r => r.RunProperties?.RunFonts?.Ascii?.Value == "Consolas");

        directCode.Should().BeTrue("direct pipeline should use Consolas for inline code");
        otkCode.Should().BeTrue("OTK pipeline should use Consolas for inline code");
    }

    [Fact]
    public void MixedFormatting_AllTextPresent()
    {
        var markdown = "Normal **bold** *italic* `code` text.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var directText = string.Join("", direct.Select(p => p.Text));
        var otkText = string.Join("", otk.Select(p => p.Text));

        // Both should contain all the text fragments
        foreach (var expected in new[] { "Normal", "bold", "italic", "code", "text." })
        {
            directText.Should().Contain(expected, $"direct should contain '{expected}'");
            otkText.Should().Contain(expected, $"OTK should contain '{expected}'");
        }
    }

    #endregion

    #region Code Blocks

    [Fact]
    public void FencedCodeBlock_SameLineCount()
    {
        var markdown = "# Title\n\n```json\n{\n  \"key\": \"value\"\n}\n```";

        var (direct, otk) = ConvertBothPipelines(markdown);

        // Filter to code-like paragraphs (non-heading, non-empty)
        var directCode = direct.Where(p => p.Style != "Heading1" && !string.IsNullOrWhiteSpace(p.Text)).ToList();
        var otkCode = otk.Where(p => p.Style != "Heading1" && !string.IsNullOrWhiteSpace(p.Text)).ToList();

        otkCode.Should().HaveCount(directCode.Count, "code block line count should match");
    }

    [Fact]
    public void CodeBlock_TextContentMatches()
    {
        var markdown = "```\nline one\nline two\nline three\n```";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var directTexts = direct.Select(p => p.Text.Trim()).Where(t => t.Length > 0).ToList();
        var otkTexts = otk.Select(p => p.Text.Trim()).Where(t => t.Length > 0).ToList();

        otkTexts.Should().BeEquivalentTo(directTexts, "code block text content should match");
    }

    [Fact]
    public void SyntaxHighlightedCode_UsesConsolas()
    {
        var markdown = "```json\n{\"key\": \"value\"}\n```";

        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        using var ms = new MemoryStream(otkBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        var codeRuns = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>()
            .Where(r => r.RunProperties?.RunFonts?.Ascii?.Value == "Consolas")
            .ToList();

        codeRuns.Should().NotBeEmpty("OTK pipeline should produce Consolas runs for syntax-highlighted code");
    }

    #endregion

    #region Tables

    [Fact]
    public void SimpleTable_SameDimensions()
    {
        var markdown = "| A | B | C |\n|---|---|---|\n| 1 | 2 | 3 |\n| 4 | 5 | 6 |";

        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        var directTables = ExtractTables(directBytes);
        var otkTables = ExtractTables(otkBytes);

        directTables.Should().HaveCount(1, "direct should produce one table");
        otkTables.Should().HaveCount(1, "OTK should produce one table");

        var dTable = directTables[0];
        var oTable = otkTables[0];

        oTable.Should().HaveCount(dTable.Count, "row count should match");
        for (int r = 0; r < dTable.Count; r++)
        {
            oTable[r].Should().HaveCount(dTable[r].Count, $"column count in row {r} should match");
        }
    }

    [Fact]
    public void SimpleTable_CellTextMatches()
    {
        var markdown = "| Name | Age |\n|------|-----|\n| Alice | 30 |\n| Bob | 25 |";

        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        var directTables = ExtractTables(directBytes);
        var otkTables = ExtractTables(otkBytes);

        var dTable = directTables[0];
        var oTable = otkTables[0];

        for (int r = 0; r < dTable.Count; r++)
        {
            for (int c = 0; c < dTable[r].Count; c++)
            {
                oTable[r][c].Should().Be(dTable[r][c], $"cell [{r},{c}] text should match");
            }
        }
    }

    #endregion

    #region Mixed Documents

    [Fact]
    public void HeadingParagraphCode_StructureMatches()
    {
        var markdown = "# Title\n\nSome intro text.\n\n```\ncode here\n```\n\n## Next Section\n\nMore text.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        // Both should contain the same headings
        var directHeadings = direct.Where(p => p.Style.Contains("Heading")).ToList();
        var otkHeadings = otk.Where(p => p.Style.Contains("Heading")).ToList();

        otkHeadings.Select(h => h.Text).Should().BeEquivalentTo(
            directHeadings.Select(h => h.Text),
            config => config.WithStrictOrdering(),
            "heading text and order should match");

        otkHeadings.Select(h => h.Style).Should().BeEquivalentTo(
            directHeadings.Select(h => h.Style),
            config => config.WithStrictOrdering(),
            "heading styles should match");

        // Both should contain the body text
        otk.Should().Contain(p => p.Text == "Some intro text.");
        otk.Should().Contain(p => p.Text == "More text.");
    }

    [Fact]
    public void HeadingsWithParagraphsAndTable_AllElementsPresent()
    {
        var markdown = """
            # Report

            Introduction paragraph.

            ## Data

            | Item | Value |
            |------|-------|
            | A    | 100   |

            ## Conclusion

            Final thoughts.
            """;

        var (direct, otk) = ConvertBothPipelines(markdown);

        // Headings
        otk.Should().Contain(p => p.Text == "Report" && p.Style == "Heading1");
        otk.Should().Contain(p => p.Text == "Data" && p.Style == "Heading2");
        otk.Should().Contain(p => p.Text == "Conclusion" && p.Style == "Heading2");

        // Paragraphs
        otk.Should().Contain(p => p.Text == "Introduction paragraph.");
        otk.Should().Contain(p => p.Text == "Final thoughts.");

        // Table
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);
        var otkTables = ExtractTables(otkBytes);
        otkTables.Should().HaveCount(1);
    }

    [Fact]
    public void LargeDocument_ParagraphCountsClose()
    {
        var markdown = """
            # Main Title

            First paragraph of the document.

            ## Section One

            Content for section one. It has multiple sentences. This is the third.

            ### Subsection

            Subsection content here.

            ## Section Two

            Another section with different content.

            ```
            some code
            more code
            ```

            Final paragraph after code.
            """;

        var (direct, otk) = ConvertBothPipelines(markdown);

        // Total non-empty elements should be close
        var directCount = direct.Count;
        var otkCount = otk.Count;

        // Allow small variance (e.g., trailing empty paragraphs)
        otkCount.Should().BeInRange(directCount - 2, directCount + 2,
            $"total element count should be close (direct={directCount}, otk={otkCount})");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EmptyCodeBlock_BothHandleGracefully()
    {
        var markdown = "# Title\n\n```\n\n```";

        // Both should produce valid documents without throwing
        var directBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);

        directBytes.Length.Should().BeGreaterThan(0);
        otkBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void OnlyHeadings_AllStylesCorrect()
    {
        var markdown = "# H1\n\n## H2\n\n### H3";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var otkStyles = otk.Select(p => p.Style).ToList();
        var directStyles = direct.Select(p => p.Style).ToList();

        otkStyles.Should().BeEquivalentTo(directStyles, config => config.WithStrictOrdering(),
            "heading style sequence should match");
    }

    [Fact]
    public void SpecialCharacters_PreservedInBoth()
    {
        var markdown = "Text with \"quotes\" and <angles> and &amp; entities.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var directText = direct.First().Text;
        var otkText = otk.First().Text;

        otkText.Should().Be(directText, "special characters should be preserved identically");
    }

    [Fact]
    public void ConsecutiveCodeBlocks_SameOutput()
    {
        var markdown = "```\nblock one\n```\n\n```\nblock two\n```";

        var (direct, otk) = ConvertBothPipelines(markdown);

        var directTexts = direct.Select(p => p.Text).ToList();
        var otkTexts = otk.Select(p => p.Text).ToList();

        otkTexts.Should().BeEquivalentTo(directTexts, config => config.WithStrictOrdering(),
            "consecutive code blocks should produce same text");
    }

    [Fact]
    public void HeadingAfterCodeBlock_CorrectSequence()
    {
        var markdown = "```\nsome code\n```\n\n## After Code\n\nText here.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        // OTK should have the heading after the code
        var otkHeading = otk.FirstOrDefault(p => p.Text == "After Code");
        otkHeading.Should().NotBeNull();
        otkHeading!.Style.Should().Be("Heading2");

        // And the body text after that
        otk.Should().Contain(p => p.Text == "Text here.");
    }

    #endregion

    #region Blockquotes

    [Fact]
    public void Blockquote_TextPreserved()
    {
        var markdown = "> This is a quoted paragraph.";

        var (direct, otk) = ConvertBothPipelines(markdown);

        otk.Should().Contain(p => p.Text == "This is a quoted paragraph.");
        direct.Should().Contain(p => p.Text == "This is a quoted paragraph.");
    }

    #endregion

    #region Full Document Roundtrip

    [Fact]
    public void ReadmeStyle_AllSectionsPresent()
    {
        var markdown = """
            # My Project

            A brief description of the project.

            ## Installation

            ```
            npm install my-project
            ```

            ## Usage

            Import the module and call the main function:

            ```
            import { run } from 'my-project';
            run();
            ```

            ## API

            | Method | Description |
            |--------|-------------|
            | run()  | Starts it   |
            | stop() | Stops it    |

            ## License

            MIT
            """;

        var (direct, otk) = ConvertBothPipelines(markdown);

        // All headings present with correct styles
        var expectedHeadings = new[]
        {
            ("My Project", "Heading1"),
            ("Installation", "Heading2"),
            ("Usage", "Heading2"),
            ("API", "Heading2"),
            ("License", "Heading2"),
        };

        foreach (var (text, style) in expectedHeadings)
        {
            otk.Should().Contain(p => p.Text == text && p.Style == style,
                $"OTK should have heading '{text}' with style {style}");
        }

        // Key paragraphs present
        otk.Should().Contain(p => p.Text == "A brief description of the project.");
        otk.Should().Contain(p => p.Text.Contains("Import the module"));
        otk.Should().Contain(p => p.Text == "MIT");

        // Table present
        var otkBytes = MarkdownConverter.ConvertToDocxViaOtkBytes(markdown);
        var tables = ExtractTables(otkBytes);
        tables.Should().HaveCount(1);
        tables[0].Should().HaveCount(3); // header + 2 data rows
    }

    #endregion
}
