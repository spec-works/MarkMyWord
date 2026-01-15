# Release Notes

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
