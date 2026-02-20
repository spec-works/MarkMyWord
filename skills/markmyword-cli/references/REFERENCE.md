# MarkMyWord CLI — Detailed Reference

## Architecture

MarkMyWord uses a three-stage conversion pipeline:

1. **Parse** — Markdown is parsed into an AST using [Markdig](https://github.com/xoofx/markdig)
2. **Render** — The AST is traversed and converted to OpenXML elements using specialized renderers
3. **Style** — Styles are applied and the document is saved using [DocumentFormat.OpenXml](https://github.com/dotnet/Open-XML-SDK)

```
Markdown Text               Word Document (.docx)
     ↓                            ↓
  Markdig Parser              WordToMarkdownConverter
     ↓                            ↓
  AST (Syntax Tree)           Markdown Text
     ↓
  OpenXmlRenderer
     ↓
  OpenXML Elements
     ↓
  Word Document (.docx)
```

## Advanced Styling Options

### Full Style Configuration JSON

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

**Notes:**
- Colors are hex values without the `#` prefix
- Spacing values are in twips (1440 twips = 1 inch)
- Font sizes are in points

### Custom Syntax Highlighting Colors

```csharp
var options = new ConversionOptions
{
    Styles = new StyleConfiguration
    {
        SyntaxColorScheme = new SyntaxColorScheme
        {
            KeywordColor = "0000FF",
            StringColor = "A31515",
            NumberColor = "098658",
            CommentColor = "6A9955",
            TypeColor = "4EC9B0",
            FunctionColor = "C4A000"
        }
    }
};
```

### Disable Syntax Highlighting

```csharp
var options = new ConversionOptions
{
    EnableSyntaxHighlighting = false
};
```

## Word to Markdown — Advanced Options

### Full Options Reference

```csharp
var options = new WordToMarkdownOptions
{
    // Output flavor
    Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,  // or CommonMark

    // LLM optimization (clean, semantic content)
    OptimizeForLLM = true,

    // Image handling
    ExtractImages = true,
    ImageOutputDirectory = "./images",
    ImageUrlPrefix = "https://example.com/images/",

    // Metadata
    IncludeMetadata = true,

    // Roundtripping
    PreserveFormattingMetadata = false,

    // Complex formatting
    UseHtmlForComplexFormatting = false,

    // Line endings
    LineEndings = LineEndingStyle.LF  // or CRLF
};
```

### LLM-Optimized Output

When `--optimize-llm` is enabled:
- Removes decorative formatting that doesn't convey meaning
- Produces clean, semantic Markdown ideal for AI/LLM grounding
- Focuses on content structure over visual presentation
- Good default for RAG pipelines and document indexing

### Metadata Extraction

When `--include-metadata` is enabled, document properties are output as YAML frontmatter:

```markdown
---
title: "Quarterly Report"
author: "Jane Doe"
subject: "Q4 2025 Results"
created: "2025-12-01T10:00:00Z"
modified: "2025-12-15T14:30:00Z"
---

# Quarterly Report
...
```

## Example: Technical Document

```markdown
# API Reference — User Service

## Overview

The User Service provides CRUD operations for user management.
All endpoints require authentication via Bearer token.

## Endpoints

### Create User

```json
POST /api/v1/users
Content-Type: application/json

{
  "name": "Jane Doe",
  "email": "jane@example.com",
  "role": "admin"
}
```

### List Users

```bash
curl -H "Authorization: Bearer $TOKEN" \
     https://api.example.com/api/v1/users
```

## Response Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Successful request |
| 201 | Created | Resource created |
| 400 | Bad Request | Invalid input |
| 401 | Unauthorized | Missing or invalid token |
| 404 | Not Found | Resource does not exist |
| 500 | Server Error | Internal failure |

## Data Model

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string (UUID) | Auto | Unique identifier |
| `name` | string | Yes | Full name |
| `email` | string | Yes | Email address |
| `role` | enum | No | `user`, `admin`, `viewer` |
| `created_at` | datetime | Auto | Creation timestamp |

> **Note:** All timestamps are in UTC ISO 8601 format.

## Rate Limits

- **Standard tier:** 100 requests/minute
- **Premium tier:** 1000 requests/minute
- Rate limit headers are included in all responses
```

This produces a well-formatted Word document with:
- Hierarchical heading structure visible in Word's navigation pane
- Syntax-highlighted JSON and Bash code blocks
- Properly formatted tables with header shading
- Styled block quote for the note callout

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Markdig | 0.37.0 | Markdown parsing |
| DocumentFormat.OpenXml | 3.1.0 | Word document generation |
| ColorCode.Core | 2.0.15 | Syntax highlighting |

## Troubleshooting

### "dotnet tool" command not found

Ensure the .NET SDK (not just the runtime) is installed. The `dotnet tool` command requires the SDK.

### Output file already exists

Use `--force` to overwrite, or specify a different output path with `-o`.

### Images not appearing in Word output

- Local images: ensure the file path is relative to the Markdown file's location
- URL images: ensure network access is available at conversion time
- Supported formats: PNG, JPG/JPEG, GIF, BMP

### Spell-check squiggles in code blocks

Code blocks automatically have spell/grammar checking suppressed. If you see red squiggles, ensure you're using the latest version of the tool.

### Word-to-Markdown produces unexpected output

- Ensure the Word document uses proper heading styles (not just large/bold text)
- Complex layouts (multi-column, text boxes) are not supported and will be skipped
- Try `--optimize-llm` for cleaner output

### Roundtripping loses formatting

Some formatting loss is expected when roundtripping (Word → Markdown → Word) because Markdown has fewer formatting options than Word. Use `PreserveFormattingMetadata = true` to minimize loss.
