using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using Sidemark;

namespace MarkMyWord.Comments;

/// <summary>
/// Injects MRSF comments into an OpenXML Word document as native Word comments
/// with proper CommentRangeStart/CommentRangeEnd anchoring.
/// </summary>
internal static class WordCommentInjector
{
    private sealed record ParagraphTarget(Paragraph Paragraph, int StartLine, int EndLine, int SpanLength, int Index);

    /// <summary>
    /// Adds comments from an MRSF document into an existing Word document.
    /// Maps markdown line numbers to Word paragraphs and creates proper comment anchors.
    /// </summary>
    /// <param name="document">The Word document to inject comments into.</param>
    /// <param name="commentMappings">Comments paired with their markdown start/end lines.</param>
    public static void InjectComments(
        WordprocessingDocument document,
        List<(MrsfComment Comment, int StartLine, int EndLine)> commentMappings,
        IReadOnlyList<(Block SourceBlock, Paragraph Paragraph)> paragraphMappings,
        string markdown)
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

        var paragraphTargets = paragraphMappings
            .Select((mapping, index) => new ParagraphTarget(
                mapping.Paragraph,
                GetLineNumber(markdown, mapping.SourceBlock.Span.Start),
                GetLineNumber(markdown, mapping.SourceBlock.Span.End),
                Math.Max(0, mapping.SourceBlock.Span.End - mapping.SourceBlock.Span.Start),
                index))
            .ToList();
        var paragraphs = paragraphTargets.Select(target => target.Paragraph).ToList();

        int nextCommentId = 0;

        foreach (var (comment, startLine, endLine) in commentMappings)
        {
            var commentId = nextCommentId.ToString();
            nextCommentId++;

            // Create the Comment element in the Comments part
            var wordComment = CreateWordComment(commentId, comment);
            commentsPart.Comments.AppendChild(wordComment);

            if (paragraphs.Count == 0)
                continue;

            var startIdx = FindTargetParagraph(paragraphTargets, startLine, comment.SelectedText);
            if (startLine > 0)
            {
                var endIdx = FindTargetParagraph(paragraphTargets, endLine, null);
                AnchorComment(commentId, paragraphs, startIdx, Math.Max(startIdx, endIdx), comment.SelectedText);
            }
            else
            {
                AnchorComment(commentId, paragraphs, 0, 0, comment.SelectedText);
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

    private static int FindTargetParagraph(
        List<ParagraphTarget> paragraphTargets,
        int targetLine,
        string? selectedText)
    {
        if (!string.IsNullOrEmpty(selectedText))
        {
            var textMatches = paragraphTargets
                .Where(target => target.Paragraph.InnerText.Contains(selectedText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (textMatches.Count > 0)
            {
                return textMatches
                    .OrderBy(target => targetLine > 0 && targetLine >= target.StartLine && targetLine <= target.EndLine ? 0 : 1)
                    .ThenBy(target => target.SpanLength)
                    .First().Index;
            }
        }

        if (targetLine <= 0)
            return 0;

        var lineMatches = paragraphTargets
            .Where(target => targetLine >= target.StartLine && targetLine <= target.EndLine)
            .ToList();
        if (lineMatches.Count > 0)
            return lineMatches.OrderBy(target => target.SpanLength).First().Index;

        return paragraphTargets
            .OrderBy(target => Math.Min(Math.Abs(targetLine - target.StartLine), Math.Abs(targetLine - target.EndLine)))
            .First().Index;
    }

    private static int GetLineNumber(string markdown, int position)
    {
        var line = 1;
        var end = Math.Clamp(position, 0, markdown.Length);
        for (var i = 0; i < end; i++)
        {
            if (markdown[i] == '\n')
                line++;
        }

        return line;
    }

    private static void AnchorComment(
        string commentId,
        List<Paragraph> paragraphs,
        int startIdx,
        int endIdx,
        string? selectedText)
    {
        var startParagraph = paragraphs[startIdx];
        var endParagraph = paragraphs[endIdx];

        if (startIdx == endIdx && !string.IsNullOrEmpty(selectedText) &&
            TryAnchorSelectedText(commentId, startParagraph, selectedText))
        {
            return;
        }

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

    private static bool TryAnchorSelectedText(string commentId, Paragraph paragraph, string selectedText)
    {
        var segment = new List<Run>();
        foreach (var child in paragraph.ChildElements)
        {
            if (child is Run run &&
                run.ChildElements.All(runChild => runChild is RunProperties or Text))
            {
                segment.Add(run);
                continue;
            }

            if (TryAnchorSelectedTextInRuns(commentId, paragraph, segment, selectedText))
                return true;
            segment.Clear();
        }

        return TryAnchorSelectedTextInRuns(commentId, paragraph, segment, selectedText);
    }

    private static bool TryAnchorSelectedTextInRuns(
        string commentId,
        Paragraph paragraph,
        List<Run> runs,
        string selectedText)
    {
        if (runs.Count == 0)
            return false;

        var runTexts = runs
            .Select(run => string.Concat(run.Elements<Text>().Select(text => text.Text)))
            .ToList();
        var start = string.Concat(runTexts).IndexOf(selectedText, StringComparison.Ordinal);
        if (start < 0)
            return false;

        var selectionEnd = start + selectedText.Length;
        var currentPosition = 0;
        var firstRunIndex = -1;
        var lastRunIndex = -1;
        var selectedRuns = new List<Run>();
        string? prefix = null;
        string? suffix = null;

        for (var i = 0; i < runs.Count; i++)
        {
            var runText = runTexts[i];
            var runStart = currentPosition;
            var runEnd = runStart + runText.Length;
            currentPosition = runEnd;

            var selectedStart = Math.Max(start, runStart);
            var selectedEnd = Math.Min(selectionEnd, runEnd);
            if (selectedStart >= selectedEnd)
                continue;

            if (firstRunIndex < 0)
            {
                firstRunIndex = i;
                if (selectedStart > runStart)
                    prefix = runText[..(selectedStart - runStart)];
            }

            lastRunIndex = i;
            selectedRuns.Add(CreateTextRun(runs[i], runText[(selectedStart - runStart)..(selectedEnd - runStart)]));
            if (selectedEnd < runEnd)
                suffix = runText[(selectedEnd - runStart)..];
        }

        if (firstRunIndex < 0 || lastRunIndex < 0)
            return false;

        var firstRun = runs[firstRunIndex];
        if (!string.IsNullOrEmpty(prefix))
            paragraph.InsertBefore(CreateTextRun(firstRun, prefix), firstRun);

        paragraph.InsertBefore(new CommentRangeStart { Id = commentId }, firstRun);
        foreach (var selectedRun in selectedRuns)
            paragraph.InsertBefore(selectedRun, firstRun);
        paragraph.InsertBefore(new CommentRangeEnd { Id = commentId }, firstRun);
        paragraph.InsertBefore(new Run(new CommentReference { Id = commentId }), firstRun);

        if (!string.IsNullOrEmpty(suffix))
            paragraph.InsertBefore(CreateTextRun(runs[lastRunIndex], suffix), firstRun);

        for (var i = firstRunIndex; i <= lastRunIndex; i++)
            runs[i].Remove();

        return true;
    }

    private static Run CreateTextRun(Run originalRun, string text)
    {
        var run = new Run();
        if (originalRun.RunProperties != null)
            run.RunProperties = (RunProperties)originalRun.RunProperties.CloneNode(true);
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return run;
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
