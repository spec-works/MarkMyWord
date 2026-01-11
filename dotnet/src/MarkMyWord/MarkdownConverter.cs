using Markdig;
using MarkMyWord.Configuration;
using MarkMyWord.Converters;
using MarkMyWord.Diff;

namespace MarkMyWord;

/// <summary>
/// Provides methods to convert Markdown to Word documents.
/// </summary>
public static class MarkdownConverter
{
    /// <summary>
    /// Converts markdown text to a Word document and saves it to a file.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <param name="outputPath">The path where the .docx file will be saved.</param>
    /// <param name="options">Optional conversion options.</param>
    public static void ConvertToDocx(string markdown, string outputPath, ConversionOptions? options = null)
    {
        if (string.IsNullOrEmpty(markdown))
            throw new ArgumentException("Markdown content cannot be null or empty.", nameof(markdown));

        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite);
        ConvertToDocx(markdown, fileStream, options);
    }

    /// <summary>
    /// Converts markdown text to a Word document and writes it to a stream.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <param name="outputStream">The stream to write the .docx file to.</param>
    /// <param name="options">Optional conversion options.</param>
    public static void ConvertToDocx(string markdown, Stream outputStream, ConversionOptions? options = null)
    {
        if (string.IsNullOrEmpty(markdown))
            throw new ArgumentException("Markdown content cannot be null or empty.", nameof(markdown));

        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));

        // Parse markdown using Markdig with extensions
        var pipelineBuilder = new MarkdownPipelineBuilder();

        // Enable extensions based on options
        if (options?.EnableTables ?? true)
        {
            pipelineBuilder = pipelineBuilder.UseAdvancedExtensions();
        }

        var pipeline = pipelineBuilder.Build();
        var document = Markdown.Parse(markdown, pipeline);

        // Render to OpenXML
        using var renderer = new OpenXmlRenderer(outputStream, options);
        renderer.Render(document);
        renderer.FinalizeDocument();
    }

    /// <summary>
    /// Converts markdown from a stream to a Word document and writes it to another stream.
    /// </summary>
    /// <param name="markdownStream">The stream containing markdown text.</param>
    /// <param name="outputStream">The stream to write the .docx file to.</param>
    /// <param name="options">Optional conversion options.</param>
    public static void ConvertToDocx(Stream markdownStream, Stream outputStream, ConversionOptions? options = null)
    {
        if (markdownStream == null)
            throw new ArgumentNullException(nameof(markdownStream));

        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));

        using var reader = new StreamReader(markdownStream);
        var markdown = reader.ReadToEnd();
        ConvertToDocx(markdown, outputStream, options);
    }

    /// <summary>
    /// Converts markdown text to a Word document and returns it as a byte array.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <returns>The .docx file as a byte array.</returns>
    public static byte[] ConvertToDocxBytes(string markdown, ConversionOptions? options = null)
    {
        using var ms = new MemoryStream();
        ConvertToDocx(markdown, ms, options);
        return ms.ToArray();
    }

    /// <summary>
    /// Asynchronously converts markdown text to a Word document and saves it to a file.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <param name="outputPath">The path where the .docx file will be saved.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ConvertToDocxAsync(string markdown, string outputPath, ConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ConvertToDocx(markdown, outputPath, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously converts markdown text to a Word document and writes it to a stream.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <param name="outputStream">The stream to write the .docx file to.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ConvertToDocxAsync(string markdown, Stream outputStream, ConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ConvertToDocx(markdown, outputStream, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously converts markdown from a stream to a Word document and writes it to another stream.
    /// </summary>
    /// <param name="markdownStream">The stream containing markdown text.</param>
    /// <param name="outputStream">The stream to write the .docx file to.</param>
    /// <param name="options">Optional conversion options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ConvertToDocxAsync(Stream markdownStream, Stream outputStream, ConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ConvertToDocx(markdownStream, outputStream, options), cancellationToken);
    }

    // ========== Diff Application Methods ==========

    /// <summary>
    /// Applies a unified diff to an existing Word document.
    /// </summary>
    /// <param name="docxPath">Path to the existing .docx file.</param>
    /// <param name="diffPath">Path to the unified diff file.</param>
    /// <param name="options">Optional diff options.</param>
    public static void ApplyDiffToDocx(string docxPath, string diffPath, DiffOptions? options = null)
    {
        var applicator = new DiffApplicator();
        applicator.ApplyDiffToFile(docxPath, diffPath, options);
    }

    /// <summary>
    /// Applies a unified diff to an existing Word document using streams.
    /// </summary>
    /// <param name="docxStream">Stream containing the existing .docx file.</param>
    /// <param name="diffStream">Stream containing the unified diff.</param>
    /// <param name="options">Optional diff options.</param>
    public static void ApplyDiffToDocx(Stream docxStream, Stream diffStream, DiffOptions? options = null)
    {
        var applicator = new DiffApplicator();
        applicator.ApplyDiffToStream(docxStream, diffStream, options);
    }

    /// <summary>
    /// Applies a unified diff string to an existing Word document.
    /// </summary>
    /// <param name="docxPath">Path to the existing .docx file.</param>
    /// <param name="diffContent">The unified diff content as a string.</param>
    /// <param name="options">Optional diff options.</param>
    public static void ApplyDiffToDocx(string docxPath, string diffContent, DiffOptions? options = null)
    {
        var applicator = new DiffApplicator();
        applicator.ApplyDiffString(docxPath, diffContent, options);
    }

    /// <summary>
    /// Asynchronously applies a unified diff to an existing Word document.
    /// </summary>
    /// <param name="docxPath">Path to the existing .docx file.</param>
    /// <param name="diffPath">Path to the unified diff file.</param>
    /// <param name="options">Optional diff options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyDiffToDocxAsync(string docxPath, string diffPath, DiffOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ApplyDiffToDocx(docxPath, diffPath, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a unified diff to an existing Word document using streams.
    /// </summary>
    /// <param name="docxStream">Stream containing the existing .docx file.</param>
    /// <param name="diffStream">Stream containing the unified diff.</param>
    /// <param name="options">Optional diff options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyDiffToDocxAsync(Stream docxStream, Stream diffStream, DiffOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ApplyDiffToDocx(docxStream, diffStream, options), cancellationToken);
    }

    /// <summary>
    /// Asynchronously applies a unified diff string to an existing Word document.
    /// </summary>
    /// <param name="docxPath">Path to the existing .docx file.</param>
    /// <param name="diffContent">The unified diff content as a string.</param>
    /// <param name="options">Optional diff options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task ApplyDiffToDocxAsync(string docxPath, string diffContent, DiffOptions? options = null, CancellationToken cancellationToken = default)
    {
        await Task.Run(() => ApplyDiffToDocx(docxPath, diffContent, options), cancellationToken);
    }
}
