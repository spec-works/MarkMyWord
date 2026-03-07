using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkMyWord.Configuration;
using MarkMyWord.Diagrams;
using MarkMyWord.SyntaxHighlighting;
using SkiaSharp;
using Svg.Skia;
using System.Text;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for code blocks (both fenced and indented).
/// </summary>
public class CodeBlockRenderer : OpenXmlObjectRenderer<CodeBlock>
{
    private readonly SyntaxHighlighterFactory _highlighterFactory = new();
    private MermaidRenderer? _mermaidRenderer;

    private MermaidRenderer GetMermaidRenderer(ConversionOptions options)
    {
        _mermaidRenderer ??= new MermaidRenderer(options.Theme);
        return _mermaidRenderer;
    }

    protected override void Write(OpenXmlRenderer renderer, CodeBlock obj)
    {
        // Extract language identifier if this is a fenced code block
        string? language = null;
        if (obj is FencedCodeBlock fencedBlock && !string.IsNullOrEmpty(fencedBlock.Info))
        {
            language = fencedBlock.Info.Trim();
        }

        // Check if this is a Mermaid diagram (before syntax highlighting check)
        if (renderer.Options.EnableMermaidDiagrams &&
            MermaidRenderer.IsMermaidLanguage(language))
        {
            RenderMermaidDiagram(renderer, obj);
            return;  // Exit early - diagram has been rendered
        }

        // Determine if syntax highlighting should be used
        bool useSyntaxHighlighting = renderer.Options.EnableSyntaxHighlighting &&
                                      !string.IsNullOrWhiteSpace(language) &&
                                      _highlighterFactory.IsLanguageSupported(language);

        // Render each line of code as a separate paragraph
        var lines = obj.Lines.Lines;
        if (lines != null)
        {
            // Find the last non-empty line to strip trailing empty lines
            int lastNonEmptyLine = lines.Length - 1;
            while (lastNonEmptyLine >= 0 && string.IsNullOrWhiteSpace(lines[lastNonEmptyLine].Slice.ToString()))
            {
                lastNonEmptyLine--;
            }

            // Render lines up to the last non-empty line
            for (int i = 0; i <= lastNonEmptyLine; i++)
            {
                var line = lines[i];
                var text = line.Slice.ToString();

                var paragraph = renderer.DocumentBuilder.AddParagraph();
                paragraph.ParagraphProperties = renderer.StyleManager.GetCodeBlockProperties();

                if (useSyntaxHighlighting)
                {
                    // Use syntax highlighting - create multiple runs with colors
                    var tokens = _highlighterFactory.Highlight(text, language!);

                    foreach (var token in tokens)
                    {
                        var run = new Run(
                            renderer.StyleManager.GetSyntaxTokenRunProperties(token.Type),
                            new Text(token.Text) { Space = SpaceProcessingModeValues.Preserve }
                        );
                        paragraph.AppendChild(run);
                    }
                }
                else
                {
                    // Plain text rendering (original behavior)
                    var run = new Run(
                        renderer.StyleManager.GetCodeRunProperties(),
                        new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                    );
                    paragraph.AppendChild(run);
                }
            }
        }
    }

    /// <summary>
    /// Renders a Mermaid diagram as an embedded SVG image.
    /// Falls back to code block rendering if conversion fails.
    /// </summary>
    private void RenderMermaidDiagram(OpenXmlRenderer renderer, CodeBlock obj)
    {
        try
        {
            // Extract the Mermaid code from the code block
            string mermaidCode = ExtractCodeContent(obj);

            if (string.IsNullOrWhiteSpace(mermaidCode))
            {
                RenderAsCodeBlockFallback(renderer, obj, "[Error: Empty Mermaid diagram]");
                return;
            }

            // Render Mermaid to SVG using Naiad (pure .NET, no browser)
            string? svg = GetMermaidRenderer(renderer.Options).RenderToSvg(mermaidCode);

            if (svg == null)
            {
                RenderAsCodeBlockFallback(renderer, obj, "[Error rendering Mermaid diagram]");
                return;
            }

            // Parse SVG dimensions and calculate constrained dimensions in EMUs
            var (widthPixels, heightPixels) = GetSvgDimensions(svg);
            var (widthEmu, heightEmu) = CalculateDiagramDimensions(
                widthPixels,
                heightPixels,
                renderer.Options.MaxDiagramWidthInches,
                renderer.Options.MaxDiagramHeightInches);

            // Rasterize SVG to high-quality PNG for reliable Word/web rendering
            byte[] pngBytes = RasterizeSvgToPng(svg);
            ImagePart imagePart = renderer.DocumentBuilder.AddImagePart(pngBytes, "image/png");
            string relationshipId = renderer.DocumentBuilder.GetImageRelationshipId(imagePart);

            // Insert the diagram as a Drawing element
            InsertDiagramDrawing(renderer, relationshipId, widthEmu, heightEmu);
        }
        catch
        {
            // On any error, fall back to code block rendering
            RenderAsCodeBlockFallback(renderer, obj, "[Error rendering Mermaid diagram]");
        }
    }

