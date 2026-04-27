using MarkMyWord.Configuration;
using MarkMyWord.OpenXml;
using MermaidSharp;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkMyWord.Diagrams;

/// <summary>
/// Provides functionality to render Mermaid diagrams to SVG format using MermaidSharp (Naiad).
/// Pure .NET implementation — no browser or JavaScript runtime required.
/// </summary>
public partial class MermaidRenderer
{
    private const string DiagramFont = "Consolas, 'Cascadia Code', 'Fira Code', monospace";

    private readonly DocumentTheme _theme;

    // Modern chart series palette — used for pie slices, etc.
    // These replace the default Mermaid section colors in order.
    private static readonly string[] LightSeriesColors =
    [
        "#3B82F6", // blue-500
        "#10B981", // emerald-500
        "#F59E0B", // amber-500
        "#8B5CF6", // violet-500
        "#EF4444", // red-500
        "#06B6D4", // cyan-500
        "#EC4899", // pink-500
        "#F97316", // orange-500
    ];

    private static readonly string[] DarkSeriesColors =
    [
        "#60A5FA", // blue-400
        "#34D399", // emerald-400
        "#FBBF24", // amber-400
        "#A78BFA", // violet-400
        "#F87171", // red-400
        "#22D3EE", // cyan-400
        "#F472B6", // pink-400
        "#FB923C", // orange-400
    ];

    // Default Mermaid section/pie colors (hex) in order
    private static readonly string[] MermaidDefaultHexColors =
    [
        "#ECECFF", "#ffffde", "#B5FF20", "#B9B9FF", "#FFB9B9",
        "#ececff", "#FFFFDE", "#b5ff20", "#b9b9ff", "#ffb9b9",
    ];

    // Default Mermaid section/pie colors (rgb inline style) in order
    private static readonly (string rgb, int index)[] MermaidDefaultRgbColors =
    [
        ("rgb(236, 236, 255)", 0),  // #ECECFF
        ("rgb(255, 255, 222)", 1),  // #ffffde
        ("rgb(181, 255, 32)",  2),  // #B5FF20
        ("rgb(185, 185, 255)", 3),  // #B9B9FF
        ("rgb(255, 185, 185)", 4),  // #FFB9B9
    ];

    // Structural colors (non-series) — light theme
    private static readonly (string old, string @new)[] LightStructuralColors =
    [
        ("#9370DB", "#3B82F6"),  // node stroke/header → blue-500
        ("#9370db", "#3B82F6"),
        ("#333333", "#1E293B"),  // text/arrows → slate-800
        ("#aaaa33", "#94A3B8"),  // cluster stroke → slate-400
        ("#AAAA33", "#94A3B8"),
        ("#552222", "#DC2626"),  // error → red-600
        ("#4CAF50", "#3B82F6"),  // gantt task bars → blue-500
        ("#666",    "#64748B"),  // gantt date labels → slate-500
    ];

    // Structural colors — dark theme
    private static readonly (string old, string @new)[] DarkStructuralColors =
    [
        ("#9370DB", "#3B82F6"),  // keep blue for headers (white text on top)
        ("#9370db", "#3B82F6"),
        ("#333333", "#E2E8F0"),  // text/arrows → slate-200
        ("#aaaa33", "#63B3ED"),
        ("#AAAA33", "#63B3ED"),
        ("#552222", "#FC8181"),
        ("#4CAF50", "#3B82F6"),  // gantt task bars → blue-500
        ("#666",    "#94A3B8"),  // gantt date labels → slate-400
    ];

    public MermaidRenderer(DocumentTheme theme = DocumentTheme.Light)
    {
        _theme = theme;
    }

