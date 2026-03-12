namespace MarkMyWord.Configuration;

/// <summary>
/// Options for applying diffs to Word documents.
/// </summary>
public class DiffOptions
{
    /// <summary>
    /// Whether to preserve formatting from the original document.
    /// Default: true.
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;

    /// <summary>
    /// Whether to validate diff context lines before applying.
    /// Default: true.
    /// </summary>
    public bool ValidateDiff { get; set; } = true;

    /// <summary>
    /// Whether to create a backup before modifying the document.
    /// Default: true.
    /// </summary>
    public bool CreateBackup { get; set; } = true;

    /// <summary>
    /// The suffix to append to backup files.
    /// Default: ".backup".
    /// </summary>
    public string BackupSuffix { get; set; } = ".backup";

    /// <summary>
    /// Conversion options to use when re-rendering changed content.
    /// If null, default conversion options will be used.
    /// </summary>
    public ConversionOptions? ConversionOptions { get; set; }
}
