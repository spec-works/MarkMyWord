using System.Globalization;
using System.Text;

namespace MarkMyWord.OpenXml;

/// <summary>
/// Segments text into emoji and non-emoji runs for proper font handling in Word.
/// Uses grapheme-cluster-aware segmentation to correctly handle composite emoji
/// (ZWJ sequences, flags, skin tones, variation selectors).
/// </summary>
public static class EmojiSegmenter
{
    /// <summary>
    /// Font name used for emoji rendering in Word documents.
    /// Segoe UI Emoji provides full-color emoji on Windows.
    /// </summary>
    public const string EmojiFontName = "Segoe UI Emoji";

    /// <summary>
    /// Segments a string into contiguous runs of emoji and non-emoji text.
    /// Adjacent segments of the same type are merged for efficiency.
    /// </summary>
    public static IReadOnlyList<TextSegment> Segment(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [new TextSegment(text ?? string.Empty, false)];

        // Fast path: no emoji at all
        if (!ContainsEmoji(text))
            return [new TextSegment(text, false)];

        var segments = new List<TextSegment>();
        var current = new StringBuilder();
        bool currentIsEmoji = false;
        bool firstSegment = true;

        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            string element = enumerator.GetTextElement();
            bool isEmoji = IsEmojiGraphemeCluster(element);

            if (firstSegment)
            {
                currentIsEmoji = isEmoji;
                firstSegment = false;
            }

            if (isEmoji == currentIsEmoji)
            {
                current.Append(element);
            }
            else
            {
                if (current.Length > 0)
                    segments.Add(new TextSegment(current.ToString(), currentIsEmoji));

                current.Clear();
                current.Append(element);
                currentIsEmoji = isEmoji;
            }
        }

        if (current.Length > 0)
            segments.Add(new TextSegment(current.ToString(), currentIsEmoji));

        return segments;
    }

    /// <summary>
    /// Checks whether the text contains any emoji characters.
    /// </summary>
    public static bool ContainsEmoji(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var rune in text.EnumerateRunes())
        {
            if (IsEmojiCodePoint(rune.Value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a grapheme cluster represents an emoji.
    /// A cluster is emoji if any of its constituent code points is an emoji code point.
    /// </summary>
    private static bool IsEmojiGraphemeCluster(string cluster)
    {
        foreach (var rune in cluster.EnumerateRunes())
        {
            if (IsEmojiCodePoint(rune.Value))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether a Unicode code point is an emoji or emoji-related character.
    /// Covers supplementary plane emoji, variation selectors, ZWJ,
    /// regional indicators, and common BMP emoji.
    /// </summary>
    private static bool IsEmojiCodePoint(int cp) =>
        // Supplementary Multilingual Plane emoji blocks
        (cp >= 0x1F000 && cp <= 0x1FAFF) ||
        // Supplementary symbols and pictographs (extended)
        (cp >= 0x1FC00 && cp <= 0x1FFFF) ||
        // Regional indicator symbols (flags)
        (cp >= 0x1F1E0 && cp <= 0x1F1FF) ||
        // Variation Selector 16 (requests emoji presentation)
        cp == 0xFE0F ||
        // Zero-width joiner (compound emoji like 👩‍💻)
        cp == 0x200D ||
        // Miscellaneous Symbols (sun, cloud, stars, etc.)
        (cp >= 0x2600 && cp <= 0x26FF) ||
        // Dingbats (scissors, pencil, checkmarks, arrows, etc.)
        (cp >= 0x2700 && cp <= 0x27BF) ||
        // Miscellaneous Technical (hourglass, player controls, etc.)
        (cp >= 0x2300 && cp <= 0x23FF) ||
        // Geometric Shapes (squares, circles, triangles)
        (cp >= 0x25A0 && cp <= 0x25FF) ||
        // Misc Symbols and Arrows
        (cp >= 0x2B05 && cp <= 0x2B55) ||
        // Curved arrows
        cp == 0x2934 || cp == 0x2935 ||
        // Copyright and registered
        cp == 0x00A9 || cp == 0x00AE ||
        // Trademark
        cp == 0x2122 ||
        // Double exclamation, exclamation question
        cp == 0x203C || cp == 0x2049;
}

/// <summary>
/// Represents a contiguous segment of text that is either all-emoji or all-non-emoji.
/// </summary>
public readonly record struct TextSegment(string Text, bool IsEmoji);
