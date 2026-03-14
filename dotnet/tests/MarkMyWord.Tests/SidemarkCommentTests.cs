using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using MarkMyWord.Comments;
using MarkMyWord.Configuration;
using Sidemark;

namespace MarkMyWord.Tests;

/// <summary>
/// Tests for Sidemark (MRSF) comment roundtripping between Word and Markdown.
/// </summary>
public class SidemarkCommentTests
{
    #region Word → Markdown (Comment Extraction)

    [Fact]
    public void WordToMarkdown_WithComments_ExtractsSidemarkDocument()
    {
        // Arrange: create a Word document with comments
        var docxBytes = CreateWordDocWithComments(
            "Hello world, this is a test paragraph.",
            ("0", "John Doe", "This is a review comment", "Hello world"));

        using var stream = new MemoryStream(docxBytes);
        var options = new WordToMarkdownOptions { ExtractCommentsAsSidemark = true };

        // Act
        var result = WordConverter.ConvertToMarkdownWithComments(stream, options);

        // Assert
        result.Markdown.Should().Contain("Hello world");
        result.HasComments.Should().BeTrue();
        result.SidemarkDocument.Should().NotBeNull();
        result.SidemarkDocument!.MrsfVersion.Should().Be("1.0");
        result.SidemarkDocument.Comments.Should().HaveCount(1);

        var comment = result.SidemarkDocument.Comments[0];
        comment.Author.Should().Be("John Doe");
        comment.Text.Should().Be("This is a review comment");
        comment.Resolved.Should().BeFalse();
        comment.Id.Should().NotBeNullOrEmpty();
        comment.Timestamp.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WordToMarkdown_WithMultipleComments_ExtractsAll()
    {
        var docxBytes = CreateWordDocWithComments(
            "First paragraph. Second paragraph.",
            ("0", "Alice", "Comment on first part", "First paragraph"),
            ("1", "Bob", "Comment on second part", "Second paragraph"));

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        result.SidemarkDocument.Should().NotBeNull();
        result.SidemarkDocument!.Comments.Should().HaveCount(2);
        result.SidemarkDocument.Comments.Select(c => c.Author)
            .Should().Contain("Alice").And.Contain("Bob");
    }

    [Fact]
    public void WordToMarkdown_WithNoComments_ReturnsNullSidemark()
    {
        var markdown = "# Simple heading\n\nJust a paragraph.";
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        result.Markdown.Should().Contain("Simple heading");
        result.HasComments.Should().BeFalse();
        result.SidemarkDocument.Should().BeNull();
    }

    [Fact]
    public void WordToMarkdown_WithComments_IncludesAnchorText()
    {
        var docxBytes = CreateWordDocWithComments(
            "The quick brown fox jumps over the lazy dog.",
            ("0", "Reviewer", "Consider different wording", "quick brown fox"));

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        result.SidemarkDocument.Should().NotBeNull();
        var comment = result.SidemarkDocument!.Comments[0];
        comment.SelectedText.Should().Be("quick brown fox");
        comment.SelectedTextHash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WordToMarkdown_WithComments_SetsDocumentPath()
    {
        var docxBytes = CreateWordDocWithComments(
            "Test content",
            ("0", "Author", "A comment", "Test"));

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream, documentPath: "readme.md");

        result.SidemarkDocument.Should().NotBeNull();
        result.SidemarkDocument!.Document.Should().Be("readme.md");
    }

    [Fact]
    public void WordToMarkdown_WithoutSidemarkOption_DoesNotExtract()
    {
        var docxBytes = CreateWordDocWithComments(
            "Some text",
            ("0", "Author", "Comment", "Some"));

        using var stream = new MemoryStream(docxBytes);
        var options = new WordToMarkdownOptions { ExtractCommentsAsSidemark = false };

        // Regular conversion should work fine
        var markdown = WordConverter.ConvertToMarkdown(stream, options);
        markdown.Should().Contain("Some text");
    }

    #endregion

    #region Markdown → Word (Comment Injection)

    [Fact]
    public void MarkdownToWord_WithSidemarkDocument_InjectsComments()
    {
        // Arrange
        var markdown = "# Title\n\nThis is a test paragraph.\n\nAnother paragraph here.";
        var mrsfDoc = new MrsfDocument
        {
            MrsfVersion = "1.0",
            Document = "test.md",
            Comments =
            [
                new MrsfComment
                {
                    Id = "comment-1",
                    Author = "Jane Smith",
                    Timestamp = "2025-01-15T10:30:00Z",
                    Text = "Please revise this paragraph",
                    Resolved = false,
                    Line = 3,
                    SelectedText = "This is a test paragraph."
                }
            ]
        };

        var options = new ConversionOptions { SidemarkDocument = mrsfDoc };

        // Act
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown, options);

        // Assert: verify the Word document has comments
        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        var commentsPart = doc.MainDocumentPart?.WordprocessingCommentsPart;
        commentsPart.Should().NotBeNull();

        var comments = commentsPart!.Comments?.Elements<Comment>().ToList();
        comments.Should().NotBeNull();
        comments!.Count.Should().Be(1);

        var comment = comments[0];
        comment.Author?.Value.Should().Be("Jane Smith");

        // Verify anchoring elements exist in the body
        var body = doc.MainDocumentPart!.Document!.Body!;
        body.Descendants<CommentRangeStart>().Should().HaveCount(1);
        body.Descendants<CommentRangeEnd>().Should().HaveCount(1);
        body.Descendants<CommentReference>().Should().HaveCount(1);
    }

