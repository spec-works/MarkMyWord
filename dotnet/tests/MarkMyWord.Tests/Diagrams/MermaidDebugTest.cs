using MarkMyWord.Diagrams;

namespace MarkMyWord.Tests.Diagrams;

public class MermaidDebugTest
{
    [Fact]
    public async Task DebugMermaidTextRenderingWithPlaywright()
    {
        // Arrange
        var mermaidCode = @"flowchart TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]";

        // Render to PNG using Playwright
        await using var renderer = new MermaidRenderer();
        var pngBytes = await renderer.RenderToPngAsync(mermaidCode);

        Assert.NotNull(pngBytes);
        File.WriteAllBytes("debug-playwright-output.png", pngBytes!);

        // Get PNG dimensions by parsing header
        int width = (pngBytes[16] << 24) | (pngBytes[17] << 16) | (pngBytes[18] << 8) | pngBytes[19];
        int height = (pngBytes[20] << 24) | (pngBytes[21] << 16) | (pngBytes[22] << 8) | pngBytes[23];

        Console.WriteLine($"=== PNG Analysis (Playwright) ===");
        Console.WriteLine($"PNG size: {pngBytes.Length} bytes");
        Console.WriteLine($"PNG dimensions: {width}x{height}");

        // Assert - PNG should be generated
        Assert.True(pngBytes.Length > 1000, "PNG should be substantial size");
        Assert.True(width > 100 && height > 100, "PNG should have reasonable dimensions");
    }
}
