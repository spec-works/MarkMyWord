using System.Text;

namespace MarkMyWord.OpenXml;

/// <summary>
/// Sanitizes text for safe inclusion in OpenXML Word documents.
/// Preserves all valid Unicode including astral plane characters (emoji, symbols).
/// Strips only XML-invalid control characters and orphaned surrogates.
/// </summary>
public static class TextSanitizer
{
    /// <summary>
    /// Sanitizes a string for safe use in an OpenXML Text element.
    /// Preserves valid surrogate pairs (emoji, astral plane Unicode) as-is.
    /// Removes only XML-invalid control characters and orphaned surrogates.
    /// </summary>
    /// <param name="text">The input text that may contain problematic characters.</param>
    /// <returns>A sanitized string safe for OpenXML serialization.</returns>
    public static string Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Fast path: check if sanitization is needed at all
        if (!NeedsSanitization(text))
            return text;

        var sb = new StringBuilder(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            // Handle surrogate pairs (astral plane characters U+10000+)
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    // Valid surrogate pair — keep both chars (Word supports these)
                    sb.Append(c);
                    sb.Append(text[i + 1]);
                    i++; // Skip the low surrogate (already appended)
                }
                // else: orphaned high surrogate — skip it
                continue;
            }

            if (char.IsLowSurrogate(c))
            {
                // Orphaned low surrogate — skip it
                continue;
            }

            // Remove XML-invalid control characters
            // XML 1.0 allows: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
            if (IsValidXmlChar(c))
            {
                sb.Append(c);
            }
            // else: invalid XML character — skip it
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks whether the given text contains any characters that need sanitization.
    /// Returns true only for orphaned surrogates or XML-invalid control characters.
    /// Valid surrogate pairs do NOT trigger sanitization on the fast path.
    /// </summary>
    private static bool NeedsSanitization(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (char.IsHighSurrogate(c))
            {
                if (i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    i++; // Valid pair — skip both, no sanitization needed
                    continue;
                }
                return true; // Orphaned high surrogate
            }

            if (char.IsLowSurrogate(c))
                return true; // Orphaned low surrogate

            if (!IsValidXmlChar(c))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a BMP character is valid in XML 1.0.
    /// Valid ranges: #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD]
    /// </summary>
    private static bool IsValidXmlChar(char c)
    {
        return c == '\x09' ||
               c == '\x0A' ||
               c == '\x0D' ||
               (c >= '\x20' && c <= '\uD7FF') ||
               (c >= '\uE000' && c <= '\uFFFD');
    }
}
