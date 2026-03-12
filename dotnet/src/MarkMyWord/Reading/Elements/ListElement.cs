using DocumentFormat.OpenXml;
using System.Text;

namespace MarkMyWord.Reading.Elements;

/// <summary>
/// Represents a list item element from a Word document.
/// </summary>
public class ListElement : DocumentElement
{
    /// <summary>
    /// Whether this is an ordered list item.
    /// </summary>
    public bool IsOrdered { get; set; }

    /// <summary>
    /// The indentation level (0 for top level).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// The text content of the list item.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// The numbering ID from the Word document.
    /// </summary>
    public int? NumberingId { get; set; }

    /// <summary>
    /// Initializes a new instance of the ListElement class.
    /// </summary>
    /// <param name="xmlElement">The OpenXML paragraph element with list properties.</param>
    /// <param name="isOrdered">Whether this is an ordered list.</param>
    /// <param name="level">The indentation level.</param>
    /// <param name="content">The text content.</param>
    public ListElement(OpenXmlElement xmlElement, bool isOrdered, int level, string content) : base(xmlElement)
    {
        IsOrdered = isOrdered;
        Level = Math.Max(0, level);
        Content = content ?? string.Empty;
    }

    /// <summary>
    /// Converts this list item to markdown.
    /// </summary>
    public override string ToMarkdown()
    {
        var indent = new string(' ', Level * 2); // 2 spaces per level
        var marker = IsOrdered ? "1." : "-";
        return $"{indent}{marker} {Content}";
    }
}
