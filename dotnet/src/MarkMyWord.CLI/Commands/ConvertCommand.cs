using System.Text.Json;
using MarkMyWord;
using MarkMyWord.Configuration;
using MarkMyWord.Exceptions;

namespace MarkMyWord.CLI.Commands;

/// <summary>
/// Handles the convert command logic.
/// </summary>
public static class ConvertCommand
{
    public static async Task<int> ExecuteAsync(
        FileInfo input,
        FileInfo? output,
        bool toMarkdown,
        bool verbose,
        string? font,
        int? fontSize,
        FileInfo? styleConfig,
        bool force,
        bool extractImages,
        bool optimizeLlm,
        bool useCommonMark,
        bool includeMetadata)
    {
        try
        {
            // Validate input file
            if (!input.Exists)
            {
                Console.Error.WriteLine($"Error: Input file not found: {input.FullName}");
                return 1;
            }

            // Auto-detect conversion direction if not explicitly specified
            var inputExtension = input.Extension.ToLowerInvariant();
            bool convertingToMarkdown = toMarkdown || inputExtension == ".docx" || inputExtension == ".doc";

            // Determine output path
            string outputPath;
            if (output != null)
            {
                outputPath = output.FullName;
            }
            else
            {
                var targetExtension = convertingToMarkdown ? ".md" : ".docx";
                outputPath = Path.ChangeExtension(input.FullName, targetExtension);
            }

            // Check if output file exists
            if (File.Exists(outputPath) && !force)
            {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
                Console.Error.WriteLine("Use --force to overwrite.");
                return 1;
            }

            if (verbose)
            {
                var direction = convertingToMarkdown ? "Word to Markdown" : "Markdown to Word";
                Console.WriteLine($"MarkMyWord - {direction} Converter");
                Console.WriteLine($"Input:  {input.FullName}");
                Console.WriteLine($"Output: {outputPath}");
                Console.WriteLine();
            }

            // Route to appropriate converter
            if (convertingToMarkdown)
            {
                return await ConvertWordToMarkdown(input, outputPath, verbose, force,
                    extractImages, optimizeLlm, useCommonMark, includeMetadata);
            }
            else
            {
                return await ConvertMarkdownToWord(input, outputPath, verbose, font, fontSize,
                    styleConfig, force);
            }
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

    private static async Task<int> ConvertMarkdownToWord(
        FileInfo input,
        string outputPath,
        bool verbose,
        string? font,
        int? fontSize,
        FileInfo? styleConfig,
        bool force)
    {
        try
        {
            // Check if output file exists
            if (File.Exists(outputPath) && !force)
            {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
                Console.Error.WriteLine("Use --force to overwrite.");
                return 1;
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
            Console.Error.WriteLine($"Error converting Markdown to Word: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
    }

    private static async Task<int> ConvertWordToMarkdown(
        FileInfo input,
        string outputPath,
        bool verbose,
        bool force,
        bool extractImages,
        bool optimizeLlm,
        bool useCommonMark,
        bool includeMetadata)
    {
        try
        {
            // Check if output file exists
            if (File.Exists(outputPath) && !force)
            {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
                Console.Error.WriteLine("Use --force to overwrite.");
                return 1;
            }

            // Create Word to Markdown options
            var options = new WordToMarkdownOptions
            {
                Flavor = useCommonMark ? MarkdownFlavor.CommonMark : MarkdownFlavor.GitHubFlavoredMarkdown,
                ExtractImages = extractImages,
                OptimizeForLLM = optimizeLlm,
                IncludeMetadata = includeMetadata,
                ImageOutputDirectory = Path.GetDirectoryName(outputPath)
            };

            if (verbose)
            {
                Console.WriteLine($"Markdown flavor: {(useCommonMark ? "CommonMark" : "GitHub Flavored Markdown")}");
                Console.WriteLine($"Optimize for LLM: {(optimizeLlm ? "Yes" : "No")}");
                Console.WriteLine($"Extract images: {(extractImages ? "Yes" : "No")}");
                Console.WriteLine($"Include metadata: {(includeMetadata ? "Yes" : "No")}");
                Console.WriteLine();
            }

            // Convert
            if (verbose)
            {
                Console.WriteLine("Converting to Markdown...");
                var startTime = DateTime.Now;

                await WordConverter.ConvertToMarkdownAsync(input.FullName, outputPath, options);

                var elapsed = DateTime.Now - startTime;
                Console.WriteLine($"Conversion completed in {elapsed.TotalMilliseconds:F0}ms");
            }
            else
            {
                await WordConverter.ConvertToMarkdownAsync(input.FullName, outputPath, options);
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
        catch (EncryptedDocumentException ex)
        {
            // Special handling for encrypted documents
            Console.Error.WriteLine();
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════");
            Console.Error.WriteLine("ERROR: Document is Encrypted or Password-Protected");
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════");
            Console.Error.WriteLine();
            Console.Error.WriteLine("The Word document cannot be converted because it is encrypted");
            Console.Error.WriteLine("or password-protected.");
            Console.Error.WriteLine();
            Console.Error.WriteLine("To convert this document:");
            Console.Error.WriteLine("  1. Open the document in Microsoft Word");
            Console.Error.WriteLine("  2. Go to: File → Info → Protect Document");
            Console.Error.WriteLine("  3. Remove the password or encryption");
            Console.Error.WriteLine("  4. Save the document");
            Console.Error.WriteLine("  5. Try the conversion again");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Alternative: Use 'Save As' to create an unencrypted copy.");
            Console.Error.WriteLine();
            Console.Error.WriteLine($"File: {input.FullName}");
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════");

            if (verbose)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Technical details:");
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.StackTrace);
            }

            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error converting Word to Markdown: {ex.Message}");
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
