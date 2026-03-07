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

convertCommand.AddOption(inputOption);
convertCommand.AddOption(outputOption);
convertCommand.AddOption(toMarkdownOption);
convertCommand.AddOption(verboseOption);
convertCommand.AddOption(fontOption);
convertCommand.AddOption(fontSizeOption);
convertCommand.AddOption(styleOption);
convertCommand.AddOption(forceOption);
convertCommand.AddOption(extractImagesOption);
convertCommand.AddOption(optimizeLlmOption);
convertCommand.AddOption(useCommonMarkOption);
convertCommand.AddOption(includeMetadataOption);

convertCommand.SetHandler(async (context) =>
{
    var input = context.ParseResult.GetValueForOption(inputOption)!;
    var output = context.ParseResult.GetValueForOption(outputOption);
    var toMarkdown = context.ParseResult.GetValueForOption(toMarkdownOption);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);
    var font = context.ParseResult.GetValueForOption(fontOption);
    var fontSize = context.ParseResult.GetValueForOption(fontSizeOption);
    var style = context.ParseResult.GetValueForOption(styleOption);
    var force = context.ParseResult.GetValueForOption(forceOption);
    var extractImages = context.ParseResult.GetValueForOption(extractImagesOption);
    var optimizeLlm = context.ParseResult.GetValueForOption(optimizeLlmOption);
    var useCommonMark = context.ParseResult.GetValueForOption(useCommonMarkOption);
    var includeMetadata = context.ParseResult.GetValueForOption(includeMetadataOption);

    var exitCode = await ConvertCommand.ExecuteAsync(
        input, output, toMarkdown, verbose, font, fontSize, style, force,
        extractImages, optimizeLlm, useCommonMark, includeMetadata);
    Environment.Exit(exitCode);
});

rootCommand.AddCommand(convertCommand);

// Apply-diff command
var applyDiffCommand = new Command("apply-diff", "Apply a unified diff to an existing Word document");

var documentOption = new Option<FileInfo>(
    aliases: new[] { "--document", "-d" },
    description: "Existing Word document (.docx) to apply diff to"
)
{
    IsRequired = true
};
documentOption.AddValidator(result =>
{
    var fileInfo = result.GetValueForOption(documentOption);
    if (fileInfo != null && !fileInfo.Exists)
    {
        result.ErrorMessage = $"Document file not found: {fileInfo.FullName}";
    }
});

var diffOption = new Option<FileInfo>(
    aliases: new[] { "--diff" },
    description: "Unified diff file (git diff format)"
)
{
    IsRequired = true
};
diffOption.AddValidator(result =>
{
    var fileInfo = result.GetValueForOption(diffOption);
    if (fileInfo != null && !fileInfo.Exists)
    {
        result.ErrorMessage = $"Diff file not found: {fileInfo.FullName}";
    }
});

var diffOutputOption = new Option<FileInfo?>(
    aliases: new[] { "--output", "-o" },
    description: "Output .docx file path (default: modify document in-place)"
);

var diffVerboseOption = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "Enable verbose output",
    getDefaultValue: () => false
);

var noBackupOption = new Option<bool>(
    aliases: new[] { "--no-backup" },
    description: "Don't create a backup before modifying (when modifying in-place)",
    getDefaultValue: () => false
);

var diffForceOption = new Option<bool>(
    aliases: new[] { "--force" },
    description: "Overwrite output file if it exists",
    getDefaultValue: () => false
);

applyDiffCommand.AddOption(documentOption);
applyDiffCommand.AddOption(diffOption);
applyDiffCommand.AddOption(diffOutputOption);
applyDiffCommand.AddOption(diffVerboseOption);
applyDiffCommand.AddOption(noBackupOption);
applyDiffCommand.AddOption(diffForceOption);

applyDiffCommand.SetHandler(async (document, diff, output, verbose, noBackup, force) =>
{
    var exitCode = await ApplyDiffCommand.ExecuteAsync(document, diff, output, verbose, noBackup, force);
    Environment.Exit(exitCode);
}, documentOption, diffOption, diffOutputOption, diffVerboseOption, noBackupOption, diffForceOption);

rootCommand.AddCommand(applyDiffCommand);

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
