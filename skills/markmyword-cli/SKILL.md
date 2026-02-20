---
name: markmyword-cli
description: >
  Convert between Markdown and Microsoft Word (.docx) documents using the MarkMyWord CLI tool.
  Use this skill when the user wants to create a Word document from Markdown, convert a .docx file
  to Markdown, generate documentation in Word format, or roundtrip between Markdown and Word.
  Activate when the user mentions Word documents, .docx files, document conversion, or
  Markdown-to-Word workflows. Also useful for LLM grounding from Word documents.
license: MIT
compatibility: Requires .NET 9.0 or later SDK. Works on Windows, macOS, and Linux.
metadata:
  author: spec-works
  version: "1.0"
  repository: https://github.com/spec-works/MarkMyWord
---

# MarkMyWord CLI — Markdown ↔ Word Conversion

Bidirectional conversion between CommonMark/GitHub Flavored Markdown and Microsoft Word (.docx) documents.

## Prerequisites — Installing .NET

The MarkMyWord CLI requires .NET 9.0 or later. Install the SDK for your platform:

### Windows

```powershell
# Using winget (recommended)
winget install Microsoft.DotNet.SDK.9

# Or download from https://dotnet.microsoft.com/download/dotnet/9.0
```

### macOS

```bash
# Using Homebrew (recommended)
brew install dotnet-sdk

# Or download from https://dotnet.microsoft.com/download/dotnet/9.0
```

### Linux (Ubuntu/Debian)

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

### Linux (Fedora/RHEL)

```bash
sudo dnf install dotnet-sdk-9.0
```

### Verify Installation

```bash
dotnet --version
# Should show 9.x.x or later
```

## Installing the CLI Tool

```bash
dotnet tool install --global SpecWorks.MarkMyWord.CLI
```

Verify it works:

```bash
markmyword version
```

## Basic Usage

The CLI auto-detects conversion direction from the file extension.

### Markdown to Word

```bash
# Basic conversion (output: README.docx)
markmyword convert -i README.md

# Specify output file
markmyword convert -i input.md -o document.docx

# Custom font and size
markmyword convert -i input.md --font "Times New Roman" --font-size 12

# With custom style configuration
markmyword convert -i input.md --style custom-style.json

# Verbose output with overwrite
markmyword convert -i input.md -v --force
```

### Word to Markdown

```bash
# Basic conversion (output: document.md)
markmyword convert -i document.docx

# GitHub Flavored Markdown (default)
markmyword convert -i document.docx -o output.md

# Strict CommonMark output
markmyword convert -i document.docx --commonmark

# Optimize for LLM grounding
markmyword convert -i document.docx --optimize-llm

# Include document metadata as YAML frontmatter
markmyword convert -i document.docx --include-metadata

# Skip image extraction
markmyword convert -i document.docx --extract-images false
```

### CLI Options

#### Common Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--input` | `-i` | Input file path (.md or .docx) (required) |
| `--output` | `-o` | Output file path (auto-detects extension) |
| `--verbose` | `-v` | Enable verbose output |
| `--force` | — | Overwrite output file if it exists |

#### Markdown → Word Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--font` | `-f` | Default font name (e.g., 'Calibri') |
| `--font-size` | `-s` | Default font size (6–72 points) |
| `--style` | — | Path to JSON style configuration file |

#### Word → Markdown Options

| Option | Description | Default |
|--------|-------------|---------|
| `--extract-images` | Extract and save embedded images | `true` |
| `--optimize-llm` | Optimize output for LLM grounding | `true` |
| `--commonmark` | Use strict CommonMark instead of GFM | `false` |
| `--include-metadata` | Include document metadata as YAML frontmatter | `false` |

## Structuring Markdown for Best Word Output

### Document Structure

Word documents map naturally from Markdown — use standard document conventions:

```markdown
# Document Title

Introductory paragraph for the document.

## First Section

Body text with **bold**, *italic*, and `inline code` formatting.

### Subsection

More detailed content here.

## Second Section

Another major section of the document.
```

### Headings

All heading levels (H1–H6) map to Word heading styles:
- `# H1` → Heading 1 (typically the document title)
- `## H2` → Heading 2 (major sections)
- `### H3` through `###### H6` → Heading 3–6 (subsections)

### Best Practices for Word Output

1. **Use H1 sparingly** — typically just for the document title
2. **Use H2 for major sections** — these appear in Word's navigation pane
3. **Tables work well** — GFM table syntax converts to proper Word tables with borders and header shading
4. **Images are fully supported** — local files and URLs; embedded in the document
5. **Code blocks get syntax highlighting** — JSON, TypeSpec, and Bash are color-highlighted
6. **Block quotes render styled** — left border and background color, great for callouts
7. **Lists support deep nesting** — ordered and unordered with proper indentation
8. **Keep paragraphs focused** — Word renders each Markdown paragraph as a Word paragraph

### Style Configuration

