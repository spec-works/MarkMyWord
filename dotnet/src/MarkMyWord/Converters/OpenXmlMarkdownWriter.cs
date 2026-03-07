using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MarkMyWord.Configuration;
using MarkMyWord.Exceptions;

namespace MarkMyWord.Converters;

/// <summary>
/// Converts OpenXML Word documents to Markdown format.
/// </summary>
public class OpenXmlMarkdownWriter : IDisposable
{
    private readonly WordToMarkdownOptions _options;
    private readonly string _baseDirectory;
    private readonly StringBuilder _markdown;
    private bool _inCodeBlock;
    private readonly Stack<ListInfo> _listStack;

    public OpenXmlMarkdownWriter(WordToMarkdownOptions options, string baseDirectory)
    {
        _options = options;
        _baseDirectory = baseDirectory;
        _markdown = new StringBuilder();
        _inCodeBlock = false;
        _listStack = new Stack<ListInfo>();
    }

    public string ConvertToMarkdown(Stream docxStream)
    {
        try
        {
            using var document = WordprocessingDocument.Open(docxStream, false);

            if (document.MainDocumentPart?.Document?.Body == null)
                throw new InvalidOperationException("Invalid Word document: missing main document part or body.");

            // Extract metadata if requested
            if (_options.IncludeMetadata)
            {
                WriteMetadata(document);
            }

            // Process document body
            ProcessBody(document.MainDocumentPart.Document.Body, document);

            return GetMarkdown();
        }
        catch (System.IO.FileFormatException ex)
        {
            // FileFormatException is thrown for encrypted documents
            if (ex.Message.Contains("corrupted") || ex.Message.Contains("encrypted"))
            {
                throw new EncryptedDocumentException(
                    "The Word document is encrypted or password-protected and cannot be converted. " +
                    "Please remove the encryption in Microsoft Word (File → Info → Protect Document) and try again.",
                    ex);
            }
            throw;
        }
        catch (System.IO.IOException ex) when (ex.Message.Contains("corrupted"))
        {
            // IOException with "corrupted data" message indicates encryption
            throw new EncryptedDocumentException(
                "The Word document is encrypted or password-protected and cannot be converted. " +
                "Please remove the encryption in Microsoft Word (File → Info → Protect Document) and try again.",
                ex);
        }
    }

    private void WriteMetadata(WordprocessingDocument document)
    {
        var properties = document.PackageProperties;
        var hasMetadata = false;

        _markdown.AppendLine("---");

        if (!string.IsNullOrEmpty(properties.Title))
        {
            _markdown.AppendLine($"title: {EscapeYaml(properties.Title)}");
            hasMetadata = true;
        }

        if (!string.IsNullOrEmpty(properties.Creator))
        {
            _markdown.AppendLine($"author: {EscapeYaml(properties.Creator)}");
            hasMetadata = true;
        }

        if (!string.IsNullOrEmpty(properties.Subject))
        {
            _markdown.AppendLine($"subject: {EscapeYaml(properties.Subject)}");
            hasMetadata = true;
        }

        if (hasMetadata)
        {
            _markdown.AppendLine("---");
            _markdown.AppendLine();
        }
        else
        {
            // Clear the opening "---" if no metadata
            _markdown.Clear();
        }
    }

    private void ProcessBody(Body body, WordprocessingDocument document)
    {
        var numberingPart = document.MainDocumentPart?.NumberingDefinitionsPart;

        foreach (var element in body.Elements())
        {
            switch (element)
            {
                case Paragraph paragraph:
                    ProcessParagraph(paragraph, numberingPart);
                    break;

                case Table table:
                    ProcessTable(table);
                    break;

                default:
                    // Unsupported elements are skipped
                    break;
            }
        }
    }

