using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace MarkMyWord.Converters.InlineRenderers;

/// <summary>
/// Renderer for literal inline text.
/// </summary>
public class LiteralInlineRenderer : OpenXmlObjectRenderer<LiteralInline>
{
    protected override void Write(OpenXmlRenderer renderer, LiteralInline obj)
    {
        if (obj.Content.IsEmpty)
            return;

        var text = obj.Content.ToString();
        if (string.IsNullOrEmpty(text))
            return;

        // Get the current paragraph - it should have been created by the block renderer
        var currentParagraph = renderer.DocumentBuilder.Body.Elements<Paragraph>().LastOrDefault();
        if (currentParagraph == null)
        {
            // Fallback: create a new paragraph if needed
            currentParagraph = renderer.DocumentBuilder.AddParagraph();
        }

        // Create a run with the text
        var run = new Run();
        run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        currentParagraph.AppendChild(run);
    }
}
