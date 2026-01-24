using MarkMyWord;
using MarkMyWord.Configuration;
using MarkMyWord.Exceptions;
using Xunit;
using Xunit.Abstractions;

namespace MarkMyWord.Tests;

public class WordToMarkdownTests
{
    private readonly ITestOutputHelper _output;

    public WordToMarkdownTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ConvertSimpleDocumentToMarkdown()
    {
        // Arrange
        var markdown = @"# Hello World

This is a **bold** statement and this is *italic*.

## Features

- Item 1
- Item 2
- Item 3";

        // Create a Word document from markdown
        using var docxStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, docxStream);
        docxStream.Position = 0;

        // Act - Convert Word document back to markdown
        var options = new WordToMarkdownOptions
        {
            Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,
            OptimizeForLLM = true
        };

        var result = WordConverter.ConvertToMarkdown(docxStream, options);

        // Assert
        _output.WriteLine("Generated Markdown:");
        _output.WriteLine(result);

        Assert.Contains("# Hello World", result);
        Assert.Contains("## Features", result);
    }

    [Fact]
    public void ConvertDocumentWithTable()
    {
        // Arrange
        var markdown = @"# Sales Report

| Product | Q1 | Q2 |
|---------|----|----|
| Widget A | $100 | $120 |
| Widget B | $85 | $90 |";

        // Create a Word document from markdown
        using var docxStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, docxStream);
        docxStream.Position = 0;

        // Act - Convert Word document back to markdown with GFM (supports tables)
        var options = new WordToMarkdownOptions
        {
            Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,
            OptimizeForLLM = false
        };

        var result = WordConverter.ConvertToMarkdown(docxStream, options);

        // Assert
        _output.WriteLine("Generated Markdown:");
        _output.WriteLine(result);

        Assert.Contains("# Sales Report", result);
        Assert.Contains("|", result); // Should contain table pipes
        Assert.Contains("Product", result);
    }

    [Fact]
    public void ConvertDocumentWithCommonMark()
    {
        // Arrange
        var markdown = @"# Document Title

This is a paragraph with **bold** and *italic* text.

## Section

Another paragraph.";

        // Create a Word document from markdown
        using var docxStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, docxStream);
        docxStream.Position = 0;

        // Act - Convert to strict CommonMark
        var options = new WordToMarkdownOptions
        {
            Flavor = MarkdownFlavor.CommonMark,
            OptimizeForLLM = true
        };

        var result = WordConverter.ConvertToMarkdown(docxStream, options);

        // Assert
        _output.WriteLine("Generated Markdown:");
        _output.WriteLine(result);

        Assert.Contains("# Document Title", result);
        Assert.Contains("## Section", result);
    }

    [Fact]
    public void ConvertDocumentWithMetadata()
    {
        // Arrange
        var markdown = "# Test Document\n\nContent here.";
        var conversionOptions = new ConversionOptions
        {
            DocumentTitle = "Test Title",
            Author = "Test Author",
            Subject = "Test Subject"
        };

        // Create a Word document from markdown with metadata
        using var docxStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, docxStream, conversionOptions);
        docxStream.Position = 0;

        // Act - Convert back with metadata extraction
        var options = new WordToMarkdownOptions
        {
            IncludeMetadata = true
        };

        var result = WordConverter.ConvertToMarkdown(docxStream, options);

        // Assert
        _output.WriteLine("Generated Markdown:");
        _output.WriteLine(result);

        Assert.Contains("---", result); // YAML frontmatter
        Assert.Contains("title:", result);
    }

    [Fact]
    public void ConvertDocumentWithLists()
    {
        // Arrange
        var markdown = @"# Shopping List

## Groceries
- Milk
- Eggs
- Bread

## Tasks
1. Buy groceries
2. Clean house
3. Do laundry";

        // Create a Word document from markdown
        using var docxStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, docxStream);
        docxStream.Position = 0;

        // Act
        var result = WordConverter.ConvertToMarkdown(docxStream);

        // Assert
        _output.WriteLine("Generated Markdown:");
        _output.WriteLine(result);

        Assert.Contains("# Shopping List", result);
        Assert.Contains("- ", result); // Unordered list marker
        Assert.Contains("1. ", result); // Ordered list marker
    }

    [Fact]
    public void RoundTripConversion()
    {
        // Arrange - Start with markdown
        var originalMarkdown = @"# Test Document

This is a paragraph with **bold** and *italic* text.

## Features

- Feature 1
- Feature 2
- Feature 3

### Code Example

```
var x = 10;
```";

        // Act 1 - Convert to Word
        using var docxStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(originalMarkdown, docxStream);
        docxStream.Position = 0;

        // Act 2 - Convert back to Markdown
        var resultMarkdown = WordConverter.ConvertToMarkdown(docxStream);

        // Assert
        _output.WriteLine("Original Markdown:");
        _output.WriteLine(originalMarkdown);
        _output.WriteLine("\nRound-trip Markdown:");
        _output.WriteLine(resultMarkdown);

        // Check that key elements are preserved
        Assert.Contains("# Test Document", resultMarkdown);
        Assert.Contains("## Features", resultMarkdown);
        Assert.Contains("```", resultMarkdown); // Code block
    }

    [Fact]
    public void EncryptedDocumentThrowsClearException()
    {
        // Create a stream with invalid/corrupted data to simulate an encrypted document
        using var fakeEncryptedStream = new MemoryStream(new byte[] { 0x50, 0x4B, 0x03, 0x04 }); // ZIP header but corrupted

        // Act & Assert
        var exception = Assert.Throws<EncryptedDocumentException>(() =>
        {
            WordConverter.ConvertToMarkdown(fakeEncryptedStream);
        });

        // Verify the exception has a helpful message
        Assert.Contains("encrypted", exception.Message.ToLower());
        Assert.Contains("password", exception.Message.ToLower());

        // Verify the detailed message method works
        var detailedMessage = exception.GetDetailedMessage();
        Assert.Contains("Microsoft Word", detailedMessage);
        Assert.Contains("File → Info → Protect Document", detailedMessage);
    }
}
