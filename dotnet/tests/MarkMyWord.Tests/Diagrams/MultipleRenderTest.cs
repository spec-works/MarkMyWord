using MarkMyWord.Diagrams;

namespace MarkMyWord.Tests.Diagrams;

public class MultipleRenderTest
{
    [Fact]
    public void MultipleSequentialRendersWork()
    {
        var renderer = new MermaidRenderer();

        var svg1 = renderer.RenderToSvg(@"flowchart TD
    A[First] --> B[Diagram]");
        Assert.NotNull(svg1);
        Assert.Contains("<svg", svg1);

        var svg2 = renderer.RenderToSvg(@"flowchart LR
    X[Second] --> Y[Diagram]");
        Assert.NotNull(svg2);
        Assert.Contains("<svg", svg2);

        var svg3 = renderer.RenderToSvg(@"sequenceDiagram
    Alice->>Bob: Third
    Bob->>Alice: Diagram");
        Assert.NotNull(svg3);
        Assert.Contains("<svg", svg3);

        // All three should produce different SVGs
        Assert.NotEqual(svg1, svg2);
        Assert.NotEqual(svg2, svg3);
        Assert.NotEqual(svg1, svg3);
    }
}
