using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MarkMyWord.Configuration;

namespace MarkMyWord.OpenXml;

/// <summary>
/// Manages styles for the Word document.
/// </summary>
public class StyleManager
{
    private readonly StyleConfiguration _config;

    public StyleManager(StyleConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Applies all style definitions to the document.
    /// </summary>
    public void ApplyStylesToDocument(WordprocessingDocument document)
    {
        var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart is null");

        var stylesPart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles();

        // Add default document style
        stylesPart.Styles.AppendChild(CreateDefaultParagraphStyle());
        stylesPart.Styles.AppendChild(CreateDefaultCharacterStyle());

        // Add heading styles
        for (int i = 1; i <= 6; i++)
        {
            stylesPart.Styles.AppendChild(CreateHeadingStyle(i));
        }

        // Add code style
        stylesPart.Styles.AppendChild(CreateCodeCharacterStyle());

        // Add hyperlink style
        stylesPart.Styles.AppendChild(CreateHyperlinkStyle());
    }

    /// <summary>
    /// Creates paragraph properties for a heading of the specified level.
    /// </summary>
    public ParagraphProperties GetHeadingProperties(int level)
    {
        var props = new ParagraphProperties
        {
            ParagraphStyleId = new ParagraphStyleId { Val = $"Heading{level}" }
        };

        return props;
    }

    /// <summary>
    /// Creates run properties for inline code.
    /// </summary>
    public RunProperties GetCodeRunProperties()
    {
        return new RunProperties(
            new RunFonts { Ascii = _config.CodeFontName, HighAnsi = _config.CodeFontName },
            new FontSize { Val = (_config.CodeFontSize * 2).ToString() }, // Half-points
            new Shading { Fill = _config.CodeBackgroundColor },
            new NoProof() // Disable spelling and grammar checking
        );
    }

    /// <summary>
    /// Creates run properties for syntax-highlighted code tokens.
    /// </summary>
    public RunProperties GetSyntaxTokenRunProperties(SyntaxHighlighting.TokenType tokenType)
    {
        var colorScheme = _config.SyntaxColorScheme ?? new Configuration.SyntaxColorScheme();
        var color = colorScheme.GetColorForTokenType(tokenType);

        return new RunProperties(
            new RunFonts { Ascii = _config.CodeFontName, HighAnsi = _config.CodeFontName },
            new FontSize { Val = (_config.CodeFontSize * 2).ToString() }, // Half-points
            new Shading { Fill = _config.CodeBackgroundColor },
            new Color { Val = color },
            new NoProof() // Disable spelling and grammar checking
        );
    }

    /// <summary>
    /// Creates paragraph properties for a quote block.
    /// </summary>
    public ParagraphProperties GetQuoteProperties()
    {
        return new ParagraphProperties(
            new Indentation { Left = "720" }, // 0.5 inch
            new ParagraphBorders(
                new LeftBorder
                {
                    Val = BorderValues.Single,
                    Color = _config.QuoteLeftBorderColor,
                    Size = (uint)_config.QuoteLeftBorderWidth,
                    Space = 4
                }
            ),
            new Shading { Fill = _config.QuoteBackgroundColor }
        );
    }

    /// <summary>
    /// Creates paragraph properties for a code block.
    /// </summary>
    public ParagraphProperties GetCodeBlockProperties()
    {
        return new ParagraphProperties(
            new Shading { Fill = _config.CodeBackgroundColor },
            new SpacingBetweenLines { After = "0", Before = "0", Line = "240" }
        );
    }

    private Style CreateDefaultParagraphStyle()
    {
        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = "Normal",
            Default = true,
            StyleName = new StyleName { Val = "Normal" },
            StyleParagraphProperties = new StyleParagraphProperties(
                new SpacingBetweenLines { After = "160", Line = "240" }
            ),
            StyleRunProperties = new StyleRunProperties(
                new RunFonts { Ascii = _config.DefaultFontName, HighAnsi = _config.DefaultFontName },
                new FontSize { Val = (_config.DefaultFontSize * 2).ToString() }
            )
        };
    }

