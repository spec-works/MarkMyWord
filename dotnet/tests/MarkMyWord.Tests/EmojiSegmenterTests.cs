using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using MarkMyWord.OpenXml;

namespace MarkMyWord.Tests;

/// <summary>
/// Tests for the EmojiSegmenter and EmojiRunHelper classes.
/// Verifies correct detection, segmentation, and font application for emoji characters.
/// </summary>
public class EmojiSegmenterTests
{
    #region ContainsEmoji

    [Theory]
    [InlineData("Hello World", false)]
    [InlineData("", false)]
    [InlineData("Hello 😀 World", true)]
    [InlineData("🔴", true)]
    [InlineData("🔍 Discover", true)]
    [InlineData("Tier: 🔴 Critical", true)]
    [InlineData("Flag 🇺🇸 here", true)]
    [InlineData("ABC 123 !@#", false)]
    public void ContainsEmoji_DetectsCorrectly(string input, bool expected)
    {
        EmojiSegmenter.ContainsEmoji(input).Should().Be(expected);
    }

    [Fact]
    public void ContainsEmoji_NullInput_ReturnsFalse()
    {
        EmojiSegmenter.ContainsEmoji(null!).Should().BeFalse();
    }

    #endregion

    #region Segment

    [Fact]
    public void Segment_PlainText_ReturnsSingleNonEmojiSegment()
    {
        var segments = EmojiSegmenter.Segment("Hello World");

        segments.Should().HaveCount(1);
        segments[0].Text.Should().Be("Hello World");
        segments[0].IsEmoji.Should().BeFalse();
    }

    [Fact]
    public void Segment_OnlyEmoji_ReturnsSingleEmojiSegment()
    {
        var segments = EmojiSegmenter.Segment("🔴");

        segments.Should().HaveCount(1);
        segments[0].Text.Should().Be("🔴");
        segments[0].IsEmoji.Should().BeTrue();
    }

    [Fact]
    public void Segment_EmojiInMiddle_ReturnsThreeSegments()
    {
        var segments = EmojiSegmenter.Segment("Tier: 🔴 Critical");

        segments.Should().HaveCount(3);
        segments[0].Text.Should().Be("Tier: ");
        segments[0].IsEmoji.Should().BeFalse();
        segments[1].Text.Should().Be("🔴");
        segments[1].IsEmoji.Should().BeTrue();
        segments[2].Text.Should().Be(" Critical");
        segments[2].IsEmoji.Should().BeFalse();
    }

    [Fact]
    public void Segment_MultipleEmojis_MergesAdjacentEmoji()
    {
        var segments = EmojiSegmenter.Segment("🚀🎉💯");

        segments.Should().HaveCount(1);
        segments[0].IsEmoji.Should().BeTrue();
        segments[0].Text.Should().Contain("🚀");
        segments[0].Text.Should().Contain("🎉");
        segments[0].Text.Should().Contain("💯");
    }

    [Fact]
    public void Segment_EmptyString_ReturnsSingleEmptySegment()
    {
        var segments = EmojiSegmenter.Segment("");

        segments.Should().HaveCount(1);
        segments[0].Text.Should().BeEmpty();
        segments[0].IsEmoji.Should().BeFalse();
    }

    [Fact]
    public void Segment_MixedContent_PreservesAllText()
    {
        var input = "🔍 Discover → 🤔 Understand → 🛠️ Build";
        var segments = EmojiSegmenter.Segment(input);

        // Verify all text is preserved when segments are concatenated
        var reconstructed = string.Concat(segments.Select(s => s.Text));
        reconstructed.Should().Be(input);
    }

    [Fact]
    public void Segment_VariationSelector_TreatedAsEmoji()
    {
        // 🛠️ is U+1F6E0 + U+FE0F (variation selector 16)
        var segments = EmojiSegmenter.Segment("Tools 🛠️ here");

        segments.Should().HaveCountGreaterOrEqualTo(3);
        segments.Should().Contain(s => s.IsEmoji && s.Text.Contains("🛠"));
    }

    #endregion

    #region EmojiRunHelper

    [Fact]
    public void AppendText_PlainText_CreatesSingleRun()
    {
        var paragraph = new Paragraph();

        EmojiRunHelper.AppendText(paragraph, "Hello World");

        var runs = paragraph.Elements<Run>().ToList();
        runs.Should().HaveCount(1);
        runs[0].InnerText.Should().Be("Hello World");
        // No emoji font should be applied
        runs[0].RunProperties?.GetFirstChild<RunFonts>()?.Ascii?.Value
            .Should().NotBe(EmojiSegmenter.EmojiFontName);
    }

