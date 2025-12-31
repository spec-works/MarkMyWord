using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;

namespace MarkMyWord.Converters.InlineRenderers;

/// <summary>
/// Renderer for emphasis (bold/italic) inline elements.
/// </summary>
public class EmphasisInlineRenderer : OpenXmlObjectRenderer<EmphasisInline>
{
    protected override void Write(OpenXmlRenderer renderer, EmphasisInline obj)
    {
        // Get the current paragraph
        var currentParagraph = renderer.DocumentBuilder.Body.Elements<Paragraph>().LastOrDefault();
        if (currentParagraph == null)
        {
            currentParagraph = renderer.DocumentBuilder.AddParagraph();
        }

        // Create run properties based on emphasis type
        var runProperties = new RunProperties();

        if (obj.DelimiterCount == 2)
        {
            // Bold
            runProperties.AppendChild(new Bold());
            runProperties.AppendChild(new BoldComplexScript());
        }
        else if (obj.DelimiterCount == 1)
        {
            // Italic
            runProperties.AppendChild(new Italic());
            runProperties.AppendChild(new ItalicComplexScript());
        }
        else if (obj.DelimiterCount >= 3)
        {
            // Bold and Italic
            runProperties.AppendChild(new Bold());
            runProperties.AppendChild(new BoldComplexScript());
            runProperties.AppendChild(new Italic());
            runProperties.AppendChild(new ItalicComplexScript());
        }

        // Create a run and apply properties
        var run = new Run(runProperties);

        // Add the run to the paragraph
        currentParagraph.AppendChild(run);

        // Render children inline elements within this run
        if (obj.FirstChild != null)
        {
            RenderChildrenInRun(renderer, obj, run);
        }
    }

    private void RenderChildrenInRun(OpenXmlRenderer renderer, EmphasisInline obj, Run run)
    {
        // We need to render the children and capture their text
        // For simplicity, we'll traverse the children and add them to this run
        var child = obj.FirstChild;
        while (child != null)
        {
            if (child is LiteralInline literal)
            {
                run.AppendChild(new Text(literal.Content.ToString()) { Space = SpaceProcessingModeValues.Preserve });
            }
            else
            {
                // For other inline types, render them normally
                // Note: This handles nested emphasis
                renderer.Write(child);
            }

            child = child.NextSibling;
        }
    }
}