    /// <summary>
    /// Extracts the code content from a CodeBlock.
    /// </summary>
    private static string ExtractCodeContent(CodeBlock obj)
    {
        var sb = new StringBuilder();
        var lines = obj.Lines.Lines;

        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                sb.AppendLine(line.Slice.ToString());
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Renders the code block as a plain code block with an error message.
    /// </summary>
    private void RenderAsCodeBlockFallback(OpenXmlRenderer renderer, CodeBlock obj, string errorMessage)
    {
        // Add error message as first paragraph
        var errorPara = renderer.DocumentBuilder.AddParagraph();
        errorPara.ParagraphProperties = renderer.StyleManager.GetCodeBlockProperties();

        var errorRun = new Run(
            renderer.StyleManager.GetCodeRunProperties(),
            new Text(errorMessage) { Space = SpaceProcessingModeValues.Preserve }
        );
        errorPara.AppendChild(errorRun);

        // Render the original code content
        var lines = obj.Lines.Lines;
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var text = line.Slice.ToString();

                var paragraph = renderer.DocumentBuilder.AddParagraph();
                paragraph.ParagraphProperties = renderer.StyleManager.GetCodeBlockProperties();

                var run = new Run(
                    renderer.StyleManager.GetCodeRunProperties(),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                );
                paragraph.AppendChild(run);
            }
        }
    }

    /// <summary>
    /// Calculates diagram dimensions in EMUs. Always fills the full page width
    /// and derives height from the SVG aspect ratio, capped by max height.
    /// </summary>
    private static (long widthEmu, long heightEmu) CalculateDiagramDimensions(
        double svgWidth,
        double svgHeight,
        double maxWidthInches,
        double maxHeightInches)
    {
        const int emusPerInch = 914400;

        double aspectRatio = svgWidth / svgHeight;

        // Fill the full page width, derive height from aspect ratio
        double widthInches = maxWidthInches;
        double heightInches = widthInches / aspectRatio;

        // Cap height if it exceeds the max, and shrink width to match
        if (heightInches > maxHeightInches)
        {
            heightInches = maxHeightInches;
            widthInches = heightInches * aspectRatio;
        }

        return ((long)(widthInches * emusPerInch), (long)(heightInches * emusPerInch));
    }

    /// <summary>
    /// Gets the aspect ratio dimensions from SVG viewBox attribute.
    /// Naiad SVGs use width="100%" so viewBox is the authoritative source.
    /// Returns default dimensions (4:3) if parsing fails.
    /// </summary>
    private static (double width, double height) GetSvgDimensions(string svg)
    {
        try
        {
            var viewBoxMatch = System.Text.RegularExpressions.Regex.Match(
                svg, @"viewBox=""[0-9.-]+\s+[0-9.-]+\s+([0-9.]+)\s+([0-9.]+)""");

            if (viewBoxMatch.Success &&
                double.TryParse(viewBoxMatch.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double vw) &&
                double.TryParse(viewBoxMatch.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double vh) &&
                vw > 0 && vh > 0)
            {
                return (vw, vh);
            }
        }
        catch
        {
            // Fall through to default
        }

        return (800, 600);
    }

    /// <summary>
    /// Rasterizes an SVG string to a high-quality PNG byte array using Svg.Skia.
    /// Renders at 3x scale for crisp output in print and high-DPI displays.
    /// </summary>
    private static byte[] RasterizeSvgToPng(string svgContent, float scale = 3.0f)
    {
        using var svg = new SKSvg();
        using var svgStream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
        svg.Load(svgStream);

        var picture = svg.Picture
            ?? throw new InvalidOperationException("Failed to parse SVG for rasterization");

        var bounds = picture.CullRect;
        int width = Math.Max(1, (int)(bounds.Width * scale));
        int height = Math.Max(1, (int)(bounds.Height * scale));

        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        return data.ToArray();
    }

    /// <summary>
    /// Inserts a Drawing element containing the diagram image into the document.
    /// </summary>
    private static void InsertDiagramDrawing(
        OpenXmlRenderer renderer,
        string relationshipId,
        long widthEmu,
        long heightEmu)
    {
        var paragraph = renderer.DocumentBuilder.AddParagraph();

        // Generate unique IDs for the drawing
        uint drawingId = (uint)(DateTime.UtcNow.Ticks % int.MaxValue);

        var drawing = new Drawing(
            new DW.Inline(
                new DW.Extent { Cx = widthEmu, Cy = heightEmu },
                new DW.EffectExtent { LeftEdge = 0, TopEdge = 0, RightEdge = 0, BottomEdge = 0 },
                new DW.DocProperties { Id = drawingId, Name = $"Mermaid Diagram {drawingId}" },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }
                ),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties { Id = drawingId, Name = $"Mermaid {drawingId}" },
                                new PIC.NonVisualPictureDrawingProperties()
                            ),
                            new PIC.BlipFill(
                                new A.Blip { Embed = relationshipId },
                                new A.Stretch(new A.FillRectangle())
                            ),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0, Y = 0 },
                                    new A.Extents { Cx = widthEmu, Cy = heightEmu }
                                ),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                            )
                        )
                    )
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
            {
                DistanceFromTop = 0,
                DistanceFromBottom = 0,
                DistanceFromLeft = 0,
                DistanceFromRight = 0
            }
        );

        paragraph.AppendChild(new Run(drawing));
    }
}
