using System.Text;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.Yaml;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MarkMyWord.Configuration;
using MarkMyWord.SyntaxHighlighting;

namespace MarkMyWord.OfficeTalk;

/// <summary>
/// Compiles Markdown to an OfficeTalk (.otk) document string.
/// Walks the Markdig AST and emits OfficeTalk operations instead of OpenXML elements.
/// </summary>
public class OfficeTalkCompiler
{
    private ConversionOptions _options;
    private readonly StyleConfiguration _styles;
    private readonly SyntaxHighlighterFactory _highlighter = new();
    private readonly StringBuilder _output = new();
    private int _headingIndex;
    private int _paragraphIndex;
    private int _tableIndex;
    // Tracks the address of the last emitted element for INSERT AFTER chaining
    private string? _lastAddress;

    public OfficeTalkCompiler(ConversionOptions? options = null)
    {
        _options = options ?? new ConversionOptions();
        _styles = _options.Styles;
    }

    /// <summary>
    /// Compiles markdown text to an OfficeTalk document string.
    /// </summary>
    public string Compile(string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseYamlFrontMatter()
            .UseAdvancedExtensions()
            .Build();

        var document = Markdown.Parse(markdown, pipeline);

        // Extract frontmatter title and apply it to document properties
        var frontmatter = FrontmatterExtractor.Extract(document);
        if (frontmatter?.Title != null && string.IsNullOrEmpty(_options.DocumentTitle))
        {
            _options = new ConversionOptions
            {
                DocumentTitle = frontmatter.Title,
                Author = _options.Author,
                Subject = _options.Subject,
                Styles = _options.Styles,
                EnableAdvancedExtensions = _options.EnableAdvancedExtensions,
                EnableTables = _options.EnableTables,
                EnableTaskLists = _options.EnableTaskLists,
                EnableSyntaxHighlighting = _options.EnableSyntaxHighlighting,
                ImageStrategy = _options.ImageStrategy,
                MaxImageWidthInches = _options.MaxImageWidthInches,
                EnableMermaidDiagrams = _options.EnableMermaidDiagrams,
                MaxDiagramWidthInches = _options.MaxDiagramWidthInches,
                MaxDiagramHeightInches = _options.MaxDiagramHeightInches,
                Theme = _options.Theme,
                SidemarkDocument = _options.SidemarkDocument,
                SidemarkFilePath = _options.SidemarkFilePath
            };
        }

        return Compile(document);
    }

    /// <summary>
    /// Compiles a parsed Markdig document to an OfficeTalk document string.
    /// </summary>
    public string Compile(MarkdownDocument document)
    {
        _output.Clear();
        _headingIndex = 0;
        _paragraphIndex = 1; // blank doc starts with paragraph[1]
        _tableIndex = 0;
        _lastAddress = $"body/paragraph[{_paragraphIndex}]";

        // Header
        _output.AppendLine("OFFICETALK/1.0");
        _output.AppendLine("DOCTYPE word");
        _output.AppendLine();

        // Document properties
        if (_options.DocumentTitle != null)
            _output.AppendLine($"PROPERTY title=\"{Escape(_options.DocumentTitle)}\"");
        if (_options.Author != null)
            _output.AppendLine($"PROPERTY author=\"{Escape(_options.Author)}\"");
        if (_options.Subject != null)
            _output.AppendLine($"PROPERTY subject=\"{Escape(_options.Subject)}\"");

        if (_options.DocumentTitle != null || _options.Author != null || _options.Subject != null)
            _output.AppendLine();

        // Walk AST blocks (skip frontmatter blocks)
        bool isFirst = true;
        foreach (var block in document)
        {
            if (block is YamlFrontMatterBlock)
                continue;

            CompileBlock(block, isFirst);
            isFirst = false;
        }

        return _output.ToString();
    }

