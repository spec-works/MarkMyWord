using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace MarkMyWord.Converters.InlineRenderers;

/// <summary>
/// Renderer for line break inline elements.
/// </summary>
public class LineBreakInlineRenderer : OpenXmlObjectRenderer<LineBreakInline>
{
    protected override void Write(OpenXmlRenderer renderer, LineBreakInline obj)
    {
        var currentParagraph = renderer.DocumentBuilder.Body.Elements<Paragraph>().LastOrDefault();
        if (currentParagraph == null)
        {
            currentParagraph = renderer.DocumentBuilder.AddParagraph();
        }

        // Both hard and soft line breaks produce a new line in the same paragraph.
        // Hard breaks come from explicit "  " or "\" at end of line.
        // Soft breaks come from regular newlines within a paragraph block.
        // Without this, consecutive markdown lines (e.g. "**Authors:** ...\n**Date:** ...")
        // merge into a single line in Word with no separation.
        var run = new Run();
        run.AppendChild(new Break());
        currentParagraph.AppendChild(run);
    }
}
