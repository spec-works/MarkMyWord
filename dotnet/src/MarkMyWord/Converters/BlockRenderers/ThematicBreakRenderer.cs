using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for thematic breaks (horizontal rules).
/// </summary>
public class ThematicBreakRenderer : OpenXmlObjectRenderer<ThematicBreakBlock>
{
    protected override void Write(OpenXmlRenderer renderer, ThematicBreakBlock obj)
    {
        // Create a paragraph with a bottom border to simulate a horizontal rule
        var paragraph = renderer.DocumentBuilder.AddParagraph();
        paragraph.ParagraphProperties = new ParagraphProperties(
            new ParagraphBorders(
                new BottomBorder
                {
                    Val = BorderValues.Single,
                    Size = 6,
                    Color = "000000"
                }
            ),
            new SpacingBetweenLines { After = "160", Before = "160" }
        );
    }
}
