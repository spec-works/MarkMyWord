using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkMyWord.Configuration;
using MarkMyWord.Converters.BlockRenderers;
using MarkMyWord.Converters.InlineRenderers;
using MarkMyWord.OpenXml;

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

    public DocumentBuilder DocumentBuilder => _documentBuilder;
    public StyleManager StyleManager => _styleManager;
    public ListManager ListManager => _listManager;
    public ConversionOptions Options => _options;

    public OpenXmlRenderer(Stream outputStream, ConversionOptions? options = null)
    {
        _options = options ?? new ConversionOptions();
        _documentBuilder = new DocumentBuilder(outputStream, leaveOpen: true);
        _styleManager = new StyleManager(_options.Styles);
        _listManager = new ListManager(_documentBuilder, _options.Styles);

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
