using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using MarkMyWord.OpenXml;

namespace MarkMyWord.Converters.InlineRenderers;

/// <summary>
/// Renderer for inline code elements.
/// </summary>
public class CodeInlineRenderer : OpenXmlObjectRenderer<CodeInline>
{
    protected override void Write(OpenXmlRenderer renderer, CodeInline obj)
    {
        var currentParagraph = renderer.DocumentBuilder.Body.Elements<Paragraph>().LastOrDefault();
        if (currentParagraph == null)
        {
            currentParagraph = renderer.DocumentBuilder.AddParagraph();
        }

        // Create a run with code styling
        var runProperties = renderer.StyleManager.GetCodeRunProperties();
        var run = new Run(runProperties);
        run.AppendChild(new Text(TextSanitizer.Sanitize(obj.Content)) { Space = SpaceProcessingModeValues.Preserve });

        currentParagraph.AppendChild(run);
    }
}
