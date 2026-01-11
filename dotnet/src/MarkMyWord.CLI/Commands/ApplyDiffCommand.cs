using MarkMyWord.Configuration;

namespace MarkMyWord.CLI.Commands;

/// <summary>
/// Handles the apply-diff command logic.
/// </summary>
public static class ApplyDiffCommand
{
    public static async Task<int> ExecuteAsync(
        FileInfo document,
        FileInfo diff,
        FileInfo? output,
        bool verbose,
        bool noBackup,
        bool force)
    {
        try
        {
            // Validate document file
            if (!document.Exists)
            {
                Console.Error.WriteLine($"Error: Document file not found: {document.FullName}");
                return 1;
            }

            // Validate diff file
            if (!diff.Exists)
            {
                Console.Error.WriteLine($"Error: Diff file not found: {diff.FullName}");
                return 1;
            }

            // Determine output path
            string outputPath;
            bool inPlace = output == null;

            if (inPlace)
            {
                // Modify in-place
                outputPath = document.FullName;
            }
            else
            {
                // Write to different file
                outputPath = output.FullName;

                // Check if output file exists
                if (File.Exists(outputPath) && !force)
                {
                    Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
                    Console.Error.WriteLine("Use --force to overwrite.");
                    return 1;
                }
            }

            if (verbose)
            {
                Console.WriteLine($"MarkMyWord - Apply Diff to Word Document");
                Console.WriteLine($"Document: {document.FullName}");
                Console.WriteLine($"Diff:     {diff.FullName}");
                Console.WriteLine($"Output:   {outputPath}");
                Console.WriteLine();
            }

            // Create diff options
            var options = new DiffOptions
            {
                CreateBackup = !noBackup && inPlace, // Only create backup when modifying in-place
                ValidateDiff = true,
                PreserveFormatting = true
            };

            if (verbose && options.CreateBackup)
            {
                Console.WriteLine($"Backup will be created: {outputPath}{options.BackupSuffix}");
            }

            // Apply diff
            if (verbose)
            {
                Console.WriteLine("Applying diff to Word document...");
                var startTime = DateTime.Now;

                if (inPlace)
                {
                    await MarkdownConverter.ApplyDiffToDocxAsync(document.FullName, diff.FullName, options);
                }
                else
                {
                    // Copy document to output location first, then apply diff
                    File.Copy(document.FullName, outputPath, overwrite: true);
                    options.CreateBackup = false; // Don't create backup for the copy
                    await MarkdownConverter.ApplyDiffToDocxAsync(outputPath, diff.FullName, options);
                }

                var elapsed = DateTime.Now - startTime;
                Console.WriteLine($"Diff applied successfully in {elapsed.TotalMilliseconds:F0}ms");
            }
            else
            {
                if (inPlace)
                {
                    await MarkdownConverter.ApplyDiffToDocxAsync(document.FullName, diff.FullName, options);
                }
                else
                {
                    File.Copy(document.FullName, outputPath, overwrite: true);
                    options.CreateBackup = false;
                    await MarkdownConverter.ApplyDiffToDocxAsync(outputPath, diff.FullName, options);
                }
            }

            // Success
            if (inPlace)
            {
                Console.WriteLine($"✓ Updated: {outputPath}");
            }
            else
            {
                Console.WriteLine($"✓ Created: {outputPath}");
            }

            // Show file size
            if (verbose)
            {
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"File size: {FormatFileSize(fileInfo.Length)}");
            }

            return 0;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Context mismatch") || ex.Message.Contains("mismatch"))
        {
            // Diff validation error - provide helpful message
            Console.Error.WriteLine($"Error: Diff validation failed.");
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine("This usually means:");
            Console.Error.WriteLine("  - The Word document has been modified outside the diff workflow");
            Console.Error.WriteLine("  - The diff was created from a different version of the markdown");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Tip: Regenerate the diff from the current document state.");
            if (verbose)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
        }
        catch (FormatException ex)
        {
            // Diff parsing error
            Console.Error.WriteLine($"Error: Invalid diff format.");
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine("The diff file must be in unified diff format (git diff style).");
            if (verbose)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Stack trace:");
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 1;
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
