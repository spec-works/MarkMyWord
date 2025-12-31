using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for quote blocks.
/// </summary>
public class QuoteBlockRenderer : OpenXmlObjectRenderer<QuoteBlock>
{
    protected override void Write(OpenXmlRenderer renderer, QuoteBlock obj)
    {
        // Render each child block with quote styling
        foreach (var child in obj)
        {
            if (child is ParagraphBlock paragraphBlock)
            {
                // Create paragraph with quote styling
                var paragraph = renderer.DocumentBuilder.AddParagraph();
                paragraph.ParagraphProperties = renderer.StyleManager.GetQuoteProperties();

                // Render inline content
                if (paragraphBlock.Inline != null)
                {
                    renderer.WriteChildren(paragraphBlock.Inline);
                }
            }
            else
            {
                // For other block types, render them normally
                renderer.Write(child);
            }
        }
    }
}
