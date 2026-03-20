using FluentAssertions;
using MarkMyWord.Configuration;
using MarkMyWord.OfficeTalk;

namespace MarkMyWord.Tests;

public class OfficeTalkCompilerTests
{
    [Fact]
    public void Compile_EmptyDocument_ProducesHeader()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("");

        result.Should().Contain("OFFICETALK/1.0");
        result.Should().Contain("DOCTYPE word");
    }

    [Fact]
    public void Compile_SingleHeading_ProducesSetAndStyle()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("# Hello World");

        result.Should().Contain("AT body/paragraph[1]");
        result.Should().Contain("SET \"Hello World\"");
        result.Should().Contain("STYLE \"Heading1\"");
    }

    [Fact]
    public void Compile_HeadingLevels_ProducesCorrectStyle()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("## Section\n\n### Subsection");

        result.Should().Contain("STYLE \"Heading2\"");
        result.Should().Contain("STYLE \"Heading3\"");
    }

    [Fact]
    public void Compile_Paragraph_ProducesSetContent()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("Hello world");

        result.Should().Contain("AT body/paragraph[1]");
        result.Should().Contain("SET \"Hello world\"");
    }

    [Fact]
    public void Compile_BoldText_ProducesSetRunsWithBold()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("This is **bold** text");

        result.Should().Contain("SET RUNS");
        result.Should().Contain("RUN \"bold\" bold=true");
    }

    [Fact]
    public void Compile_ItalicText_ProducesSetRunsWithItalic()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("This is *italic* text");

        result.Should().Contain("SET RUNS");
        result.Should().Contain("RUN \"italic\" italic=true");
    }

    [Fact]
    public void Compile_InlineCode_ProducesSetRunsWithCodeFormatting()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("Run `npm start` now");

        result.Should().Contain("SET RUNS");
        result.Should().Contain("RUN \"npm start\" font-name=\"Consolas\"");
        result.Should().Contain("background-color=#F5F5F5");
    }

    [Fact]
    public void Compile_Link_ProducesSetRunsWithHref()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("Visit [our site](https://example.com) today");

        result.Should().Contain("SET RUNS");
        result.Should().Contain("href=\"https://example.com\"");
    }

    [Fact]
    public void Compile_ThematicBreak_ProducesBorderFormat()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("Text\n\n---\n\nMore text");

        result.Should().Contain("FORMAT border-bottom=single");
    }

    [Fact]
    public void Compile_UnorderedList_ProducesInsertList()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("- First\n- Second\n- Third");

        result.Should().Contain("INSERT LIST AFTER unordered");
        result.Should().Contain("ITEM \"First\"");
        result.Should().Contain("ITEM \"Second\"");
        result.Should().Contain("ITEM \"Third\"");
    }

    [Fact]
    public void Compile_OrderedList_ProducesInsertListOrdered()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("1. First\n2. Second\n3. Third");

        result.Should().Contain("INSERT LIST AFTER ordered");
        result.Should().Contain("ITEM \"First\"");
    }

    [Fact]
    public void Compile_Table_ProducesInsertTableAndSetCells()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("| Name | Age |\n|------|-----|\n| Alice | 30 |");

        result.Should().Contain("INSERT TABLE AFTER rows=");
        result.Should().Contain("SET CELLS");
    }

    [Fact]
    public void Compile_BlockQuote_ProducesQuoteStyle()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("> This is a quote");

        result.Should().Contain("STYLE \"Quote\"");
    }

    [Fact]
    public void Compile_CodeBlock_ProducesCodeFormatting()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("```\nvar x = 1;\n```");

        result.Should().Contain("SET RUNS");
        result.Should().Contain("font-name=\"Consolas\"");
    }

    [Fact]
    public void Compile_Image_ProducesInsertImage()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("![Alt text](image.png)");

        result.Should().Contain("INSERT IMAGE AFTER \"image.png\"");
        result.Should().Contain("alt=\"Alt text\"");
    }

    [Fact]
    public void Compile_DocumentProperties_EmittedInHeader()
    {
        var compiler = new OfficeTalkCompiler(new ConversionOptions
        {
            DocumentTitle = "My Doc",
            Author = "Test"
        });
        var result = compiler.Compile("# Title");

        result.Should().Contain("PROPERTY title=\"My Doc\"");
        result.Should().Contain("PROPERTY author=\"Test\"");
    }

    [Fact]
    public void Compile_MultipleParagraphs_IncrementsParagraphIndex()
    {
        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile("First paragraph\n\nSecond paragraph\n\nThird paragraph");

        result.Should().Contain("AT body/paragraph[1]");
        result.Should().Contain("INSERT AFTER");
        // Should have multiple paragraph references showing sequential building
        var lines = result.Split('\n');
        lines.Count(l => l.Contains("AT body/paragraph")).Should().BeGreaterOrEqualTo(3);
    }

    [Fact]
    public void Compile_ComplexDocument_ProducesValidOtk()
    {
        var markdown = """
            # Getting Started

            Welcome to the project.

            ## Features

            - Fast compilation
            - Easy to use
            - **Extensible**

            ---

            > Important: read the docs first.

            ```json
            { "key": "value" }
            ```
            """;

        var compiler = new OfficeTalkCompiler();
        var result = compiler.Compile(markdown);

        result.Should().StartWith("OFFICETALK/1.0");
        result.Should().Contain("STYLE \"Heading1\"");
        result.Should().Contain("STYLE \"Heading2\"");
        result.Should().Contain("INSERT LIST AFTER unordered");
        result.Should().Contain("FORMAT border-bottom=single");
        result.Should().Contain("STYLE \"Quote\"");
    }
}
