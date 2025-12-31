using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for code blocks (both fenced and indented).
/// </summary>
public class CodeBlockRenderer : OpenXmlObjectRenderer<CodeBlock>
{
    protected override void Write(OpenXmlRenderer renderer, CodeBlock obj)
    {
        // If it's a fenced code block with a language, optionally add a label
        if (obj is FencedCodeBlock fencedBlock && !string.IsNullOrEmpty(fencedBlock.Info))
        {
            var labelPara = renderer.DocumentBuilder.AddParagraph($"```{fencedBlock.Info}");
            labelPara.ParagraphProperties = renderer.StyleManager.GetCodeBlockProperties();
        }

        // Render each line of code as a separate paragraph
        var lines = obj.Lines.Lines;
        if (lines != null)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var text = line.Slice.ToString();

                var paragraph = renderer.DocumentBuilder.AddParagraph();
                paragraph.ParagraphProperties = renderer.StyleManager.GetCodeBlockProperties();

                // Create run with code font
                var run = new Run(
                    renderer.StyleManager.GetCodeRunProperties(),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                );

                paragraph.AppendChild(run);
            }
        }
    }
}
