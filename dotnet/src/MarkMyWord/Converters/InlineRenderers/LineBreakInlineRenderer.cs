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

        // Add a hard line break if this is a hard break
        if (obj.IsHard)
        {
            var run = new Run();
            run.AppendChild(new Break());
            currentParagraph.AppendChild(run);
        }
        // Soft breaks are typically ignored in Word documents (treated as spaces)
    }
}
