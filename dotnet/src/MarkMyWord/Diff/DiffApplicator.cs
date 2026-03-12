using Markdig;
using MarkMyWord.Configuration;
using MarkMyWord.Converters;
using MarkMyWord.Reading;

namespace MarkMyWord.Diff;

/// <summary>
/// Applies unified diffs to Word documents.
/// </summary>
public class DiffApplicator
{
    private readonly WordDocumentReader _reader;
    private readonly MarkdownReconstructor _reconstructor;
    private readonly UnifiedDiffParser _diffParser;
    private readonly MarkdownPatcher _patcher;

    /// <summary>
    /// Initializes a new instance of the DiffApplicator class.
    /// </summary>
    public DiffApplicator()
    {
        _reader = new WordDocumentReader();
        _reconstructor = new MarkdownReconstructor();
        _diffParser = new UnifiedDiffParser();
        _patcher = new MarkdownPatcher();
    }

    /// <summary>
    /// Applies a diff file to a Word document file.
    /// </summary>
    /// <param name="docxPath">Path to the Word document.</param>
    /// <param name="diffPath">Path to the unified diff file.</param>
    /// <param name="options">Optional diff options.</param>
    public void ApplyDiffToFile(string docxPath, string diffPath, DiffOptions? options = null)
    {
        options ??= new DiffOptions();

        // Create backup if requested
        if (options.CreateBackup && File.Exists(docxPath))
        {
            var backupPath = docxPath + options.BackupSuffix;
            File.Copy(docxPath, backupPath, overwrite: true);
        }

        // Read diff
        var diff = _diffParser.ParseFile(diffPath);

        // Apply diff to document
        using var docStream = File.Open(docxPath, FileMode.Open, FileAccess.ReadWrite);
        using var diffStream = File.OpenRead(diffPath);

        ApplyDiff(docStream, diff, options);
    }

    /// <summary>
    /// Applies a diff to a Word document stream.
    /// </summary>
    /// <param name="docxStream">Stream containing the Word document.</param>
    /// <param name="diffStream">Stream containing the unified diff.</param>
    /// <param name="options">Optional diff options.</param>
    public void ApplyDiffToStream(Stream docxStream, Stream diffStream, DiffOptions? options = null)
    {
        options ??= new DiffOptions();

        // Parse diff
        var diff = _diffParser.ParseStream(diffStream);

        // Apply diff to document
        ApplyDiff(docxStream, diff, options);
    }

    /// <summary>
    /// Applies a diff string to a Word document.
    /// </summary>
    /// <param name="docxPath">Path to the Word document.</param>
    /// <param name="diffContent">The unified diff content.</param>
    /// <param name="options">Optional diff options.</param>
    public void ApplyDiffString(string docxPath, string diffContent, DiffOptions? options = null)
    {
        options ??= new DiffOptions();

        // Create backup if requested
        if (options.CreateBackup && File.Exists(docxPath))
        {
            var backupPath = docxPath + options.BackupSuffix;
            File.Copy(docxPath, backupPath, overwrite: true);
        }

        // Parse diff
        var diff = _diffParser.Parse(diffContent);

        // Apply diff to document
        using var docStream = File.Open(docxPath, FileMode.Open, FileAccess.ReadWrite);
        ApplyDiff(docStream, diff, options);
    }

    /// <summary>
    /// Core method that applies a diff to a document stream.
    /// </summary>
    private void ApplyDiff(Stream docxStream, DiffDocument diff, DiffOptions options)
    {
        // Step 1: Read existing Word document
        var documentStructure = _reader.Read(docxStream);

        // Step 2: Reconstruct markdown from Word document
        var markdownDoc = _reconstructor.ReconstructWithLineMapping(documentStructure);
        var originalMarkdown = markdownDoc.Content;

        // Step 3: Apply diff to markdown
        var patchedMarkdown = _patcher.ApplyDiff(originalMarkdown, diff, options.ValidateDiff);

        // Step 4: Regenerate Word document from patched markdown
        // For MVP, we'll regenerate the entire document
        // A more sophisticated approach would merge changes incrementally

        // Reset stream position
        docxStream.SetLength(0);
        docxStream.Position = 0;

        // Convert patched markdown to Word
        var conversionOptions = options.ConversionOptions ?? new ConversionOptions();

        var pipelineBuilder = new MarkdownPipelineBuilder();
        if (conversionOptions.EnableTables)
        {
            pipelineBuilder = pipelineBuilder.UseAdvancedExtensions();
        }

        var pipeline = pipelineBuilder.Build();
        var document = Markdown.Parse(patchedMarkdown, pipeline);

        // Render to OpenXML
        using var renderer = new OpenXmlRenderer(docxStream, conversionOptions);
        renderer.Render(document);
        renderer.FinalizeDocument();

        // Clean up
        documentStructure.Dispose();
    }
}
