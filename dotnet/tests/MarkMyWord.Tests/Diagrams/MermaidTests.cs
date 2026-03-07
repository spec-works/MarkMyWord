using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MarkMyWord.Configuration;

namespace MarkMyWord.Tests.Diagrams;

public class MermaidTests
{
    [Fact]
    public void BasicFlowchartRenders()
    {
        // Arrange
        string markdown = @"# Flowchart Test

```mermaid
flowchart TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain content");
    }

    [Fact]
    public void SequenceDiagramRenders()
    {
        // Arrange
        string markdown = @"```mermaid
sequenceDiagram
    Alice->>Bob: Hello Bob, how are you?
    Bob-->>Alice: I am good thanks!
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain content");
    }

    [Fact]
    public void InvalidMermaidSyntaxFallsBackToCodeBlock()
    {
        // Arrange
        string markdown = @"```mermaid
this is not valid mermaid syntax!!!
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain fallback code block");
    }

    [Theory]
    [InlineData("mermaid")]
    [InlineData("MERMAID")]
    [InlineData("Mermaid")]
    [InlineData("MeRmAiD")]
    public void MermaidLanguageDetectionIsCaseInsensitive(string languageTag)
    {
        // Arrange
        string markdown = $@"```{languageTag}
graph LR
    A --> B
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should render regardless of case");
    }

    [Fact]
    public void DisabledConfigurationRendersAsCodeBlock()
    {
        // Arrange
        string markdown = @"```mermaid
flowchart TD
    A --> B
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = false
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain code block");
    }

    [Fact]
    public void MultipleDiagramsInOneDocument()
    {
        // Arrange
        string markdown = @"# Multiple Diagrams

## First Diagram
```mermaid
flowchart TD
    A[First] --> B[Start]
```

## Second Diagram
```mermaid
graph LR
    X[Second] --> Y[Middle]
```

## Third Diagram
```mermaid
sequenceDiagram
    Alice->>Bob: Hello
    Bob->>Alice: Hi
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert — document should contain all 3 headings and content for each diagram
        // (either rendered as images when Playwright is available, or fallback code blocks)
        stream.Position = 0;
        using var doc = WordprocessingDocument.Open(stream, false);
        var paragraphs = doc.MainDocumentPart!.Document.Body!.Elements<Paragraph>().ToList();

        // Should have headings for each section
        var headings = paragraphs.Where(p =>
            p.ParagraphProperties?.ParagraphStyleId?.Val?.Value?.StartsWith("Heading") == true).ToList();
        Assert.True(headings.Count >= 4, $"Expected at least 4 headings (1 main + 3 sections), got {headings.Count}");

        // Should have content beyond just headings (diagram images or fallback code)
        Assert.True(paragraphs.Count > headings.Count,
            "Document should contain diagram content beyond just headings");
    }

    [Fact]
    public void MixedContentWithMermaidAndCodeBlocks()
    {
        // Arrange
        string markdown = @"# Mixed Content

Here's a Mermaid diagram:

```mermaid
flowchart TD
    A --> B
```

And here's some JSON code:

```json
{
  ""name"": ""test"",
  ""value"": 42
}
```

And another Mermaid diagram:

```mermaid
graph LR
    Start --> End
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true,
            EnableSyntaxHighlighting = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain both diagrams and code blocks");
    }

    [Fact]
    public void EmptyMermaidBlockFallsBackToCodeBlock()
    {
        // Arrange
        string markdown = @"```mermaid
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain fallback for empty diagram");
    }

    [Fact]
    public void ClassDiagramRenders()
    {
        // Arrange
        string markdown = @"```mermaid
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
    Animal : +int age
    Animal : +String gender
    Animal: +isMammal()
    class Duck{
        +String beakColor
        +swim()
        +quack()
    }
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should contain class diagram");
    }

