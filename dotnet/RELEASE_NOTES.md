# Release Notes

## Version 0.9.0

### Bug Fixes

#### Word Online Table Rendering Compatibility
- Fixed table rendering to produce Word Online-compatible output
- Tables now use explicit `TableCellWidth` with `Dxa` units for consistent column sizing
- Added `TableBorders` with proper border definitions for reliable rendering across Word desktop and Word Online
- Improved `GridColumn` width calculations for uniform table layout

#### Unicode and Emoji Handling
- Added `TextSanitizer` utility for safe OpenXML text serialization
- Preserves valid surrogate pairs (emoji, astral plane Unicode characters U+10000+)
- Strips only XML-invalid control characters and orphaned surrogates
- Sanitizes URL fallback text and image alt text in `LinkInlineRenderer`
- Applied sanitization across all inline and block renderers: `CodeBlockRenderer`, `CodeInlineRenderer`, `EmphasisInlineRenderer`, `LiteralInlineRenderer`, and `TableRenderer`

### New Components

- `OpenXml/TextSanitizer.cs` — Static utility for sanitizing text before OpenXML insertion

### Testing

- Added `TableWebCompatibilityTests` — 460 lines of tests covering Word Online table rendering
- Added `UnicodeHandlingTests` — 423 lines of tests covering emoji, surrogate pair, and control character handling
- Cleaned up unused parameters in `PipelineParityTests`

### Breaking Changes

None

---

## Version 0.6.0

### Breaking Changes

- Dropped .NET Standard 2.1 target framework — now targets `net10.0` only (required by Naiad dependency)
- Removed Playwright dependency — Mermaid rendering no longer requires a browser runtime

### New Features

#### Pure .NET Mermaid Rendering
- Replaced Playwright (browser-based) with **Naiad** for pure .NET Mermaid-to-SVG rendering
- Added **Svg.Skia** for high-quality SVG-to-PNG rasterization at 3x scale
- No browser installation required — Mermaid diagrams render entirely in-process
- SVG post-processing pipeline for Word-compatible output
- Support for all 7 diagram types: flowchart, sequence, class, state, ER, Gantt, pie

#### Light/Dark Theme System
- New `--theme light|dark` CLI parameter
- Affects page background, text colors, headings, code blocks, syntax highlighting, and diagrams

#### Soft Line Break Support
- Soft line breaks (single newlines within paragraphs) now render as Word `Break` elements
- Previously consecutive lines like `**Author:** Name\n**Date:** Today` merged into one line

#### Strong Naming
- Assembly is now strong-named for enterprise scenarios (public key token: `b4a532ad7fdd08b9`)

#### CLI Packaging Fix
- Fixed `DotnetToolSettings.xml` packaging failure that prevented `dotnet tool install`

### Dependencies Changed

- **Added**: Naiad 0.1.2, Svg.Skia 3.4.1, SkiaSharp.NativeAssets.Linux 2.88.9
- **Removed**: Microsoft.Playwright

### Testing

All 99 tests passing.

---

## Version 0.5.0

### Breaking Changes

None

### New Features

#### Strong Naming Support
- Added strong name signing to the .NET library assembly
- Enables consumption by applications that require strong-named dependencies
- Public key token: `b4a532ad7fdd08b9`
- Backward compatible - existing consumers are not affected

### Technical Details

**New Files:**
- `MarkMyWord.snk` - Strong name key file (2048-bit RSA)

**Updated Files:**
- `MarkMyWord.csproj` - Added `<SignAssembly>true</SignAssembly>` and `<AssemblyOriginatorKeyFile>MarkMyWord.snk</AssemblyOriginatorKeyFile>`

### Notes

- The assembly is now strong-named for enterprise scenarios
- Some dependencies (e.g., Markdig) are not strong-named, which produces a warning (CS8002) but is acceptable
- The key file is committed to the repository for reproducible builds

---

## Version 0.2.0 (2026-01-14)

### New Features

#### Syntax Highlighting for Code Blocks
- Added full syntax highlighting support for code fence blocks
- **Supported languages:**
  - **JSON** - Property names, string values, numbers, keywords (true/false/null)
  - **TypeSpec** - Keywords, types, decorators, operators, comments, strings
  - **Bash/Shell** - Commands, keywords, variables, strings, comments
- Colors optimized for grey code block backgrounds with excellent contrast
- JSON property names and values are rendered in different colors for clarity
- Extensible architecture allows easy addition of more languages

#### Code Block Improvements
- Automatic spell/grammar check suppression (`NoProof` property) - no more red/blue squiggles!
- Trailing empty lines are automatically stripped from code blocks
- Language labels no longer displayed in output (cleaner appearance)

#### Configuration Options
- New `EnableSyntaxHighlighting` option (default: true)
- New `SyntaxColorScheme` configuration class for customizing syntax colors
- Full control over token colors (keywords, strings, numbers, comments, types, functions, properties, operators)

### Technical Details

**New Dependencies:**
- ColorCode.Core 2.0.15 - Powers JSON syntax highlighting

**New Components:**
- `SyntaxHighlighting/ISyntaxHighlighter.cs` - Highlighter interface
- `SyntaxHighlighting/ColorCodeHighlighter.cs` - JSON highlighting
- `SyntaxHighlighting/TypeSpecHighlighter.cs` - TypeSpec highlighting
- `SyntaxHighlighting/BashHighlighter.cs` - Bash/Shell highlighting
- `SyntaxHighlighting/SyntaxHighlighterFactory.cs` - Language routing
- `Configuration/SyntaxColorScheme.cs` - Color configuration

**Updated Components:**
- `CodeBlockRenderer.cs` - Integrated syntax highlighting
- `StyleManager.cs` - Added syntax token styling methods
- `ConversionOptions.cs` - Added syntax highlighting options
- `StyleConfiguration.cs` - Added color scheme support

### Default Color Scheme

Optimized for light grey background (F5F5F5):
- **Keywords**: Blue (569CD6)
- **Strings**: Orange (CE9178)
- **Numbers**: Dark Green (098658)
- **Comments**: Green (6A9955)
- **Operators**: Dark Gray (4A4A4A)
- **Types**: Cyan (4EC9B0)
- **Functions**: Gold (C4A000)
- **Properties**: Darker Blue (4FC1FF)
- **Identifiers**: Dark Gray (383838)

### Breaking Changes

None - syntax highlighting is enabled by default but falls back gracefully for unsupported languages.

### Bug Fixes

- Fixed code block rendering to handle trailing empty lines correctly
- Improved color contrast for better readability

### Testing

All 29 existing unit tests pass. Syntax highlighting thoroughly tested with sample documents.

---

## Version 0.1.0 (Initial Release)

Initial release with core markdown to Word conversion functionality:
- Full CommonMark support
- Headings, paragraphs, emphasis
- Code blocks and inline code
- Links and images
- Lists (ordered, unordered, nested)
- Tables
- Block quotes
- Custom styling
- CLI tool
