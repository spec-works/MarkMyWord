using System.Text.Json;
using MarkMyWord.Configuration;

namespace MarkMyWord.CLI.Commands;

/// <summary>
/// Handles the convert command logic.
/// </summary>
public static class ConvertCommand
{
    public static async Task<int> ExecuteAsync(
        FileInfo input,
        FileInfo? output,
        bool verbose,
        string? font,
        int? fontSize,
        FileInfo? styleConfig,
        bool force)
    {
        try
        {
            // Validate input file
            if (!input.Exists)
            {
                Console.Error.WriteLine($"Error: Input file not found: {input.FullName}");
                return 1;
            }

            // Determine output path
            var outputPath = output?.FullName ?? Path.ChangeExtension(input.FullName, ".docx");

            // Check if output file exists
            if (File.Exists(outputPath) && !force)
            {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
                Console.Error.WriteLine("Use --force to overwrite.");
                return 1;
            }

            if (verbose)
            {
                Console.WriteLine($"MarkMyWord - Markdown to Word Converter");
                Console.WriteLine($"Input:  {input.FullName}");
                Console.WriteLine($"Output: {outputPath}");
                Console.WriteLine();
            }

            // Load or create conversion options
            ConversionOptions? options = null;

            if (styleConfig != null)
            {
                if (!styleConfig.Exists)
                {
                    Console.Error.WriteLine($"Error: Style configuration file not found: {styleConfig.FullName}");
                    return 1;
                }

                if (verbose)
                    Console.WriteLine($"Loading style configuration from {styleConfig.FullName}...");

                try
                {
                    var json = await File.ReadAllTextAsync(styleConfig.FullName);
                    options = JsonSerializer.Deserialize<ConversionOptions>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"Error: Failed to parse style configuration: {ex.Message}");
                    return 1;
                }
            }
            else
            {
                // Create default options with command-line overrides
                options = new ConversionOptions
                {
                    Styles = new StyleConfiguration()
                };

                if (font != null)
                {
                    options.Styles.DefaultFontName = font;
                    if (verbose)
                        Console.WriteLine($"Using custom font: {font}");
                }

                if (fontSize.HasValue)
                {
                    options.Styles.DefaultFontSize = fontSize.Value;
                    if (verbose)
                        Console.WriteLine($"Using custom font size: {fontSize.Value}pt");
                }
            }

            // Read markdown file
            if (verbose)
                Console.WriteLine("Reading markdown file...");

            var markdown = await File.ReadAllTextAsync(input.FullName);

            if (string.IsNullOrWhiteSpace(markdown))
            {
                Console.Error.WriteLine("Warning: Input file is empty.");
            }

            // Convert
            if (verbose)
            {
                Console.WriteLine("Converting to Word document...");
                var startTime = DateTime.Now;

                await MarkdownConverter.ConvertToDocxAsync(markdown, outputPath, options);

                var elapsed = DateTime.Now - startTime;
                Console.WriteLine($"Conversion completed in {elapsed.TotalMilliseconds:F0}ms");
            }
            else
            {
                await MarkdownConverter.ConvertToDocxAsync(markdown, outputPath, options);
            }

            // Success
            Console.WriteLine($"✓ Created: {outputPath}");

            // Show file size
            if (verbose)
            {
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"File size: {FormatFileSize(fileInfo.Length)}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
