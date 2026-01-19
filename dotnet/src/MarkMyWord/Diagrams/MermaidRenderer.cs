using Microsoft.Playwright;

namespace MarkMyWord.Diagrams;

/// <summary>
/// Provides functionality to render Mermaid diagrams to PNG format using Playwright.
/// </summary>
public class MermaidRenderer : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private static readonly SemaphoreSlim _browserLock = new(1, 1);

    /// <summary>
    /// Renders Mermaid code to PNG format using a headless browser.
    /// </summary>
    /// <param name="mermaidCode">The Mermaid diagram code to render.</param>
    /// <returns>The PNG byte array representation of the diagram, or null if rendering fails.</returns>
    public async Task<byte[]?> RenderToPngAsync(string mermaidCode)
    {
        if (string.IsNullOrWhiteSpace(mermaidCode))
        {
            return null;
        }

        try
        {
            await EnsureBrowserInitializedAsync();

            if (_browser == null)
            {
                return null;
            }

            // Create a new context for better isolation between renders
            var context = await _browser.NewContextAsync();
            try
            {
                var page = await context.NewPageAsync();
                try
                {
                    // Create HTML with Mermaid.js CDN - use explicit rendering instead of startOnLoad
                    var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{
            margin: 0;
            padding: 20px;
            background: white;
        }}
    </style>
</head>
<body>
    <div class=""mermaid"">
{mermaidCode}
    </div>
    <script type=""module"">
        import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';

        // Initialize and explicitly run
        (async () => {{
            mermaid.initialize({{
                startOnLoad: false,
                theme: 'default',
                fontFamily: 'Arial, Helvetica, sans-serif',
                fontSize: 14
            }});

            // Explicitly render all mermaid diagrams
            await mermaid.run();

            // Signal completion
            window.mermaidReady = true;
        }})();
    </script>
</body>
</html>";

                    await page.SetContentAsync(html);

                    // Wait for Mermaid to complete rendering
                    await page.WaitForFunctionAsync("() => window.mermaidReady === true", new PageWaitForFunctionOptions { Timeout = 15000 });

                    // Wait for SVG element to be present
                    await page.WaitForSelectorAsync("svg", new() { Timeout = 5000 });

                    // Get the SVG element for sizing
                    var svgElement = await page.QuerySelectorAsync("svg");
                    if (svgElement == null)
                    {
                        return null;
                    }

                    var boundingBox = await svgElement.BoundingBoxAsync();
                    if (boundingBox == null)
                    {
                        return null;
                    }

                    // Take screenshot of the SVG element
                    var screenshot = await svgElement.ScreenshotAsync(new()
                    {
                        Type = ScreenshotType.Png,
                        OmitBackground = false
                    });

                    return screenshot;
                }
                finally
                {
                    await page.CloseAsync();
                }
            }
            finally
            {
                await context.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error for debugging (could be replaced with proper logging)
            Console.WriteLine($"Mermaid rendering error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            // Return null on any rendering failure to trigger fallback
            return null;
        }
    }

    /// <summary>
    /// Ensures the browser is initialized (lazy initialization with thread safety).
    /// </summary>
    private async Task EnsureBrowserInitializedAsync()
    {
        if (_browser != null)
        {
            return;
        }

        await _browserLock.WaitAsync();
        try
        {
            if (_browser == null)
            {
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new()
                {
                    Headless = true
                });
            }
        }
        finally
        {
            _browserLock.Release();
        }
    }

    /// <summary>
    /// Checks if the specified language identifier indicates Mermaid content.
    /// </summary>
    /// <param name="language">The language identifier to check.</param>
    /// <returns>True if the language is "mermaid" (case-insensitive), false otherwise.</returns>
    public static bool IsMermaidLanguage(string? language)
    {
        return !string.IsNullOrEmpty(language) &&
               language.Equals("mermaid", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Disposes of browser resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }
}
