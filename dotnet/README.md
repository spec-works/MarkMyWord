# MarkMyWord

A .NET 9 library for converting CommonMark markdown to Microsoft Word (.docx) documents.

## Features

MarkMyWord converts markdown documents to Word format using the Open XML SDK. It supports the CommonMark specification including:

### Currently Supported

✅ **Block Elements**
- Headings (ATX: `# H1` through `###### H6`, Setext: underlined)
- Paragraphs
- Code blocks (fenced with ``` and indented)
- Block quotes (`>`)
- Thematic breaks (`---`, `***`, `___`)
- **Lists** (ordered, unordered, nested with proper indentation)
- **Tables** (with headers, borders, and shading)

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
- Code syntax highlighting support

### Coming Soon

- Task lists
- Footnotes
- Definition lists

## Installation

```bash
dotnet add package MarkMyWord
```

## Quick Start

### Basic Usage

```csharp
using MarkMyWord;

// Convert markdown string to .docx file
string markdown = "# Hello World\n\nThis is **bold** text.";
MarkdownConverter.ConvertToDocx(markdown, "output.docx");
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

**Basic conversion:**
```bash
markmyword convert -i README.md
```

**Specify output file:**
```bash
markmyword convert -i input.md -o output.docx
```

**Custom font and size:**
```bash
markmyword convert -i document.md --font "Times New Roman" --font-size 12
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

| Option | Alias | Description |
|--------|-------|-------------|
| `--input` | `-i` | Input markdown file path (required) |
| `--output` | `-o` | Output .docx file path (default: same as input) |
| `--verbose` | `-v` | Enable verbose output |
| `--font` | `-f` | Default font name |
| `--font-size` | `-s` | Default font size (6-72 points) |
| `--style` | - | Path to JSON style configuration file |
| `--force` | - | Overwrite output file if it exists |

## Advanced Usage

### Custom Styling

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
│   │   │   │   ├── ListRenderer.cs    # List rendering
│   │   │   │   └── TableRenderer.cs   # Table rendering
│   │   │   └── InlineRenderers/       # Inline element renderers
│   │   │       └── LinkInlineRenderer.cs  # Links & images
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

### Convert with custom code block styling

```csharp
var options = new ConversionOptions
{
    Styles = new StyleConfiguration
    {
        CodeFontName = "Consolas",
        CodeFontSize = 9,
        CodeBackgroundColor = "282C34"  // Dark theme
    }
};

string code = @"
# Code Example

```csharp
public class HelloWorld
{
    public static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
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
- Code blocks (fenced and indented) with language labels
- Inline code
- Links and hyperlinks
- Block quotes
- Horizontal rules / thematic breaks
- **Lists** (ordered, unordered, nested with proper numbering)
- **Images** (local files and URLs with fallback support)
- **Tables** (with headers, borders, and shading)
- Command-line interface with full options

**Coming soon:** Task lists, footnotes, definition lists

---

Generated with ❤️ using Claude Code