    private void ProcessParagraph(Paragraph paragraph, NumberingDefinitionsPart? numberingPart)
    {
        var props = paragraph.ParagraphProperties;

        // Check if this is a code block (styled with a code style or all runs are code)
        var isCodeBlock = IsCodeBlock(paragraph);

        // Close code block if we were in one and this paragraph is not a code block
        if (_inCodeBlock && !isCodeBlock)
        {
            _markdown.AppendLine("```");
            _markdown.AppendLine();
            _inCodeBlock = false;
        }

        // Process code block
        if (isCodeBlock)
        {
            ProcessCodeBlock(paragraph);
            return;
        }

        // Check if this is a list item
        if (props?.NumberingProperties != null && numberingPart != null)
        {
            ProcessListItem(paragraph, props.NumberingProperties, numberingPart);
            return;
        }

        // Check if this is a heading
        var styleId = props?.ParagraphStyleId?.Val?.Value;
        if (styleId != null && styleId.StartsWith("Heading"))
        {
            ProcessHeading(paragraph, styleId);
            return;
        }

        // Check for block quote
        if (IsBlockQuote(paragraph))
        {
            ProcessBlockQuote(paragraph);
            return;
        }

        // Regular paragraph
        var text = ProcessRuns(paragraph);

        if (!string.IsNullOrWhiteSpace(text))
        {
            _markdown.AppendLine(text);
            _markdown.AppendLine();
        }
        else if (!_inCodeBlock)
        {
            // Preserve blank lines between paragraphs (unless we're in a code block)
            _markdown.AppendLine();
        }
    }

    private void ProcessHeading(Paragraph paragraph, string styleId)
    {
        // Extract heading level from style (e.g., "Heading1" -> 1)
        var levelStr = styleId.Replace("Heading", "").Trim();
        if (int.TryParse(levelStr, out var level) && level >= 1 && level <= 6)
        {
            var text = ProcessRuns(paragraph, ignoreFormatting: !_options.PreserveFormattingMetadata);
            _markdown.Append(new string('#', level));
            _markdown.Append(' ');
            _markdown.AppendLine(text);
            _markdown.AppendLine();
        }
        else
        {
            // Fallback to regular paragraph
            var text = ProcessRuns(paragraph);
            _markdown.AppendLine(text);
            _markdown.AppendLine();
        }
    }

    private void ProcessCodeBlock(Paragraph paragraph)
    {
        if (!_inCodeBlock)
        {
            _markdown.AppendLine("```");
            _inCodeBlock = true;
        }

        var text = GetPlainText(paragraph);
        _markdown.AppendLine(text);
    }

    private void ProcessBlockQuote(Paragraph paragraph)
    {
        var text = ProcessRuns(paragraph);
        if (!string.IsNullOrWhiteSpace(text))
        {
            _markdown.Append("> ");
            _markdown.AppendLine(text);
        }
    }

    private void ProcessListItem(Paragraph paragraph, NumberingProperties numProps, NumberingDefinitionsPart numberingPart)
    {
        var numId = numProps.NumberingId?.Val?.Value;
        var level = numProps.NumberingLevelReference?.Val?.Value ?? 0;

        if (numId == null)
            return;

        // Determine if this is an ordered or unordered list
        var isOrdered = IsOrderedList(numId.Value, level, numberingPart);

        // Update list stack
        UpdateListStack(level, isOrdered);

        // Generate list marker
        var indent = new string(' ', (int)level * 2);
        var marker = isOrdered ? "1. " : "- ";

        var text = ProcessRuns(paragraph);
        _markdown.Append(indent);
        _markdown.Append(marker);
        _markdown.AppendLine(text);
    }

    private void ProcessTable(Table table)
    {
        if (_options.Flavor != MarkdownFlavor.GitHubFlavoredMarkdown)
        {
            // Tables are not supported in CommonMark, render as plain text or skip
            if (!_options.OptimizeForLLM)
            {
                _markdown.AppendLine("<!-- Table content not supported in CommonMark -->");
                _markdown.AppendLine();
            }
            return;
        }

        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count == 0)
            return;

        // Process header row
        var headerRow = rows[0];
        var headers = headerRow.Elements<TableCell>().Select(c => ProcessTableCell(c)).ToList();

