using MarkMyWord.Diagrams;

namespace MarkMyWord.Tests.Diagrams;

public class MultipleRenderTest
{
    [Fact]
    [Trait("Category", "Playwright")]
    public async Task MultipleSequentialRendersWork()
    {
        // Test that rendering multiple diagrams with the same renderer instance works
        await using var renderer = new MermaidRenderer();

        // First diagram
        var mermaid1 = @"flowchart TD
    A[First] --> B[Diagram]";

        var png1 = await renderer.RenderToPngAsync(mermaid1);
        Assert.NotNull(png1);
        Assert.True(png1.Length > 1000, $"First diagram too small: {png1.Length} bytes");

        // Second diagram
        var mermaid2 = @"flowchart LR
    X[Second] --> Y[Diagram]";

        var png2 = await renderer.RenderToPngAsync(mermaid2);
        Assert.NotNull(png2);
        Assert.True(png2.Length > 1000, $"Second diagram too small: {png2.Length} bytes");

        // Third diagram
        var mermaid3 = @"sequenceDiagram
    Alice->>Bob: Third
    Bob->>Alice: Diagram";

        var png3 = await renderer.RenderToPngAsync(mermaid3);
        Assert.NotNull(png3);
        Assert.True(png3.Length > 1000, $"Third diagram too small: {png3.Length} bytes");

        // All three should be different
        Assert.NotEqual(png1, png2);
        Assert.NotEqual(png2, png3);
        Assert.NotEqual(png1, png3);
    }
}