    private Style CreateDefaultCharacterStyle()
    {
        return new Style
        {
            Type = StyleValues.Character,
            StyleId = "DefaultParagraphFont",
            Default = true,
            StyleName = new StyleName { Val = "Default Paragraph Font" }
        };
    }

    private Style CreateHeadingStyle(int level)
    {
        var headingStyle = _config.HeadingStyles.FirstOrDefault(h => h.Level == level)
            ?? CreateDefaultHeadingStyle(level);

        return new Style
        {
            Type = StyleValues.Paragraph,
            StyleId = $"Heading{level}",
            StyleName = new StyleName { Val = $"Heading {level}" },
            BasedOn = new BasedOn { Val = "Normal" },
            NextParagraphStyle = new NextParagraphStyle { Val = "Normal" },
            StyleParagraphProperties = new StyleParagraphProperties(
                new KeepNext(),
                new SpacingBetweenLines
                {
                    Before = headingStyle.SpacingBeforeTwips.ToString(),
                    After = headingStyle.SpacingAfterTwips.ToString()
                },
                new OutlineLevel { Val = level - 1 }
            ),
            StyleRunProperties = CreateHeadingRunProperties(headingStyle)
        };
    }

    private StyleRunProperties CreateHeadingRunProperties(HeadingStyle headingStyle)
    {
        var props = new StyleRunProperties(
            new RunFonts { Ascii = _config.DefaultFontName, HighAnsi = _config.DefaultFontName },
            new FontSize { Val = (headingStyle.FontSize * 2).ToString() },
            new Color { Val = headingStyle.Color }
        );

        if (headingStyle.Bold)
        {
            props.AppendChild(new Bold());
        }

        return props;
    }

    private HeadingStyle CreateDefaultHeadingStyle(int level)
    {
        return level switch
        {
            1 => new HeadingStyle { Level = 1, FontSize = 20, Bold = true, Color = "2E74B5", SpacingBeforeTwips = 480, SpacingAfterTwips = 240 },
            2 => new HeadingStyle { Level = 2, FontSize = 16, Bold = true, Color = "2E74B5", SpacingBeforeTwips = 400, SpacingAfterTwips = 200 },
            3 => new HeadingStyle { Level = 3, FontSize = 14, Bold = true, Color = "1F4D78", SpacingBeforeTwips = 320, SpacingAfterTwips = 160 },
            4 => new HeadingStyle { Level = 4, FontSize = 12, Bold = true, Color = "2E74B5", SpacingBeforeTwips = 280, SpacingAfterTwips = 140 },
            5 => new HeadingStyle { Level = 5, FontSize = 11, Bold = true, Color = "2E74B5", SpacingBeforeTwips = 240, SpacingAfterTwips = 120 },
            6 => new HeadingStyle { Level = 6, FontSize = 11, Bold = false, Color = "1F4D78", SpacingBeforeTwips = 240, SpacingAfterTwips = 120 },
            _ => new HeadingStyle { Level = level, FontSize = 11, Bold = true, Color = "000000", SpacingBeforeTwips = 240, SpacingAfterTwips = 120 }
        };
    }

    private Style CreateCodeCharacterStyle()
    {
        return new Style
        {
            Type = StyleValues.Character,
            StyleId = "CodeChar",
            StyleName = new StyleName { Val = "Code Character" },
            StyleRunProperties = new StyleRunProperties(
                new RunFonts { Ascii = _config.CodeFontName, HighAnsi = _config.CodeFontName },
                new FontSize { Val = (_config.CodeFontSize * 2).ToString() },
                new Shading { Fill = _config.CodeBackgroundColor }
            )
        };
    }

    private Style CreateHyperlinkStyle()
    {
        return new Style
        {
            Type = StyleValues.Character,
            StyleId = "Hyperlink",
            StyleName = new StyleName { Val = "Hyperlink" },
            StyleRunProperties = new StyleRunProperties(
                new Color { Val = "0563C1" },
                new Underline { Val = UnderlineValues.Single }
            )
        };
    }
}
