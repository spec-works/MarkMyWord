using DocumentFormat.OpenXml;

namespace MarkMyWord.Reading.Elements;

/// <summary>
/// Represents a code block element from a Word document.
/// </summary>
public class CodeBlockElement : DocumentElement
{
    /// <summary>
    /// The code content.
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// The programming language for syntax highlighting (optional).
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Initializes a new instance of the CodeBlockElement class.
    /// </summary>
    /// <param name="xmlElement">The OpenXML element(s) representing the code block.</param>
    /// <param name="content">The code content.</param>
    /// <param name="language">The programming language (optional).</param>
    public CodeBlockElement(OpenXmlElement xmlElement, string content, string? language = null) : base(xmlElement)
    {
        Content = content ?? string.Empty;
        Language = language;
    }

    /// <summary>
    /// Converts this code block to markdown (fenced code block: ```language).
    /// </summary>
    public override string ToMarkdown()
    {
        var fence = "```";
        var lang = !string.IsNullOrWhiteSpace(Language) ? Language : string.Empty;
        return $"{fence}{lang}\n{Content}\n{fence}";
    }
}
