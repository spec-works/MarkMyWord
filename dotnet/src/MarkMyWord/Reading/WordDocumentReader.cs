using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MarkMyWord.Reading.Elements;
using System.Text;

namespace MarkMyWord.Reading;

/// <summary>
/// Reads existing Word documents and creates a structured representation.
/// </summary>
public class WordDocumentReader
{
    /// <summary>
    /// Reads a Word document from a file path.
    /// </summary>
    /// <param name="docxPath">Path to the .docx file.</param>
    /// <returns>The structured document representation.</returns>
    public WordDocumentStructure Read(string docxPath)
    {
        if (string.IsNullOrEmpty(docxPath))
            throw new ArgumentException("Document path cannot be null or empty.", nameof(docxPath));

        if (!File.Exists(docxPath))
            throw new FileNotFoundException("Document file not found.", docxPath);

        using var stream = File.OpenRead(docxPath);
        return Read(stream);
    }

    /// <summary>
    /// Reads a Word document from a stream.
    /// </summary>
    /// <param name="docxStream">Stream containing the .docx file.</param>
    /// <returns>The structured document representation.</returns>
    public WordDocumentStructure Read(Stream docxStream)
    {
        if (docxStream == null)
            throw new ArgumentNullException(nameof(docxStream));

        // Copy stream to memory to avoid issues with non-seekable streams
        var memoryStream = new MemoryStream();
        docxStream.CopyTo(memoryStream);
        memoryStream.Position = 0;

        using var document = WordprocessingDocument.Open(memoryStream, false);

        if (document.MainDocumentPart == null)
            throw new InvalidOperationException("Document does not have a MainDocumentPart.");

        var elements = new List<DocumentElement>();
        var body = document.MainDocumentPart.Document.Body;

        if (body != null)
        {
            foreach (var element in body.Elements())
            {
                var docElement = ParseElement(element);
                if (docElement != null)
                {
                    elements.Add(docElement);
                }
            }
        }

        return new WordDocumentStructure
        {
            Elements = elements,
            OriginalStream = memoryStream
        };
    }

    /// <summary>
    /// Parses an OpenXML element into a DocumentElement.
    /// </summary>
    private DocumentElement? ParseElement(OpenXmlElement element)
    {
        return element switch
        {
            Paragraph para => ParseParagraph(para),
            Table table => ParseTable(table),
            _ => null
        };
    }

    /// <summary>
    /// Parses a paragraph element.
    /// </summary>
    private DocumentElement? ParseParagraph(Paragraph paragraph)
    {
        var text = ExtractText(paragraph);

        // Check if empty paragraph
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ParagraphElement(paragraph, string.Empty);
        }

        // Check if it's a heading
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (!string.IsNullOrEmpty(styleId) && styleId.StartsWith("Heading"))
        {
            if (int.TryParse(styleId.Substring(7), out int level) && level >= 1 && level <= 6)
            {
                return new HeadingElement(paragraph, level, text);
            }
        }

        // Check if it's a list item
        var numberingProperties = paragraph.ParagraphProperties?.NumberingProperties;
        if (numberingProperties != null)
        {
            var numId = numberingProperties.NumberingId?.Val?.Value;
            var ilvl = numberingProperties.NumberingLevelReference?.Val?.Value ?? 0;

            if (numId.HasValue)
            {
                // Determine if ordered or unordered based on numbering format
                bool isOrdered = IsOrderedList(paragraph, numId.Value);
                return new ListElement(paragraph, isOrdered, ilvl, text)
                {
                    NumberingId = numId.Value
                };
            }
        }

        // Check if it's a quote (left border + shading/indentation)
        var borders = paragraph.ParagraphProperties?.ParagraphBorders;
        var shading = paragraph.ParagraphProperties?.Shading;
        if (borders?.LeftBorder != null ||
            (shading != null && paragraph.ParagraphProperties?.Indentation?.Left != null))
        {
            return new QuoteElement(paragraph, text);
        }

        // Check if it's a code block (shading + specific font)
        if (IsCodeBlock(paragraph))
        {
            return new CodeBlockElement(paragraph, text);
        }

