using MarkMyWord.Configuration;
using MarkMyWord.Converters;

namespace MarkMyWord;

/// <summary>
/// Provides methods to convert Word documents to Markdown.
/// </summary>
public static class WordConverter
{
    /// <summary>
    /// Converts a Word document to markdown text and saves it to a file.
    /// </summary>
    /// <param name="docxPath">The path to the .docx file to convert.</param>
    /// <param name="outputPath">The path where the markdown file will be saved.</param>
    /// <param name="options">Optional conversion options.</param>
    public static void ConvertToMarkdown(string docxPath, string outputPath, WordToMarkdownOptions? options = null)
    {
        if (string.IsNullOrEmpty(docxPath))
            throw new ArgumentException("Document path cannot be null or empty.", nameof(docxPath));

        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        if (!File.Exists(docxPath))
            throw new FileNotFoundException($"Document file not found: {docxPath}", docxPath);

        using var fileStream = new FileStream(docxPath, FileMode.Open, FileAccess.Read);
        var markdown = ConvertToMarkdown(fileStream, options, Path.GetDirectoryName(outputPath));

        // Write to output file
        File.WriteAllText(outputPath, markdown);
    }

    /// <summary>
    /// Converts a Word document to markdown text from a stream.
    /// </summary>
    /// <param name="docxStream">The stream containing the .docx file.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="baseDirectory">Base directory for resolving image paths. If null, uses current directory.</param>
    /// <returns>The markdown text.</returns>
    public static string ConvertToMarkdown(Stream docxStream, WordToMarkdownOptions? options = null, string? baseDirectory = null)
    {
        if (docxStream == null)
            throw new ArgumentNullException(nameof(docxStream));

        options ??= new WordToMarkdownOptions();
        baseDirectory ??= Directory.GetCurrentDirectory();

        // Convert using OpenXmlMarkdownWriter
        using var writer = new OpenXmlMarkdownWriter(options, baseDirectory);
        return writer.ConvertToMarkdown(docxStream);
    }

    /// <summary>
    /// Converts a Word document to markdown text and returns it as a string.
    /// </summary>
    /// <param name="docxPath">The path to the .docx file to convert.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <returns>The markdown text.</returns>
    public static string ConvertToMarkdownString(string docxPath, WordToMarkdownOptions? options = null)
    {
        if (string.IsNullOrEmpty(docxPath))
            throw new ArgumentException("Document path cannot be null or empty.", nameof(docxPath));

        if (!File.Exists(docxPath))
            throw new FileNotFoundException($"Document file not found: {docxPath}", docxPath);

        using var fileStream = new FileStream(docxPath, FileMode.Open, FileAccess.Read);
        return ConvertToMarkdown(fileStream, options, Path.GetDirectoryName(docxPath));
    }

    /// <summary>
    /// Asynchronously converts a Word document to markdown text and saves it to a file.
    /// </summary>
    /// <param name="docxPath">The path to the .docx file to convert.</param>
    /// <param name="outputPath">The path where the markdown file will be saved.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ConvertToMarkdownAsync(string docxPath, string outputPath, WordToMarkdownOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ConvertToMarkdown(docxPath, outputPath, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously converts a Word document to markdown text from a stream.
    /// </summary>
    /// <param name="docxStream">The stream containing the .docx file.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="baseDirectory">Base directory for resolving image paths. If null, uses current directory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The markdown text.</returns>
    public static async Task<string> ConvertToMarkdownAsync(Stream docxStream, WordToMarkdownOptions? options = null, string? baseDirectory = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => ConvertToMarkdown(docxStream, options, baseDirectory), cancellationToken);
    }
}
