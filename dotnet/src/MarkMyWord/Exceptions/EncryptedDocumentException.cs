namespace MarkMyWord.Exceptions;

/// <summary>
/// Exception thrown when attempting to convert an encrypted or password-protected Word document.
/// </summary>
public class EncryptedDocumentException : Exception
{
    public EncryptedDocumentException()
        : base("The Word document is encrypted or password-protected and cannot be converted.")
    {
    }

    public EncryptedDocumentException(string message)
        : base(message)
    {
    }

    public EncryptedDocumentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public string? FilePath { get; set; }

    /// <summary>
    /// Gets a user-friendly error message with instructions on how to resolve the issue.
    /// </summary>
    public string GetDetailedMessage()
    {
        var msg = "The Word document is encrypted or password-protected and cannot be converted.\n\n";
        msg += "To convert this document:\n";
        msg += "1. Open the document in Microsoft Word\n";
        msg += "2. Go to File → Info → Protect Document\n";
        msg += "3. Remove the password or encryption\n";
        msg += "4. Save the document\n";
        msg += "5. Try the conversion again\n\n";
        msg += "Alternatively, use 'Save As' to create an unencrypted copy of the document.";

        if (!string.IsNullOrEmpty(FilePath))
        {
            msg += $"\n\nFile: {FilePath}";
        }

        return msg;
    }
}