    private void CompileBlock(Block block, bool isFirst)
    {
        switch (block)
        {
            case HeadingBlock heading:
                CompileHeading(heading, isFirst);
                break;
            case ParagraphBlock paragraph:
                CompileParagraph(paragraph, isFirst);
                break;
            case FencedCodeBlock fencedCode:
                CompileCodeBlock(fencedCode, isFirst);
                break;
            case CodeBlock code:
                CompileCodeBlock(code, isFirst);
                break;
            case ThematicBreakBlock:
                CompileThematicBreak(isFirst);
                break;
            case QuoteBlock quote:
                CompileQuoteBlock(quote, isFirst);
                break;
            case ListBlock list:
                CompileListBlock(list, isFirst);
                break;
            case Table table:
                CompileTable(table, isFirst);
                break;
        }
    }

    private void CompileHeading(HeadingBlock heading, bool isFirst)
    {
        var runs = CollectInlineRuns(heading.Inline);
        var text = runs.All(r => !r.Bold && !r.Italic && !r.IsCode && r.Href == null)
            ? string.Concat(runs.Select(r => r.Text))
            : null;

        if (isFirst)
        {
            // Use the existing first paragraph — SET + STYLE in one block is fine
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
            EmitContent(runs);
            _output.AppendLine($"STYLE \"Heading{heading.Level}\"");
            _output.AppendLine();
        }
        else
        {
            // Block 1: Insert a new empty paragraph after the last element
            _output.AppendLine($"AT {_lastAddress}");
            _output.AppendLine("INSERT AFTER \"\"");
            _output.AppendLine();
            _paragraphIndex++;

            // Block 2: Target the new paragraph, set content and style
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
            EmitContent(runs);
            _output.AppendLine($"STYLE \"Heading{heading.Level}\"");
            _output.AppendLine();
        }

        // After STYLE "HeadingN", this element is now a heading
        if (isFirst)
            _paragraphIndex--; // paragraph[1] was consumed and converted to heading
        else
            _paragraphIndex--; // the inserted paragraph was converted to heading
        _headingIndex++;
        _lastAddress = $"body/heading[{_headingIndex}]";
    }

    private void CompileParagraph(ParagraphBlock paragraph, bool isFirst)
    {
        // Check if this paragraph contains only an image
        if (paragraph.Inline?.FirstChild is LinkInline link && link.IsImage && paragraph.Inline.FirstChild == paragraph.Inline.LastChild)
        {
            CompileImage(link, isFirst);
            return;
        }

        var runs = CollectInlineRuns(paragraph.Inline);
        bool isPlainText = runs.All(r => !r.Bold && !r.Italic && !r.IsCode && r.Href == null);

        if (isFirst)
        {
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
            EmitContent(runs);
            _output.AppendLine();
        }
        else if (isPlainText)
        {
            // Simple text: INSERT AFTER carries the content directly
            var text = string.Concat(runs.Select(r => r.Text));
            _output.AppendLine($"AT {_lastAddress}");
            _output.AppendLine($"INSERT AFTER \"{Escape(text)}\"");
            _output.AppendLine();
            _paragraphIndex++;
        }
        else
        {
            // Formatted runs: two blocks — INSERT AFTER empty, then SET RUNS
            _output.AppendLine($"AT {_lastAddress}");
            _output.AppendLine("INSERT AFTER \"\"");
            _output.AppendLine();
            _paragraphIndex++;

            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
            EmitContent(runs);
            _output.AppendLine();
        }

        _lastAddress = $"body/paragraph[{_paragraphIndex}]";
    }

    private void CompileImage(LinkInline link, bool isFirst)
    {
        var source = link.Url ?? "";
        var alt = link.FirstChild is LiteralInline lit ? lit.Content.ToString() : "";

        if (isFirst)
        {
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }
        else
        {
            _output.AppendLine($"AT {_lastAddress}");
        }

        _output.AppendLine($"INSERT IMAGE AFTER \"{Escape(source)}\"");
        if (!string.IsNullOrEmpty(alt))
            _output.AppendLine($"  alt=\"{Escape(alt)}\"");
        if (_options.MaxImageWidthInches > 0)
            _output.AppendLine($"  width={_options.MaxImageWidthInches}in");
        _paragraphIndex++;
        _lastAddress = $"body/paragraph[{_paragraphIndex}]";
        _output.AppendLine();
    }

