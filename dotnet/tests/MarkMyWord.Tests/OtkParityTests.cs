using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using MarkMyWord.Configuration;

namespace MarkMyWord.Tests;

/// <summary>
/// Parity tests that verify OTK compilation produces correct OfficeTalk
/// for the same markdown inputs used in the direct OpenXML conversion path.
/// These tests ensure the OTK compiler covers all features that the existing
/// renderer handles, validating the migration from direct OpenXML to OfficeTalk.
/// </summary>
public class OtkParityTests
{
    #region Library API Tests

    [Fact]
    public void CompileToOtk_ReturnsValidOfficeTalkHeader()
    {
        var otk = MarkdownConverter.CompileToOtk("# Test");

        otk.Should().StartWith("OFFICETALK/1.0");
        otk.Should().Contain("DOCTYPE word");
    }

    [Fact]
    public void CompileToOtk_ThrowsOnNullInput()
    {
        var act = () => MarkdownConverter.CompileToOtk(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompileToOtk_ThrowsOnEmptyInput()
    {
        var act = () => MarkdownConverter.CompileToOtk("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompileToOtkFile_WritesToDisk()
    {
        var tempFile = Path.GetTempFileName() + ".otk";
        try
        {
            MarkdownConverter.CompileToOtkFile("# Hello", tempFile);

            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            content.Should().Contain("OFFICETALK/1.0");
            content.Should().Contain("SET \"Hello\"");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task CompileToOtkFileAsync_WritesToDisk()
    {
        var tempFile = Path.GetTempFileName() + ".otk";
        try
        {
            await MarkdownConverter.CompileToOtkFileAsync("# Hello", tempFile);

            File.Exists(tempFile).Should().BeTrue();
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("OFFICETALK/1.0");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    #endregion

    #region Heading Parity

    [Theory]
    [InlineData("# Heading 1", "Heading1")]
    [InlineData("## Heading 2", "Heading2")]
    [InlineData("### Heading 3", "Heading3")]
    [InlineData("#### Heading 4", "Heading4")]
    [InlineData("##### Heading 5", "Heading5")]
    [InlineData("###### Heading 6", "Heading6")]
    public void Headings_AllLevels_MapToCorrectStyles(string markdown, string expectedStyle)
    {
        var otk = MarkdownConverter.CompileToOtk(markdown);

        otk.Should().Contain($"STYLE \"{expectedStyle}\"");
    }

    [Fact]
    public void Headings_WithInlineFormatting_ProducesSetRuns()
    {
        var otk = MarkdownConverter.CompileToOtk("# This is **bold** heading");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("RUN \"bold\" bold=true");
        otk.Should().Contain("STYLE \"Heading1\"");
    }

    #endregion

    #region Paragraph and Inline Formatting Parity

    [Fact]
    public void PlainParagraph_ProducesSimpleSet()
    {
        var otk = MarkdownConverter.CompileToOtk("Just some text.");

        otk.Should().Contain("SET \"Just some text.\"");
        otk.Should().NotContain("SET RUNS");
    }

    [Fact]
    public void Bold_ProducesSetRunsWithBold()
    {
        var otk = MarkdownConverter.CompileToOtk("Text with **bold** word.");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("RUN \"bold\" bold=true");
    }

    [Fact]
    public void Italic_ProducesSetRunsWithItalic()
    {
        var otk = MarkdownConverter.CompileToOtk("Text with *italic* word.");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("RUN \"italic\" italic=true");
    }

    [Fact]
    public void BoldItalic_ProducesBothAttributes()
    {
        var otk = MarkdownConverter.CompileToOtk("Text with ***bold italic*** word.");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("bold=true");
        otk.Should().Contain("italic=true");
    }

    [Fact]
    public void InlineCode_ProducesCodeFontAndBackground()
    {
        var otk = MarkdownConverter.CompileToOtk("Run `npm install` first.");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("font-name=\"Consolas\"");
        otk.Should().Contain("background-color=");
    }

    [Fact]
    public void Hyperlink_ProducesHrefAndLinkStyle()
    {
        var otk = MarkdownConverter.CompileToOtk("Visit [GitHub](https://github.com) today.");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("href=\"https://github.com\"");
        otk.Should().Contain("color=#0563C1");
        otk.Should().Contain("underline=single");
    }

    [Fact]
    public void MixedFormatting_AllRunsPresent()
    {
        var otk = MarkdownConverter.CompileToOtk(
            "Normal **bold** *italic* `code` [link](http://example.com).");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("RUN \"bold\" bold=true");
        otk.Should().Contain("RUN \"italic\" italic=true");
        otk.Should().Contain("font-name=\"Consolas\"");
        otk.Should().Contain("href=\"http://example.com\"");
    }

    #endregion

    #region Code Block Parity

    [Fact]
    public void FencedCodeBlock_ProducesCodeFont()
    {
        var otk = MarkdownConverter.CompileToOtk("```\nvar x = 1;\nvar y = 2;\n```");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("font-name=\"Consolas\"");
        // Should produce separate paragraphs for each line
        var paraCount = otk.Split('\n').Count(l => l.Contains("AT body/paragraph"));
        paraCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public void FencedCodeBlock_WithLanguage_ProducesSyntaxHighlighting()
    {
        var otk = MarkdownConverter.CompileToOtk("```json\n{\"key\": \"value\"}\n```");

        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("font-name=\"Consolas\"");
        // JSON highlighting should produce colored runs
        otk.Should().Contain("color=#");
    }

    [Fact]
    public void CodeBlock_PreservesLineStructure()
    {
        var code = "```\nline1\nline2\nline3\n```";
        var otk = MarkdownConverter.CompileToOtk(code);

        otk.Should().Contain("\"line1\"");
        otk.Should().Contain("\"line2\"");
        otk.Should().Contain("\"line3\"");
    }

    #endregion

    #region List Parity

    [Fact]
    public void UnorderedList_ProducesInsertListUnordered()
    {
        var otk = MarkdownConverter.CompileToOtk("- Alpha\n- Beta\n- Gamma");

        otk.Should().Contain("INSERT LIST AFTER unordered");
        otk.Should().Contain("ITEM \"Alpha\"");
        otk.Should().Contain("ITEM \"Beta\"");
        otk.Should().Contain("ITEM \"Gamma\"");
    }

    [Fact]
    public void OrderedList_ProducesInsertListOrdered()
    {
        var otk = MarkdownConverter.CompileToOtk("1. First\n2. Second\n3. Third");

        otk.Should().Contain("INSERT LIST AFTER ordered");
        otk.Should().Contain("ITEM \"First\"");
        otk.Should().Contain("ITEM \"Second\"");
        otk.Should().Contain("ITEM \"Third\"");
    }

    [Fact]
    public void NestedList_ProducesNestedItems()
    {
        var otk = MarkdownConverter.CompileToOtk("- Parent\n  - Child\n- Sibling");

        otk.Should().Contain("INSERT LIST AFTER unordered");
        otk.Should().Contain("ITEM \"Parent\"");
        otk.Should().Contain("ITEM \"Child\" nested");
        otk.Should().Contain("ITEM \"Sibling\"");
    }

    #endregion

    #region Table Parity

    [Fact]
    public void Table_ProducesInsertTableAndSetCells()
    {
        var markdown = "| Name | Age |\n|------|-----|\n| Alice | 30 |\n| Bob | 25 |";
        var otk = MarkdownConverter.CompileToOtk(markdown);

        otk.Should().Contain("INSERT TABLE AFTER rows=3, columns=2");
        otk.Should().Contain("SET CELLS \"Name\", \"Age\"");
        otk.Should().Contain("SET CELLS \"Alice\", \"30\"");
        otk.Should().Contain("SET CELLS \"Bob\", \"25\"");
    }

    [Fact]
    public void Table_HeaderRow_GetsFormatting()
    {
        var markdown = "| H1 | H2 |\n|----|----|\n| A | B |";
        var otk = MarkdownConverter.CompileToOtk(markdown);

        otk.Should().Contain("FORMAT bold=true, fill-color=#D3D3D3");
    }

    #endregion

    #region Block Quote Parity

    [Fact]
    public void BlockQuote_ProducesQuoteStyle()
    {
        var otk = MarkdownConverter.CompileToOtk("> This is a quote");

        otk.Should().Contain("STYLE \"Quote\"");
        otk.Should().Contain("SET \"This is a quote\"");
    }

    [Fact]
    public void BlockQuote_MultiParagraph_ProducesMultipleQuoteStyles()
    {
        var otk = MarkdownConverter.CompileToOtk("> First paragraph\n>\n> Second paragraph");

        var quoteCount = otk.Split('\n').Count(l => l.Contains("STYLE \"Quote\""));
        quoteCount.Should().BeGreaterOrEqualTo(2);
    }

    #endregion

    #region Thematic Break Parity

    [Fact]
    public void ThematicBreak_ProducesBorderFormat()
    {
        var otk = MarkdownConverter.CompileToOtk("---");

        otk.Should().Contain("FORMAT border-bottom=single");
        otk.Should().Contain("border-color=#000000");
    }

    [Theory]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("___")]
    public void ThematicBreak_AllSyntaxVariants_ProduceSameOutput(string syntax)
    {
        var otk = MarkdownConverter.CompileToOtk(syntax);

        otk.Should().Contain("FORMAT border-bottom=single");
    }

    #endregion

    #region Image Parity

    [Fact]
    public void Image_ProducesInsertImage()
    {
        var otk = MarkdownConverter.CompileToOtk("![Logo](logo.png)");

        otk.Should().Contain("INSERT IMAGE AFTER \"logo.png\"");
        otk.Should().Contain("alt=\"Logo\"");
    }

    [Fact]
    public void Image_WithUrl_ProducesInsertImage()
    {
        var otk = MarkdownConverter.CompileToOtk("![Photo](https://example.com/photo.jpg)");

        otk.Should().Contain("INSERT IMAGE AFTER \"https://example.com/photo.jpg\"");
        otk.Should().Contain("alt=\"Photo\"");
    }

    #endregion

    #region Document Properties Parity

    [Fact]
    public void DocumentProperties_EmittedInHeader()
    {
        var options = new ConversionOptions
        {
            DocumentTitle = "Test Document",
            Author = "Test Author",
            Subject = "Test Subject"
        };
        var otk = MarkdownConverter.CompileToOtk("# Hello", options);

        otk.Should().Contain("PROPERTY title=\"Test Document\"");
        otk.Should().Contain("PROPERTY author=\"Test Author\"");
        otk.Should().Contain("PROPERTY subject=\"Test Subject\"");
    }

    #endregion

    #region Sequential Construction Parity

    [Fact]
    public void MultiBlock_FirstBlockUsesExistingParagraph()
    {
        var otk = MarkdownConverter.CompileToOtk("# Title\n\nParagraph");

        var lines = otk.Split('\n');
        // First AT should reference paragraph[1] without INSERT AFTER
        var firstAt = lines.First(l => l.Contains("AT body/paragraph"));
        firstAt.Should().Contain("paragraph[1]");
    }

    [Fact]
    public void MultiBlock_SubsequentBlocksInsertAfter()
    {
        var otk = MarkdownConverter.CompileToOtk("# Title\n\nParagraph");

        otk.Should().Contain("INSERT AFTER");
    }

    [Fact]
    public void MultiBlock_ParagraphIndicesIncrement()
    {
        var otk = MarkdownConverter.CompileToOtk("First\n\nSecond\n\nThird\n\nFourth");

        otk.Should().Contain("paragraph[1]");
        otk.Should().Contain("paragraph[2]");
        otk.Should().Contain("paragraph[3]");
    }

    #endregion

    #region Full Document Parity

    [Fact]
    public void CompleteDocument_AllElementsCovered()
    {
        var markdown = """
            # Project README

            Welcome to the project. This has **bold** and *italic* text.

            ## Installation

            Run `npm install` to get started:

            ```bash
            npm install
            npm start
            ```

            ## Features

            - Fast compilation
            - Easy to use
            - **Extensible** API

            | Feature | Status |
            |---------|--------|
            | Core | ✓ |
            | Plugins | ✓ |

            ---

            > **Note:** Read the docs before using.

            ![Logo](assets/logo.png)

            1. Clone the repo
            2. Install dependencies
            3. Run tests
            """;

        var otk = MarkdownConverter.CompileToOtk(markdown);

        // Verify all element types appear
        otk.Should().Contain("OFFICETALK/1.0");
        otk.Should().Contain("STYLE \"Heading1\"");
        otk.Should().Contain("STYLE \"Heading2\"");
        otk.Should().Contain("SET RUNS");                     // inline formatting
        otk.Should().Contain("bold=true");                    // bold
        otk.Should().Contain("italic=true");                  // italic
        otk.Should().Contain("font-name=\"Consolas\"");       // inline code + code blocks
        otk.Should().Contain("INSERT LIST AFTER unordered");  // unordered list
        otk.Should().Contain("INSERT LIST AFTER ordered");    // ordered list
        otk.Should().Contain("INSERT TABLE AFTER");           // table
        otk.Should().Contain("SET CELLS");                    // table cells
        otk.Should().Contain("FORMAT border-bottom=single");  // thematic break
        otk.Should().Contain("STYLE \"Quote\"");              // block quote
        otk.Should().Contain("INSERT IMAGE AFTER");           // image
    }

    [Fact]
    public void CompleteDocument_BothPathsProduceContent()
    {
        var markdown = "# Hello\n\nWorld with **bold** text.\n\n- Item 1\n- Item 2";

        // Direct OpenXML path should produce a valid document
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
        docxBytes.Should().NotBeNull();
        docxBytes.Length.Should().BeGreaterThan(0);

        // OTK path should produce valid OfficeTalk
        var otk = MarkdownConverter.CompileToOtk(markdown);
        otk.Should().Contain("OFFICETALK/1.0");
        otk.Should().Contain("SET RUNS");
        otk.Should().Contain("INSERT LIST AFTER");
    }

    #endregion

    #region Special Character Handling

    [Fact]
    public void SpecialCharacters_AreEscapedInOtk()
    {
        var otk = MarkdownConverter.CompileToOtk("Text with \"quotes\" and \\backslash.");

        otk.Should().Contain("\\\"quotes\\\"");
        otk.Should().Contain("\\\\backslash");
    }

    #endregion
}
