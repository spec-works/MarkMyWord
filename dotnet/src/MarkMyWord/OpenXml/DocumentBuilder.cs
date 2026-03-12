using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MarkMyWord.OpenXml;

/// <summary>
/// Manages the creation and manipulation of OpenXML Word documents.
/// </summary>
public class DocumentBuilder : IDisposable
{
    private readonly Stream _outputStream;
    private readonly bool _leaveOpen;
    private bool _disposed;
    private readonly bool _isExistingDocument;

    public WordprocessingDocument WordDocument { get; private set; }
    public MainDocumentPart MainDocumentPart { get; private set; }
    public Body Body { get; private set; }

    private NumberingDefinitionsPart? _numberingPart;
    private int _nextNumberingId = 1;
    private readonly Dictionary<string, string> _hyperlinkRelationships = new();

    /// <summary>
    /// Initializes a new instance of the DocumentBuilder class for creating a new document.
    /// </summary>
    /// <param name="outputStream">The stream to write the document to.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after disposal.</param>
    public DocumentBuilder(Stream outputStream, bool leaveOpen = false)
    {
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        _leaveOpen = leaveOpen;
        _isExistingDocument = false;

        WordDocument = WordprocessingDocument.Create(_outputStream, WordprocessingDocumentType.Document, autoSave: false);
        MainDocumentPart = WordDocument.AddMainDocumentPart();
        MainDocumentPart.Document = new Document();
        Body = MainDocumentPart.Document.AppendChild(new Body());
    }

