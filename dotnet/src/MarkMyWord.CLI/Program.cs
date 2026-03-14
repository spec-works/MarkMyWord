using System.CommandLine;
using MarkMyWord;
using MarkMyWord.CLI.Commands;

var rootCommand = new RootCommand("MarkMyWord - Convert between Markdown and Word documents")
{
    Name = "markmyword"
};

// Convert command
var convertCommand = new Command("convert", "Convert between Markdown and Word documents (auto-detects direction)");

var inputOption = new Option<FileInfo>(
    aliases: new[] { "--input", "-i" },
    description: "Input file path (.md or .docx)"
)
{
    IsRequired = true
};
inputOption.AddValidator(result =>
{
    var fileInfo = result.GetValueForOption(inputOption);
    if (fileInfo != null && !fileInfo.Exists)
    {
        result.ErrorMessage = $"Input file not found: {fileInfo.FullName}";
    }
});

var outputOption = new Option<FileInfo?>(
    aliases: new[] { "--output", "-o" },
    description: "Output file path (default: same name with appropriate extension)"
);

var toMarkdownOption = new Option<bool>(
    aliases: new[] { "--to-markdown" },
    description: "Convert Word document to Markdown (auto-detected from file extension)",
    getDefaultValue: () => false
);

var verboseOption = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "Enable verbose output",
    getDefaultValue: () => false
);

var fontOption = new Option<string?>(
    aliases: new[] { "--font", "-f" },
    description: "Default font name (e.g., 'Calibri', 'Times New Roman')"
);

var fontSizeOption = new Option<int?>(
    aliases: new[] { "--font-size", "-s" },
    description: "Default font size in points (e.g., 11, 12)"
);
fontSizeOption.AddValidator(result =>
{
    var value = result.GetValueForOption(fontSizeOption);
    if (value.HasValue && (value.Value < 6 || value.Value > 72))
    {
        result.ErrorMessage = "Font size must be between 6 and 72 points";
    }
});

var styleOption = new Option<FileInfo?>(
    aliases: new[] { "--style" },
    description: "Path to JSON style configuration file"
);

var themeOption = new Option<string?>(
    aliases: new[] { "--theme" },
    description: "Color theme: 'light' (default) or 'dark'"
);
themeOption.AddValidator(result =>
{
    var value = result.GetValueForOption(themeOption);
    if (value != null && value != "light" && value != "dark")
    {
        result.ErrorMessage = "Theme must be 'light' or 'dark'";
    }
});

var forceOption = new Option<bool>(
    aliases: new[] { "--force" },
    description: "Overwrite output file if it exists",
    getDefaultValue: () => false
);

// Word to Markdown specific options
var extractImagesOption = new Option<bool>(
    aliases: new[] { "--extract-images" },
    description: "Extract images from Word document (Word to Markdown only)",
    getDefaultValue: () => true
);

var optimizeLlmOption = new Option<bool>(
    aliases: new[] { "--optimize-llm" },
    description: "Optimize markdown for LLM grounding (Word to Markdown only)",
    getDefaultValue: () => true
);

var useCommonMarkOption = new Option<bool>(
    aliases: new[] { "--commonmark" },
    description: "Use strict CommonMark instead of GitHub Flavored Markdown (Word to Markdown only)",
    getDefaultValue: () => false
);

var includeMetadataOption = new Option<bool>(
    aliases: new[] { "--include-metadata" },
    description: "Include document metadata as YAML frontmatter (Word to Markdown only)",
    getDefaultValue: () => false
);

var commentsOption = new Option<bool>(
    aliases: new[] { "--comments" },
    description: "Enable Sidemark comment roundtripping. Word→Markdown: extract comments to .review.yaml sidecar. Markdown→Word: inject comments from .review.yaml sidecar.",
    getDefaultValue: () => false
);

var sidemarkFileOption = new Option<FileInfo?>(
    aliases: new[] { "--sidemark-file" },
    description: "Path to a Sidemark .review.yaml file (Markdown to Word only). If omitted with --comments, auto-discovers <input>.review.yaml."
);

convertCommand.AddOption(inputOption);
convertCommand.AddOption(outputOption);
convertCommand.AddOption(toMarkdownOption);
convertCommand.AddOption(verboseOption);
convertCommand.AddOption(fontOption);
convertCommand.AddOption(fontSizeOption);
convertCommand.AddOption(styleOption);
convertCommand.AddOption(themeOption);
convertCommand.AddOption(forceOption);
convertCommand.AddOption(extractImagesOption);
convertCommand.AddOption(optimizeLlmOption);
convertCommand.AddOption(useCommonMarkOption);
convertCommand.AddOption(includeMetadataOption);
convertCommand.AddOption(commentsOption);
convertCommand.AddOption(sidemarkFileOption);

convertCommand.SetHandler(async (context) =>
{
    var input = context.ParseResult.GetValueForOption(inputOption)!;
    var output = context.ParseResult.GetValueForOption(outputOption);
    var toMarkdown = context.ParseResult.GetValueForOption(toMarkdownOption);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);
    var font = context.ParseResult.GetValueForOption(fontOption);
    var fontSize = context.ParseResult.GetValueForOption(fontSizeOption);
    var style = context.ParseResult.GetValueForOption(styleOption);
    var theme = context.ParseResult.GetValueForOption(themeOption);
    var force = context.ParseResult.GetValueForOption(forceOption);
    var extractImages = context.ParseResult.GetValueForOption(extractImagesOption);
    var optimizeLlm = context.ParseResult.GetValueForOption(optimizeLlmOption);
    var useCommonMark = context.ParseResult.GetValueForOption(useCommonMarkOption);
    var includeMetadata = context.ParseResult.GetValueForOption(includeMetadataOption);
    var comments = context.ParseResult.GetValueForOption(commentsOption);
    var sidemarkFile = context.ParseResult.GetValueForOption(sidemarkFileOption);

    var exitCode = await ConvertCommand.ExecuteAsync(
        input, output, toMarkdown, verbose, font, fontSize, style, theme, force,
        extractImages, optimizeLlm, useCommonMark, includeMetadata, comments, sidemarkFile);
    Environment.Exit(exitCode);
});

rootCommand.AddCommand(convertCommand);

// Version command
var versionCommand = new Command("version", "Display version information");
versionCommand.SetHandler(() =>
{
    var version = typeof(MarkdownConverter).Assembly.GetName().Version;
    Console.WriteLine($"MarkMyWord v{version?.ToString(3) ?? "1.0.0"}");
    Console.WriteLine(".NET 9 Markdown to Word Converter");
    Console.WriteLine();
    Console.WriteLine("Built with:");
    Console.WriteLine("  - Markdig (CommonMark parser)");
    Console.WriteLine("  - DocumentFormat.OpenXml");
    Console.WriteLine();
    Console.WriteLine("https://github.com/yourusername/MarkMyWord");
});

rootCommand.AddCommand(versionCommand);

return await rootCommand.InvokeAsync(args);
