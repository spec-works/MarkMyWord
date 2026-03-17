using Markdig;
using MarkMyWord.Comments;
using MarkMyWord.Configuration;
using MarkMyWord.Converters;
using MarkMyWord.OfficeTalk;
using Sidemark;

namespace MarkMyWord;

/// <summary>
/// Provides methods to convert Markdown to Word documents.
/// </summary>
public static class MarkdownConverter
{
    /// <summary>
    /// Compiles markdown text to an OfficeTalk (.otk) document string.
    /// </summary>
    /// <param name="markdown">The markdown text to compile.</param>
    /// <param name="options">Optional conversion options (styles, highlighting, etc.).</param>
    /// <returns>The OfficeTalk document as a string.</returns>
    public static string CompileToOtk(string markdown, ConversionOptions? options = null)
    {
        if (string.IsNullOrEmpty(markdown))
            throw new ArgumentException("Markdown content cannot be null or empty.", nameof(markdown));

        var compiler = new OfficeTalkCompiler(options);
        return compiler.Compile(markdown);
    }

    /// <summary>
    /// Compiles markdown text to an OfficeTalk (.otk) document and saves it to a file.
    /// </summary>
    /// <param name="markdown">The markdown text to compile.</param>
    /// <param name="outputPath">The path where the .otk file will be saved.</param>
    /// <param name="options">Optional conversion options.</param>
    public static void CompileToOtkFile(string markdown, string outputPath, ConversionOptions? options = null)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        var otk = CompileToOtk(markdown, options);
        File.WriteAllText(outputPath, otk);
    }

    /// <summary>
    /// Asynchronously compiles markdown text to an OfficeTalk (.otk) document and saves it to a file.
    /// </summary>
    public static async Task CompileToOtkFileAsync(string markdown, string outputPath, ConversionOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentException("Output path cannot be null or empty.", nameof(outputPath));

        var otk = CompileToOtk(markdown, options);
        await File.WriteAllTextAsync(outputPath, otk, cancellationToken);
    }

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

        // Inject Sidemark comments if provided
        var mrsfDoc = options?.SidemarkDocument;
        if (mrsfDoc == null && !string.IsNullOrEmpty(options?.SidemarkFilePath))
        {
            if (File.Exists(options.SidemarkFilePath))
                mrsfDoc = MrsfParser.ParseFile(options.SidemarkFilePath);
        }

        if (mrsfDoc != null && mrsfDoc.Comments.Count > 0)
        {
            var mappings = SidemarkCommentMapper.MapToMarkdownLines(mrsfDoc, markdown);
            WordCommentInjector.InjectComments(renderer.DocumentBuilder.WordDocument, mappings);
        }

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
}
