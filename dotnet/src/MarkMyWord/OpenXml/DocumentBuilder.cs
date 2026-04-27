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

    public WordprocessingDocument WordDocument { get; private set; }
    public MainDocumentPart MainDocumentPart { get; private set; }
    public Body Body { get; private set; }

    private NumberingDefinitionsPart? _numberingPart;
    private int _nextNumberingId = 1;
    private readonly Dictionary<string, string> _hyperlinkRelationships = new();

    /// <summary>
    /// Initializes a new instance of the DocumentBuilder class.
    /// </summary>
    /// <param name="outputStream">The stream to write the document to.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after disposal.</param>
    public DocumentBuilder(Stream outputStream, bool leaveOpen = false)
    {
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
        _leaveOpen = leaveOpen;

        WordDocument = WordprocessingDocument.Create(_outputStream, WordprocessingDocumentType.Document, autoSave: false);
        MainDocumentPart = WordDocument.AddMainDocumentPart();
        MainDocumentPart.Document = new Document();
        Body = MainDocumentPart.Document.AppendChild(new Body());

        CreateFontTable();
    }

    /// <summary>
    /// Creates a font table part declaring fonts used in the document.
    /// Notably declares "Segoe UI Emoji" so Word can properly resolve it
    /// for supplementary-plane emoji characters (color rendering).
    /// </summary>
    private void CreateFontTable()
    {
        var fontTablePart = MainDocumentPart.AddNewPart<FontTablePart>();

        var fonts = new Fonts();

        fonts.AppendChild(new Font(
            new FontCharSet { Val = "00" },
            new FontFamily { Val = FontFamilyValues.Swiss },
            new Pitch { Val = FontPitchValues.Variable }
        )
        { Name = "Calibri" });

        fonts.AppendChild(new Font(
            new FontCharSet { Val = "00" },
            new FontFamily { Val = FontFamilyValues.Modern },
            new Pitch { Val = FontPitchValues.Fixed }
        )
        { Name = "Consolas" });

        fonts.AppendChild(new Font(
            new FontCharSet { Val = "00" },
            new FontFamily { Val = FontFamilyValues.Auto },
            new Pitch { Val = FontPitchValues.Variable }
        )
        { Name = EmojiSegmenter.EmojiFontName });

        fontTablePart.Fonts = fonts;
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
    /// Creates a new numbering instance that references an existing abstract numbering definition.
    /// Adds a level override to restart numbering from 1 for each new list.
    /// </summary>
    /// <param name="abstractNumId">The abstract numbering definition ID to reference.</param>
    /// <param name="level">The list level (0-8).</param>
    /// <returns>The numbering ID to use in paragraph properties.</returns>
    public int CreateNumberingInstance(int abstractNumId, int level)
    {
        var numberingPart = GetOrCreateNumberingPart();
        var numbering = numberingPart.Numbering;

        var numId = _nextNumberingId++;
        var numberingInstance = new NumberingInstance(
            new AbstractNumId { Val = abstractNumId }
        )
        { NumberID = numId };

        // Override the start value so each separate list restarts at 1
        numberingInstance.AppendChild(new LevelOverride
        {
            LevelIndex = level,
            StartOverrideNumberingValue = new StartOverrideNumberingValue { Val = 1 }
        });

        numbering.AppendChild(numberingInstance);
        return numId;
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
