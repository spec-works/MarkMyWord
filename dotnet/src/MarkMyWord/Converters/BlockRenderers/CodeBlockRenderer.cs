using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Renderers;
using Markdig.Syntax;
using MarkMyWord.SyntaxHighlighting;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for code blocks (both fenced and indented).
/// </summary>
public class CodeBlockRenderer : OpenXmlObjectRenderer<CodeBlock>
{
    private readonly SyntaxHighlighterFactory _highlighterFactory = new();

    protected override void Write(OpenXmlRenderer renderer, CodeBlock obj)
    {
        // Extract language identifier if this is a fenced code block
        string? language = null;
        if (obj is FencedCodeBlock fencedBlock && !string.IsNullOrEmpty(fencedBlock.Info))
        {
            language = fencedBlock.Info.Trim();
        }

        // Determine if syntax highlighting should be used
        bool useSyntaxHighlighting = renderer.Options.EnableSyntaxHighlighting &&
                                      !string.IsNullOrWhiteSpace(language) &&
                                      _highlighterFactory.IsLanguageSupported(language);

        // Render each line of code as a separate paragraph
        var lines = obj.Lines.Lines;
        if (lines != null)
        {
            // Find the last non-empty line to strip trailing empty lines
            int lastNonEmptyLine = lines.Length - 1;
            while (lastNonEmptyLine >= 0 && string.IsNullOrWhiteSpace(lines[lastNonEmptyLine].Slice.ToString()))
            {
                lastNonEmptyLine--;
            }

            // Render lines up to the last non-empty line
            for (int i = 0; i <= lastNonEmptyLine; i++)
            {
                var line = lines[i];
                var text = line.Slice.ToString();

                var paragraph = renderer.DocumentBuilder.AddParagraph();
                paragraph.ParagraphProperties = renderer.StyleManager.GetCodeBlockProperties();

                if (useSyntaxHighlighting)
                {
                    // Use syntax highlighting - create multiple runs with colors
                    var tokens = _highlighterFactory.Highlight(text, language!);

                    foreach (var token in tokens)
                    {
                        var run = new Run(
                            renderer.StyleManager.GetSyntaxTokenRunProperties(token.Type),
                            new Text(token.Text) { Space = SpaceProcessingModeValues.Preserve }
                        );
                        paragraph.AppendChild(run);
                    }
                }
                else
                {
                    // Plain text rendering (original behavior)
                    var run = new Run(
                        renderer.StyleManager.GetCodeRunProperties(),
                        new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                    );
                    paragraph.AppendChild(run);
                }
            }
        }
    }
}