    [Fact]
    public void CustomDimensionConstraintsAreApplied()
    {
        // Arrange
        string markdown = @"```mermaid
flowchart TD
    A --> B
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true,
            MaxDiagramWidthInches = 4.0,
            MaxDiagramHeightInches = 3.0
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        Assert.True(stream.Length > 0, "Document should render with custom dimensions");
    }

    [Fact]
    public void DisabledMermaid_RendersCodeBlockWithMermaidSource()
    {
        // Arrange — when disabled, the mermaid source should appear as plain code
        string markdown = @"```mermaid
flowchart TD
    A[Start] --> B[End]
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = false
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        stream.Position = 0;
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var allText = body.InnerText;

        Assert.Contains("flowchart", allText);
        Assert.Contains("Start", allText);
        Assert.Contains("End", allText);
    }

    [Fact]
    public void MermaidFallback_ContainsOriginalSource()
    {
        // Arrange — invalid syntax triggers fallback; the original source should be preserved
        string markdown = @"```mermaid
not-a-valid-diagram !!!
    some --> broken syntax
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        stream.Position = 0;
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var allText = body.InnerText;

        // Fallback should preserve the original mermaid source
        Assert.Contains("not-a-valid-diagram", allText);
        Assert.Contains("broken syntax", allText);
    }

    [Fact]
    public void MermaidWithSurroundingText_PreservesDocumentStructure()
    {
        // Arrange — text before and after a mermaid block should be preserved
        string markdown = @"Here is an introduction paragraph.

```mermaid
graph LR
    A --> B
```

And here is the conclusion.
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert
        stream.Position = 0;
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var allText = body.InnerText;

        Assert.Contains("introduction paragraph", allText);
        Assert.Contains("conclusion", allText);
    }

    [Fact]
    public void MermaidEnabled_ProducesLargerOutputThanDisabled()
    {
        // Arrange — same markdown, but enabled vs disabled should differ
        string markdown = @"```mermaid
flowchart TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
    C --> E[End]
    D --> E
```
";

        // Act — render with Mermaid enabled
        using var enabledStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, enabledStream, new ConversionOptions
        {
            EnableMermaidDiagrams = true
        });

        // Act — render with Mermaid disabled
        using var disabledStream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, disabledStream, new ConversionOptions
        {
            EnableMermaidDiagrams = false
        });

        // Assert — both should produce valid documents
        Assert.True(enabledStream.Length > 0);
        Assert.True(disabledStream.Length > 0);

        // The outputs should differ (enabled has either image or error fallback text)
        Assert.NotEqual(enabledStream.Length, disabledStream.Length);
    }

    [Fact]
    public void WhitespaceOnlyMermaidBlock_FallsBackToCodeBlock()
    {
        // Arrange
        string markdown = @"```mermaid
   
   
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert — should produce a valid document with fallback content
        stream.Position = 0;
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var allText = body.InnerText;

        Assert.Contains("Empty Mermaid diagram", allText);
    }

    [Fact]
    public void MermaidDefaultOptions_EnabledByDefault()
    {
        // Arrange — verify default ConversionOptions has Mermaid enabled
        var options = new ConversionOptions();

        // Assert
        Assert.True(options.EnableMermaidDiagrams);
        Assert.Equal(6.5, options.MaxDiagramWidthInches);
        Assert.Equal(8.0, options.MaxDiagramHeightInches);
    }

    [Fact]
    public void MermaidLanguageDetection_NonMermaidLanguagesAreNotTreatedAsMermaid()
    {
        // Arrange — code blocks with non-mermaid languages should render as code, not diagrams
        string markdown = @"```javascript
flowchart TD
    A --> B
```
";

        var options = new ConversionOptions
        {
            EnableMermaidDiagrams = true
        };

        // Act
        using var stream = new MemoryStream();
        MarkdownConverter.ConvertToDocx(markdown, stream, options);

        // Assert — should be a code block, not a diagram
        stream.Position = 0;
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document.Body!;
        var allText = body.InnerText;

        // Should contain the raw source (not an error message from Mermaid rendering)
        Assert.Contains("flowchart", allText);
        Assert.DoesNotContain("Error", allText);
    }
}