    private void CompileCodeBlock(CodeBlock codeBlock, bool isFirst)
    {
        var code = ExtractCodeContent(codeBlock);
        var language = (codeBlock as FencedCodeBlock)?.Info ?? "";
        var lines = code.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        // Check for mermaid diagrams
        if (_options.EnableMermaidDiagrams && IsMermaidLanguage(language))
        {
            CompileMermaidBlock(code, isFirst);
            return;
        }

        bool highlight = _options.EnableSyntaxHighlighting &&
                         !string.IsNullOrEmpty(language) &&
                         _highlighter.IsLanguageSupported(language);

        foreach (var line in lines)
        {
            if (isFirst)
            {
                _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
                isFirst = false;
            }
            else
            {
                // Block 1: Insert empty paragraph
                _output.AppendLine($"AT {_lastAddress}");
                _output.AppendLine("INSERT AFTER \"\"");
                _output.AppendLine();
                _paragraphIndex++;

                // Block 2: Target new paragraph with SET RUNS
                _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
            }

            if (highlight)
            {
                var tokens = _highlighter.Highlight(line, language);
                _output.AppendLine("SET RUNS");
                foreach (var token in tokens)
                {
                    var colorHex = GetSyntaxColor(token.Type);
                    _output.Append($"  RUN \"{Escape(token.Text)}\"");
                    _output.Append($" font-name=\"{_styles.CodeFontName}\"");
                    _output.Append($" font-size={_styles.CodeFontSize}pt");
                    if (!string.IsNullOrEmpty(_styles.CodeBackgroundColor))
                        _output.Append($" background-color=#{_styles.CodeBackgroundColor}");
                    if (!string.IsNullOrEmpty(colorHex))
                        _output.Append($" color=#{colorHex}");
                    _output.AppendLine();
                }
            }
            else
            {
                _output.AppendLine("SET RUNS");
                _output.Append($"  RUN \"{Escape(line)}\"");
                _output.Append($" font-name=\"{_styles.CodeFontName}\"");
                _output.Append($" font-size={_styles.CodeFontSize}pt");
                if (!string.IsNullOrEmpty(_styles.CodeBackgroundColor))
                    _output.Append($" background-color=#{_styles.CodeBackgroundColor}");
                _output.AppendLine();
            }

            _output.AppendLine();
            _lastAddress = $"body/paragraph[{_paragraphIndex}]";
        }
    }

    private void CompileMermaidBlock(string code, bool isFirst)
    {
        if (!isFirst)
        {
            _output.AppendLine($"AT {_lastAddress}");
            _output.AppendLine("INSERT AFTER \"\"");
            _output.AppendLine();
            _paragraphIndex++;

            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }
        else
        {
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }

        _output.AppendLine("# TODO: Mermaid diagram — pre-render to PNG and use INSERT IMAGE");
        _output.AppendLine($"SET \"[Mermaid diagram]\"");
        _output.AppendLine();
        _lastAddress = $"body/paragraph[{_paragraphIndex}]";
    }

    private void CompileThematicBreak(bool isFirst)
    {
        if (isFirst)
        {
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }
        else
        {
            // Block 1: Insert empty paragraph
            _output.AppendLine($"AT {_lastAddress}");
            _output.AppendLine("INSERT AFTER \"\"");
            _output.AppendLine();
            _paragraphIndex++;

            // Block 2: Format the new paragraph as a horizontal rule
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }

        _output.AppendLine("FORMAT border-bottom=single, border-color=#000000, spacing-before=8pt, spacing-after=8pt");
        _output.AppendLine();
        _lastAddress = $"body/paragraph[{_paragraphIndex}]";
    }

    private void CompileQuoteBlock(QuoteBlock quote, bool isFirst)
    {
        foreach (var child in quote)
        {
            if (child is ParagraphBlock para)
            {
                var runs = CollectInlineRuns(para.Inline);

                if (isFirst)
                {
                    _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
                    isFirst = false;
                }
                else
                {
                    // Block 1: Insert empty paragraph
                    _output.AppendLine($"AT {_lastAddress}");
                    _output.AppendLine("INSERT AFTER \"\"");
                    _output.AppendLine();
                    _paragraphIndex++;

                    // Block 2: Target new paragraph
                    _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
                }

                EmitContent(runs);
                _output.AppendLine("STYLE \"Quote\"");
                _output.AppendLine();
                _lastAddress = $"body/paragraph[{_paragraphIndex}]";
            }
        }
    }

