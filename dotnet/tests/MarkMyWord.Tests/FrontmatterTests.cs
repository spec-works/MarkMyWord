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

    private const string MarkdownWithMultipleAuthors = """
        ---
        title: Team Document
        authors:
          - Alice Smith
          - Bob Jones
          - Carol White
        date: 2026-05-22
        ---

        ## Introduction

        Some content.
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

    private const string MarkdownWithCreatedDate = """
        ---
        title: Date Variants
        createdDate: 2026-01-15
        ---

        Content here.
        """;

    private const string MarkdownWithPublishDate = """
        ---
        title: Published Doc
        publishDate: 2026-03-01
        ---

        Content here.
        """;

    [Fact]
    public void Frontmatter_ShouldNotRenderRawYamlAsText()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;

        // Raw frontmatter fields should not appear as-is
        bodyText.Should().NotContain("---");
        bodyText.Should().NotContain("title:");
        bodyText.Should().NotContain("author:");
        bodyText.Should().Contain("Visible Heading");
        bodyText.Should().Contain("Body text content.");
    }

    [Fact]
    public void FrontmatterTitle_ShouldRenderAsH1Heading()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        var titleParagraph = paragraphs.First();

        titleParagraph.InnerText.Should().Be("Hidden Metadata Title");
        titleParagraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value
            .Should().Be("Heading1");
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
    public void FrontmatterAuthor_ShouldRenderWithBoldLabel()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        var authorParagraph = paragraphs.First(p => p.InnerText.Contains("Author:"));

        // Label "Author: " should be bold
        var runs = authorParagraph.Elements<Run>().ToList();
        runs.Should().HaveCountGreaterThanOrEqualTo(2);
        runs[0].RunProperties?.Bold.Should().NotBeNull();
        runs[0].InnerText.Should().Be("Author: ");

        // Value should not be bold
        runs[1].InnerText.Should().Be("Jane Doe");
    }

    [Fact]
    public void FrontmatterMultipleAuthors_ShouldRenderAsList()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithMultipleAuthors);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        var authorParagraph = paragraphs.First(p => p.InnerText.Contains("Authors:"));

        var runs = authorParagraph.Elements<Run>().ToList();
        runs[0].RunProperties?.Bold.Should().NotBeNull();
        runs[0].InnerText.Should().Be("Authors: ");
        runs[1].InnerText.Should().Be("Alice Smith, Bob Jones, Carol White");
    }

    [Fact]
    public void FrontmatterDate_ShouldRenderWithBoldLabel()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();
        var dateParagraph = paragraphs.First(p => p.InnerText.Contains("Date:"));

        var runs = dateParagraph.Elements<Run>().ToList();
        runs[0].RunProperties?.Bold.Should().NotBeNull();
        runs[0].InnerText.Should().Be("Date: ");
        runs[1].InnerText.Should().Be("2026-05-22");
    }

    [Fact]
    public void FrontmatterDate_ShouldSupportCreatedDateField()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithCreatedDate);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;
        bodyText.Should().Contain("Date: ");
        bodyText.Should().Contain("2026-01-15");
    }

    [Fact]
    public void FrontmatterDate_ShouldSupportPublishDateField()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithPublishDate);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var bodyText = doc.MainDocumentPart!.Document.Body!.InnerText;
        bodyText.Should().Contain("Date: ");
        bodyText.Should().Contain("2026-03-01");
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
        bodyText.Should().NotContain("Author:");
        bodyText.Should().NotContain("Date:");
    }

    [Fact]
    public void OtkPipeline_FrontmatterTitle_ShouldBeInDocumentProperties()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithFrontmatter);

        otk.Should().Contain("PROPERTY title=\"Hidden Metadata Title\"");
    }

    [Fact]
    public void OtkPipeline_FrontmatterTitle_ShouldEmitHeading1()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithFrontmatter);

        otk.Should().Contain("STYLE \"Heading1\"");
        otk.Should().Contain("SET \"Hidden Metadata Title\"");
    }

    [Fact]
    public void OtkPipeline_FrontmatterAuthor_ShouldEmitBoldRun()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithFrontmatter);

        otk.Should().Contain("RUN \"Author: \" bold=true");
        otk.Should().Contain("RUN \"Jane Doe\"");
    }

    [Fact]
    public void OtkPipeline_FrontmatterDate_ShouldEmitBoldRun()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithFrontmatter);

        otk.Should().Contain("RUN \"Date: \" bold=true");
        otk.Should().Contain("RUN \"2026-05-22\"");
    }

    [Fact]
    public void OtkPipeline_MultipleAuthors_ShouldEmitAuthorsLabel()
    {
        var otk = MarkdownConverter.CompileToOtk(MarkdownWithMultipleAuthors);

        otk.Should().Contain("RUN \"Authors: \" bold=true");
        otk.Should().Contain("RUN \"Alice Smith, Bob Jones, Carol White\"");
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
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var document = Markdig.Markdown.Parse("# Just a heading\n\nSome text.", pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().BeNull();
    }

    [Fact]
    public void FrontmatterExtractor_ShouldParseFlatYaml()
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var document = Markdig.Markdown.Parse("---\ntitle: My Title\nauthor: John\n---\n\nContent", pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().NotBeNull();
        result!.Title.Should().Be("My Title");
        result.Authors.Should().ContainSingle().Which.Should().Be("John");
    }

    [Fact]
    public void FrontmatterExtractor_ShouldParseAuthorsArray()
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var md = "---\ntitle: Doc\nauthors:\n  - Alice\n  - Bob\n---\n\nContent";
        var document = Markdig.Markdown.Parse(md, pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().NotBeNull();
        result!.Authors.Should().HaveCount(2);
        result.Authors[0].Should().Be("Alice");
        result.Authors[1].Should().Be("Bob");
    }

    [Fact]
    public void FrontmatterExtractor_ShouldStripQuotesFromValues()
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();
        var document = Markdig.Markdown.Parse("---\ntitle: \"Quoted Title\"\nother: 'Single Quoted'\n---\n\nContent", pipeline);

        var result = FrontmatterExtractor.Extract(document);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Quoted Title");
        result.Fields["other"].Should().Be("Single Quoted");
    }

    [Fact]
    public void FrontmatterExtractor_ShouldResolveDateFromVariousFieldNames()
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .Build();

        // "createdTimestamp" should be recognized
        var document = Markdig.Markdown.Parse("---\ncreatedTimestamp: 2025-12-01\n---\n\nContent", pipeline);
        var result = FrontmatterExtractor.Extract(document);

        result.Should().NotBeNull();
        result!.Date.Should().Be("2025-12-01");
    }

    [Fact]
    public void TitleHeaderOrder_ShouldBeTitleThenAuthorThenDate()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(MarkdownWithFrontmatter);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();

        // First paragraph: title heading
        paragraphs[0].InnerText.Should().Be("Hidden Metadata Title");
        paragraphs[0].ParagraphProperties?.ParagraphStyleId?.Val?.Value.Should().Be("Heading1");

        // Second paragraph: author
        paragraphs[1].InnerText.Should().Contain("Author:");
        paragraphs[1].InnerText.Should().Contain("Jane Doe");

        // Third paragraph: date
        paragraphs[2].InnerText.Should().Contain("Date:");
        paragraphs[2].InnerText.Should().Contain("2026-05-22");
    }
}
