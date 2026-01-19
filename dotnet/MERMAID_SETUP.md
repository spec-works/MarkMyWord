# Mermaid Diagram Support - Setup Requirements

MarkMyWord now supports rendering Mermaid diagrams in Word documents using Microsoft Playwright for headless browser automation.

## Prerequisites

### 1. Install Playwright Browsers

After installing or building the MarkMyWord package, you **must** install Playwright browser binaries.

#### Option A: Using Playwright CLI (Recommended)

```bash
# Install Playwright CLI globally
dotnet tool install --global Microsoft.Playwright.CLI

# Install Chromium browser (required for Mermaid rendering)
playwright install chromium
```

#### Option B: Using PowerShell Script

```powershell
# Navigate to your project's output directory
cd path/to/your/project/bin/Debug/net10.0

# Run the Playwright installation script
pwsh bin/playwright.ps1 install chromium
```

### 2. Verify Installation

To verify that Playwright browsers are installed correctly:

```bash
playwright install --dry-run
```

This should show that Chromium is already installed at:
- **Windows**: `%USERPROFILE%\AppData\Local\ms-playwright\chromium-*`
- **macOS**: `~/Library/Caches/ms-playwright/chromium-*`
- **Linux**: `~/.cache/ms-playwright/chromium-*`

## Browser Storage Requirements

The Playwright Chromium browser requires approximately **230 MB** of disk space:
- Chromium browser: ~140 MB
- Chromium Headless Shell: ~87 MB
- FFMPEG: ~1.3 MB
- Winldd (Windows only): ~100 KB

## Usage

Once Playwright is installed, Mermaid diagrams in your markdown will be automatically rendered:

```markdown
# My Document

\`\`\`mermaid
flowchart TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Action 1]
    B -->|No| D[Action 2]
\`\`\`
```

### Configuration Options

You can configure Mermaid diagram rendering via `ConversionOptions`:

```csharp
var options = new ConversionOptions
{
    EnableMermaidDiagrams = true,        // Toggle Mermaid rendering (default: true)
    MaxDiagramWidthInches = 6.5,         // Max diagram width (default: 6.5)
    MaxDiagramHeightInches = 8.0         // Max diagram height (default: 8.0)
};

MarkdownConverter.ConvertToDocx(markdown, "output.docx", options);
```

## Troubleshooting

### "Playwright executable doesn't exist" Error

If you see an error about Playwright executable not existing:
1. Ensure you've run `playwright install chromium`
2. Check that the browser was downloaded to the cache directory (see above)
3. Try reinstalling: `playwright install chromium --force`

### "Browser closed" or Timeout Errors

If diagrams fail to render:
1. Ensure you have internet connectivity (Mermaid.js is loaded from CDN)
2. Try increasing the timeout in your code if you have very complex diagrams
3. Check firewall settings aren't blocking the headless browser

### Mermaid Syntax Errors

If a diagram fails to render, MarkMyWord will fall back to rendering it as a plain code block with an error message. Check that your Mermaid syntax is valid at [Mermaid Live Editor](https://mermaid.live).

## CI/CD Environments

In CI/CD pipelines, add these steps before running your application:

### GitHub Actions
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '10.0.x'

- name: Install Playwright
  run: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium
```

### Azure DevOps
```yaml
- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- script: |
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install chromium
  displayName: 'Install Playwright Browsers'
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0

# Install Playwright dependencies
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2

# Install Playwright CLI and browsers
RUN dotnet tool install --global Microsoft.Playwright.CLI
ENV PATH="$PATH:/root/.dotnet/tools"
RUN playwright install chromium
```

## Performance Considerations

- **First render**: ~2-3 seconds (includes browser startup)
- **Subsequent renders**: ~500ms-1s per diagram
- **Browser reuse**: The browser instance is reused across diagrams in a single conversion

## Supported Mermaid Diagram Types

All official Mermaid diagram types are supported, including:
- Flowcharts
- Sequence diagrams
- Class diagrams
- State diagrams
- Entity Relationship diagrams
- Gantt charts
- Pie charts
- Git graphs
- And more...

For the complete list, see the [Mermaid documentation](https://mermaid.js.org/).
