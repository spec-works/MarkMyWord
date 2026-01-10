using DocumentFormat.OpenXml;

namespace MarkMyWord.Reading.Elements;

/// <summary>
/// Represents a block quote element from a Word document.
/// </summary>
public class QuoteElement : DocumentElement
{
    /// <summary>
    /// The quoted text content.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Initializes a new instance of the QuoteElement class.
    /// </summary>
    /// <param name="xmlElement">The OpenXML paragraph element with quote styling.</param>
    /// <param name="content">The quoted text content.</param>
    public QuoteElement(OpenXmlElement xmlElement, string content) : base(xmlElement)
    {
        Content = content ?? string.Empty;
    }

    /// <summary>
    /// Converts this quote to markdown (> prefix).
    /// </summary>
    public override string ToMarkdown()
    {
        // Handle multi-line quotes by prefixing each line with >
        var lines = Content.Split('\n');
        return string.Join("\n", lines.Select(line => $"> {line}"));
    }
}
