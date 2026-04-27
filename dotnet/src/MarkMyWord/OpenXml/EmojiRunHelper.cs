using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace MarkMyWord.OpenXml;

/// <summary>
/// Helper for creating Word runs that apply the correct font for emoji characters.
/// Splits text into emoji and non-emoji segments, applying "Segoe UI Emoji" to
/// emoji runs so that Word renders them in full color instead of monochrome outlines.
/// </summary>
public static class EmojiRunHelper
{
    /// <summary>
    /// Appends one or more runs to the parent element, splitting text into emoji and
    /// non-emoji segments. Emoji segments get the emoji font applied.
    /// Works with Paragraph, Hyperlink, or any OpenXmlCompositeElement that accepts Run children.
    /// </summary>
    /// <param name="parent">Target element to append runs to (Paragraph, Hyperlink, etc.).</param>
    /// <param name="text">The text content to append.</param>
    /// <param name="baseRunProperties">
    /// Optional base run properties (bold, italic, color, etc.) to clone for each run.
    /// Pass null for unstyled runs.
    /// </param>
    public static void AppendText(OpenXmlCompositeElement parent, string text, RunProperties? baseRunProperties = null)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var segments = EmojiSegmenter.Segment(text);

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment.Text))
                continue;

            var run = new Run();

            if (segment.IsEmoji)
            {
                var props = baseRunProperties != null
                    ? (RunProperties)baseRunProperties.CloneNode(true)
                    : new RunProperties();
                ApplyEmojiFont(props);
                run.RunProperties = props;
            }
            else if (baseRunProperties != null)
            {
                run.RunProperties = (RunProperties)baseRunProperties.CloneNode(true);
            }

            run.AppendChild(new Text(segment.Text) { Space = SpaceProcessingModeValues.Preserve });
            parent.AppendChild(run);
        }
    }

    /// <summary>
    /// Sets all font slots on the run properties to the emoji font.
    /// Covers Ascii, HighAnsi, ComplexScript, and EastAsia to ensure
    /// consistent emoji rendering regardless of text direction or script.
    /// </summary>
    private static void ApplyEmojiFont(RunProperties props)
    {
        var existingFonts = props.GetFirstChild<RunFonts>();
        if (existingFonts != null)
        {
            existingFonts.Ascii = EmojiSegmenter.EmojiFontName;
            existingFonts.HighAnsi = EmojiSegmenter.EmojiFontName;
            existingFonts.ComplexScript = EmojiSegmenter.EmojiFontName;
            existingFonts.EastAsia = EmojiSegmenter.EmojiFontName;
        }
        else
        {
            props.PrependChild(new RunFonts
            {
                Ascii = EmojiSegmenter.EmojiFontName,
                HighAnsi = EmojiSegmenter.EmojiFontName,
                ComplexScript = EmojiSegmenter.EmojiFontName,
                EastAsia = EmojiSegmenter.EmojiFontName,
            });
        }
    }
}
