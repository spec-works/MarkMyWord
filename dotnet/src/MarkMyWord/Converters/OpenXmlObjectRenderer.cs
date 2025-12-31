using Markdig.Renderers;
using Markdig.Syntax;

namespace MarkMyWord.Converters;

/// <summary>
/// Base class for OpenXML object renderers.
/// </summary>
/// <typeparam name="TObject">The type of markdown object to render.</typeparam>
public abstract class OpenXmlObjectRenderer<TObject> : MarkdownObjectRenderer<OpenXmlRenderer, TObject>
    where TObject : MarkdownObject
{
}