    [Fact]
    public void MarkdownToWord_WithMultipleSidemarkComments_InjectsAll()
    {
        var markdown = "# Title\n\nFirst paragraph.\n\nSecond paragraph.\n\nThird paragraph.";
        var mrsfDoc = new MrsfDocument
        {
            MrsfVersion = "1.0",
            Document = "test.md",
            Comments =
            [
                new MrsfComment
                {
                    Id = "c1",
                    Author = "Alice",
                    Timestamp = "2025-01-15T10:00:00Z",
                    Text = "First comment",
                    Resolved = false,
                    Line = 3
                },
                new MrsfComment
                {
                    Id = "c2",
                    Author = "Bob",
                    Timestamp = "2025-01-15T11:00:00Z",
                    Text = "Second comment",
                    Resolved = true,
                    Line = 5
                }
            ]
        };

        var options = new ConversionOptions { SidemarkDocument = mrsfDoc };
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown, options);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        var comments = doc.MainDocumentPart?.WordprocessingCommentsPart?.Comments
            ?.Elements<Comment>().ToList();
        comments.Should().HaveCount(2);
    }

    [Fact]
    public void MarkdownToWord_WithNoSidemark_ProducesNormalDocument()
    {
        var markdown = "# Title\n\nJust a normal paragraph.";
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);

        using var stream = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(stream, false);

        doc.MainDocumentPart?.WordprocessingCommentsPart.Should().BeNull();
    }

    [Fact]
    public void MarkdownToWord_WithSidemarkFilePath_InjectsComments()
    {
        var markdown = "# Title\n\nA paragraph with content.";
        var mrsfDoc = new MrsfDocument
        {
            MrsfVersion = "1.0",
            Document = "test.md",
            Comments =
            [
                new MrsfComment
                {
                    Id = "file-comment-1",
                    Author = "FileReviewer",
                    Timestamp = "2025-02-01T09:00:00Z",
                    Text = "Comment from sidecar file",
                    Resolved = false,
                    Line = 3
                }
            ]
        };

        // Write a temp sidecar file
        var tempDir = Path.Combine(Path.GetTempPath(), $"sidemark-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sidecarPath = Path.Combine(tempDir, "test.md.review.yaml");
        try
        {
            MrsfSerializer.WriteFile(mrsfDoc, sidecarPath);

            var options = new ConversionOptions { SidemarkFilePath = sidecarPath };
            var docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown, options);

            using var stream = new MemoryStream(docxBytes);
            using var doc = WordprocessingDocument.Open(stream, false);
            var comments = doc.MainDocumentPart?.WordprocessingCommentsPart?.Comments
                ?.Elements<Comment>().ToList();
            comments.Should().HaveCount(1);
            comments![0].Author?.Value.Should().Be("FileReviewer");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Full Roundtrip Tests

    [Fact]
    public void Roundtrip_MarkdownToWordToMarkdown_PreservesComments()
    {
        // Start with markdown + Sidemark document
        var originalMarkdown = "# Project README\n\nThis project implements a parser.\n\nIt supports multiple formats.";
        var originalMrsf = new MrsfDocument
        {
            MrsfVersion = "1.0",
            Document = "readme.md",
            Comments =
            [
                new MrsfComment
                {
                    Id = "roundtrip-1",
                    Author = "Reviewer One",
                    Timestamp = "2025-03-01T14:00:00Z",
                    Text = "Consider adding more detail here",
                    Resolved = false,
                    Line = 3,
                    SelectedText = "This project implements a parser."
                }
            ]
        };

        // Step 1: Markdown → Word (with comments)
        var options = new ConversionOptions { SidemarkDocument = originalMrsf };
        var docxBytes = MarkdownConverter.ConvertToDocxBytes(originalMarkdown, options);

        // Step 2: Word → Markdown (extracting comments)
        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        // Verify markdown content is preserved
        result.Markdown.Should().Contain("Project README");
        result.Markdown.Should().Contain("parser");

        // Verify comments were roundtripped
        result.HasComments.Should().BeTrue();
        result.SidemarkDocument!.Comments.Should().HaveCount(1);

        var roundtrippedComment = result.SidemarkDocument.Comments[0];
        roundtrippedComment.Author.Should().Be("Reviewer One");
        roundtrippedComment.Text.Should().Be("Consider adding more detail here");
    }

    [Fact]
    public void Roundtrip_WordToMarkdownToWord_PreservesComments()
    {
        // Start with a Word document with comments
        var docxBytes = CreateWordDocWithComments(
            "Important specification text that needs review.",
            ("0", "Spec Editor", "This wording needs to be more precise", "Important specification"));

        // Step 1: Word → Markdown (extracting comments)
        using var stream1 = new MemoryStream(docxBytes);
        var extractResult = WordConverter.ConvertToMarkdownWithComments(stream1);

        extractResult.HasComments.Should().BeTrue();

        // Step 2: Markdown → Word (injecting comments back)
        var injectOptions = new ConversionOptions { SidemarkDocument = extractResult.SidemarkDocument };
        var newDocxBytes = MarkdownConverter.ConvertToDocxBytes(extractResult.Markdown, injectOptions);

        // Verify the new Word document has comments
        using var stream2 = new MemoryStream(newDocxBytes);
        using var doc = WordprocessingDocument.Open(stream2, false);

        var comments = doc.MainDocumentPart?.WordprocessingCommentsPart?.Comments
            ?.Elements<Comment>().ToList();
        comments.Should().NotBeNull();
        comments!.Count.Should().BeGreaterOrEqualTo(1);
        comments.Any(c => c.Author?.Value == "Spec Editor").Should().BeTrue();
    }

    #endregion

    #region MRSF Serialization Tests

    [Fact]
    public void ExtractedSidemark_CanBeSerializedToYaml()
    {
        var docxBytes = CreateWordDocWithComments(
            "Content to review",
            ("0", "YAML Tester", "Needs revision", "Content"));

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        result.SidemarkDocument.Should().NotBeNull();

        var yaml = MrsfSerializer.ToYaml(result.SidemarkDocument!);
        yaml.Should().Contain("mrsf_version");
        yaml.Should().Contain("1.0");
        yaml.Should().Contain("author: YAML Tester");
        yaml.Should().Contain("text: Needs revision");
    }

    [Fact]
    public void ExtractedSidemark_CanBeSerializedToJson()
    {
        var docxBytes = CreateWordDocWithComments(
            "Content for JSON test",
            ("0", "JSON Tester", "Check this", "Content"));

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        result.SidemarkDocument.Should().NotBeNull();

        var json = MrsfSerializer.ToJson(result.SidemarkDocument!);
        json.Should().Contain("\"mrsf_version\": \"1.0\"");
        json.Should().Contain("\"author\": \"JSON Tester\"");
    }

    [Fact]
    public void ExtractedSidemark_HasValidSelectedTextHash()
    {
        var docxBytes = CreateWordDocWithComments(
            "Hash verification text here",
            ("0", "Hash Tester", "Verify hash", "Hash verification"));

        using var stream = new MemoryStream(docxBytes);
        var result = WordConverter.ConvertToMarkdownWithComments(stream);

        var comment = result.SidemarkDocument?.Comments.FirstOrDefault();
        if (comment?.SelectedText != null)
        {
            comment.SelectedTextHash.Should().NotBeNullOrEmpty();
            // Verify it's a valid hex string (SHA-256 = 64 hex chars)
            comment.SelectedTextHash!.Length.Should().Be(64);
            comment.SelectedTextHash.Should().MatchRegex("^[0-9a-f]{64}$");
        }
    }

    #endregion

    #region Comment Mapper Unit Tests

    [Fact]
    public void SidemarkCommentMapper_ComputeSha256_ProducesConsistentHash()
    {
        var hash1 = SidemarkCommentMapper.ComputeSha256("test text");
        var hash2 = SidemarkCommentMapper.ComputeSha256("test text");
        hash1.Should().Be(hash2);
        hash1.Length.Should().Be(64);
    }

    [Fact]
    public void SidemarkCommentMapper_ComputeSha256_ProducesDifferentHashForDifferentInput()
    {
        var hash1 = SidemarkCommentMapper.ComputeSha256("text A");
        var hash2 = SidemarkCommentMapper.ComputeSha256("text B");
        hash1.Should().NotBe(hash2);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a Word document with comments anchored to specific text.
    /// Each comment tuple is (id, author, commentText, anchorText).
    /// </summary>
    private static byte[] CreateWordDocWithComments(
        string bodyText,
        params (string Id, string Author, string CommentText, string AnchorText)[] comments)
    {
        using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Create the paragraph with anchor text marked by CommentRangeStart/End
            var paragraph = new Paragraph();

            if (comments.Length == 0)
            {
                // Simple case: no comments
                paragraph.AppendChild(new Run(new Text(bodyText) { Space = SpaceProcessingModeValues.Preserve }));
            }
            else
            {
                // Split text around anchor regions and insert comment markers
                var remainingText = bodyText;
                foreach (var (id, author, commentText, anchorText) in comments)
                {
                    var anchorIdx = remainingText.IndexOf(anchorText, StringComparison.Ordinal);
                    if (anchorIdx >= 0)
                    {
                        // Text before anchor
                        if (anchorIdx > 0)
                        {
                            paragraph.AppendChild(new Run(
                                new Text(remainingText[..anchorIdx]) { Space = SpaceProcessingModeValues.Preserve }));
                        }

                        // CommentRangeStart
                        paragraph.AppendChild(new CommentRangeStart { Id = id });

                        // Anchored text
                        paragraph.AppendChild(new Run(
                            new Text(anchorText) { Space = SpaceProcessingModeValues.Preserve }));

                        // CommentRangeEnd + CommentReference
                        paragraph.AppendChild(new CommentRangeEnd { Id = id });
                        paragraph.AppendChild(new Run(new CommentReference { Id = id }));

                        remainingText = remainingText[(anchorIdx + anchorText.Length)..];
                    }
                }

                // Remaining text after all anchors
                if (remainingText.Length > 0)
                {
                    paragraph.AppendChild(new Run(
                        new Text(remainingText) { Space = SpaceProcessingModeValues.Preserve }));
                }
            }

            body.AppendChild(paragraph);

            // Create the Comments part
            if (comments.Length > 0)
            {
                var commentsPart = mainPart.AddNewPart<WordprocessingCommentsPart>();
                commentsPart.Comments = new DocumentFormat.OpenXml.Wordprocessing.Comments();

                foreach (var (id, author, commentText, _) in comments)
                {
                    var comment = new Comment
                    {
                        Id = id,
                        Author = author,
                        Initials = author[..Math.Min(2, author.Length)].ToUpperInvariant(),
                        Date = DateTime.UtcNow
                    };
                    comment.AppendChild(new Paragraph(
                        new Run(new Text(commentText) { Space = SpaceProcessingModeValues.Preserve })));
                    commentsPart.Comments.AppendChild(comment);
                }

                commentsPart.Comments.Save();
            }

            doc.Save();
        }

        return ms.ToArray();
    }

    #endregion
}
