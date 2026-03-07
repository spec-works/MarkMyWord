using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkMyWord.Diagrams;
using MarkMyWord.SyntaxHighlighting;
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
    private readonly MermaidRenderer _mermaidRenderer = new();

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

            // Render Mermaid to PNG using Playwright (async bridge for sync context)
            byte[]? pngBytes = _mermaidRenderer.RenderToPngAsync(mermaidCode).GetAwaiter().GetResult();

            if (pngBytes == null)
            {
                RenderAsCodeBlockFallback(renderer, obj, "[Error rendering Mermaid diagram]");
                return;
            }

            // Get PNG dimensions and calculate constrained dimensions in EMUs
            var (widthPixels, heightPixels) = GetPngDimensions(pngBytes);
            var (widthEmu, heightEmu) = CalculateDiagramDimensions(
                widthPixels,
                heightPixels,
                renderer.Options.MaxDiagramWidthInches,
                renderer.Options.MaxDiagramHeightInches);

            // Add PNG as image part
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
    /// Calculates diagram dimensions in EMUs, applying max width/height constraints.
    /// Maintains aspect ratio.
    /// </summary>
    private static (long widthEmu, long heightEmu) CalculateDiagramDimensions(
        double widthPixels,
        double heightPixels,
        double maxWidthInches,
        double maxHeightInches)
    {
        const int emusPerInch = 914400;
        const double pixelsPerInch = 96.0;

        // Convert pixels to inches
        double widthInches = widthPixels / pixelsPerInch;
        double heightInches = heightPixels / pixelsPerInch;

        // Apply constraints while maintaining aspect ratio
        double aspectRatio = widthInches / heightInches;

        if (widthInches > maxWidthInches)
        {
            widthInches = maxWidthInches;
            heightInches = widthInches / aspectRatio;
        }

        if (heightInches > maxHeightInches)
        {
            heightInches = maxHeightInches;
            widthInches = heightInches * aspectRatio;
        }

        // Convert to EMUs
        long widthEmu = (long)(widthInches * emusPerInch);
        long heightEmu = (long)(heightInches * emusPerInch);

        return (widthEmu, heightEmu);
    }

    /// <summary>
    /// Gets image dimensions from PNG byte array by parsing PNG header.
    /// Returns default dimensions (800x600) if parsing fails.
    /// </summary>
    private static (double width, double height) GetPngDimensions(byte[] pngBytes)
    {
        try
        {
            // PNG header format:
            // Bytes 16-19: Width (big-endian)
            // Bytes 20-23: Height (big-endian)
            if (pngBytes.Length >= 24)
            {
                int width = (pngBytes[16] << 24) | (pngBytes[17] << 16) | (pngBytes[18] << 8) | pngBytes[19];
                int height = (pngBytes[20] << 24) | (pngBytes[21] << 16) | (pngBytes[22] << 8) | pngBytes[23];

                if (width > 0 && height > 0 && width < 10000 && height < 10000)
                {
                    return (width, height);
                }
            }
        }
        catch
        {
            // Fall through to default
        }

        return (800, 600);
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
