using DocumentFormat.OpenXml;

namespace MarkMyWord.Reading.Elements;

/// <summary>
/// Represents a heading element from a Word document.
/// </summary>
public class HeadingElement : DocumentElement
{
    /// <summary>
    /// The heading level (1-6).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// The text content of the heading.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Initializes a new instance of the HeadingElement class.
    /// </summary>
    /// <param name="xmlElement">The OpenXML paragraph element with heading style.</param>
    /// <param name="level">The heading level (1-6).</param>
    /// <param name="content">The text content.</param>
    public HeadingElement(OpenXmlElement xmlElement, int level, string content) : base(xmlElement)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "Heading level must be between 1 and 6.");

        Level = level;
        Content = content ?? string.Empty;
    }

    /// <summary>
    /// Converts this heading to markdown (ATX style: # Heading).
    /// </summary>
    public override string ToMarkdown()
    {
        return $"{new string('#', Level)} {Content}";
    }
}
