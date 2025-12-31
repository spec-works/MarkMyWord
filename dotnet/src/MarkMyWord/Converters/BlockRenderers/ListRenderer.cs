using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// Renderer for list blocks (ordered and unordered).
/// </summary>
public class ListRenderer : OpenXmlObjectRenderer<ListBlock>
{
    protected override void Write(OpenXmlRenderer renderer, ListBlock listBlock)
    {
        // Enter list context
        renderer.ListManager.EnterList(listBlock.IsOrdered);

        try
        {
            // Render each list item
            foreach (var item in listBlock)
            {
                if (item is ListItemBlock listItem)
                {
                    WriteListItem(renderer, listItem);
                }
            }
        }
        finally
        {
            // Exit list context
            renderer.ListManager.ExitList();
        }
    }

    private void WriteListItem(OpenXmlRenderer renderer, ListItemBlock listItem)
    {
        // Get numbering properties for this list item
        var numberingProperties = renderer.ListManager.GetNumberingProperties();

        // Check if the list item contains only a single paragraph
        // or multiple blocks (which makes it a "loose" list item)
        bool isLoose = listItem.Count > 1 ||
                      (listItem.Count == 1 && listItem[0] is not ParagraphBlock);

        if (listItem.Count == 0)
        {
            // Empty list item - create empty paragraph with numbering
            var emptyPara = renderer.DocumentBuilder.AddParagraph();
            if (emptyPara.ParagraphProperties == null)
                emptyPara.ParagraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
            emptyPara.ParagraphProperties.AppendChild(numberingProperties!);
            return;
        }

        bool isFirstBlock = true;

        foreach (var block in listItem)
        {
            if (block is ParagraphBlock paragraphBlock)
            {
                // Create paragraph
                var paragraph = renderer.DocumentBuilder.AddParagraph();

                // Apply numbering only to the first paragraph
                if (isFirstBlock)
                {
                    if (paragraph.ParagraphProperties == null)
                        paragraph.ParagraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
                    paragraph.ParagraphProperties.AppendChild(numberingProperties!);
                    isFirstBlock = false;
                }
                else
                {
                    // Continuation paragraphs need proper indentation
                    if (paragraph.ParagraphProperties == null)
                        paragraph.ParagraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();

                    var indentTwips = renderer.Options.Styles.ListIndentationTwips * (renderer.ListManager.CurrentLevel + 1);
                    paragraph.ParagraphProperties.AppendChild(
                        new DocumentFormat.OpenXml.Wordprocessing.Indentation { Left = indentTwips.ToString() }
                    );
                }

                // Render inline content
                if (paragraphBlock.Inline != null)
                {
                    renderer.WriteChildren(paragraphBlock.Inline);
                }
            }
            else if (block is ListBlock nestedList)
            {
                // Nested list - render recursively
                renderer.Write(nestedList);
            }
            else
            {
                // Other block types (code blocks, quotes, etc.)
                // For the first block, we still need to add numbering
                if (isFirstBlock)
                {
                    // Create an empty paragraph with numbering
                    var numberPara = renderer.DocumentBuilder.AddParagraph();
                    if (numberPara.ParagraphProperties == null)
                        numberPara.ParagraphProperties = new DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties();
                    numberPara.ParagraphProperties.AppendChild(numberingProperties!);
                    isFirstBlock = false;
                }

                // Render the block
                renderer.Write(block);
            }
        }
    }
}
