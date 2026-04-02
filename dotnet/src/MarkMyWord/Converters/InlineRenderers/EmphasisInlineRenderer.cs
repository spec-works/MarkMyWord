using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using MarkMyWord.OpenXml;

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

        // Render each child as its own run so inline code stays in reading order
        var child = obj.FirstChild;
        while (child != null)
        {
            if (child is LiteralInline literal)
            {
                var run = new Run(CreateEmphasisRunProperties(obj));
                run.AppendChild(new Text(TextSanitizer.Sanitize(literal.Content.ToString())) { Space = SpaceProcessingModeValues.Preserve });
                currentParagraph.AppendChild(run);
            }
            else if (child is CodeInline codeInline)
            {
                // Merge emphasis formatting into code styling so bold/italic is preserved
                var codeRunProps = renderer.StyleManager.GetCodeRunProperties();
                ApplyEmphasisFormatting(obj, codeRunProps);
                var codeRun = new Run(codeRunProps);
                codeRun.AppendChild(new Text(TextSanitizer.Sanitize(codeInline.Content)) { Space = SpaceProcessingModeValues.Preserve });
                currentParagraph.AppendChild(codeRun);
            }
            else
            {
                // For other inline types (nested emphasis, line breaks, etc.)
                renderer.Write(child);
            }

            child = child.NextSibling;
        }
    }

    private static RunProperties CreateEmphasisRunProperties(EmphasisInline obj)
    {
        var props = new RunProperties();
        ApplyEmphasisFormatting(obj, props);
        return props;
    }

    private static void ApplyEmphasisFormatting(EmphasisInline obj, RunProperties props)
    {
        if (obj.DelimiterCount == 2 || obj.DelimiterCount >= 3)
        {
            if (props.Bold == null) props.AppendChild(new Bold());
            if (props.BoldComplexScript == null) props.AppendChild(new BoldComplexScript());
        }
        if (obj.DelimiterCount == 1 || obj.DelimiterCount >= 3)
        {
            if (props.Italic == null) props.AppendChild(new Italic());
            if (props.ItalicComplexScript == null) props.AppendChild(new ItalicComplexScript());
        }
    }
}
