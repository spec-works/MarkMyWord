using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sidemark;

namespace MarkMyWord.Comments;

/// <summary>
/// Extracts Word comments and their anchor positions from an OpenXML document,
/// producing an MRSF-compatible <see cref="MrsfDocument"/>.
/// </summary>
internal class WordCommentExtractor
{
    /// <summary>
    /// Extracted comment with its OpenXML metadata and anchor text.
    /// </summary>
    internal class ExtractedComment
    {
        public required string Id { get; init; }
        public required string Author { get; init; }
        public required string Date { get; init; }
        public required string Text { get; init; }
        public string? AnchoredText { get; set; }
    }

    /// <summary>
    /// Extracts all comments from a Word document along with their anchored text.
    /// </summary>
    public static List<ExtractedComment> Extract(WordprocessingDocument document)
    {
        var commentsPart = document.MainDocumentPart?.WordprocessingCommentsPart;
        if (commentsPart?.Comments == null)
            return [];

        var body = document.MainDocumentPart?.Document?.Body;
        if (body == null)
            return [];

        // Build a map of comment ID -> Comment element
        var commentElements = commentsPart.Comments.Elements<Comment>()
            .Where(c => c.Id?.Value != null)
            .ToDictionary(c => c.Id!.Value!, c => c);

        // Build a map of comment ID -> anchored text by scanning CommentRangeStart/End
        var anchorMap = BuildAnchorMap(body);

        var results = new List<ExtractedComment>();
        foreach (var (id, comment) in commentElements)
        {
            var text = GetCommentText(comment);
            var extracted = new ExtractedComment
            {
                Id = id,
                Author = comment.Author?.Value ?? "Unknown",
                Date = comment.Date?.Value is DateTime dt
                    ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)).ToString("o")
                    : DateTimeOffset.UtcNow.ToString("o"),
                Text = text,
                AnchoredText = anchorMap.GetValueOrDefault(id)
            };
            results.Add(extracted);
        }

        return results;
    }

    /// <summary>
    /// Builds a map of comment ID to the plain text anchored between
    /// CommentRangeStart and CommentRangeEnd elements.
    /// </summary>
    private static Dictionary<string, string> BuildAnchorMap(Body body)
    {
        var map = new Dictionary<string, string>();
        var activeRanges = new Dictionary<string, StringBuilder>();

        foreach (var element in body.Descendants())
        {
            if (element is CommentRangeStart start && start.Id?.Value != null)
            {
                activeRanges[start.Id.Value] = new StringBuilder();
            }
            else if (element is CommentRangeEnd end && end.Id?.Value != null)
            {
                if (activeRanges.TryGetValue(end.Id.Value, out var sb))
                {
                    var text = sb.ToString().Trim();
                    if (!string.IsNullOrEmpty(text))
                        map[end.Id.Value] = text;
                    activeRanges.Remove(end.Id.Value);
                }
            }
            else if (element is Text textElement && activeRanges.Count > 0)
            {
                foreach (var sb in activeRanges.Values)
                {
                    sb.Append(textElement.Text);
                }
            }
        }

        return map;
    }

    private static string GetCommentText(Comment comment)
    {
        var sb = new StringBuilder();
        foreach (var para in comment.Elements<Paragraph>())
        {
            if (sb.Length > 0) sb.Append('\n');
            foreach (var text in para.Descendants<Text>())
            {
                sb.Append(text.Text);
            }
        }
        return sb.ToString();
    }
}
