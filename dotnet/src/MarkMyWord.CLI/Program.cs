using System.CommandLine;
using MarkMyWord;
using MarkMyWord.CLI.Commands;

var rootCommand = new RootCommand("MarkMyWord - Convert Markdown to Word documents")
{
    Name = "markmyword"
};

// Convert command
var convertCommand = new Command("convert", "Convert a markdown file to a Word document");

var inputOption = new Option<FileInfo>(
    aliases: new[] { "--input", "-i" },
    description: "Input markdown file path"
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
    description: "Output .docx file path (default: same name as input with .docx extension)"
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

convertCommand.AddOption(inputOption);
convertCommand.AddOption(outputOption);
convertCommand.AddOption(verboseOption);
convertCommand.AddOption(fontOption);
convertCommand.AddOption(fontSizeOption);
convertCommand.AddOption(styleOption);
convertCommand.AddOption(forceOption);

convertCommand.SetHandler(async (input, output, verbose, font, fontSize, style, force) =>
{
    var exitCode = await ConvertCommand.ExecuteAsync(input, output, verbose, font, fontSize, style, force);
    Environment.Exit(exitCode);
}, inputOption, outputOption, verboseOption, fontOption, fontSizeOption, styleOption, forceOption);

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
