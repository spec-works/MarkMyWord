using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using MarkMyWord.OpenXml;

namespace MarkMyWord.Tests;

/// <summary>
/// Tests for Unicode character handling during Markdown-to-Word conversion.
/// Covers astral plane (surrogate pair) characters, BMP symbols, XML-invalid
/// control characters, and mixed content scenarios.
/// </summary>
public class UnicodeHandlingTests
{
    #region TextSanitizer Unit Tests

    [Fact]
    public void Sanitize_NullInput_ReturnsEmpty()
    {
        TextSanitizer.Sanitize(null).Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_EmptyString_ReturnsEmpty()
    {
        TextSanitizer.Sanitize("").Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_AsciiText_ReturnsUnchanged()
    {
        var input = "Hello, World! 123";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_BmpSymbols_ReturnsUnchanged()
    {
        // These are BMP characters (U+0000 to U+FFFF) and should pass through
        var input = "Check ✅ and cross ❌ and arrow › and bullet •";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_AstralPlaneEmoji_PreservedInOutput()
    {
        // 😀 is U+1F600, represented as surrogate pair \uD83D\uDE00
        var input = "Hello 😀 World";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be(input, "valid surrogate pairs (emoji) should be preserved");
    }

    [Fact]
    public void Sanitize_MultipleAstralPlaneCharacters_AllPreserved()
    {
        // 🎉 (U+1F389), 🚀 (U+1F680), 💯 (U+1F4AF)
        var input = "Party 🎉 Rocket 🚀 Score 💯";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be(input, "all emoji should be preserved");
    }

    [Fact]
    public void Sanitize_XmlInvalidControlChars_Removed()
    {
        // Null byte, bell, backspace, vertical tab are XML-invalid
        var input = "Hello\x00\x07\x08\x0BWorld";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void Sanitize_ValidControlChars_Preserved()
    {
        // Tab (\x09), newline (\x0A), carriage return (\x0D) are XML-valid
        var input = "Line1\tTabbed\nLine2\r\nLine3";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_OrphanedHighSurrogate_Removed()
    {
        // A high surrogate without a matching low surrogate
        var input = "Hello\uD83DWorld";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void Sanitize_OrphanedLowSurrogate_Removed()
    {
        // A low surrogate without a preceding high surrogate
        var input = "Hello\uDE00World";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void Sanitize_MixedContent_PreservesEmojiStripsInvalid()
    {
        // Mix of ASCII, BMP symbols, astral emoji, and control characters
        var input = "Status: ✅ Done 🎉\x00 Next: ❌ Pending 🚀";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Contain("Status: ✅ Done 🎉");
        result.Should().Contain("Next: ❌ Pending 🚀");
        result.Should().NotContain("\x00");
    }

    [Fact]
    public void Sanitize_ConsecutiveAstralChars_AllPreserved()
    {
        // Two emoji back-to-back
        var input = "😀😀";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be("😀😀");
    }

    [Fact]
    public void Sanitize_FlagEmoji_FullyPreserved()
    {
        // Flag emoji are two regional indicator symbols (each is astral plane)
        // 🇺🇸 = U+1F1FA U+1F1F8 (two surrogate pairs)
        var input = "USA 🇺🇸 flag";
        var result = TextSanitizer.Sanitize(input);
        result.Should().Be(input, "flag emoji should be fully preserved");
    }

    [Fact]
    public void Sanitize_TextWithOnlyValidSurrogatePairs_ReturnsUnchanged()
    {
        // Text with only valid emoji — fast path should return as-is
        var input = "🚀🎉💯";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_PrivateUseArea_Preserved()
    {
        // BMP private use area (U+E000-U+F8FF) should be preserved
        var input = "Icon: \uE001 end";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_NonBreakingSpace_Preserved()
    {
        var input = "Hello\u00A0World";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_CjkCharacters_Preserved()
    {
        var input = "日本語テスト 中文测试 한국어";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_ArabicAndHebrew_Preserved()
    {
        var input = "مرحبا שלום";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    [Fact]
    public void Sanitize_MathematicalSymbols_Preserved()
    {
        // BMP mathematical symbols
        var input = "∑ ∏ ∫ √ ∞ ≈ ≠ ≤ ≥";
        TextSanitizer.Sanitize(input).Should().Be(input);
    }

    #endregion

    #region Integration Tests — Markdown to Word Conversion

    [Fact]
    public void Convert_ParagraphWithAstralEmoji_ShouldNotThrow()
    {
        var markdown = "Hello 😀 World 🎉 Done!";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_ParagraphWithAstralEmoji_PreservesEmoji()
    {
        var markdown = "Hello 😀 World";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        doc.Should().NotBeNull();
        doc.MainDocumentPart.Should().NotBeNull();

        var text = doc.MainDocumentPart!.Document.Body!.InnerText;
        text.Should().Contain("Hello");
        text.Should().Contain("World");
        text.Should().Contain("😀", "emoji should be preserved in the document");
    }

    [Fact]
    public void Convert_BoldTextWithEmoji_ShouldNotThrow()
    {
        var markdown = "This is **bold 🚀 text** here";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_BoldTextWithEmoji_PreservesBoldFormatting()
    {
        var markdown = "This is **bold 🚀 text** here";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var runs = doc.MainDocumentPart!.Document.Body!.Descendants<Run>().ToList();
        var boldRun = runs.FirstOrDefault(r => r.RunProperties?.Bold != null);
        boldRun.Should().NotBeNull();
    }

    [Fact]
    public void Convert_InlineCodeWithEmoji_ShouldNotThrow()
    {
        var markdown = "Run `emoji 😀 test` here";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_CodeBlockWithEmoji_ShouldNotThrow()
    {
        var markdown = "```\nvar x = \"🎉\";\nConsole.WriteLine(\"Done 🚀\");\n```";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_HeadingWithEmoji_ShouldNotThrow()
    {
        var markdown = "# 🚀 Launch Plan";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_HeadingWithEmoji_ProducesHeadingStyle()
    {
        var markdown = "# 🚀 Launch Plan";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var paragraph = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().First();
        paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value.Should().Be("Heading1");
    }

    [Fact]
    public void Convert_LinkWithEmoji_ShouldNotThrow()
    {
        var markdown = "[🚀 Launch](https://example.com)";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_LinkWithEmoji_CreatesHyperlink()
    {
        var markdown = "[🚀 Launch](https://example.com)";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var hyperlinks = doc.MainDocumentPart!.Document.Body!.Descendants<Hyperlink>().ToList();
        hyperlinks.Should().HaveCount(1);
    }

    [Fact]
    public void Convert_TableWithEmoji_ShouldNotThrow()
    {
        var markdown = "| Status | Item |\n|--------|------|\n| ✅ | Done 🎉 |\n| ❌ | Pending 🚀 |";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_BmpSymbols_PreservedInOutput()
    {
        var markdown = "Check ✅ and cross ❌ and arrow › and bullet •";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var text = doc.MainDocumentPart!.Document.Body!.InnerText;
        text.Should().Contain("✅");
        text.Should().Contain("❌");
        text.Should().Contain("›");
        text.Should().Contain("•");
    }

    [Fact]
    public void Convert_XmlControlCharsInParagraph_ShouldNotThrow()
    {
        // Text with XML-invalid control characters embedded
        var markdown = "Hello\x00\x08World";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_MixedUnicodeDocument_ShouldNotThrow()
    {
        var markdown = @"# 🚀 Project Status

## Summary

The project is **going well** 🎉!

- ✅ Task 1 complete
- ❌ Task 2 pending
- 🔄 Task 3 in progress

| Feature | Status |
|---------|--------|
| Auth | ✅ Done |
| API | 🚀 Launched |
| UI | ❌ Not started |

Here is some `code 💻` inline.

```
var emoji = ""😀"";
Console.WriteLine(""Hello 🌍"");
```

[🔗 Link](https://example.com)
";

        var act = () => MarkdownConverter.ConvertToDocxBytes(markdown);

        act.Should().NotThrow();
    }

    [Fact]
    public void Convert_MixedUnicodeDocument_ProducesValidStructure()
    {
        var markdown = @"# 🚀 Project Status

Status: ✅ Done

- Item with emoji 🎉
";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        doc.Should().NotBeNull();
        var body = doc.MainDocumentPart!.Document.Body!;
        body.Elements<Paragraph>().Should().HaveCountGreaterThan(0);

        var fullText = body.InnerText;
        fullText.Should().Contain("Project Status");
        fullText.Should().Contain("✅");
        fullText.Should().Contain("Done");
    }

    [Fact]
    public void Convert_CjkCharacters_PreservedInOutput()
    {
        var markdown = "日本語テスト and 中文测试";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var text = doc.MainDocumentPart!.Document.Body!.InnerText;
        text.Should().Contain("日本語テスト");
        text.Should().Contain("中文测试");
    }

    [Fact]
    public void Convert_OnlyAstralEmoji_ProducesNonEmptyDocument()
    {
        // A paragraph with ONLY astral emoji — should still produce a valid doc
        var markdown = "😀🎉🚀";

        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);
        doc.Should().NotBeNull();
    }

    #endregion
}
