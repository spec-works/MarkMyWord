using DocumentFormat.OpenXml;

namespace MarkMyWord.Reading;

/// <summary>
/// Represents an element from a Word document with its corresponding markdown representation.
/// </summary>
public abstract class DocumentElement
{
    /// <summary>
    /// The OpenXML element from the Word document.
    /// </summary>
    public OpenXmlElement XmlElement { get; set; }

    /// <summary>
    /// The starting line number in the markdown representation (1-based).
    /// </summary>
    public int StartLine { get; set; }

    /// <summary>
    /// The ending line number in the markdown representation (1-based, inclusive).
    /// </summary>
    public int EndLine { get; set; }

    /// <summary>
    /// Initializes a new instance of the DocumentElement class.
    /// </summary>
    /// <param name="xmlElement">The OpenXML element.</param>
    protected DocumentElement(OpenXmlElement xmlElement)
    {
        XmlElement = xmlElement ?? throw new ArgumentNullException(nameof(xmlElement));
    }

    /// <summary>
    /// Converts this element to its markdown representation.
    /// </summary>
    /// <returns>The markdown string for this element.</returns>
    public abstract string ToMarkdown();
}