        // Default to paragraph
        return new ParagraphElement(paragraph, text);
    }

    /// <summary>
    /// Parses a table element (simplified - treat as text for now).
    /// </summary>
    private DocumentElement? ParseTable(Table table)
    {
        // For now, we'll represent tables as a special paragraph element
        // A more complete implementation would have a dedicated TableElement
        var text = ExtractTextFromTable(table);
        return new ParagraphElement(table, text);
    }

    /// <summary>
    /// Extracts text content from a paragraph.
    /// </summary>
    private string ExtractText(Paragraph paragraph)
    {
        var sb = new StringBuilder();

        foreach (var run in paragraph.Elements<Run>())
        {
            foreach (var text in run.Elements<Text>())
            {
                sb.Append(text.Text);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Extracts text from a table.
    /// </summary>
    private string ExtractTextFromTable(Table table)
    {
        var sb = new StringBuilder();

        foreach (var row in table.Elements<TableRow>())
        {
            foreach (var cell in row.Elements<TableCell>())
            {
                foreach (var para in cell.Elements<Paragraph>())
                {
                    sb.Append(ExtractText(para));
                    sb.Append(" | ");
                }
            }
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Determines if a list is ordered based on its numbering format.
    /// </summary>
    private bool IsOrderedList(Paragraph paragraph, int numId)
    {
        // Try to get numbering part and check format
        var document = paragraph.Ancestors<Document>().FirstOrDefault();
        var mainPart = document?.OpenXmlPart as MainDocumentPart;
        if (mainPart?.NumberingDefinitionsPart?.Numbering != null)
        {
            var numbering = mainPart.NumberingDefinitionsPart.Numbering;
            var numInstance = numbering.Elements<NumberingInstance>()
                .FirstOrDefault(ni => ni.NumberID?.Value == numId);

            if (numInstance != null)
            {
                var abstractNumId = numInstance.AbstractNumId?.Val?.Value;
                if (abstractNumId.HasValue)
                {
                    var abstractNum = numbering.Elements<AbstractNum>()
                        .FirstOrDefault(an => an.AbstractNumberId?.Value == abstractNumId.Value);

                    if (abstractNum != null)
                    {
                        var level = abstractNum.Elements<Level>().FirstOrDefault();
                        var format = level?.NumberingFormat?.Val?.Value;

                        // Decimal, upperRoman, lowerRoman, upperLetter, lowerLetter are ordered
                        // Bullet is unordered
                        return format != NumberFormatValues.Bullet;
                    }
                }
            }
        }

        // Default to unordered if we can't determine
        return false;
    }

    /// <summary>
    /// Checks if a paragraph is a code block.
    /// </summary>
    private bool IsCodeBlock(Paragraph paragraph)
    {
        // Code blocks typically have:
        // 1. Shading with specific background color
        // 2. Monospace font (Consolas, Courier New, etc.)

        var shading = paragraph.ParagraphProperties?.Shading;
        if (shading == null)
            return false;

        // Check for monospace font in runs
        foreach (var run in paragraph.Elements<Run>())
        {
            var font = run.RunProperties?.RunFonts?.Ascii?.Value;
            if (font != null && IsMonospaceFont(font))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a font is a monospace font typically used for code.
    /// </summary>
    private bool IsMonospaceFont(string fontName)
    {
        var monospaceFonts = new[] { "Consolas", "Courier New", "Courier", "Monaco", "Menlo", "Source Code Pro" };
        return monospaceFonts.Any(f => fontName.Equals(f, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Represents the structure of a Word document.
/// </summary>
public class WordDocumentStructure : IDisposable
{
    /// <summary>
    /// The list of document elements.
    /// </summary>
    public List<DocumentElement> Elements { get; set; } = new();

    /// <summary>
    /// The original document stream (for later modification).
    /// </summary>
    public MemoryStream? OriginalStream { get; set; }

    /// <summary>
    /// Disposes the original stream.
    /// </summary>
    public void Dispose()
    {
        OriginalStream?.Dispose();
    }
}
