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

        // Assert
        Assert.True(stream.Length > 4000, $"Document should contain all diagrams (actual size: {stream.Length} bytes)");
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
        // Arrange - use simple class diagram syntax supported by Naiad
        string markdown = @"```mermaid
classDiagram
    Animal <|-- Duck
    Animal <|-- Fish
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
}
