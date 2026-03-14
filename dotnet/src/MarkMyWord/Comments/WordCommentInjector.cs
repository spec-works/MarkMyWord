using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sidemark;

namespace MarkMyWord.Comments;

/// <summary>
/// Injects MRSF comments into an OpenXML Word document as native Word comments
/// with proper CommentRangeStart/CommentRangeEnd anchoring.
/// </summary>
internal static class WordCommentInjector
{
    /// <summary>
    /// Adds comments from an MRSF document into an existing Word document.
    /// Maps markdown line numbers to Word paragraphs and creates proper comment anchors.
    /// </summary>
    /// <param name="document">The Word document to inject comments into.</param>
    /// <param name="commentMappings">Comments paired with their markdown start/end lines.</param>
    public static void InjectComments(
        WordprocessingDocument document,
        List<(MrsfComment Comment, int StartLine, int EndLine)> commentMappings)
    {
        if (commentMappings.Count == 0) return;

        var mainPart = document.MainDocumentPart
            ?? throw new InvalidOperationException("Document has no main part.");
        var body = mainPart.Document?.Body
            ?? throw new InvalidOperationException("Document has no body.");

        // Create the comments part if it doesn't exist
        var commentsPart = mainPart.WordprocessingCommentsPart
            ?? mainPart.AddNewPart<WordprocessingCommentsPart>();
        commentsPart.Comments ??= new DocumentFormat.OpenXml.Wordprocessing.Comments();

        // Get all paragraphs in document order (these roughly correspond to markdown lines)
        var paragraphs = body.Elements<Paragraph>().ToList();

        int nextCommentId = 0;

        foreach (var (comment, startLine, endLine) in commentMappings)
        {
            var commentId = nextCommentId.ToString();
            nextCommentId++;

            // Create the Comment element in the Comments part
            var wordComment = CreateWordComment(commentId, comment);
            commentsPart.Comments.AppendChild(wordComment);

            // Determine which paragraph(s) to anchor to
            // Markdown lines map roughly to paragraphs, but it's not 1:1
            // We use a best-effort mapping: find the paragraph closest to the line number
            if (startLine > 0 && paragraphs.Count > 0)
            {
                var startIdx = Math.Min(startLine - 1, paragraphs.Count - 1);
                var endIdx = endLine > 0
                    ? Math.Min(endLine - 1, paragraphs.Count - 1)
                    : startIdx;

                AnchorComment(commentId, paragraphs, startIdx, endIdx);
            }
            else
            {
                // Document-level comment: anchor to first paragraph
                if (paragraphs.Count > 0)
                {
                    AnchorComment(commentId, paragraphs, 0, 0);
                }
            }
        }

        commentsPart.Comments.Save();
    }

    private static Comment CreateWordComment(string commentId, MrsfComment mrsfComment)
    {
        // Parse the MRSF timestamp to a DateTime for Word
        DateTimeOffset? timestamp = null;
        if (!string.IsNullOrEmpty(mrsfComment.Timestamp))
        {
            DateTimeOffset.TryParse(mrsfComment.Timestamp, out var parsed);
            timestamp = parsed;
        }

        var comment = new Comment
        {
            Id = commentId,
            Author = mrsfComment.Author,
            Initials = GetInitials(mrsfComment.Author),
            Date = timestamp?.UtcDateTime ?? DateTime.UtcNow
        };

        // Split comment text into paragraphs
        var textLines = mrsfComment.Text.Split('\n');
        foreach (var line in textLines)
        {
            var para = new Paragraph(
                new Run(
                    new Text(line) { Space = SpaceProcessingModeValues.Preserve }
                )
            );
            comment.AppendChild(para);
        }

        return comment;
    }

    private static void AnchorComment(string commentId, List<Paragraph> paragraphs, int startIdx, int endIdx)
    {
        var startParagraph = paragraphs[startIdx];
        var endParagraph = paragraphs[endIdx];

        // Insert CommentRangeStart before the first run of the start paragraph
        var rangeStart = new CommentRangeStart { Id = commentId };
        var firstRun = startParagraph.GetFirstChild<Run>();
        if (firstRun != null)
        {
            startParagraph.InsertBefore(rangeStart, firstRun);
        }
        else
        {
            startParagraph.PrependChild(rangeStart);
        }

        // Insert CommentRangeEnd and CommentReference after the last run of the end paragraph
        var rangeEnd = new CommentRangeEnd { Id = commentId };
        var commentRef = new Run(
            new CommentReference { Id = commentId }
        );

        endParagraph.AppendChild(rangeEnd);
        endParagraph.AppendChild(commentRef);
    }

    private static string GetInitials(string author)
    {
        if (string.IsNullOrWhiteSpace(author)) return "?";

        // Handle "Display Name (identifier)" format from MRSF spec
        var displayName = author;
        var parenIdx = author.IndexOf('(');
        if (parenIdx > 0)
            displayName = author[..parenIdx].Trim();

        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])))
        };
    }
}
