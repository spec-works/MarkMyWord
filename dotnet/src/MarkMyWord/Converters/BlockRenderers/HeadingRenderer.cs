using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for heading blocks.
/// </summary>
public class HeadingRenderer : OpenXmlObjectRenderer<HeadingBlock>
{
    protected override void Write(OpenXmlRenderer renderer, HeadingBlock obj)
    {
        // Create a paragraph with heading style
        var properties = renderer.StyleManager.GetHeadingProperties(obj.Level);
        var paragraph = renderer.DocumentBuilder.AddParagraph();
        paragraph.ParagraphProperties = properties;

        // Render inline content
        if (obj.Inline != null)
        {
            renderer.WriteChildren(obj.Inline);
        }
    }
}
