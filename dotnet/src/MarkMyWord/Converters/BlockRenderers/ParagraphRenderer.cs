using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for paragraph blocks.
/// </summary>
public class ParagraphRenderer : OpenXmlObjectRenderer<ParagraphBlock>
{
    protected override void Write(OpenXmlRenderer renderer, ParagraphBlock obj)
    {
        // Create a new paragraph
        var paragraph = renderer.DocumentBuilder.AddParagraph();

        // Render inline content
        if (obj.Inline != null)
        {
            renderer.WriteChildren(obj.Inline);
        }
    }
}
