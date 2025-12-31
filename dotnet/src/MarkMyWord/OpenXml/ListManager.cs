using DocumentFormat.OpenXml.Wordprocessing;
using MarkMyWord.Configuration;

namespace MarkMyWord.OpenXml;

/// <summary>
/// Manages list numbering definitions and state.
/// </summary>
public class ListManager
{
    private readonly DocumentBuilder _builder;
    private readonly StyleConfiguration _config;
    private readonly Stack<ListContext> _listStack = new();
    private int _nextAbstractNumId = 1;
    private readonly Dictionary<string, int> _abstractNumCache = new();

    public ListManager(DocumentBuilder builder, StyleConfiguration config)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Enters a new list context.
    /// </summary>
    public void EnterList(bool isOrdered)
    {
        var level = _listStack.Count;
        var cacheKey = $"{(isOrdered ? "ordered" : "unordered")}";

        // Get or create abstract numbering definition
        if (!_abstractNumCache.TryGetValue(cacheKey, out var abstractNumId))
        {
            abstractNumId = _nextAbstractNumId++;
            CreateAbstractNum(abstractNumId, isOrdered);
            _abstractNumCache[cacheKey] = abstractNumId;
        }

        // Create a new numbering instance
        var numberingId = _builder.GetOrCreateNumberingId(
            isOrdered ? NumberFormatValues.Decimal : NumberFormatValues.Bullet,
            level
        );

        _listStack.Push(new ListContext
        {
            IsOrdered = isOrdered,
            Level = level,
            NumberingId = numberingId,
            AbstractNumId = abstractNumId
        });
    }

    /// <summary>
    /// Exits the current list context.
    /// </summary>
    public void ExitList()
    {
        if (_listStack.Count > 0)
        {
            _listStack.Pop();
        }
    }

    /// <summary>
    /// Gets the current list level (0-based).
    /// </summary>
    public int CurrentLevel => _listStack.Count > 0 ? _listStack.Peek().Level : 0;

    /// <summary>
    /// Gets numbering properties for the current list item.
    /// </summary>
    public NumberingProperties? GetNumberingProperties()
    {
        if (_listStack.Count == 0)
            return null;

        var context = _listStack.Peek();

        return new NumberingProperties(
            new NumberingLevelReference { Val = context.Level },
            new NumberingId { Val = context.NumberingId }
        );
    }

    /// <summary>
    /// Creates an abstract numbering definition.
    /// </summary>
    private void CreateAbstractNum(int abstractNumId, bool isOrdered)
    {
        var numberingPart = _builder.GetOrCreateNumberingPart();
        var numbering = numberingPart.Numbering;

        var abstractNum = new AbstractNum { AbstractNumberId = abstractNumId };

        // Create levels 0-8 (OpenXML supports up to 9 levels)
        for (int i = 0; i <= 8; i++)
        {
            var level = new Level
            {
                LevelIndex = i,
                StartNumberingValue = new StartNumberingValue { Val = 1 }
            };

            if (isOrdered)
            {
                level.NumberingFormat = new NumberingFormat { Val = NumberFormatValues.Decimal };
                level.LevelText = new LevelText { Val = $"%{i + 1}." };
                level.LevelJustification = new LevelJustification { Val = LevelJustificationValues.Left };
            }
            else
            {
                level.NumberingFormat = new NumberingFormat { Val = NumberFormatValues.Bullet };
                // Use different bullet styles for different levels
                level.LevelText = new LevelText { Val = GetBulletChar(i) };
                level.LevelJustification = new LevelJustification { Val = LevelJustificationValues.Left };
            }

            // Set indentation
            var indentTwips = _config.ListIndentationTwips * (i + 1);
            level.AppendChild(new PreviousParagraphProperties(
                new Indentation
                {
                    Left = indentTwips.ToString(),
                    Hanging = "360" // 0.25 inch hanging indent
                }
            ));

            abstractNum.AppendChild(level);
        }

        numbering.AppendChild(abstractNum);
    }

    /// <summary>
    /// Gets the bullet character for a given level.
    /// </summary>
    private string GetBulletChar(int level)
    {
        return (level % 3) switch
        {
            0 => "●",  // Solid bullet
            1 => "○",  // Hollow bullet
            2 => "■",  // Square bullet
            _ => "●"
        };
    }
}

/// <summary>
/// Represents the context of a list being rendered.
/// </summary>
internal class ListContext
{
    public bool IsOrdered { get; set; }
    public int Level { get; set; }
    public int NumberingId { get; set; }
    public int AbstractNumId { get; set; }
}
