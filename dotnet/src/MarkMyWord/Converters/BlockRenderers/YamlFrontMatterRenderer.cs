using Markdig.Extensions.Yaml;

namespace MarkMyWord.Converters.BlockRenderers;

/// <summary>
/// No-op renderer for YAML frontmatter blocks.
/// Frontmatter is consumed for metadata extraction and should not appear in the document body.
/// </summary>
public class YamlFrontMatterRenderer : OpenXmlObjectRenderer<YamlFrontMatterBlock>
{
    protected override void Write(OpenXmlRenderer renderer, YamlFrontMatterBlock obj)
    {
        // Intentionally empty — frontmatter is not rendered as document content.
    }
}
