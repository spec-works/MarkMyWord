using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using Xunit;

namespace MarkMyWord.Tests.BlockElements;

public class TableHyperlinkTests
{
    private const string ReproMarkdown = @"# Repro

A link in prose: [foo](https://example.com/foo)

| col1 | col2 |
|---|---|
| plain | [bar](https://example.com/bar) |
| [baz](https://example.com/baz) | text with [inline link](https://example.com/inline) inside |
";

    [Fact]
    public void TableCellsWithLinks_ShouldPreserveAllHyperlinks()
    {
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(ReproMarkdown);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var mainPart = doc.MainDocumentPart!;

        var expectedUrls = new[]
        {
            "https://example.com/foo",
            "https://example.com/bar",
            "https://example.com/baz",
            "https://example.com/inline",
        };

        var relUrls = mainPart.HyperlinkRelationships.Select(r => r.Uri.ToString()).ToList();
        foreach (var url in expectedUrls)
        {
            relUrls.Should().Contain(url, $"hyperlink relationship for {url} should exist");
        }

        var hyperlinks = mainPart.Document.Body!.Descendants<Hyperlink>().ToList();
        hyperlinks.Should().HaveCount(4, "all four markdown links (prose + 3 in-table) should be preserved as Word hyperlinks");

        foreach (var url in expectedUrls)
        {
            var rel = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Uri.ToString() == url);
            rel.Should().NotBeNull($"relationship for {url} should be present");
            hyperlinks.Should().Contain(h => h.Id == rel!.Id, $"a Hyperlink element should reference the relationship for {url}");
        }
    }

    [Fact]
    public void TableCellLink_ShouldUseHyperlinkRelationshipFromMainDocumentPart()
    {
        var markdown = @"| col |
|---|
| [only](https://example.com/only) |
";
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        var mainPart = doc.MainDocumentPart!;

        var hyperlink = mainPart.Document.Body!.Descendants<Hyperlink>().Single();
        var rel = mainPart.HyperlinkRelationships.Single(r => r.Id == hyperlink.Id);
        rel.Uri.ToString().Should().Be("https://example.com/only");
    }
}