    [Fact]
    public void AppendText_EmojiText_AppliesEmojiFont()
    {
        var paragraph = new Paragraph();

        EmojiRunHelper.AppendText(paragraph, "Tier: 🔴 Critical");

        var runs = paragraph.Elements<Run>().ToList();
        runs.Should().HaveCount(3);

        // First run: "Tier: " — no emoji font
        runs[0].InnerText.Should().Be("Tier: ");

        // Second run: "🔴" — should have emoji font
        runs[1].InnerText.Should().Be("🔴");
        var emojiFont = runs[1].RunProperties?.GetFirstChild<RunFonts>();
        emojiFont.Should().NotBeNull();
        emojiFont!.Ascii!.Value.Should().Be(EmojiSegmenter.EmojiFontName);
        emojiFont!.HighAnsi!.Value.Should().Be(EmojiSegmenter.EmojiFontName);

        // Third run: " Critical" — no emoji font
        runs[2].InnerText.Should().Be(" Critical");
    }

    [Fact]
    public void AppendText_WithBaseRunProperties_PreservesFormattingOnBothSegments()
    {
        var paragraph = new Paragraph();
        var boldProps = new RunProperties(new Bold());

        EmojiRunHelper.AppendText(paragraph, "Bold 🔴 text", boldProps);

        var runs = paragraph.Elements<Run>().ToList();
        runs.Should().HaveCount(3);

        // All runs should be bold
        foreach (var run in runs)
        {
            run.RunProperties.Should().NotBeNull();
            run.RunProperties!.GetFirstChild<Bold>().Should().NotBeNull();
        }

        // Emoji run should also have the emoji font
        runs[1].RunProperties!.GetFirstChild<RunFonts>().Should().NotBeNull();
    }

    [Fact]
    public void AppendText_EmptyText_AddsNoRuns()
    {
        var paragraph = new Paragraph();

        EmojiRunHelper.AppendText(paragraph, "");

        paragraph.Elements<Run>().Should().BeEmpty();
    }

    #endregion

    #region Integration — Emoji Font in Converted Documents

    [Fact]
    public void Convert_InlineEmoji_AppliesEmojiFont()
    {
        var markdown = "Tier: 🔴 Critical";
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var body = doc.MainDocumentPart!.Document.Body!;
        var runs = body.Descendants<Run>().ToList();

        // Find the run containing the red circle emoji
        var emojiRun = runs.FirstOrDefault(r => r.InnerText.Contains("🔴"));
        emojiRun.Should().NotBeNull("emoji should be present in the document");

        var fonts = emojiRun!.RunProperties?.GetFirstChild<RunFonts>();
        fonts.Should().NotBeNull("emoji run should have font properties");
        fonts!.Ascii!.Value.Should().Be(EmojiSegmenter.EmojiFontName,
            "emoji run should use Segoe UI Emoji for color rendering");
    }

    [Fact]
    public void Convert_BoldWithEmoji_PreservesBoldAndAppliesEmojiFont()
    {
        var markdown = "**Tier: 🔴 Critical**";
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var body = doc.MainDocumentPart!.Document.Body!;
        var runs = body.Descendants<Run>().ToList();

        // Find the emoji run
        var emojiRun = runs.FirstOrDefault(r => r.InnerText.Contains("🔴"));
        emojiRun.Should().NotBeNull();

        // Should have both bold and emoji font
        var props = emojiRun!.RunProperties;
        props.Should().NotBeNull();
        props!.GetFirstChild<Bold>().Should().NotBeNull("emphasis should be preserved on emoji run");
        props!.GetFirstChild<RunFonts>()?.Ascii?.Value.Should().Be(EmojiSegmenter.EmojiFontName);
    }

    [Fact]
    public void Convert_MultipleEmojis_AllHaveEmojiFont()
    {
        var markdown = "🔍 Discover → 🤔 Understand → 📦 Publish";
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var ms = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(ms, false);

        var body = doc.MainDocumentPart!.Document.Body!;
        var runs = body.Descendants<Run>().ToList();

        // All runs containing emoji should have the emoji font
        var emojiRuns = runs.Where(r =>
            EmojiSegmenter.ContainsEmoji(r.InnerText)).ToList();

        emojiRuns.Should().NotBeEmpty("there should be emoji runs in the document");

        foreach (var run in emojiRuns)
        {
            var fonts = run.RunProperties?.GetFirstChild<RunFonts>();
            fonts.Should().NotBeNull($"emoji run '{run.InnerText}' should have font properties");
            fonts!.Ascii!.Value.Should().Be(EmojiSegmenter.EmojiFontName);
        }
    }

    #endregion
}
