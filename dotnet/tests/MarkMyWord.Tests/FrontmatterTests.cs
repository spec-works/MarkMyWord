using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Markdig;
using MarkMyWord.Configuration;

namespace MarkMyWord.Tests;

public class FrontmatterTests
{
    private const string MarkdownWithFrontmatter = """
        ---
        title: Hidden Metadata Title
        author: Jane Doe
        date: 2026-05-22
        ---

        # Visible Heading

        Body text content.
        """;

    private const string MarkdownWithQuotedTitle = """
        ---
        title: "Quoted Document Title"
        ---

        # Visible Heading

        Body text content.
        """;

    private const string MarkdownWithoutFrontmatter = """
        # Visible Heading

        Body text content.
        """;

    [Fact]
    public void Frontmatter_ShouldNotAppearAsVisibleText()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;

        bodyText.Should().NotContain("Hidden Metadata Title");
        bodyText.Should().NotContain("Jane Doe");
        bodyText.Should().NotContain("2026-05-22");
        bodyText.Should().Contain("Visible Heading");
        bodyText.Should().Contain("Body text content.");
    }

    [Fact]
    public void FrontmatterTitle_ShouldBeUsedAsDocumentTitle()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        doc.PackageProperties.Title.Should().Be("Hidden Metadata Title");
    }

    [Fact]
    public void FrontmatterQuotedTitle_ShouldBeStripped()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithQuotedTitle);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        doc.PackageProperties.Title.Should().Be("Quoted Document Title");
    }

    [Fact]
    public void ExplicitDocumentTitle_ShouldTakePrecedenceOverFrontmatter()
    {
        var options = new ConversionOptions { DocumentTitle = "Explicit Title" };
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter, options);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        doc.PackageProperties.Title.Should().Be("Explicit Title");
    }

    [Fact]
    public void WithoutFrontmatter_ShouldStillWorkNormally()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithoutFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;
        bodyText.Should().Contain("Visible Heading");
        bodyText.Should().Contain("Body text content.");
    }

    [Fact]
    public void OtkPipeline_FrontmatterTitle_ShouldBeInDocumentProperties()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithFrontmatter);

        otk.Should().Contain("PROPERTY title=\"Hidden Metadata Title\"");
        otk.Should().NotContain("Jane Doe");
        otk.Should().NotContain("2026-05-22");
    }

    [Fact]
    public void OtkPipeline_FrontmatterShouldNotAppearAsContent()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithFrontmatter);

        // The frontmatter text should not appear in any SET or INSERT operations
        // Only the title should appear as a PROPERTY
        var lines = otk.Split('\n');
        var contentLines = lines.Where(l =>
            l.TrimStart().StartsWith("SET ") ||
            l.TrimStart().StartsWith("INSERT "));

        foreach (var line in contentLines)
        {
            line.Should().NotContain("Hidden Metadata Title");
            line.Should().NotContain("Jane Doe");
            line.Should().NotContain("2026-05-22");
        }
    }

    [Fact]
    public void OtkPipeline_WithoutFrontmatter_ShouldStillWorkNormally()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithoutFrontmatter);

        otk.Should().Contain("Visible Heading");
        otk.Should().Contain("Body text content.");
        otk.Should().NotContain("PROPERTY title=");
    }

    [Fact]
    public void FrontmatterExtractor_ShouldReturnNullForNoFrontmatter()
    {
        var pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var document = Markdig.Markdown.Parse("# Just a heading\n\nSome text.", pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().BeNull();
    }

    [Fact]
    public void FrontmatterExtractor_ShouldParseFlatYaml()
    {
        var pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var document = Markdig.Markdown.Parse("---\ntitle: My Title\nauthor: John\n---\n\nContent", pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().NotBeNull();
        result!.Title.Should().Be("My Title");
        result.Fields.Should().ContainKey("author");
        result.Fields["author"].Should().Be("John");
    }

    [Fact]
    public void FrontmatterExtractor_ShouldStripQuotesFromValues()
    {
        var pipeline = new Markdig.MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var document = Markdig.Markdown.Parse("---\ntitle: \"Quoted Title\"\nother: 'Single Quoted'\n---\n\nContent", pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Quoted Title");
        result.Fields["other"].Should().Be("Single Quoted");
    }
}
