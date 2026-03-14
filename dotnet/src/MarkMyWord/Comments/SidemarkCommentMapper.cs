using System.Security.Cryptography;
using System.Text;
using Sidemark;

namespace MarkMyWord.Comments;

/// <summary>
/// Converts between Word document comments (via <see cref="WordCommentExtractor"/>) and
/// MRSF sidecar documents (<see cref="MrsfDocument"/>). Handles the bidirectional mapping
/// of comment anchors to markdown line numbers.
/// </summary>
public static class SidemarkCommentMapper
{
    /// <summary>
    /// Creates an <see cref="MrsfDocument"/> from extracted Word comments and the resulting markdown text.
    /// Maps comment anchor text to markdown line numbers for precise targeting.
    /// </summary>
    /// <param name="extractedComments">Comments extracted from a Word document.</param>
    /// <param name="markdown">The markdown output text (used to compute line mappings).</param>
    /// <param name="documentPath">Relative path to the markdown document for the MRSF header.</param>
    /// <returns>An MRSF document with all comments mapped to markdown positions.</returns>
    internal static MrsfDocument FromWordComments(
        List<WordCommentExtractor.ExtractedComment> extractedComments,
        string markdown,
        string documentPath)
    {
        var doc = new MrsfDocument
        {
            MrsfVersion = "1.0",
            Document = documentPath,
            Comments = []
        };

        if (extractedComments.Count == 0)
            return doc;

        var lines = markdown.Split('\n');

        foreach (var wc in extractedComments)
        {
            var comment = new MrsfComment
            {
                Id = Guid.NewGuid().ToString(),
                Author = wc.Author,
                Timestamp = wc.Date,
                Text = wc.Text,
                Resolved = false
            };

            // Try to map anchor text to markdown line numbers
            if (!string.IsNullOrEmpty(wc.AnchoredText))
            {
                comment.SelectedText = wc.AnchoredText;
                comment.SelectedTextHash = ComputeSha256(wc.AnchoredText);

                var (startLine, endLine) = FindTextInLines(lines, wc.AnchoredText);
                if (startLine > 0)
                {
                    comment.Line = startLine;
                    if (endLine > startLine)
                        comment.EndLine = endLine;
                }
            }

            doc.Comments.Add(comment);
        }

        return doc;
    }

    /// <summary>
    /// Maps MRSF comments to Word paragraph indices by finding where comment
    /// anchor text (selected_text or line-based targeting) appears in the markdown source.
    /// Returns a list of (MrsfComment, paragraphIndex) tuples for comment injection.
    /// </summary>
    /// <param name="mrsfDocument">The MRSF sidecar document with comments.</param>
    /// <param name="markdown">The markdown source text.</param>
    /// <returns>Comments paired with their target line numbers (1-based) in the markdown.</returns>
    internal static List<(MrsfComment Comment, int StartLine, int EndLine)> MapToMarkdownLines(
        MrsfDocument mrsfDocument,
        string markdown)
    {
        var result = new List<(MrsfComment, int, int)>();
        var lines = markdown.Split('\n');

        foreach (var comment in mrsfDocument.Comments)
        {
            int startLine = comment.Line ?? 0;
            int endLine = comment.EndLine ?? startLine;

            // If we have selected_text but no line, try to find it
            if (startLine == 0 && !string.IsNullOrEmpty(comment.SelectedText))
            {
                var (foundStart, foundEnd) = FindTextInLines(lines, comment.SelectedText);
                startLine = foundStart;
                endLine = foundEnd > 0 ? foundEnd : startLine;
            }

            if (startLine > 0)
            {
                result.Add((comment, startLine, endLine > 0 ? endLine : startLine));
            }
            else
            {
                // Document-level comment (no anchor)
                result.Add((comment, 0, 0));
            }
        }

        return result;
    }

    /// <summary>
    /// Finds the text fragment within the markdown lines, returning 1-based line numbers.
    /// </summary>
    private static (int StartLine, int EndLine) FindTextInLines(string[] lines, string searchText)
    {
        var normalizedSearch = NormalizeWhitespace(searchText);

        // First try exact single-line match
        for (int i = 0; i < lines.Length; i++)
        {
            if (NormalizeWhitespace(lines[i]).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            {
                return (i + 1, i + 1);
            }
        }

        // Try multi-line match: build a sliding window of concatenated lines
        for (int i = 0; i < lines.Length; i++)
        {
            var combined = new StringBuilder();
            for (int j = i; j < lines.Length && j < i + 20; j++)
            {
                if (combined.Length > 0) combined.Append(' ');
                combined.Append(NormalizeWhitespace(lines[j]));

                if (combined.ToString().Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                {
                    return (i + 1, j + 1);
                }
            }
        }

        return (0, 0);
    }

    private static string NormalizeWhitespace(string text) =>
        string.Join(' ', text.Split(default(char[]), StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Computes a SHA-256 hash of the given text, returned as a lowercase hex string.
    /// This matches the MRSF spec's selected_text_hash field format.
    /// </summary>
    public static string ComputeSha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexStringLower(bytes);
    }
}
