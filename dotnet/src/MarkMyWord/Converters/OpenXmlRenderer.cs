using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkMyWord.Configuration;
using MarkMyWord.Converters.BlockRenderers;
using MarkMyWord.Converters.InlineRenderers;
using MarkMyWord.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MarkMyWord.Converters;

/// <summary>
/// Renders Markdig AST to OpenXML format.
/// </summary>
public class OpenXmlRenderer : RendererBase, IDisposable
{
    private readonly DocumentBuilder _documentBuilder;
    private readonly StyleManager _styleManager;
    private readonly ListManager _listManager;
    private readonly ConversionOptions _options;
    private readonly List<(Block SourceBlock, Paragraph Paragraph)> _paragraphMappings = [];

    public DocumentBuilder DocumentBuilder => _documentBuilder;
    public StyleManager StyleManager => _styleManager;
    public ListManager ListManager => _listManager;
    public ConversionOptions Options => _options;
    internal IReadOnlyList<(Block SourceBlock, Paragraph Paragraph)> ParagraphMappings => _paragraphMappings;

    public OpenXmlRenderer(Stream outputStream, ConversionOptions? options = null)
        : this(outputStream, options, null)
    {
    }

    internal OpenXmlRenderer(Stream outputStream, ConversionOptions? options, string? documentTitleOverride)
    {
        _options = options ?? new ConversionOptions();
        _documentBuilder = new DocumentBuilder(outputStream, leaveOpen: true);
        _styleManager = new StyleManager(_options.Styles);
        _listManager = new ListManager(_documentBuilder, _options.Styles);

        // Set document metadata if provided
        var documentTitle = _options.DocumentTitle ?? documentTitleOverride;
        if (documentTitle != null || _options.Author != null || _options.Subject != null)
        {
            _documentBuilder.SetDocumentProperties(documentTitle, _options.Author, _options.Subject);
        }

        // Register block renderers
        ObjectRenderers.Add(new HeadingRenderer());
        ObjectRenderers.Add(new ParagraphRenderer());
        ObjectRenderers.Add(new CodeBlockRenderer());
        ObjectRenderers.Add(new ThematicBreakRenderer());
        ObjectRenderers.Add(new QuoteBlockRenderer());
        ObjectRenderers.Add(new ListRenderer());
        ObjectRenderers.Add(new TableRenderer());

        // Register inline renderers
        ObjectRenderers.Add(new LiteralInlineRenderer());
        ObjectRenderers.Add(new EmphasisInlineRenderer());
        ObjectRenderers.Add(new CodeInlineRenderer());
        ObjectRenderers.Add(new LineBreakInlineRenderer());
        ObjectRenderers.Add(new LinkInlineRenderer());
    }

    internal void TrackParagraph(Block sourceBlock, Paragraph paragraph)
    {
        _paragraphMappings.Add((sourceBlock, paragraph));
    }

    /// <summary>
    /// Renders a markdown object to OpenXML.
    /// </summary>
    public override object Render(MarkdownObject markdownObject)
    {
        Write(markdownObject);
        return null!;
    }

    /// <summary>
    /// Finalizes the document by applying styles and saving.
    /// </summary>
    public void FinalizeDocument()
    {
        _styleManager.ApplyStylesToDocument(_documentBuilder.WordDocument);
        _documentBuilder.Save();
    }

    /// <summary>
    /// Disposes the renderer and underlying document builder.
    /// </summary>
    public void Dispose()
    {
        _documentBuilder?.Dispose();
    }
}
