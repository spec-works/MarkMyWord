using MarkMyWord.Diagrams;

namespace MarkMyWord.Tests.Diagrams;

public class MermaidRendererTests
{
    [Fact]
    public void RenderFlowchartToSvg()
    {
        var renderer = new MermaidRenderer();
        var svg = renderer.RenderToSvg(@"flowchart TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]");

        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
        Assert.Contains("</svg>", svg);
    }

    [Fact]
    public void RenderSequenceDiagramToSvg()
    {
        var renderer = new MermaidRenderer();
        var svg = renderer.RenderToSvg(@"sequenceDiagram
    Alice->>Bob: Hello
    Bob-->>Alice: Hi");

        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void RenderClassDiagramToSvg()
    {
        var renderer = new MermaidRenderer();
        var svg = renderer.RenderToSvg(@"classDiagram
    Animal <|-- Duck
    Animal <|-- Fish");

        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void EmptyInput_ReturnsNull()
    {
        var renderer = new MermaidRenderer();
        Assert.Null(renderer.RenderToSvg(""));
        Assert.Null(renderer.RenderToSvg("   "));
        Assert.Null(renderer.RenderToSvg(null!));
    }

    [Fact]
    public void IsMermaidLanguage_CaseInsensitive()
    {
        Assert.True(MermaidRenderer.IsMermaidLanguage("mermaid"));
        Assert.True(MermaidRenderer.IsMermaidLanguage("MERMAID"));
        Assert.True(MermaidRenderer.IsMermaidLanguage("Mermaid"));
        Assert.False(MermaidRenderer.IsMermaidLanguage("javascript"));
        Assert.False(MermaidRenderer.IsMermaidLanguage(null));
        Assert.False(MermaidRenderer.IsMermaidLanguage(""));
    }
}
