using DocumentFormat.OpenXml;
using System.Text;

namespace MarkMyWord.Reading.Elements;

/// <summary>
/// Represents a paragraph element from a Word document.
/// </summary>
public class ParagraphElement : DocumentElement
{
    /// <summary>
    /// The text content of the paragraph.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Whether this paragraph is empty.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Initializes a new instance of the ParagraphElement class.
    /// </summary>
    /// <param name="xmlElement">The OpenXML paragraph element.</param>
    /// <param name="content">The text content.</param>
    public ParagraphElement(OpenXmlElement xmlElement, string content) : base(xmlElement)
    {
        Content = content ?? string.Empty;
    }

    /// <summary>
    /// Converts this paragraph to markdown.
    /// </summary>
    public override string ToMarkdown()
    {
        return Content;
    }
}
