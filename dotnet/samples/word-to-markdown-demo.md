# Word to Markdown Conversion Demo

This document demonstrates the new Word → Markdown conversion feature in MarkMyWord.

## Overview

MarkMyWord now supports bidirectional conversion between Markdown and Word documents. This feature is optimized for:
- **LLM grounding**: Clean, semantic markdown perfect for AI/LLM use cases
- **Roundtripping**: Convert Markdown → Word → Markdown with minimal loss
- **GitHub Flavored Markdown**: Full support for tables, strikethrough, and other GFM extensions

## Basic Usage

### Using the Library

```csharp
using MarkMyWord;
using MarkMyWord.Configuration;

// Simple conversion
WordConverter.ConvertToMarkdown("document.docx", "output.md");

// With options
var options = new WordToMarkdownOptions
{
    Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,
    OptimizeForLLM = true,
    ExtractImages = true,
    IncludeMetadata = false
};

WordConverter.ConvertToMarkdown("document.docx", "output.md", options);

// Get markdown as string
string markdown = WordConverter.ConvertToMarkdownString("document.docx");
```

### Using the CLI

```bash
# Auto-detects conversion direction from file extension
markmyword convert -i document.docx -o output.md

# Explicit options
markmyword convert -i document.docx --optimize-llm --extract-images

# Use strict CommonMark instead of GFM
markmyword convert -i document.docx --commonmark

# Include document metadata as YAML frontmatter
markmyword convert -i document.docx --include-metadata
```

## Conversion Options

### MarkdownFlavor

Choose between two markdown flavors:

- **GitHubFlavoredMarkdown** (default): Supports tables, strikethrough, task lists
- **CommonMark**: Strict CommonMark compliance for maximum compatibility

### OptimizeForLLM

When `true` (default), the converter:
- Removes unnecessary formatting details
- Focuses on semantic content over visual styling
- Produces clean, consistent markdown ideal for LLM consumption

### ExtractImages

When `true` (default):
- Embedded images are extracted to separate files
- Images are saved in the same directory as the output markdown
- Image links use relative paths

### IncludeMetadata

When `true`:
- Document title, author, and subject are extracted
- Metadata is output as YAML frontmatter at the top of the markdown file

Example output:
```markdown
---
title: My Document
author: John Doe
subject: Technical Documentation
---

# Content starts here...
```

## Supported Elements

### Headings
Word headings (Heading 1-6) → Markdown headings (`#` through `######`)

### Text Formatting
- **Bold** → `**bold**`
- *Italic* → `*italic*`
- `Inline code` → `` `code` ``

### Lists
- Unordered lists → `- item`
- Ordered lists → `1. item`
- Nested lists with proper indentation

### Tables (GFM only)
Word tables → GitHub Flavored Markdown table syntax

### Code Blocks
Code-styled paragraphs → Fenced code blocks with ` ``` `

### Block Quotes
Quote-styled paragraphs → `> quoted text`

### Links
Hyperlinks → `[text](url)`

### Images
Embedded images → `![alt](path)` with optional extraction

## Roundtrip Example

```csharp
// Start with markdown
var markdown = @"
# My Document

This is **bold** and this is *italic*.

## Features
- Feature 1
- Feature 2
";

// Convert to Word
MarkdownConverter.ConvertToDocx(markdown, "temp.docx");

// Convert back to Markdown
var result = WordConverter.ConvertToMarkdownString("temp.docx");

// Result preserves structure and formatting
Console.WriteLine(result);
```

## LLM Grounding Use Case

The Word → Markdown converter is optimized for extracting content from Word documents to use as context/grounding for Large Language Models:

```csharp
var options = new WordToMarkdownOptions
{
    Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,
    OptimizeForLLM = true,  // Clean, semantic output
    ExtractImages = false,  // Skip images for text-focused LLMs
    IncludeMetadata = true  // Include context about the document
};

// Convert Word doc to markdown suitable for LLM input
var markdown = WordConverter.ConvertToMarkdownString("report.docx", options);

// Use with your LLM
var llmContext = $"Context from document:\n\n{markdown}";
```

## Advanced Options

### Custom Line Endings

```csharp
var options = new WordToMarkdownOptions
{
    LineEndings = LineEndingStyle.LF  // Unix-style
    // or LineEndingStyle.CRLF        // Windows-style
    // or LineEndingStyle.Environment // Platform default
};
```

### Image URL Prefix

```csharp
var options = new WordToMarkdownOptions
{
    ExtractImages = true,
    ImageOutputDirectory = "./images",
    ImageUrlPrefix = "https://cdn.example.com/docs/images/"
};

// Images will be saved to ./images/
// But markdown will reference: https://cdn.example.com/docs/images/image1.png
```

### HTML for Complex Formatting (GFM only)

```csharp
var options = new WordToMarkdownOptions
{
    Flavor = MarkdownFlavor.GitHubFlavoredMarkdown,
    UseHtmlForComplexFormatting = true  // When markdown lacks equivalent
};
```

## Best Practices

1. **For LLM grounding**: Use `OptimizeForLLM = true` and `Flavor = GitHubFlavoredMarkdown`
2. **For roundtripping**: Use `PreserveFormattingMetadata = true` (coming soon)
3. **For web publishing**: Extract images and set appropriate URL prefixes
4. **For strict compatibility**: Use `Flavor = CommonMark`

## See Also

- [Main README](../README.md)
- [Markdown to Word Examples](./README.md)
- [API Documentation](../docs/api.md)
