# MarkMyWord

A .NET 9 library for bidirectional conversion between CommonMark/GitHub Flavored Markdown and Microsoft Word (.docx) documents.

## Features

MarkMyWord provides bidirectional conversion between Markdown and Word:
- **Markdown → Word**: Convert CommonMark markdown to Word (.docx) documents using the Open XML SDK
- **Word → Markdown**: Convert Word documents back to CommonMark or GitHub Flavored Markdown, optimized for LLM grounding and roundtripping

### Markdown to Word - Currently Supported

✅ **Block Elements**
- Headings (ATX: `# H1` through `###### H6`, Setext: underlined)
- Paragraphs
- Code blocks (fenced with ``` and indented)
- Block quotes (`>`)
- Thematic breaks (`---`, `***`, `___`)
- **Lists** (ordered, unordered, nested with proper indentation)
- **Tables** (with headers, borders, and shading)
  - All inline elements supported in prose (hyperlinks, inline code, emphasis/strong, line breaks, images) are also rendered inside table cells. Images currently render as `[Image: alt]` placeholders.

✅ **Inline Elements**
- Bold (`**text**` or `__text__`)
- Italic (`*text*` or `_text_`)
- Bold + Italic (`***text***`)
- Inline code (`` `code` ``)
- Links (`[text](url)`)
- **Images** (`![alt](url)` - supports local files and URLs with fallback)
- Hard line breaks (two spaces or `\` at end of line)

✅ **Styling**
- Customizable fonts and colors
- Configurable heading styles
- **Code syntax highlighting** for JSON, TypeSpec, and Bash
- Automatic spell/grammar check suppression for code blocks

### Coming Soon

- Task lists
- Footnotes
- Definition lists

### Word to Markdown Conversion

✅ **Supported Elements**
- Headings (H1-H6) → Markdown headings (`#` through `######`)
- Paragraphs → Plain text with proper spacing
- **Bold** and *Italic* text → Markdown emphasis (`**bold**`, `*italic*`)
- Inline code → Backtick syntax (`` `code` ``)
- Lists (ordered and unordered, nested) → Markdown lists
- Tables → GitHub Flavored Markdown table syntax
- Links → `[text](url)` syntax
- Images → `![alt](path)` with optional extraction
- Code blocks → Fenced code blocks with ` ``` `
- Block quotes → `>` prefix

✅ **Conversion Options**
- **CommonMark or GitHub Flavored Markdown** output
- **LLM-optimized output** - Clean, semantic content for AI grounding
- **Metadata extraction** - Document properties as YAML frontmatter
- **Image extraction** - Save embedded images to files
- **Roundtripping support** - Preserve formatting for Word ↔ Markdown conversion

## Installation

```bash
dotnet add package MarkMyWord
```

## Quick Start

### Markdown to Word

```csharp
using MarkMyWord;

// Convert markdown string to .docx file
string markdown = "# Hello World\n\nThis is **bold** text.";
MarkdownConverter.ConvertToDocx(markdown, "output.docx");
```

### Word to Markdown

```csharp
using MarkMyWord;
using MarkMyWord.Configuration;

// Convert Word document to markdown file
WordConverter.ConvertToMarkdown("input.docx", "output.md");

// With options for LLM grounding
var options = new WordToMarkdownOptions
{
    Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,
    OptimizeForLLM = true,
    ExtractImages = true,
    IncludeMetadata = false
};
WordConverter.ConvertToMarkdown("document.docx", "output.md", options);

// Convert to string
string markdown = WordConverter.ConvertToMarkdownString("document.docx");
```

### Convert to Byte Array

```csharp
// Get the document as a byte array (useful for web scenarios)
byte[] docxBytes = MarkdownConverter.ConvertToDocxBytes(markdown);
```

### Stream-based Conversion

```csharp
// Convert from stream to stream
using var inputStream = File.OpenRead("input.md");
using var outputStream = File.Create("output.docx");
MarkdownConverter.ConvertToDocx(inputStream, outputStream);
```

### Async Conversion

```csharp
// Async conversion
await MarkdownConverter.ConvertToDocxAsync(markdown, "output.docx");
```

### Lists

```csharp
// Unordered list
string unorderedList = @"
# Shopping List
- Apples
- Bananas
- Oranges
  - Naval oranges
  - Blood oranges
";

MarkdownConverter.ConvertToDocx(unorderedList, "shopping.docx");

// Ordered list
string orderedList = @"
# Instructions
1. Preheat oven to 350°F
2. Mix ingredients
3. Bake for 30 minutes
   1. Check at 25 minutes
   2. Test with toothpick
4. Let cool
";

MarkdownConverter.ConvertToDocx(orderedList, "instructions.docx");
```

## Command-Line Interface

MarkMyWord includes a powerful CLI for converting markdown files from the command line.

### Installation

```bash
dotnet tool install --global SpecWorks.MarkMyWord.CLI
```

Or run directly from the project:

```bash
dotnet run --project src/MarkMyWord.CLI/MarkMyWord.CLI.csproj -- [command] [options]
```

### Usage

The CLI automatically detects the conversion direction based on file extension:
- `.md` or `.markdown` input → converts to Word (`.docx`)
- `.docx` input → converts to Markdown (`.md`)

**Markdown to Word:**
```bash
markmyword convert -i README.md
markmyword convert -i input.md -o output.docx
```

**Word to Markdown:**
```bash
markmyword convert -i document.docx
markmyword convert -i input.docx -o output.md
```

**Specify output file:**
```bash
markmyword convert -i input.md -o output.docx
```

**Custom font and size:**
```bash
markmyword convert -i document.md --font "Times New Roman" --font-size 12
```

**Use dark theme:**
```bash
markmyword convert -i document.md --theme dark
```

**Use custom style configuration:**
```bash
markmyword convert -i document.md --style custom-style.json
```

**Verbose output:**
```bash
markmyword convert -i document.md -v
```

**Force overwrite:**
```bash
markmyword convert -i document.md --force
```

**Word to Markdown with options:**
```bash
# Convert Word to GitHub Flavored Markdown (default)
markmyword convert -i document.docx -o output.md

# Convert to strict CommonMark
markmyword convert -i document.docx --commonmark

# Optimize for LLM grounding (clean, semantic output)
markmyword convert -i document.docx --optimize-llm

# Include document metadata as YAML frontmatter
markmyword convert -i document.docx --include-metadata

# Don't extract images
markmyword convert -i document.docx --extract-images false
```

**View version:**
```bash
markmyword version
```

**Get help:**
```bash
markmyword --help
markmyword convert --help
```

### CLI Options

#### Common Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--input` | `-i` | Input file path (.md or .docx) (required) |
| `--output` | `-o` | Output file path (auto-detects extension) |
| `--verbose` | `-v` | Enable verbose output |
| `--force` | - | Overwrite output file if it exists |

#### Markdown to Word Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--font` | `-f` | Default font name (e.g., 'Calibri') |
| `--font-size` | `-s` | Default font size (6-72 points) |
| `--theme` | - | Color theme: `light` (default) or `dark` |
| `--style` | - | Path to JSON style configuration file |

#### Word to Markdown Options

| Option | Description | Default |
|--------|-------------|---------|
| `--to-markdown` | Explicitly convert to Markdown (auto-detected) | `false` |
| `--extract-images` | Extract and save embedded images | `true` |
| `--optimize-llm` | Optimize output for LLM grounding | `true` |
| `--commonmark` | Use strict CommonMark instead of GFM | `false` |
| `--include-metadata` | Include document metadata as YAML frontmatter | `false` |

## Advanced Usage

### Word to Markdown Options

```csharp
using MarkMyWord;
using MarkMyWord.Configuration;

// Configure Word to Markdown conversion
var options = new WordToMarkdownOptions
{
    // Use GitHub Flavored Markdown (supports tables, strikethrough, etc.)
    Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,

    // Optimize for LLM grounding (clean, semantic content)
    OptimizeForLLM = true,

    // Extract embedded images to files
    ExtractImages = true,

    // Directory for extracted images (default: same as output file)
    ImageOutputDirectory = "./images",

    // URL prefix for image links in markdown
    ImageUrlPrefix = "https://example.com/images/",

    // Include document metadata as YAML frontmatter
    IncludeMetadata = true,

    // Preserve formatting metadata for roundtripping
    PreserveFormattingMetadata = false,

    // Use HTML for complex formatting when no markdown equivalent exists
    UseHtmlForComplexFormatting = false,

    // Line ending style
    LineEndings = LineEndingStyle.LF
};

WordConverter.ConvertToMarkdown("document.docx", "output.md", options);
```

### Markdown to Word Custom Styling

```csharp
using MarkMyWord.Configuration;

var options = new ConversionOptions
{
    Styles = new StyleConfiguration
    {
        DefaultFontName = "Georgia",
        DefaultFontSize = 12,
        CodeFontName = "Fira Code",
        CodeFontSize = 10,
        CodeBackgroundColor = "F5F5F5",
        QuoteLeftBorderColor = "4A90E2",
        QuoteBackgroundColor = "EEF7FF"
    }
};

MarkdownConverter.ConvertToDocx(markdown, "output.docx", options);
```

### Custom Heading Styles

```csharp
var options = new ConversionOptions
{
    Styles = new StyleConfiguration
    {
        HeadingStyles = new[]
        {
            new HeadingStyle
            {
                Level = 1,
                FontSize = 24,
                Bold = true,
                Color = "2E74B5",
                SpacingBeforeTwips = 480,  // 1/3 inch
                SpacingAfterTwips = 240    // 1/6 inch
            },
            // ... configure levels 2-6
        }
    }
};
```

### JSON Style Configuration

You can define styles in a JSON file and load them using the CLI or programmatically:

**custom-style.json:**
```json
{
  "styles": {
    "defaultFontName": "Georgia",
    "defaultFontSize": 12,
    "headingStyles": [
      {
        "level": 1,
        "fontSize": 28,
        "bold": true,
        "color": "2E74B5",
        "spacingBeforeTwips": 480,
        "spacingAfterTwips": 240
      },
      {
        "level": 2,
        "fontSize": 20,
        "bold": true,
        "color": "2E74B5",
        "spacingBeforeTwips": 400,
        "spacingAfterTwips": 200
      },
      {
        "level": 3,
        "fontSize": 16,
        "bold": true,
        "color": "1F4D78",
        "spacingBeforeTwips": 320,
        "spacingAfterTwips": 160
      }
    ],
    "codeFontName": "Fira Code",
    "codeFontSize": 10,
    "codeBackgroundColor": "282C34",
    "quoteLeftBorderColor": "4A90E2",
    "quoteLeftBorderWidth": 4,
    "quoteBackgroundColor": "EEF7FF",
    "listIndentationTwips": 720
  }
}
```

**Use with CLI:**
```bash
markmyword convert -i document.md --style custom-style.json
```

**Load programmatically:**
```csharp
using System.Text.Json;

var json = File.ReadAllText("custom-style.json");
var config = JsonSerializer.Deserialize<ConversionOptions>(json);
MarkdownConverter.ConvertToDocx(markdown, "output.docx", config);
```

**Notes:**
- Colors are hex values without the `#` prefix
- Spacing values are in twips (1440 twips = 1 inch)
- Font sizes are in points

### Document Metadata

```csharp
var options = new ConversionOptions
{
    DocumentTitle = "My Document",
    Author = "John Doe",
    Subject = "Technical Documentation"
};

MarkdownConverter.ConvertToDocx(markdown, "output.docx", options);
```

## Architecture

MarkMyWord uses a three-stage conversion pipeline:

1. **Parse**: Markdown is parsed into an Abstract Syntax Tree (AST) using [Markdig](https://github.com/xoofx/markdig)
2. **Render**: The AST is traversed and converted to OpenXML elements using specialized renderers
3. **Style**: Styles are applied and the document is saved using [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK)

```
Markdown Text
     ↓
  Markdig Parser
     ↓
   AST (Syntax Tree)
     ↓
OpenXmlRenderer
     ↓
  OpenXML Elements
     ↓
 Word Document (.docx)
```

## Project Structure

```
MarkMyWord/
├── src/
│   ├── MarkMyWord/                    # Core library
│   │   ├── MarkdownConverter.cs       # Public API
│   │   ├── Converters/
│   │   │   ├── OpenXmlRenderer.cs     # Main renderer
│   │   │   ├── BlockRenderers/        # Block element renderers
│   │   │   │   ├── CodeBlockRenderer.cs  # Code block rendering
│   │   │   │   ├── ListRenderer.cs    # List rendering
│   │   │   │   └── TableRenderer.cs   # Table rendering
│   │   │   └── InlineRenderers/       # Inline element renderers
│   │   │       └── LinkInlineRenderer.cs  # Links & images
│   │   ├── SyntaxHighlighting/        # Syntax highlighting
│   │   │   ├── ISyntaxHighlighter.cs  # Highlighter interface
│   │   │   ├── ColorCodeHighlighter.cs   # JSON highlighting
│   │   │   ├── TypeSpecHighlighter.cs    # TypeSpec highlighting
│   │   │   ├── BashHighlighter.cs     # Bash/Shell highlighting
│   │   │   └── SyntaxHighlighterFactory.cs
│   │   ├── OpenXml/
│   │   │   ├── DocumentBuilder.cs     # OpenXML document builder
│   │   │   ├── StyleManager.cs        # Style management
│   │   │   └── ListManager.cs         # List numbering management
│   │   └── Configuration/             # Configuration classes
│   └── MarkMyWord.CLI/                # Command-line tool
└── tests/
    └── MarkMyWord.Tests/              # Unit tests (29 tests)
```

## Requirements

- .NET 9.0 or later
- Dependencies:
  - Markdig 0.37.0
  - DocumentFormat.OpenXml 3.1.0
  - ColorCode.Core 2.0.15

## Building from Source

```bash
git clone https://github.com/yourusername/MarkMyWord.git
cd MarkMyWord
dotnet build
dotnet test
```

## Examples

### Convert a README file

```csharp
var markdown = File.ReadAllText("README.md");
MarkdownConverter.ConvertToDocx(markdown, "README.docx");
```

### Lists Example

```csharp
string markdown = @"
# Shopping List

## Groceries
- Milk
- Eggs
- Bread

## Tasks
1. Buy groceries
2. Clean house
3. Do laundry
   - Whites
   - Colors
";

MarkdownConverter.ConvertToDocx(markdown, "shopping-list.docx");
```

### Images Example

```csharp
string markdown = @"
# Product Documentation

![Product Logo](logo.png)

Our product features:
![Feature 1](https://example.com/feature1.png)
![Feature 2](./images/feature2.jpg)
";

MarkdownConverter.ConvertToDocx(markdown, "product-doc.docx");
```

### Tables Example

```csharp
string markdown = @"
# Sales Report

| Product | Q1 Sales | Q2 Sales | Total |
|---------|----------|----------|-------|
| Widget A | $1,000 | $1,200 | $2,200 |
| Widget B | $850 | $900 | $1,750 |
| Widget C | $1,500 | $1,600 | $3,100 |
";

MarkdownConverter.ConvertToDocx(markdown, "sales-report.docx");
```

### Syntax Highlighting

MarkMyWord supports syntax highlighting for code blocks, making code more readable in Word documents:

**Supported Languages:**
- **JSON** - Property names, strings, numbers, keywords (true/false/null)
- **TypeSpec** - Keywords, types, decorators, comments
- **Bash/Shell** - Commands, keywords, variables, strings, comments

**Example:**
```csharp
string markdown = @"
# API Response

```json
{
  ""name"": ""MarkMyWord"",
  ""version"": ""0.2.0"",
  ""enabled"": true
}
```

```bash
#!/bin/bash
echo ""Converting files...""
for file in *.md; do
    markmyword convert -i ""$file""
done
```
";

MarkdownConverter.ConvertToDocx(markdown, "highlighted.docx");
```

**Features:**
- Colors optimized for grey code block backgrounds
- Automatic spell/grammar check suppression (no red squiggles!)
- Trailing empty lines automatically removed
- Property names vs values distinguished in JSON

**Disable syntax highlighting:**
```csharp
var options = new ConversionOptions
{
    EnableSyntaxHighlighting = false
};

MarkdownConverter.ConvertToDocx(markdown, "plain-code.docx", options);
```

**Custom syntax colors:**
```csharp
var options = new ConversionOptions
{
    Styles = new StyleConfiguration
    {
        SyntaxColorScheme = new SyntaxColorScheme
        {
            KeywordColor = "0000FF",     // Blue
            StringColor = "A31515",      // Red
            NumberColor = "098658",      // Green
            CommentColor = "6A9955",     // Green
            TypeColor = "4EC9B0",        // Cyan
            FunctionColor = "C4A000"     // Gold
        }
    }
};
```

### Convert with custom code block styling

```csharp
var options = new ConversionOptions
{
    Styles = new StyleConfiguration
    {
        CodeFontName = "Consolas",
        CodeFontSize = 9,
        CodeBackgroundColor = "F5F5F5"  // Light grey
    }
};

string code = @"
# Code Example

```json
{
  ""message"": ""Hello, World!""
}
```
";

MarkdownConverter.ConvertToDocx(code, "code.docx", options);
```

## Testing

The library includes comprehensive unit tests covering all supported markdown syntax:

```bash
dotnet test
```

Current test coverage (29 tests):
- ✅ Basic paragraph conversion
- ✅ Headings (levels 1-6)
- ✅ Bold and italic text
- ✅ Inline code
- ✅ Code blocks with language labels
- ✅ Hyperlinks
- ✅ **Lists** (ordered, unordered, nested, mixed, with formatting)
- ✅ **Images** (with fallback for missing images)
- ✅ **Tables** (headers, multiple rows, shading, borders)

## Specifications

MarkMyWord implements:
- [CommonMark 0.31.2](https://spec.commonmark.org/0.31.2/) - Markdown specification
- [Office Open XML](https://learn.microsoft.com/en-us/office/open-xml/) - Word document format

## Contributing

Contributions are welcome! Areas for contribution:
- Task lists and checkboxes
- Footnotes and references
- Definition lists
- Extended markdown features
- Performance optimizations
- Additional tests

## License

MIT License - See LICENSE file for details

## Acknowledgments

- Built with [Markdig](https://github.com/xoofx/markdig) by Alexandre Mutel
- Uses [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK) by Microsoft
- Implements [CommonMark](https://commonmark.org/) specification

## Status

🚀 **Beta** - Core functionality and CLI are fully working with comprehensive CommonMark support.

**Fully supported:**
- Headings, paragraphs, emphasis (bold/italic)
- Code blocks (fenced and indented) with **syntax highlighting** (JSON, TypeSpec, Bash)
- Inline code
- Links and hyperlinks
- Block quotes
- Horizontal rules / thematic breaks
- **Lists** (ordered, unordered, nested with proper numbering)
- **Images** (local files and URLs with fallback support)
- **Tables** (with headers, borders, and shading)
  - All inline elements supported in prose (hyperlinks, inline code, emphasis/strong, line breaks, images) are also rendered inside table cells. Images currently render as `[Image: alt]` placeholders.
- Command-line interface with full options
- Spell/grammar check suppression for code

**Coming soon:** Task lists, footnotes, definition lists

---

Generated with ❤️ using Claude Code