    /// <summary>
    /// Renders Mermaid code to SVG format.
    /// </summary>
    public string? RenderToSvg(string mermaidCode)
    {
        if (string.IsNullOrWhiteSpace(mermaidCode))
        {
            return null;
        }

        try
        {
            // Strip emoji characters before parsing — MermaidSharp's parser
            // cannot handle astral-plane Unicode. We replace each unique emoji
            // grapheme cluster with a single Private Use Area character and
            // restore them in the SVG output after rendering.
            var (sanitizedCode, emojiMap) = StripEmojisFromMermaid(mermaidCode);

            var svg = Mermaid.Render(sanitizedCode, RenderOptions.Default);
            svg = ReplaceForeignObjectsWithText(svg);
            svg = NormalizeErDiagram(svg);
            svg = NormalizeClassDiagram(svg);
            svg = ApplyModernTheme(svg);
            svg = EnsureOpaqueBackground(svg);

            // Restore emoji characters in the SVG text content
            if (emojiMap.Count > 0)
            {
                svg = RestoreEmojisInSvg(svg, emojiMap);
            }

            return svg;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Mermaid rendering error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if the specified language identifier indicates Mermaid content.
    /// </summary>
    public static bool IsMermaidLanguage(string? language)
    {
        return !string.IsNullOrEmpty(language) &&
               language.Equals("mermaid", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Applies the modern color palette and font to the SVG.
    /// </summary>
    private string ApplyModernTheme(string svg)
    {
        bool isDark = _theme == DocumentTheme.Dark;
        var structuralColors = isDark ? DarkStructuralColors : LightStructuralColors;
        bool isPieChart = svg.Contains("pieCircle");
        string textColor = isDark ? "#E2E8F0" : "#1E293B";

        // 1. Replace section/node fill colors
        if (isPieChart)
        {
            svg = ApplyPieChartColors(svg, isDark);
        }
        else
        {
            string nodeFill = isDark ? "#2D3748" : "#DBEAFE";
            foreach (var hex in MermaidDefaultHexColors)
            {
                svg = svg.Replace(hex, nodeFill);
            }
        }

        // 2. Replace structural colors (strokes, task bars, date labels, etc.)
        foreach (var (oldColor, newColor) in structuralColors)
        {
            svg = svg.Replace(oldColor, newColor);
        }

        // 2a. Gantt-specific colors (use attribute context to avoid substring issues)
        if (svg.Contains("stroke=\"#ddd\""))
        {
            string doneColor = isDark ? "#475569" : "#94A3B8";     // slate-600/400
            string activeColor = isDark ? "#60A5FA" : "#3B82F6";   // blue-400/500
            string gridMajor = isDark ? "#334155" : "#CBD5E1";     // slate-700/300
            string gridMinor = isDark ? "#1E293B" : "#E2E8F0";     // slate-800/200

            svg = svg.Replace("fill=\"#808080\"", $"fill=\"{doneColor}\"");
            svg = svg.Replace("fill=\"#2196F3\"", $"fill=\"{activeColor}\"");
            svg = svg.Replace("stroke=\"#ddd\"", $"stroke=\"{gridMajor}\"");
            svg = svg.Replace("stroke=\"#eee\"", $"stroke=\"{gridMinor}\"");
        }

        // 3. Handle short color codes contextually
        // #333 is used in stroke="", fill="" on text, and in CSS
        svg = svg.Replace("stroke=\"#333\"", $"stroke=\"{textColor}\"");
        svg = svg.Replace("fill=\"#333\"", $"fill=\"{textColor}\"");
        svg = svg.Replace("fill:#333;", $"fill:{textColor};");
        svg = svg.Replace("stroke:#333;", $"stroke:{textColor};");

        // #000 in CSS/attributes
        svg = svg.Replace("fill:#000;", $"fill:{textColor};");
        svg = svg.Replace("stroke:#000;", $"stroke:{textColor};");
        svg = svg.Replace("fill=\"#000\"", $"fill=\"{textColor}\"");

        // #fff on rect backgrounds (not on text — text needs to stay white for contrast on colored headers)
        string rectBg = isDark ? "#2D3748" : "#FFFFFF";
        svg = WhiteRectFillRegex().Replace(svg, $"<rect${{attrs}}fill=\"{rectBg}\"");

        // 4. Replace CSS "fill:black" / "stroke:black"
        svg = svg.Replace("fill:black", $"fill:{textColor}");
        svg = svg.Replace("fill: black", $"fill:{textColor}");
        svg = svg.Replace("stroke:black", $"stroke:{textColor}");
        svg = svg.Replace("stroke: black", $"stroke:{textColor}");

        // 5. Set fill on <text> elements that have no fill attribute (they default to black)
        svg = TextOpenTagRegex().Replace(svg, match =>
        {
            var attrs = match.Groups["attrs"].Value;
            if (attrs.Contains("fill=")) return match.Value;
            return $"<text {attrs.TrimEnd()} fill=\"{textColor}\">";
        });

        // 6. Replace edge label backgrounds (handle spacing variants in rgba)
        string edgeBg = isDark ? "rgba(45,55,72,0.95)" : "rgba(241,245,249,0.95)";
        svg = svg.Replace("rgba(232,232,232, 0.8)", edgeBg);
        svg = svg.Replace("rgba(232,232,232,0.8)", edgeBg);
        svg = svg.Replace("rgba(232, 232, 232, 0.8)", edgeBg);
        svg = svg.Replace("rgba(232, 232, 232, 0.5)", edgeBg);

        // 7. Replace all font-family declarations
        svg = MermaidFontCssRegex().Replace(svg, $"font-family:{DiagramFont};");
        svg = MermaidFontAttrRegex().Replace(svg, $"font-family=\"{DiagramFont}\"");

        // 8. Bump stroke widths for crisper lines
        svg = svg.Replace("stroke-width:1px", "stroke-width:1.5px");
        svg = svg.Replace("stroke-width:2.0px", "stroke-width:2px");

        return svg;
    }

    /// <summary>
    /// Recolors pie chart slices and legend swatches using the series palette.
    /// Replaces fills by order (handles hex, hsl, and rgb values).
    /// </summary>
    private string ApplyPieChartColors(string svg, bool isDark)
    {
        var palette = isDark ? DarkSeriesColors : LightSeriesColors;
        int sliceIndex = 0;

        // Recolor pie slices by order — each <path> with class="pieCircle"
        svg = PieSliceFillRegex().Replace(svg, match =>
        {
            var color = palette[sliceIndex % palette.Length];
            sliceIndex++;
            return $"{match.Groups["pre"].Value}fill=\"{color}\"{match.Groups["post"].Value}";
        });

        // Recolor legend swatches to match slices
        int legendIndex = 0;
        svg = PieLegendStyleRegex().Replace(svg, match =>
        {
            var color = palette[legendIndex % palette.Length];
            legendIndex++;
            var rgb = HexToRgb(color);
            return $"style=\"fill: {rgb}; stroke: {rgb};\"";
        });

        return svg;
    }

    /// <summary>
    /// Normalizes ER diagram entities to use a consistent style (light fill, dark text,
    /// matching border style and font size with other diagram types).
    /// </summary>
    private string NormalizeErDiagram(string svg)
    {
        // ER diagrams have <circle> elements (crow's foot notation) but not pieCircle
        if (!svg.Contains("<circle") || svg.Contains("pieCircle"))
            return svg;

        // ER entities have two overlapping rects at the same position:
        //   body:   fill="#ECECFF" stroke="#9370DB" stroke-width="2"
        //   header: fill="#9370DB" stroke="#9370DB" stroke-width="1"  (covers body)
        // Remove the header overlay so entities show as light-filled boxes
        svg = ErHeaderOverlayRegex().Replace(svg, "");

        // Normalize entity rect borders: stroke-width 2→1, add rounded corners
        svg = svg.Replace(
            "fill=\"#ECECFF\" stroke=\"#9370DB\" stroke-width=\"2\"/>",
            "fill=\"#ECECFF\" stroke=\"#9370DB\" stroke-width=\"1\" rx=\"3\" ry=\"3\"/>");

        // Entity name text had fill="#fff" for contrast on the purple header.
        // With the header removed, switch to standard dark text for theming.
        svg = svg.Replace(
            "font-weight=\"bold\" fill=\"#fff\"",
            "font-weight=\"bold\" fill=\"#333\"");

        // Normalize relationship label font size to match entity names (14px)
        svg = svg.Replace("font-size=\"12px\"", "font-size=\"14px\"");

        return svg;
    }

    /// <summary>
    /// Normalizes class diagram boxes to use the same border style as flowcharts/ER
    /// (blue stroke with rounded corners instead of dark stroke with sharp corners).
    /// </summary>
    private static string NormalizeClassDiagram(string svg)
    {
        // Class diagrams have inheritance/aggregation/composition markers
        if (!svg.Contains("\"inheritance\"") &&
            !svg.Contains("\"aggregation\"") &&
            !svg.Contains("\"composition\""))
            return svg;

        // Class rects use fill="#FFFFDE" stroke="#333" — change stroke to #9370DB
        // (which ApplyModernTheme will then theme to #3B82F6) and add rounded corners
        svg = svg.Replace(
            "fill=\"#FFFFDE\" stroke=\"#333\" stroke-width=\"1\"/>",
            "fill=\"#FFFFDE\" stroke=\"#9370DB\" stroke-width=\"1\" rx=\"3\" ry=\"3\"/>");

        return svg;
    }

    /// <summary>
    /// Injects a background rectangle so diagrams are visible.
    /// </summary>
    private string EnsureOpaqueBackground(string svg)
    {
        var bgColor = _theme == DocumentTheme.Dark ? "#1A1A2E" : "#FFFFFF";
        var bgRect = $"<rect width=\"100%\" height=\"100%\" fill=\"{bgColor}\"/>";

        int insertPos = svg.IndexOf("<g ", StringComparison.Ordinal);
        if (insertPos < 0) insertPos = svg.IndexOf("<g>", StringComparison.Ordinal);
        if (insertPos < 0) insertPos = svg.IndexOf("<rect", StringComparison.Ordinal);
        if (insertPos < 0) insertPos = svg.IndexOf("<line", StringComparison.Ordinal);
        if (insertPos < 0) insertPos = svg.IndexOf("<text", StringComparison.Ordinal);

        if (insertPos < 0)
        {
            int defsEnd = svg.LastIndexOf("</defs>", StringComparison.Ordinal);
            if (defsEnd >= 0) insertPos = defsEnd + "</defs>".Length;
        }
        if (insertPos < 0)
        {
            int styleEnd = svg.LastIndexOf("</style>", StringComparison.Ordinal);
            if (styleEnd >= 0) insertPos = styleEnd + "</style>".Length;
        }

        return insertPos >= 0 ? svg.Insert(insertPos, bgRect) : svg;
    }

    /// <summary>
    /// Converts foreignObject elements (HTML-in-SVG) to native SVG text elements.
    /// </summary>
    private string ReplaceForeignObjectsWithText(string svg)
    {
        var textFill = _theme == DocumentTheme.Dark ? "#E2E8F0" : "#1E293B";

        return ForeignObjectRegex().Replace(svg, match =>
        {
            double x = ParseDouble(match.Groups["x"].Value);
            double y = ParseDouble(match.Groups["y"].Value);
            double w = ParseDouble(match.Groups["w"].Value);
            double h = ParseDouble(match.Groups["h"].Value);
            string inner = match.Groups["inner"].Value;

            var textMatch = ParagraphTextRegex().Match(inner);
            string text = textMatch.Success ? textMatch.Groups[1].Value.Trim() : "";
            if (string.IsNullOrEmpty(text)) return match.Value;

            double cx = x + w / 2;
            double cy = y + h / 2;
            string fontSize = h > 30 ? "13" : "11";

            return $"<text x=\"{F(cx)}\" y=\"{F(cy)}\" " +
                   $"text-anchor=\"middle\" dominant-baseline=\"central\" " +
                   $"font-family=\"{DiagramFont}\" font-size=\"{fontSize}\" " +
                   $"fill=\"{textFill}\">" +
                   $"{System.Security.SecurityElement.Escape(text)}</text>";
        });
    }

    private static string HexToRgb(string hex)
    {
        hex = hex.TrimStart('#');
        int r = Convert.ToInt32(hex[..2], 16);
        int g = Convert.ToInt32(hex[2..4], 16);
        int b = Convert.ToInt32(hex[4..6], 16);
        return $"rgb({r}, {g}, {b})";
    }

    private static double ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : 0;

    private static string F(double value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);

    /// <summary>
    /// Replaces emoji grapheme clusters in Mermaid source code with single Private Use Area
    /// characters so MermaidSharp can parse the diagram without failing on astral-plane Unicode.
    /// Returns the sanitized code and a mapping from PUA placeholder → original emoji.
    /// </summary>
    private static (string sanitized, Dictionary<string, string> emojiMap) StripEmojisFromMermaid(string mermaidCode)
    {
        if (!EmojiSegmenter.ContainsEmoji(mermaidCode))
            return (mermaidCode, new Dictionary<string, string>());

        var emojiToPlaceholder = new Dictionary<string, string>();
        var placeholderToEmoji = new Dictionary<string, string>();
        char nextPua = '\uE000';

        var result = new StringBuilder(mermaidCode.Length);
        var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(mermaidCode);

        while (enumerator.MoveNext())
        {
            string element = enumerator.GetTextElement();

            if (EmojiSegmenter.ContainsEmoji(element))
            {
                if (!emojiToPlaceholder.TryGetValue(element, out string? placeholder))
                {
                    placeholder = nextPua.ToString();
                    emojiToPlaceholder[element] = placeholder;
                    placeholderToEmoji[placeholder] = element;
                    nextPua++;
                }

                result.Append(placeholder);
            }
            else
            {
                result.Append(element);
            }
        }

        return (result.ToString(), placeholderToEmoji);
    }

    /// <summary>
    /// Restores emoji characters in SVG output by replacing PUA placeholders.
    /// PUA characters (U+E000+) don't appear in SVG syntax, CSS, or attribute names,
    /// so global replacement is safe and only affects text content.
    /// </summary>
    private static string RestoreEmojisInSvg(string svg, Dictionary<string, string> emojiMap)
    {
        foreach (var (placeholder, emoji) in emojiMap)
        {
            svg = svg.Replace(placeholder, emoji);
        }

        return svg;
    }

    [GeneratedRegex(
        @"<foreignObject\s+x=""(?<x>[0-9.]+)""\s+y=""(?<y>[0-9.]+)""\s+width=""(?<w>[0-9.]+)""\s+height=""(?<h>[0-9.]+)""[^>]*>(?<inner>.*?)</foreignObject>",
        RegexOptions.Singleline)]
    private static partial Regex ForeignObjectRegex();

    [GeneratedRegex(@"<p>(.*?)</p>", RegexOptions.Singleline)]
    private static partial Regex ParagraphTextRegex();

    [GeneratedRegex(@"font-family:[""']?[^;""']+[""']?;")]
    private static partial Regex MermaidFontCssRegex();

    [GeneratedRegex(@"font-family=""[^""]+""")]
    private static partial Regex MermaidFontAttrRegex();

    // Matches <rect ... fill="#fff" or fill="#FFF"> — only on rect elements, not text
    [GeneratedRegex(@"<rect(?<attrs>[^>]*)fill=""#[fF]{3}""")]
    private static partial Regex WhiteRectFillRegex();

    // Matches opening <text ...> tags (captures attributes for fill injection)
    [GeneratedRegex(@"<text (?<attrs>[^>]*)>")]
    private static partial Regex TextOpenTagRegex();

    // Matches <path ...fill="..."...class="pieCircle"...> for recoloring pie slices by order
    [GeneratedRegex(@"(?<pre><path[^>]*?)fill=""[^""]*""(?<post>[^>]*class=""pieCircle""[^>]*>)")]
    private static partial Regex PieSliceFillRegex();

    // Matches legend rect inline styles: style="fill: rgb(...); stroke: rgb(...);"
    [GeneratedRegex(@"style=""fill:\s*rgb\([^)]*\);\s*stroke:\s*rgb\([^)]*\);""")]
    private static partial Regex PieLegendStyleRegex();

    // Matches ER entity header overlay rects (fill and stroke both #9370DB)
    [GeneratedRegex(@"<rect[^>]*fill=""#9370[dD][bB]""[^>]*stroke=""#9370[dD][bB]""[^>]*/>")]
    private static partial Regex ErHeaderOverlayRegex();
}