Create a JSON file for custom styling:

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
        "color": "2E74B5"
      },
      {
        "level": 2,
        "fontSize": 20,
        "bold": true,
        "color": "2E74B5"
      }
    ],
    "codeFontName": "Fira Code",
    "codeFontSize": 10,
    "codeBackgroundColor": "F5F5F5"
  }
}
```

Use with: `markmyword convert -i input.md --style custom-style.json`

## Supported Markdown Elements

### Markdown → Word

| Element | Syntax | Word Result |
|---------|--------|-------------|
| Headings (ATX) | `# H1` – `###### H6` | Word Heading 1–6 styles |
| Headings (Setext) | Underlined with `=` or `-` | Word Heading 1–2 styles |
| Paragraphs | Plain text | Word paragraphs |
| Code blocks | ` ```lang ` or indented | Styled code with syntax highlighting |
| Block quotes | `> text` | Styled with left border and background |
| Thematic breaks | `---`, `***`, `___` | Horizontal rule |
| Unordered lists | `- item` | Bulleted list |
| Ordered lists | `1. item` | Numbered list |
| Nested lists | Indented items | Proper Word indentation |
| Tables | GFM table syntax | Word table with borders and header shading |
| Bold | `**text**` | Bold run |
| Italic | `*text*` | Italic run |
| Bold+Italic | `***text***` | Bold italic run |
| Inline code | `` `code` `` | Monospace font run |
| Links | `[text](url)` | Word hyperlink |
| Images | `![alt](url)` | Embedded image (local files and URLs) |
| Hard line breaks | Two spaces or `\` at EOL | Line break |

### Word → Markdown

| Word Element | Markdown Result |
|-------------|-----------------|
| Heading 1–6 | `#` through `######` |
| Paragraphs | Plain text with spacing |
| Bold / Italic | `**bold**` / `*italic*` |
| Inline code | `` `code` `` |
| Lists (ordered/unordered) | Markdown lists with nesting |
| Tables | GFM table syntax |
| Hyperlinks | `[text](url)` |
| Images | `![alt](path)` with optional extraction |
| Code blocks | Fenced code blocks |
| Block quotes | `>` prefix |

### Syntax Highlighting (Markdown → Word)

Supported languages for color-highlighted code blocks:
- **JSON** — property names, strings, numbers, booleans
- **TypeSpec** — keywords, types, decorators, comments
- **Bash/Shell** — commands, keywords, variables, strings

## Limitations — What MarkMyWord Cannot Do

### Markdown → Word Limitations

1. **No custom Word templates** — output uses default styles; you cannot apply a `.dotx` template
2. **No math/LaTeX rendering** — formulas render as plain text
3. **No Mermaid diagram rendering** — diagram code blocks render as plain code text
4. **No task lists** — `- [ ]` checkboxes are not rendered as checkboxes
5. **No footnotes** — footnote syntax is not converted
6. **No definition lists** — definition list syntax is not supported
7. **No strikethrough** — `~~text~~` is not rendered with strikethrough
8. **Limited syntax highlighting** — only JSON, TypeSpec, and Bash; other languages render as plain monospace
9. **No table alignment** — GFM column alignment (`:---`, `:---:`, `---:`) is not preserved
10. **No embedded video/audio** — media links render as text hyperlinks

### Word → Markdown Limitations

1. **Complex formatting loss** — Word features without Markdown equivalents (text boxes, SmartArt, charts) are skipped
2. **Style-based detection** — relies on Word styles for heading detection; manually sized text may not be recognized as headings
3. **No track changes** — revision marks are not preserved
4. **No comments** — Word comments are stripped
5. **Table cell merging** — merged cells may not convert correctly
6. **Image format limits** — some embedded image formats may not extract properly

## Installing This Skill

### GitHub Copilot CLI (personal)

```bash
git clone https://github.com/spec-works/MarkMyWord.git /tmp/MarkMyWord
mkdir -p ~/.copilot/skills/markmyword-cli
cp -r /tmp/MarkMyWord/skills/markmyword-cli/* ~/.copilot/skills/markmyword-cli/
```

### GitHub Copilot CLI (project)

```bash
mkdir -p .github/skills/markmyword-cli
cp -r /path/to/MarkMyWord/skills/markmyword-cli/* .github/skills/markmyword-cli/
git add .github/skills/
git commit -m "Add MarkMyWord CLI agent skill"
```

### Claude Code (personal)

```bash
mkdir -p ~/.claude/skills/markmyword-cli
cp -r /path/to/MarkMyWord/skills/markmyword-cli/* ~/.claude/skills/markmyword-cli/
```

### Claude Code (project)

```bash
mkdir -p .claude/skills/markmyword-cli
cp -r /path/to/MarkMyWord/skills/markmyword-cli/* .claude/skills/markmyword-cli/
```

### VS Code / Cursor (project)

```bash
mkdir -p .github/skills/markmyword-cli
cp -r /path/to/MarkMyWord/skills/markmyword-cli/* .github/skills/markmyword-cli/
```

After installing, restart your agent session to pick up the new skill.

## Further Reference

See [references/REFERENCE.md](references/REFERENCE.md) for detailed examples, advanced styling, Word-to-Markdown options, and the full architecture overview.