    private void CompileListBlock(ListBlock list, bool isFirst)
    {
        var listType = list.IsOrdered ? "ordered" : "unordered";
        var items = CollectListItems(list);

        if (isFirst)
        {
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }
        else
        {
            _output.AppendLine($"AT {_lastAddress}");
        }

        _output.AppendLine($"INSERT LIST AFTER {listType}");
        foreach (var (text, nested) in items)
        {
            _output.Append($"  ITEM \"{Escape(text)}\"");
            if (nested) _output.Append(" nested");
            _output.AppendLine();
        }

        // INSERT LIST creates N paragraphs (one per item)
        _paragraphIndex += items.Count;
        _lastAddress = $"body/paragraph[{_paragraphIndex}]";
        _output.AppendLine();
    }

    private void CompileTable(Table table, bool isFirst)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0) return;

        int colCount = rows.Max(r => r.Count);

        if (isFirst)
        {
            _output.AppendLine($"AT body/paragraph[{_paragraphIndex}]");
        }
        else
        {
            _output.AppendLine($"AT {_lastAddress}");
        }

        _output.AppendLine($"INSERT TABLE AFTER rows={rows.Count}, columns={colCount}");
        _tableIndex++;
        _lastAddress = $"body/table[{_tableIndex}]";
        _output.AppendLine();

        // Populate rows
        for (int i = 0; i < rows.Count; i++)
        {
            var cells = rows[i].OfType<TableCell>().ToList();
            var cellTexts = cells.Select(c => ExtractCellText(c)).ToList();

            // Pad to column count
            while (cellTexts.Count < colCount)
                cellTexts.Add("");

            _output.AppendLine($"AT body/table[{_tableIndex}]/row[{i + 1}]");
            _output.Append("SET CELLS ");
            _output.AppendLine(string.Join(", ", cellTexts.Select(t => $"\"{Escape(t)}\"")));
            _output.AppendLine();
        }

        // Format header row
        if (rows.Count > 0 && rows[0].IsHeader)
        {
            _output.AppendLine($"AT body/table[{_tableIndex}]/row[1]");
            _output.AppendLine("FORMAT bold=true, fill-color=#D3D3D3");
            _output.AppendLine();
        }
    }

    #region Inline Collection

    /// <summary>
    /// Walks a ContainerInline tree and collects a flat list of OTK run definitions.
    /// </summary>
    private List<OtkRun> CollectInlineRuns(ContainerInline? container)
    {
        var runs = new List<OtkRun>();
        if (container == null) return runs;

        CollectInlinesRecursive(container, runs, bold: false, italic: false, isCode: false, href: null);
        return runs;
    }

    private void CollectInlinesRecursive(
        MarkdownObject inline, List<OtkRun> runs,
        bool bold, bool italic, bool isCode, string? href)
    {
        switch (inline)
        {
            case LiteralInline literal:
                var text = literal.Content.ToString();
                if (!string.IsNullOrEmpty(text))
                    runs.Add(new OtkRun(text, bold, italic, isCode, href));
                break;

            case EmphasisInline emphasis:
                bool newBold = bold || emphasis.DelimiterCount >= 2;
                bool newItalic = italic || emphasis.DelimiterCount == 1 || emphasis.DelimiterCount >= 3;
                foreach (var child in emphasis)
                    CollectInlinesRecursive(child, runs, newBold, newItalic, isCode, href);
                break;

            case CodeInline code:
                runs.Add(new OtkRun(code.Content, false, false, true, null));
                break;

            case LinkInline link when !link.IsImage:
                var linkHref = link.Url ?? "";
                foreach (var child in link)
                    CollectInlinesRecursive(child, runs, bold, italic, isCode, linkHref);
                break;

            case LinkInline link when link.IsImage:
                // Images handled at block level
                break;

            case LineBreakInline:
                runs.Add(new OtkRun("\n", bold, italic, isCode, href));
                break;

            case ContainerInline container:
                foreach (var child in container)
                    CollectInlinesRecursive(child, runs, bold, italic, isCode, href);
                break;
        }
    }

    #endregion

    #region OTK Emission

    private void EmitContent(List<OtkRun> runs)
    {
        if (runs.Count == 0)
        {
            _output.AppendLine("SET \"\"");
            return;
        }

        // If all runs are plain text with no formatting, use simple SET
        if (runs.All(r => !r.Bold && !r.Italic && !r.IsCode && r.Href == null))
        {
            var text = string.Concat(runs.Select(r => r.Text));
            _output.AppendLine($"SET \"{Escape(text)}\"");
            return;
        }

        // Emit SET RUNS for mixed formatting
        _output.AppendLine("SET RUNS");
        foreach (var run in runs)
        {
            _output.Append($"  RUN \"{Escape(run.Text)}\"");

            if (run.IsCode)
            {
                _output.Append($" font-name=\"{_styles.CodeFontName}\"");
                _output.Append($" font-size={_styles.CodeFontSize}pt");
                if (!string.IsNullOrEmpty(_styles.CodeBackgroundColor))
                    _output.Append($" background-color=#{_styles.CodeBackgroundColor}");
            }
            else
            {
                if (run.Bold) _output.Append(" bold=true");
                if (run.Italic) _output.Append(" italic=true");
            }

            if (run.Href != null)
            {
                _output.Append($" href=\"{Escape(run.Href)}\"");
                _output.Append(" color=#0563C1 underline=single");
            }

            _output.AppendLine();
        }
    }

    #endregion

    #region Helpers

    private static string Escape(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    private static string ExtractCodeContent(CodeBlock codeBlock)
    {
        var sb = new StringBuilder();
        foreach (var line in codeBlock.Lines)
        {
            var text = line.ToString();
            if (text != null)
                sb.AppendLine(text);
        }
        // Remove trailing newline
        var result = sb.ToString().TrimEnd('\r', '\n');
        return result;
    }

    private static bool IsMermaidLanguage(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "mermaid" or "flowchart" or "graph" or "sequencediagram"
                or "classdiagram" or "statediagram" or "gantt" => true,
            _ => false
        };
    }

    private string GetSyntaxColor(TokenType tokenType)
    {
        var scheme = _styles.SyntaxColorScheme ?? new SyntaxColorScheme();
        return scheme.GetColorForTokenType(tokenType);
    }

    private static string ExtractCellText(TableCell cell)
    {
        var sb = new StringBuilder();
        foreach (var block in cell)
        {
            if (block is ParagraphBlock para && para.Inline != null)
            {
                foreach (var inline in para.Inline)
                {
                    if (inline is LiteralInline lit)
                        sb.Append(lit.Content);
                    else if (inline is EmphasisInline emph)
                    {
                        foreach (var child in emph)
                            if (child is LiteralInline childLit)
                                sb.Append(childLit.Content);
                    }
                    else if (inline is CodeInline code)
                        sb.Append(code.Content);
                }
            }
        }
        return sb.ToString();
    }

    private List<(string Text, bool Nested)> CollectListItems(ListBlock list, bool nested = false)
    {
        var items = new List<(string Text, bool Nested)>();
        foreach (var item in list)
        {
            if (item is ListItemBlock listItem)
            {
                // Get text from first paragraph
                var firstPara = listItem.OfType<ParagraphBlock>().FirstOrDefault();
                var text = "";
                if (firstPara?.Inline != null)
                {
                    text = string.Concat(
                        firstPara.Inline.OfType<LiteralInline>().Select(l => l.Content.ToString()));
                }
                items.Add((text, nested));

                // Check for nested lists
                foreach (var child in listItem)
                {
                    if (child is ListBlock nestedList)
                    {
                        items.AddRange(CollectListItems(nestedList, nested: true));
                    }
                }
            }
        }
        return items;
    }

    #endregion
}

/// <summary>
/// Represents a single run of text with formatting for OTK emission.
/// </summary>
internal record OtkRun(string Text, bool Bold, bool Italic, bool IsCode, string? Href);
