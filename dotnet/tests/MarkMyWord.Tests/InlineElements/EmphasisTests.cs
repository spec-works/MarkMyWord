using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace MarkMyWord.Tests.InlineElements;

public class EmphasisTests
{
    [Fact]
    public void CodeInsideBold_ShouldPreserveReadingOrder()
    {
        // Arrange — code span inside bold text
        var markdown = "**No `add` command**";

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        // Assert
        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>().ToList();

        // Extract the full text in document order
        var fullText = string.Join("", runs.Select(r =>
            string.Join("", r.Descendants<Text>().Select(t => t.Text))));

        fullText.Should().Be("No add command",
            "text must appear in reading order: 'No ' then 'add' then ' command'");

        // The 'add' run should carry both Bold and code font styling
        var addRun = runs.First(r =>
            r.Descendants<Text>().Any(t => t.Text == "add"));

        addRun.RunProperties.Should().NotBeNull();
        addRun.RunProperties!.Bold.Should().NotBeNull("code inside bold should remain bold");
        addRun.RunProperties!.RunFonts.Should().NotBeNull("code inside bold should have code font");
    }

    [Fact]
    public void CodeInsideItalic_ShouldPreserveReadingOrder()
    {
        var markdown = "*before `code` after*";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>().ToList();

        var fullText = string.Join("", runs.Select(r =>
            string.Join("", r.Descendants<Text>().Select(t => t.Text))));

        fullText.Should().Be("before code after");

        var codeRun = runs.First(r =>
            r.Descendants<Text>().Any(t => t.Text == "code"));

        codeRun.RunProperties.Should().NotBeNull();
        codeRun.RunProperties!.Italic.Should().NotBeNull("code inside italic should remain italic");
    }

    [Fact]
    public void MultipleCodeSpansInsideBold_ShouldAllBeInOrder()
    {
        var markdown = "**Use `atk add plugin` or `atk add skill` commands**";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!
            .Descendants<Run>().ToList();

        var fullText = string.Join("", runs.Select(r =>
            string.Join("", r.Descendants<Text>().Select(t => t.Text))));

        fullText.Should().Be("Use atk add plugin or atk add skill commands");
    }
}