        _markdown.Append("| ");
        _markdown.Append(string.Join(" | ", headers));
        _markdown.AppendLine(" |");

        // Separator row
        _markdown.Append("| ");
        _markdown.Append(string.Join(" | ", headers.Select(_ => "---")));
        _markdown.AppendLine(" |");

        // Data rows
        foreach (var row in rows.Skip(1))
        {
            var cells = row.Elements<TableCell>().Select(c => ProcessTableCell(c)).ToList();
            _markdown.Append("| ");
            _markdown.Append(string.Join(" | ", cells));
            _markdown.AppendLine(" |");
        }

        _markdown.AppendLine();
    }

    private string ProcessTableCell(TableCell cell)
    {
        var text = new StringBuilder();
        foreach (var paragraph in cell.Elements<Paragraph>())
        {
            var paraText = ProcessRuns(paragraph);
            if (!string.IsNullOrWhiteSpace(paraText))
            {
                if (text.Length > 0)
                    text.Append(" ");
                text.Append(paraText);
            }
        }

        // Escape pipe characters in table cells
        return text.ToString().Replace("|", "\\|").Replace("\n", " ").Replace("\r", "").Trim();
    }

    private string ProcessRuns(Paragraph paragraph, bool ignoreFormatting = false)
    {
        var result = new StringBuilder();
        var boldOpen = false;
        var italicOpen = false;
        var codeOpen = false;

        foreach (var element in paragraph.Elements())
        {
            if (element is Run run)
            {
                var runProps = run.RunProperties;
                var text = GetRunText(run);

                if (string.IsNullOrEmpty(text))
                    continue;

                if (ignoreFormatting || _options.OptimizeForLLM)
                {
                    result.Append(text);
                    continue;
                }

                // Check for inline code
                var isCode = runProps?.RunStyle?.Val?.Value?.Contains("Code") ?? false;
                if (isCode && !codeOpen)
                {
                    result.Append('`');
                    codeOpen = true;
                }
                else if (!isCode && codeOpen)
                {
                    result.Append('`');
                    codeOpen = false;
                }

                // Check for bold
                var isBold = runProps?.Bold?.Val?.Value ?? false;
                if (isBold && !boldOpen)
                {
                    result.Append("**");
                    boldOpen = true;
                }
                else if (!isBold && boldOpen)
                {
                    result.Append("**");
                    boldOpen = false;
                }

                // Check for italic
                var isItalic = runProps?.Italic?.Val?.Value ?? false;
                if (isItalic && !italicOpen)
                {
                    result.Append('*');
                    italicOpen = true;
                }
                else if (!isItalic && italicOpen)
                {
                    result.Append('*');
                    italicOpen = false;
                }

                result.Append(text);
            }
            else if (element is Hyperlink hyperlink)
            {
                ProcessHyperlink(hyperlink, result);
            }
        }

        // Close any open formatting
        if (codeOpen) result.Append('`');
        if (boldOpen) result.Append("**");
        if (italicOpen) result.Append('*');

        return result.ToString();
    }

    private void ProcessHyperlink(Hyperlink hyperlink, StringBuilder result)
    {
        var linkText = GetPlainText(hyperlink);
        var url = hyperlink.Id?.Value; // This would need to be resolved from relationships

        if (!string.IsNullOrEmpty(linkText))
        {
            if (!string.IsNullOrEmpty(url))
            {
                result.Append($"[{linkText}]({url})");
            }
            else
            {
                result.Append(linkText);
            }
        }
    }

    private string GetRunText(Run run)
    {
        var text = new StringBuilder();

        foreach (var element in run.Elements())
        {
            switch (element)
            {
                case Text textElement:
                    text.Append(textElement.Text);
                    break;

                case TabChar:
                    text.Append('\t');
                    break;

                case Break br:
                    if (br.Type?.Value == BreakValues.Page)
                        text.Append("\n\n---\n\n"); // Page break as horizontal rule
                    else
                        text.Append("  \n"); // Line break
                    break;
            }
        }

        return text.ToString();
    }

    private string GetPlainText(OpenXmlElement element)
    {
        var text = new StringBuilder();
        foreach (var descendant in element.Descendants<Text>())
        {
            text.Append(descendant.Text);
        }
        return text.ToString();
    }

    private bool IsCodeBlock(Paragraph paragraph)
    {
        var props = paragraph.ParagraphProperties;
        if (props == null)
            return false;

        // Check if paragraph has a code style
        var styleId = props.ParagraphStyleId?.Val?.Value;
        if (styleId?.Contains("Code") ?? false)
            return true;

        // Check if paragraph has shading (code blocks have background color)
        var shading = props.Shading;
        if (shading?.Fill?.Value != null && shading.Fill.Value != "auto")
            return true;

        // Check if all runs in the paragraph use a code font (monospace)
        var runs = paragraph.Elements<Run>().ToList();
        if (runs.Count > 0)
        {
            var allCode = runs.All(run =>
            {
                var runFont = run.RunProperties?.RunFonts?.Ascii?.Value;
                return runFont != null && (runFont.Contains("Consol") || runFont.Contains("Courier") || runFont.Contains("Mono"));
            });

            if (allCode)
                return true;
        }

        return false;
    }

    private bool IsBlockQuote(Paragraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        return styleId?.Contains("Quote") ?? false;
    }

    private bool IsOrderedList(int numId, int level, NumberingDefinitionsPart numberingPart)
    {
        // Try to determine if the list is ordered or unordered from numbering definitions
        // This is a simplified version; full implementation would parse numbering XML
        var numbering = numberingPart.Numbering;
        var numInstance = numbering.Elements<NumberingInstance>()
            .FirstOrDefault(ni => ni.NumberID?.Value == numId);

        if (numInstance?.AbstractNumId?.Val?.Value != null)
        {
            var abstractNum = numbering.Elements<AbstractNum>()
                .FirstOrDefault(an => an.AbstractNumberId?.Value == numInstance.AbstractNumId.Val.Value);

            var lvl = abstractNum?.Elements<Level>()
                .FirstOrDefault(l => l.LevelIndex?.Value == level);

            var numFmt = lvl?.NumberingFormat?.Val?.Value;

            // Check if it's a bullet or numbered format
            return numFmt != null && numFmt != NumberFormatValues.Bullet;
        }

        return false; // Default to unordered
    }

    private void UpdateListStack(int level, bool isOrdered)
    {
        // Pop items from stack if we're at a lower level
        while (_listStack.Count > level + 1)
        {
            _listStack.Pop();
        }

        // Push or update current level
        if (_listStack.Count == level + 1)
        {
            var current = _listStack.Pop();
            _listStack.Push(new ListInfo(level, isOrdered, current.ItemCount + 1));
        }
        else if (_listStack.Count == level)
        {
            _listStack.Push(new ListInfo(level, isOrdered, 1));
        }
    }

    private string GetMarkdown()
    {
        // Close any open code blocks
        if (_inCodeBlock)
        {
            _markdown.AppendLine("```");
            _inCodeBlock = false;
        }

        var markdown = _markdown.ToString();

        // Apply line ending style
        if (_options.LineEndings != LineEndingStyle.Environment)
        {
            var lineEnding = _options.LineEndings == LineEndingStyle.LF ? "\n" : "\r\n";
            markdown = markdown.Replace("\r\n", "\n").Replace("\n", lineEnding);
        }

        return markdown;
    }

    private string EscapeYaml(string text)
    {
        if (text.Contains(':') || text.Contains('#') || text.Contains('-'))
        {
            return $"\"{text.Replace("\"", "\\\"")}\"";
        }
        return text;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    private class ListInfo
    {
        public int Level { get; }
        public bool IsOrdered { get; }
        public int ItemCount { get; }

        public ListInfo(int level, bool isOrdered, int itemCount)
        {
            Level = level;
            IsOrdered = isOrdered;
            ItemCount = itemCount;
        }
    }
}
