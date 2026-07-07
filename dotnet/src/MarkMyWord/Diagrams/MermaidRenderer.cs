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
    private const string DiagramFont = "Segoe UI, Arial, Helvetica, sans-serif";

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
            svg = ReflowSequenceDiagram(svg);
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

            // Decode HTML entities from the foreignObject content (e.g. &quot; → ")
            // before re-escaping for SVG — prevents double-escaping like &amp;quot;
            text = System.Net.WebUtility.HtmlDecode(text);

            // Strip surrounding quotes that are Mermaid label syntax, not content.
            // In Mermaid, A["Label"] means the label is "Label" — the quotes delimit
            // the label text and should not appear in the rendered output.
            if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
            {
                text = text[1..^1];
            }

            double cx = x + w / 2;
            double cy = y + h / 2;
            // Match Naiad's default font sizes: 16px for flowchart nodes (h>30),
            // 14px for smaller labels. These must align with the box sizes Naiad computed.
            string fontSize = h > 30 ? "14" : "12";

            return $"<text x=\"{F(cx)}\" y=\"{F(cy)}\" " +
                   $"text-anchor=\"middle\" dominant-baseline=\"central\" " +
                   $"font-family=\"{DiagramFont}\" font-size=\"{fontSize}\" " +
                   $"fill=\"{textFill}\">" +
                   $"{System.Security.SecurityElement.Escape(text)}</text>";
        });
    }

    /// <summary>
    /// Reflows sequence diagram SVG to fit long participant names and message labels.
    /// Naiad uses fixed 100px boxes and 150px spacing which causes text overflow.
    /// This method wraps participant text, repositions elements, and handles &lt;br/&gt; in notes.
    /// </summary>
    private static string ReflowSequenceDiagram(string svg)
    {
        // Detect sequence diagrams by dashed lifelines
        if (!svg.Contains("stroke-dasharray=\"5,5\""))
            return svg;

        // Parse the original viewBox
        var viewBoxMatch = ViewBoxRegex().Match(svg);
        if (!viewBoxMatch.Success) return svg;

        double origWidth = ParseDouble(viewBoxMatch.Groups[3].Value);
        double origHeight = ParseDouble(viewBoxMatch.Groups[4].Value);

        // Find all participant boxes: <rect> elements in the top row (y="20")
        // and their corresponding <text> elements
        var participantRects = ParticipantRectRegex().Matches(svg);
        if (participantRects.Count == 0) return svg;

        // Collect participant info: center x, text content
        var participants = new List<(double centerX, double rectX, double rectWidth, string text)>();
        var participantTexts = ParticipantTextRegex().Matches(svg);

        foreach (Match rect in participantRects)
        {
            double rx = ParseDouble(rect.Groups["x"].Value);
            double rw = ParseDouble(rect.Groups["w"].Value);
            double centerX = rx + rw / 2;

            // Find matching text element (same y="40" area, closest x)
            string textContent = "";
            foreach (Match txt in participantTexts)
            {
                double tx = ParseDouble(txt.Groups["x"].Value);
                if (Math.Abs(tx - centerX) < 5)
                {
                    textContent = txt.Groups["text"].Value;
                    break;
                }
            }
            participants.Add((centerX, rx, rw, textContent));
        }

        if (participants.Count < 2) return svg;

        // Calculate needed width for each participant box
        // Cap at maxBoxWidth and wrap text to multiple lines if needed
        const double charWidth = 7.5;
        const double boxPadding = 30; // 15px padding each side
        const double maxBoxWidth = 180.0; // max before wrapping
        const double lineHeight = 18.0; // line height for wrapped text
        const double baseBoxHeight = 40.0;

        var neededWidths = new double[participants.Count];
        var wrappedLines = new List<string[]>(); // wrapped text lines per participant
        double maxExtraHeight = 0;

        for (int i = 0; i < participants.Count; i++)
        {
            string text = participants[i].text;
            double naturalWidth = text.Length * charWidth + boxPadding;

            if (naturalWidth <= maxBoxWidth)
            {
                neededWidths[i] = Math.Max(100, naturalWidth);
                wrappedLines.Add([text]);
            }
            else
            {
                // Wrap text to fit within maxBoxWidth
                var lines = WrapText(text, maxBoxWidth - boxPadding, charWidth);
                neededWidths[i] = maxBoxWidth;
                wrappedLines.Add(lines);
                double extraH = (lines.Length - 1) * lineHeight;
                maxExtraHeight = Math.Max(maxExtraHeight, extraH);
            }
        }

        // Calculate new center positions with appropriate spacing
        const double startMargin = 20;
        const double gapBetween = 50; // minimum gap between box edges

        // Also consider message label widths between adjacent participants
        var messageLabelWidths = GetMessageLabelWidths(svg, participants);

        double[] newCenterXs = new double[participants.Count];
        double currentX = startMargin + neededWidths[0] / 2;
        newCenterXs[0] = currentX;

        for (int i = 1; i < participants.Count; i++)
        {
            double minSpacing = (neededWidths[i - 1] / 2) + gapBetween + (neededWidths[i] / 2);
            // Ensure enough room for message labels between these participants
            double labelWidth = messageLabelWidths.GetValueOrDefault((i - 1, i), 0);
            double labelSpacing = labelWidth + 40; // 20px padding each side of label
            double spacing = Math.Max(minSpacing, labelSpacing);
            currentX += spacing;
            newCenterXs[i] = currentX;
        }

        double newWidth = currentX + neededWidths[^1] / 2 + startMargin;
        double newBoxHeight = baseBoxHeight + maxExtraHeight;
        double yShift = maxExtraHeight; // how much to push everything below boxes down

        if (newWidth <= origWidth * 1.05 && maxExtraHeight == 0) return svg; // No significant reflow needed

        // Build a mapping from old center X → new center X
        var centerMap = new Dictionary<double, double>();
        for (int i = 0; i < participants.Count; i++)
            centerMap[participants[i].centerX] = newCenterXs[i];

        // Rewrite SVG elements
        // 1. Rewrite participant rects (top and bottom) — resize and adjust height
        svg = AllParticipantRectRegex().Replace(svg, match =>
        {
            double rx = ParseDouble(match.Groups["x"].Value);
            double rw = ParseDouble(match.Groups["w"].Value);
            double ry = ParseDouble(match.Groups["y"].Value);
            double oldCenter = rx + rw / 2;

            var (mappedCenter, newBoxWidth) = FindClosestMapping(oldCenter, centerMap, neededWidths, participants);
            if (mappedCenter < 0) return match.Value;

            double newRx = mappedCenter - newBoxWidth / 2;
            double newRy = ry == 20 ? 20 : ry + yShift; // top stays, bottom shifts
            string result = match.Value
                .Replace($"x=\"{match.Groups["x"].Value}\"", $"x=\"{F(newRx)}\"")
                .Replace($"y=\"{match.Groups["y"].Value}\"", $"y=\"{F(newRy)}\"")
                .Replace($"width=\"{match.Groups["w"].Value}\"", $"width=\"{F(newBoxWidth)}\"")
                .Replace("height=\"40\"", $"height=\"{F(newBoxHeight)}\"");
            return result;
        });

        // 2. Rewrite participant text — wrap into tspan if needed, adjust y
        // Participant text y is 40 (top row) or origHeight-40 (bottom row center)
        double bottomRowTextY = origHeight - 40; // bottom boxes: y=origHeight-60, h=40, center=origHeight-40
        svg = ParticipantTextRegex().Replace(svg, match =>
        {
            double tx = ParseDouble(match.Groups["x"].Value);
            double ty = ParseDouble(ExtractY(match.Value));
            string text = match.Groups["text"].Value;

            // Only process participant labels (top row y=40, bottom row y≈bottomRowTextY)
            bool isTopRow = Math.Abs(ty - 40) < 5;
            bool isBottomRow = Math.Abs(ty - bottomRowTextY) < 5;
            if (!isTopRow && !isBottomRow) return match.Value;

            // Find which participant this belongs to
            int idx = -1;
            for (int i = 0; i < participants.Count; i++)
            {
                if (Math.Abs(tx - participants[i].centerX) < 5) { idx = i; break; }
            }
            if (idx < 0) return match.Value;

            double newTx = newCenterXs[idx];
            string[] lines = wrappedLines[idx];

            double baseY = isTopRow
                ? 20 + newBoxHeight / 2
                : (origHeight - 60) + yShift + newBoxHeight / 2;

            if (lines.Length == 1)
            {
                // Single line — just remap x and y
                // Bottom box y = (origHeight - 60) + yShift, center = that + newBoxHeight/2
                double newY = isTopRow
                    ? 20 + newBoxHeight / 2
                    : (origHeight - 60) + yShift + newBoxHeight / 2;
                return match.Value
                    .Replace($"x=\"{match.Groups["x"].Value}\"", $"x=\"{F(newTx)}\"")
                    .Replace(YAttrRegex().Match(match.Value).Value, $"y=\"{F(newY)}\"");
            }
            else
            {
                // Multi-line — replace <text>content</text> with <text><tspan>...</tspan></text>
                double firstLineY = baseY - ((lines.Length - 1) * lineHeight / 2);
                var sb = new StringBuilder();
                // Rebuild opening tag with new x and y
                string openTag = match.Value[..match.Value.IndexOf('>')];
                openTag = openTag.Replace($"x=\"{match.Groups["x"].Value}\"", $"x=\"{F(newTx)}\"");
                openTag = YAttrRegex().Replace(openTag, $"y=\"{F(firstLineY)}\"");
                sb.Append(openTag).Append('>');
                for (int li = 0; li < lines.Length; li++)
                {
                    if (li == 0)
                        sb.Append($"<tspan x=\"{F(newTx)}\" dy=\"0\">{System.Security.SecurityElement.Escape(lines[li])}</tspan>");
                    else
                        sb.Append($"<tspan x=\"{F(newTx)}\" dy=\"{F(lineHeight)}\">{System.Security.SecurityElement.Escape(lines[li])}</tspan>");
                }
                sb.Append("</text>");
                return sb.ToString();
            }
        });

        // 3. Rewrite lifeline vertical lines (x1=x2=center of participant)
        // Also shift y1 down for taller boxes
        svg = LifelineRegex().Replace(svg, match =>
        {
            double x1 = ParseDouble(match.Groups["x1"].Value);
            double y1 = ParseDouble(match.Groups["y1"].Value);
            double y2 = ParseDouble(match.Groups["y2"].Value);
            var mapped = FindNearestCenter(x1, centerMap);
            if (mapped < 0) return match.Value;
            double newY1 = y1 + yShift; // lifelines start below taller boxes
            double newY2 = y2 + yShift;
            return match.Value
                .Replace($"x1=\"{match.Groups["x1"].Value}\"", $"x1=\"{F(mapped)}\"")
                .Replace($"x2=\"{match.Groups["x2"].Value}\"", $"x2=\"{F(mapped)}\"")
                .Replace($"y1=\"{match.Groups["y1"].Value}\"", $"y1=\"{F(newY1)}\"")
                .Replace($"y2=\"{match.Groups["y2"].Value}\"", $"y2=\"{F(newY2)}\"");
        });

        // 4. Rewrite message arrows (horizontal lines and polygons) — shift y too
        svg = MessageLineRegex().Replace(svg, match =>
        {
            double x1 = ParseDouble(match.Groups["x1"].Value);
            double x2 = ParseDouble(match.Groups["x2"].Value);
            double y1 = ParseDouble(match.Groups["y1"].Value);
            double y2 = ParseDouble(match.Groups["y2"].Value);
            double newX1 = FindNearestCenter(x1, centerMap);
            double newX2 = FindNearestCenter(x2, centerMap);
            if (newX1 < 0 || newX2 < 0) return match.Value;
            return match.Value
                .Replace($"x1=\"{match.Groups["x1"].Value}\"", $"x1=\"{F(newX1)}\"")
                .Replace($"x2=\"{match.Groups["x2"].Value}\"", $"x2=\"{F(newX2)}\"")
                .Replace($"y1=\"{match.Groups["y1"].Value}\"", $"y1=\"{F(y1 + yShift)}\"")
                .Replace($"y2=\"{match.Groups["y2"].Value}\"", $"y2=\"{F(y2 + yShift)}\"");
        });

        // 5. Rewrite arrowhead polygons — shift y
        svg = ArrowPolygonRegex().Replace(svg, match =>
        {
            string points = match.Groups["points"].Value;
            var coords = points.Split(' ');
            var newCoords = new List<string>();
            foreach (var coord in coords)
            {
                var parts = coord.Split(',');
                if (parts.Length == 2)
                {
                    double px = ParseDouble(parts[0]);
                    double py = ParseDouble(parts[1]) + yShift;
                    double newPx = FindNearestCenter(px, centerMap, tolerance: 10);
                    if (newPx >= 0) px = newPx + (ParseDouble(parts[0]) - FindNearestOriginal(px, centerMap, tolerance: 10));
                    newCoords.Add($"{F(px)},{F(py)}");
                }
                else newCoords.Add(coord);
            }
            return $"<polygon points=\"{string.Join(" ", newCoords)}\"";
        });

        // 6. Rewrite message label text positions
        var remappedLines = new List<(double x1, double x2, double y)>();
        foreach (Match lm in MessageLineRegex().Matches(svg))
        {
            double lx1 = ParseDouble(lm.Groups["x1"].Value);
            double lx2 = ParseDouble(lm.Groups["x2"].Value);
            double ly = ParseDouble(lm.Groups["y1"].Value);
            remappedLines.Add((lx1, lx2, ly));
        }

        svg = MessageTextRegex().Replace(svg, match =>
        {
            double tx = ParseDouble(match.Groups["x"].Value);
            double ty = ParseDouble(match.Groups["y"].Value);
            double newTy = ty + yShift;

            // Find the arrow line closest to this label (label is ~8px above its line)
            var bestLine = remappedLines
                .OrderBy(l => Math.Abs(l.y - (newTy + 8)))
                .FirstOrDefault();

            double newTx;
            if (bestLine != default && Math.Abs(bestLine.y - (newTy + 8)) < 5)
                newTx = (bestLine.x1 + bestLine.x2) / 2;
            else
                newTx = RemapX(tx, participants, newCenterXs);

            return match.Value
                .Replace($"x=\"{match.Groups["x"].Value}\"", $"x=\"{F(newTx)}\"")
                .Replace($"y=\"{match.Groups["y"].Value}\"", $"y=\"{F(newTy)}\"");
        });

        // 7. Rewrite note paths and text — remap x and shift y
        svg = NotePathRegex().Replace(svg, match =>
        {
            string result = RemapNotePath(match.Value, match.Groups["path"].Value, participants, newCenterXs);
            // Shift y coordinates in the path
            return ShiftPathY(result, yShift);
        });

        // Remap note fold lines (small lines at x=122 etc that aren't lifelines/messages)
        svg = NoteFoldLineRegex().Replace(svg, match =>
        {
            double x1 = ParseDouble(match.Groups["x1"].Value);
            double y1 = ParseDouble(match.Groups["y1"].Value);
            double x2 = ParseDouble(match.Groups["x2"].Value);
            double y2 = ParseDouble(match.Groups["y2"].Value);
            double newX1 = RemapX(x1, participants, newCenterXs);
            double newX2 = RemapX(x2, participants, newCenterXs);
            return match.Value
                .Replace($"x1=\"{match.Groups["x1"].Value}\"", $"x1=\"{F(newX1)}\"")
                .Replace($"y1=\"{match.Groups["y1"].Value}\"", $"y1=\"{F(y1 + yShift)}\"")
                .Replace($"x2=\"{match.Groups["x2"].Value}\"", $"x2=\"{F(newX2)}\"")
                .Replace($"y2=\"{match.Groups["y2"].Value}\"", $"y2=\"{F(y2 + yShift)}\"");
        });

        svg = NoteTextRegex().Replace(svg, match =>
        {
            double tx = ParseDouble(match.Groups["x"].Value);
            double ty = ParseDouble(match.Groups["y"].Value);

            // Skip participant text (already handled in step 2)
            for (int i = 0; i < newCenterXs.Length; i++)
            {
                if (Math.Abs(tx - newCenterXs[i]) < 5) return match.Value;
            }

            double newTx = RemapNoteX(tx, participants, newCenterXs);
            return match.Value
                .Replace($"x=\"{match.Groups["x"].Value}\"", $"x=\"{F(newTx)}\"")
                .Replace($"y=\"{match.Groups["y"].Value}\"", $"y=\"{F(ty + yShift)}\"");
        });

        // 8. Handle <br/> in text elements → multi-line tspan
        svg = HandleLineBreaksInText(svg);

        // 9. Update viewBox and max-width
        // Calculate left padding needed: note text centered at first participant may extend left of x=0
        double leftPad = 0;
        // Check if any note text centered at first participant extends past x=0
        // Note text "LLM fills slots from context" (27 chars) centered at x=80 extends ~100px left
        foreach (Match nt in NoteTextRegex().Matches(svg))
        {
            double ntx = ParseDouble(nt.Groups["x"].Value);
            string ntText = nt.Groups["text"].Value;
            double halfWidth = ntText.Length * charWidth / 2;
            double leftEdge = ntx - halfWidth;
            if (leftEdge < 0)
                leftPad = Math.Max(leftPad, -leftEdge + 10);
        }

        double newHeight = origHeight + yShift;
        double totalWidth = newWidth + leftPad;

        if (leftPad > 0)
        {
            // Shift viewBox origin left (negative x) to show content that extends past x=0
            svg = ViewBoxRegex().Replace(svg, $"viewBox=\"{F(-leftPad)} 0 {F(totalWidth)} {F(newHeight)}\"");
        }
        else
        {
            svg = ViewBoxRegex().Replace(svg, $"viewBox=\"0 0 {F(totalWidth)} {F(newHeight)}\"");
        }
        svg = MaxWidthRegex().Replace(svg, $"max-width: {F(totalWidth)}px;");

        return svg;
    }

    /// <summary>
    /// Wraps text to fit within a given pixel width, breaking at word boundaries.
    /// </summary>
    private static string[] WrapText(string text, double maxWidth, double charWidth)
    {
        var words = text.Split(' ');
        var lines = new List<string>();
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            double lineWidthWithWord = (currentLine.Length + (currentLine.Length > 0 ? 1 : 0) + word.Length) * charWidth;
            if (currentLine.Length > 0 && lineWidthWithWord > maxWidth)
            {
                lines.Add(currentLine.ToString());
                currentLine.Clear();
            }
            if (currentLine.Length > 0) currentLine.Append(' ');
            currentLine.Append(word);
        }
        if (currentLine.Length > 0) lines.Add(currentLine.ToString());

        return lines.ToArray();
    }

    private static string ShiftPathY(string pathStr, double yShift)
    {
        if (yShift == 0) return pathStr;
        return NotePathCoordRegex().Replace(pathStr, m =>
        {
            string prefix = m.Groups["prefix"].Value;
            double x = ParseDouble(m.Groups["x"].Value);
            double y = ParseDouble(m.Groups["y"].Value) + yShift;
            return $"{prefix}{F(x)},{F(y)}";
        });
    }

    private static string ExtractY(string element)
    {
        var m = YAttrRegex().Match(element);
        return m.Success ? m.Groups["y"].Value : "0";
    }

    private static Dictionary<(int from, int to), double> GetMessageLabelWidths(
        string svg, List<(double centerX, double rectX, double rectWidth, string text)> participants)
    {
        var result = new Dictionary<(int, int), double>();
        const double charWidth = 7.5;

        foreach (Match match in MessageTextRegex().Matches(svg))
        {
            double tx = ParseDouble(match.Groups["x"].Value);
            string text = match.Groups["text"].Value;
            double textWidth = text.Length * charWidth;

            // Find which two participants this label is between
            for (int i = 0; i < participants.Count - 1; i++)
            {
                double mid = (participants[i].centerX + participants[i + 1].centerX) / 2;
                if (Math.Abs(tx - mid) < 80) // within range of midpoint
                {
                    var key = (i, i + 1);
                    result[key] = Math.Max(result.GetValueOrDefault(key, 0), textWidth);
                    break;
                }
                // Check wider spans (message from participant 0 to 2, etc.)
                for (int j = i + 2; j < participants.Count; j++)
                {
                    double midWide = (participants[i].centerX + participants[j].centerX) / 2;
                    if (Math.Abs(tx - midWide) < 10)
                    {
                        // This spans multiple participants - the spacing between intermediates
                        // will naturally accommodate it
                        break;
                    }
                }
            }
        }
        return result;
    }

    private static (double mappedCenter, double newWidth) FindClosestMapping(
        double oldCenter, Dictionary<double, double> centerMap, double[] neededWidths,
        List<(double centerX, double rectX, double rectWidth, string text)> participants)
    {
        double bestDist = double.MaxValue;
        int bestIdx = -1;
        for (int i = 0; i < participants.Count; i++)
        {
            double dist = Math.Abs(oldCenter - participants[i].centerX);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx = i;
            }
        }
        if (bestIdx < 0 || bestDist > 5) return (-1, 0);
        return (centerMap[participants[bestIdx].centerX], neededWidths[bestIdx]);
    }

    private static double FindNearestCenter(double x, Dictionary<double, double> centerMap, double tolerance = 5)
    {
        foreach (var (oldC, newC) in centerMap)
        {
            if (Math.Abs(x - oldC) < tolerance) return newC;
        }
        return -1;
    }

    private static double FindNearestOriginal(double x, Dictionary<double, double> centerMap, double tolerance = 5)
    {
        foreach (var (oldC, _) in centerMap)
        {
            if (Math.Abs(x - oldC) < tolerance) return oldC;
        }
        return x;
    }

    private static double RemapMidpoint(double oldX,
        List<(double centerX, double rectX, double rectWidth, string text)> participants,
        double[] newCenters)
    {
        // Find which two participants this x is the midpoint of.
        // Prefer the narrowest span (closest pair) when multiple pairs share the same midpoint.
        double bestSpan = double.MaxValue;
        int bestI = -1, bestJ = -1;

        for (int i = 0; i < participants.Count - 1; i++)
        {
            for (int j = i + 1; j < participants.Count; j++)
            {
                double oldMid = (participants[i].centerX + participants[j].centerX) / 2;
                if (Math.Abs(oldX - oldMid) < 5)
                {
                    double span = participants[j].centerX - participants[i].centerX;
                    if (span < bestSpan)
                    {
                        bestSpan = span;
                        bestI = i;
                        bestJ = j;
                    }
                }
            }
        }

        if (bestI >= 0)
            return (newCenters[bestI] + newCenters[bestJ]) / 2;

        // Fallback: linear interpolation across full width
        return RemapX(oldX, participants, newCenters);
    }

    private static double RemapNoteX(double oldX,
        List<(double centerX, double rectX, double rectWidth, string text)> participants,
        double[] newCenters)
    {
        return RemapX(oldX, participants, newCenters);
    }

    private static double RemapX(double oldX,
        List<(double centerX, double rectX, double rectWidth, string text)> participants,
        double[] newCenters)
    {
        // Linear interpolation between participant centers
        if (participants.Count < 2) return oldX;

        // Before first participant
        if (oldX <= participants[0].centerX)
        {
            double offset = participants[0].centerX - oldX;
            return newCenters[0] - offset;
        }
        // After last participant
        if (oldX >= participants[^1].centerX)
        {
            double offset = oldX - participants[^1].centerX;
            return newCenters[^1] + offset;
        }
        // Between two participants — interpolate
        for (int i = 0; i < participants.Count - 1; i++)
        {
            if (oldX >= participants[i].centerX && oldX <= participants[i + 1].centerX)
            {
                double t = (oldX - participants[i].centerX) /
                           (participants[i + 1].centerX - participants[i].centerX);
                return newCenters[i] + t * (newCenters[i + 1] - newCenters[i]);
            }
        }
        return oldX;
    }

    private static string RemapNotePath(string fullMatch, string pathData,
        List<(double centerX, double rectX, double rectWidth, string text)> participants,
        double[] newCenters)
    {
        // Note paths look like: d="M10,260 L122,260 L130,268 L130,300 L10,300 Z"
        // Remap all x coordinates
        return NotePathCoordRegex().Replace(fullMatch, coordMatch =>
        {
            string prefix = coordMatch.Groups["prefix"].Value;
            double x = ParseDouble(coordMatch.Groups["x"].Value);
            double y = ParseDouble(coordMatch.Groups["y"].Value);
            double newX = RemapX(x, participants, newCenters);
            return $"{prefix}{F(newX)},{F(y)}";
        });
    }

    private static string HandleLineBreaksInText(string svg)
    {
        // Replace <br/> (HTML-escaped as &lt;br/&gt; in text content) with tspan line breaks
        // In SVG text elements, we need to use multiple <tspan> elements with dy offsets
        return BrTagInTextRegex().Replace(svg, match =>
        {
            string before = match.Groups["before"].Value;
            string text = match.Groups["text"].Value;

            if (!text.Contains("&lt;br/&gt;") && !text.Contains("<br/>"))
                return match.Value;

            // Split on <br/> variants
            string cleanText = text.Replace("&lt;br/&gt;", "\n").Replace("<br/>", "\n");
            var lines = cleanText.Split('\n');

            if (lines.Length <= 1) return match.Value;

            // Build multi-line text with tspan elements
            var sb = new System.Text.StringBuilder();
            sb.Append(before);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                    sb.Append($"<tspan x=\"\" dy=\"0\">{lines[i].Trim()}</tspan>");
                else
                    sb.Append($"<tspan x=\"\" dy=\"1.2em\">{lines[i].Trim()}</tspan>");
            }
            sb.Append("</text>");

            // We need to patch in the x attribute from the parent <text> element
            string result = sb.ToString();
            var xMatch = TextXAttrRegex().Match(before);
            if (xMatch.Success)
            {
                string xVal = xMatch.Groups["x"].Value;
                result = result.Replace("x=\"\"", $"x=\"{xVal}\"");
            }
            return result;
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

    // Sequence diagram reflow regexes
    [GeneratedRegex(@"viewBox=""(?<x>[0-9.\-]+)\s+(?<y>[0-9.\-]+)\s+(?<w>[0-9.]+)\s+(?<h>[0-9.]+)""")]
    private static partial Regex ViewBoxRegex();

    [GeneratedRegex(@"max-width:\s*[0-9.]+px;")]
    private static partial Regex MaxWidthRegex();

    // Participant rects at y=20 (top row)
    [GeneratedRegex(@"<rect\s+x=""(?<x>[0-9.]+)""\s+y=""20""\s+width=""(?<w>[0-9.]+)""\s+height=""40""[^>]*/>")]
    private static partial Regex ParticipantRectRegex();

    // All participant rects (top y=20 and bottom row)
    [GeneratedRegex(@"<rect\s+x=""(?<x>[0-9.]+)""\s+y=""(?<y>\d+)""\s+width=""(?<w>[0-9.]+)""\s+height=""40""[^>]*/>")]
    private static partial Regex AllParticipantRectRegex();

    // Participant text (y="40" for top, y="480" etc for bottom) — must have dominant-baseline="middle"
    // to exclude message labels which use dominant-baseline="bottom"
    [GeneratedRegex(@"<text\s+x=""(?<x>[0-9.]+)""\s+y=""(?:40|\d+)""\s+text-anchor=""middle""\s+dominant-baseline=""middle""[^>]*>(?<text>[^<]+)</text>")]
    private static partial Regex ParticipantTextRegex();

    // Lifeline vertical dashed lines
    [GeneratedRegex(@"<line\s+x1=""(?<x1>[0-9.]+)""\s+y1=""(?<y1>[0-9.]+)""\s+x2=""(?<x2>[0-9.]+)""\s+y2=""(?<y2>[0-9.]+)""\s+stroke=""#999""\s+stroke-width=""1""\s+stroke-dasharray=""5,5""/>")]
    private static partial Regex LifelineRegex();

    // Message horizontal lines (solid or dashed)
    [GeneratedRegex(@"<line\s+x1=""(?<x1>[0-9.]+)""\s+y1=""(?<y1>[0-9.]+)""\s+x2=""(?<x2>[0-9.]+)""\s+y2=""(?<y2>[0-9.]+)""\s+stroke=""#[0-9A-Fa-f]+""\s+stroke-width=""1""(?:\s+stroke-dasharray=""5,5"")?\s*/>")]
    private static partial Regex MessageLineRegex();

    // Arrowhead polygons
    [GeneratedRegex(@"<polygon\s+points=""(?<points>[^""]+)""")]
    private static partial Regex ArrowPolygonRegex();

    // Message label text (above arrows)
    [GeneratedRegex(@"<text\s+x=""(?<x>[0-9.]+)""\s+y=""(?<y>[0-9.]+)""\s+text-anchor=""middle""\s+dominant-baseline=""bottom""[^>]*>(?<text>[^<]+)</text>")]
    private static partial Regex MessageTextRegex();

    // Note path elements
    [GeneratedRegex(@"<path\s+d=""(?<path>[^""]+)""\s+fill=""#FFFFCC""[^>]*/>")]
    private static partial Regex NotePathRegex();

    // Note text elements (inside notes - positioned near note paths)
    [GeneratedRegex(@"<text\s+x=""(?<x>[0-9.]+)""\s+y=""(?<y>[0-9.]+)""\s+text-anchor=""middle""\s+dominant-baseline=""middle""[^>]*>(?<text>[^<]+)</text>")]
    private static partial Regex NoteTextRegex();

    // Coordinate pairs in note path d="" attribute
    [GeneratedRegex(@"(?<prefix>[MLZ]\s*)(?<x>[0-9.]+),(?<y>[0-9.]+)")]
    private static partial Regex NotePathCoordRegex();

    // Text elements that contain <br/> (for line break handling)
    [GeneratedRegex(@"(?<before><text[^>]*>)(?<text>.*?)</text>", RegexOptions.Singleline)]
    private static partial Regex BrTagInTextRegex();

    // Extract x attribute from text element
    [GeneratedRegex(@"x=""(?<x>[0-9.]+)""")]
    private static partial Regex TextXAttrRegex();

    // Extract/replace y attribute
    [GeneratedRegex(@"y=""(?<y>[0-9.]+)""")]
    private static partial Regex YAttrRegex();

    // Note fold lines (stroke="#AAAA33" — Naiad's default note border, before theming)
    [GeneratedRegex(@"<line\s+x1=""(?<x1>[0-9.]+)""\s+y1=""(?<y1>[0-9.]+)""\s+x2=""(?<x2>[0-9.]+)""\s+y2=""(?<y2>[0-9.]+)""\s+stroke=""#[Aa]{4}33""\s+stroke-width=""1""/>")]
    private static partial Regex NoteFoldLineRegex();
}
