using System.Text;
using MarkMyWord.Reading.Elements;

namespace MarkMyWord.Reading;

/// <summary>
/// Reconstructs markdown from a Word document structure.
/// </summary>
public class MarkdownReconstructor
{
    /// <summary>
    /// Reconstructs markdown from a document structure.
    /// </summary>
    /// <param name="structure">The Word document structure.</param>
    /// <returns>The reconstructed markdown string.</returns>
    public string Reconstruct(WordDocumentStructure structure)
    {
        var result = ReconstructWithLineMapping(structure);
        return result.Content;
    }

    /// <summary>
    /// Reconstructs markdown with line number mappings.
    /// </summary>
    /// <param name="structure">The Word document structure.</param>
    /// <returns>A MarkdownDocument with content and line mappings.</returns>
    public MarkdownDocument ReconstructWithLineMapping(WordDocumentStructure structure)
    {
        if (structure == null)
            throw new ArgumentNullException(nameof(structure));

        var sb = new StringBuilder();
        var lineToElementMap = new Dictionary<int, DocumentElement>();
        int currentLine = 1;

        for (int i = 0; i < structure.Elements.Count; i++)
        {
            var element = structure.Elements[i];
            var markdown = element.ToMarkdown();

            // Track start line
            element.StartLine = currentLine;

            // Add markdown content
            if (!string.IsNullOrEmpty(markdown))
            {
                sb.AppendLine(markdown);

                // Count lines in this element
                var lines = markdown.Split('\n').Length;
                currentLine += lines;

                // Track end line
                element.EndLine = currentLine - 1;

                // Map each line to this element
                for (int line = element.StartLine; line <= element.EndLine; line++)
                {
                    lineToElementMap[line] = element;
                }
            }
            else
            {
                // Empty element (like blank paragraph)
                element.EndLine = currentLine;
                lineToElementMap[currentLine] = element;
                currentLine++;
            }

            // Add blank line between elements (except for consecutive list items)
            bool isCurrentList = element is ListElement;
            bool isNextList = i + 1 < structure.Elements.Count && structure.Elements[i + 1] is ListElement;

            if (!isCurrentList || !isNextList)
            {
                // Add blank line unless both current and next are list items
                if (i < structure.Elements.Count - 1 && !string.IsNullOrEmpty(markdown))
                {
                    sb.AppendLine();
                    currentLine++;
                }
            }
        }

        return new MarkdownDocument
        {
            Content = sb.ToString().TrimEnd(),
            LineToElementMap = lineToElementMap,
            Elements = structure.Elements
        };
    }
}

/// <summary>
/// Represents a markdown document with line-to-element mappings.
/// </summary>
public class MarkdownDocument
{
    /// <summary>
    /// The markdown content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Maps line numbers (1-based) to document elements.
    /// </summary>
    public Dictionary<int, DocumentElement> LineToElementMap { get; set; } = new();

    /// <summary>
    /// The list of document elements in order.
    /// </summary>
    public List<DocumentElement> Elements { get; set; } = new();

    /// <summary>
    /// Gets the element at a specific line number.
    /// </summary>
    /// <param name="lineNumber">The line number (1-based).</param>
    /// <returns>The document element at that line, or null if not found.</returns>
    public DocumentElement? GetElementAtLine(int lineNumber)
    {
        return LineToElementMap.TryGetValue(lineNumber, out var element) ? element : null;
    }

    /// <summary>
    /// Gets all elements within a line range.
    /// </summary>
    /// <param name="startLine">The start line (1-based, inclusive).</param>
    /// <param name="endLine">The end line (1-based, inclusive).</param>
    /// <returns>The list of elements within the range.</returns>
    public List<DocumentElement> GetElementsInRange(int startLine, int endLine)
    {
        return Elements
            .Where(e => e.StartLine <= endLine && e.EndLine >= startLine)
            .ToList();
    }
}