    /// <summary>
    /// Initializes a new instance of the DocumentBuilder class for modifying an existing document.
    /// </summary>
    /// <param name="inputStream">The stream containing the existing document.</param>
    /// <param name="outputStream">The stream to write the modified document to.</param>
    /// <param name="leaveOpen">Whether to leave the output stream open after disposal.</param>
    public DocumentBuilder(Stream inputStream, Stream outputStream, bool leaveOpen = false)
    {
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream));

        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        _leaveOpen = leaveOpen;
        _isExistingDocument = true;

        // Copy input to output so we can modify it
        inputStream.CopyTo(_outputStream);
        _outputStream.Position = 0;

        // Open the copied document for editing
        WordDocument = WordprocessingDocument.Open(_outputStream, true);
        MainDocumentPart = WordDocument.MainDocumentPart ?? throw new InvalidOperationException("Document does not have a MainDocumentPart.");
        Body = MainDocumentPart.Document?.Body ?? throw new InvalidOperationException("Document does not have a Body.");

        // Load existing numbering part if it exists
        _numberingPart = MainDocumentPart.NumberingDefinitionsPart;
        if (_numberingPart != null)
        {
            // Find the highest numbering ID to avoid conflicts
            var numbering = _numberingPart.Numbering;
            if (numbering != null)
            {
                var maxNumId = numbering.Elements<NumberingInstance>()
                    .Select(ni => ni.NumberID?.Value ?? 0)
                    .DefaultIfEmpty(0)
                    .Max();
                _nextNumberingId = maxNumId + 1;
            }
        }
    }

    /// <summary>
    /// Sets document metadata properties.
    /// </summary>
    /// <param name="title">Document title.</param>
    /// <param name="author">Document author.</param>
    /// <param name="subject">Document subject.</param>
    public void SetDocumentProperties(string? title = null, string? author = null, string? subject = null)
    {
        var props = WordDocument.PackageProperties;

        if (!string.IsNullOrEmpty(title))
            props.Title = title;

        if (!string.IsNullOrEmpty(author))
            props.Creator = author;

        if (!string.IsNullOrEmpty(subject))
            props.Subject = subject;
    }

    /// <summary>
    /// Adds a paragraph to the document body.
    /// </summary>
    /// <param name="text">Optional text content for the paragraph.</param>
    /// <param name="configureProperties">Optional action to configure paragraph properties.</param>
    /// <returns>The created paragraph.</returns>
    public Paragraph AddParagraph(string? text = null, Action<ParagraphProperties>? configureProperties = null)
    {
        var paragraph = new Paragraph();

        if (configureProperties != null)
        {
            var props = new ParagraphProperties();
            configureProperties(props);
            paragraph.ParagraphProperties = props;
        }

        if (!string.IsNullOrEmpty(text))
        {
            var run = new Run();
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            paragraph.AppendChild(run);
        }

        Body.AppendChild(paragraph);
        return paragraph;
    }

    /// <summary>
    /// Gets or creates the numbering definitions part for lists.
    /// </summary>
    /// <returns>The numbering definitions part.</returns>
    public NumberingDefinitionsPart GetOrCreateNumberingPart()
    {
        if (_numberingPart == null)
        {
            _numberingPart = MainDocumentPart.AddNewPart<NumberingDefinitionsPart>();
            _numberingPart.Numbering = new Numbering();
        }

        return _numberingPart;
    }

    /// <summary>
    /// Creates or gets a numbering ID for a specific list format and level.
    /// </summary>
    /// <param name="format">The numbering format (e.g., bullet, decimal).</param>
    /// <param name="level">The list level (0-8).</param>
    /// <returns>The numbering ID to use in paragraph properties.</returns>
    public int GetOrCreateNumberingId(NumberFormatValues format, int level)
    {
        var numberingPart = GetOrCreateNumberingPart();
        var numbering = numberingPart.Numbering;

        // For simplicity, we'll create separate numbering definitions for bullets vs numbered
        // In a more sophisticated implementation, we'd cache and reuse these
        int abstractNumId = format == NumberFormatValues.Bullet ? 1 : 2;

        // Check if abstract num exists
        var abstractNum = numbering.Elements<AbstractNum>().FirstOrDefault(an => an.AbstractNumberId?.Value == abstractNumId);

        if (abstractNum == null)
        {
            abstractNum = CreateAbstractNum(abstractNumId, format);
            numbering.AppendChild(abstractNum);
        }

        // Create a new numbering instance
        var numId = _nextNumberingId++;
        var numberingInstance = new NumberingInstance(
            new AbstractNumId { Val = abstractNumId }
        )
        { NumberID = numId };

        numbering.AppendChild(numberingInstance);
        return numId;
    }

    /// <summary>
    /// Creates an abstract numbering definition.
    /// </summary>
    private AbstractNum CreateAbstractNum(int abstractNumId, NumberFormatValues format)
    {
        var abstractNum = new AbstractNum { AbstractNumberId = abstractNumId };

        // Create levels 0-8
        for (int i = 0; i <= 8; i++)
        {
            var level = new Level
            {
                LevelIndex = i,
                StartNumberingValue = new StartNumberingValue { Val = 1 }
            };

            if (format == NumberFormatValues.Bullet)
            {
                // Use bullet character with Calibri/Arial font (not Symbol font)
                level.NumberingFormat = new NumberingFormat { Val = NumberFormatValues.Bullet };
                level.LevelText = new LevelText { Val = "•" };
                level.LevelJustification = new LevelJustification { Val = LevelJustificationValues.Left };
                level.NumberingSymbolRunProperties = new NumberingSymbolRunProperties(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri", ComplexScript = "Calibri" }
                );
            }
            else
            {
                // Use decimal format for numbered lists
                level.NumberingFormat = new NumberingFormat { Val = NumberFormatValues.Decimal };
                level.LevelText = new LevelText { Val = $"%{i + 1}." };
                level.LevelJustification = new LevelJustification { Val = LevelJustificationValues.Left };
            }

            level.PreviousParagraphProperties = new PreviousParagraphProperties(
                new Indentation
                {
                    Left = (720 * (i + 1)).ToString(), // 0.5 inch per level
                    Hanging = "360" // 0.25 inch hanging indent
                }
            );

            abstractNum.AppendChild(level);
        }

        return abstractNum;
    }

    /// <summary>
    /// Adds a hyperlink relationship and returns the relationship ID.
    /// </summary>
    /// <param name="uri">The URI to link to.</param>
    /// <returns>The relationship ID.</returns>
    public string AddHyperlinkRelationship(string uri)
    {
        if (_hyperlinkRelationships.TryGetValue(uri, out var existingId))
        {
            return existingId;
        }

        var relationship = MainDocumentPart.AddHyperlinkRelationship(new Uri(uri, UriKind.RelativeOrAbsolute), true);
        _hyperlinkRelationships[uri] = relationship.Id;
        return relationship.Id;
    }

    /// <summary>
    /// Adds an image part to the document.
    /// </summary>
    /// <param name="imageData">The image data as byte array.</param>
    /// <param name="contentType">The image content type (e.g., "image/png").</param>
    /// <returns>The created image part.</returns>
    public ImagePart AddImagePart(byte[] imageData, string contentType)
    {
        var partType = contentType.ToLowerInvariant() switch
        {
            "image/png" => ImagePartType.Png,
            "image/jpeg" or "image/jpg" => ImagePartType.Jpeg,
            "image/gif" => ImagePartType.Gif,
            "image/bmp" => ImagePartType.Bmp,
            "image/tiff" => ImagePartType.Tiff,
            "image/svg+xml" => ImagePartType.Svg,
            _ => ImagePartType.Png
        };

        var imagePart = MainDocumentPart.AddImagePart(partType);
        using (var stream = new MemoryStream(imageData))
        {
            imagePart.FeedData(stream);
        }

        return imagePart;
    }

    /// <summary>
    /// Gets the relationship ID for an image part.
    /// </summary>
    public string GetImageRelationshipId(ImagePart imagePart)
    {
        return MainDocumentPart.GetIdOfPart(imagePart);
    }

    /// <summary>
    /// Saves the document to the output stream.
    /// </summary>
    public void Save()
    {
        WordDocument.Save();
    }

    /// <summary>
    /// Removes an element from the document.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    public void RemoveElement(OpenXmlElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        element.Remove();
    }

    /// <summary>
    /// Inserts an element before a reference element.
    /// </summary>
    /// <param name="newElement">The element to insert.</param>
    /// <param name="referenceElement">The reference element.</param>
    public void InsertElementBefore(OpenXmlElement newElement, OpenXmlElement referenceElement)
    {
        if (newElement == null)
            throw new ArgumentNullException(nameof(newElement));
        if (referenceElement == null)
            throw new ArgumentNullException(nameof(referenceElement));

        referenceElement.Parent?.InsertBefore(newElement, referenceElement);
    }

    /// <summary>
    /// Inserts an element after a reference element.
    /// </summary>
    /// <param name="newElement">The element to insert.</param>
    /// <param name="referenceElement">The reference element.</param>
    public void InsertElementAfter(OpenXmlElement newElement, OpenXmlElement referenceElement)
    {
        if (newElement == null)
            throw new ArgumentNullException(nameof(newElement));
        if (referenceElement == null)
            throw new ArgumentNullException(nameof(referenceElement));

        referenceElement.Parent?.InsertAfter(newElement, referenceElement);
    }

    /// <summary>
    /// Replaces an existing element with a new element.
    /// </summary>
    /// <param name="oldElement">The element to replace.</param>
    /// <param name="newElement">The new element.</param>
    public void ReplaceElement(OpenXmlElement oldElement, OpenXmlElement newElement)
    {
        if (oldElement == null)
            throw new ArgumentNullException(nameof(oldElement));
        if (newElement == null)
            throw new ArgumentNullException(nameof(newElement));

        oldElement.Parent?.ReplaceChild(newElement, oldElement);
    }

    /// <summary>
    /// Disposes the document and closes the stream if not configured to leave it open.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        WordDocument?.Dispose();

        if (!_leaveOpen)
        {
            _outputStream?.Dispose();
        }

        _disposed = true;
    }
}
