using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using MarkMyWord.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using WP = DocumentFormat.OpenXml.Wordprocessing;

namespace MarkMyWord.Converters.InlineRenderers;

/// <summary>
/// Renderer for link inline elements.
/// </summary>
public class LinkInlineRenderer : OpenXmlObjectRenderer<LinkInline>
{
    private static readonly HttpClient _httpClient = new();

    protected override void Write(OpenXmlRenderer renderer, LinkInline obj)
    {
        var currentParagraph = renderer.DocumentBuilder.Body.Elements<WP.Paragraph>().LastOrDefault();
        if (currentParagraph == null)
        {
            currentParagraph = renderer.DocumentBuilder.AddParagraph();
        }

        // Handle images
        if (obj.IsImage)
        {
            TryInsertImage(renderer, currentParagraph, obj);
            return;
        }

        if (string.IsNullOrEmpty(obj.Url))
        {
            // No URL, just render the text
            if (obj.FirstChild != null)
            {
                renderer.WriteChildren(obj);
            }
            return;
        }

        // Add hyperlink relationship
        var relationshipId = renderer.DocumentBuilder.AddHyperlinkRelationship(obj.Url);

        // Create hyperlink element
        var hyperlink = new WP.Hyperlink(
            new WP.RunProperties(
                new WP.Color { Val = "0563C1" },
                new WP.Underline { Val = WP.UnderlineValues.Single }
            )
        )
        {
            Id = relationshipId,
            History = OnOffValue.FromBoolean(true)
        };

        // Render link text
        if (obj.FirstChild != null)
        {
            var child = obj.FirstChild;
            while (child != null)
            {
                if (child is LiteralInline literal)
                {
                    var run = new WP.Run(
                        new WP.RunProperties(
                            new WP.Color { Val = "0563C1" },
                            new WP.Underline { Val = WP.UnderlineValues.Single }
                        ),
                        new WP.Text(TextSanitizer.Sanitize(literal.Content.ToString())) { Space = SpaceProcessingModeValues.Preserve }
                    );
                    hyperlink.AppendChild(run);
                }
                child = child.NextSibling;
            }
        }
        else
        {
            // Fallback to URL as text
            var run = new WP.Run(
                new WP.RunProperties(
                    new WP.Color { Val = "0563C1" },
                    new WP.Underline { Val = WP.UnderlineValues.Single }
                ),
                new WP.Text(TextSanitizer.Sanitize(obj.Url)) { Space = SpaceProcessingModeValues.Preserve }
            );
            hyperlink.AppendChild(run);
        }

        currentParagraph.AppendChild(hyperlink);
    }

    private void TryInsertImage(OpenXmlRenderer renderer, WP.Paragraph paragraph, LinkInline link)
    {
        try
        {
            if (string.IsNullOrEmpty(link.Url))
            {
                InsertImageFallback(paragraph, link);
                return;
            }

            byte[]? imageData = null;
            string? contentType = null;

            // Try to load the image
            if (Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    // Download from URL
                    var response = _httpClient.GetAsync(uri).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        imageData = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
                    }
                }
                else if (uri.Scheme == "file" || !uri.IsAbsoluteUri)
                {
                    // Local file
                    var filePath = uri.IsAbsoluteUri ? uri.LocalPath : link.Url;
                    if (File.Exists(filePath))
                    {
                        imageData = File.ReadAllBytes(filePath);
                        contentType = GetContentTypeFromExtension(System.IO.Path.GetExtension(filePath));
                    }
                }
            }
            else
            {
                // Relative path
                if (File.Exists(link.Url))
                {
                    imageData = File.ReadAllBytes(link.Url);
                    contentType = GetContentTypeFromExtension(System.IO.Path.GetExtension(link.Url));
                }
            }

            if (imageData == null || imageData.Length == 0)
            {
                InsertImageFallback(paragraph, link);
                return;
            }

            // Add image to document
            var imagePart = renderer.DocumentBuilder.AddImagePart(imageData, contentType ?? "image/png");
            var relationshipId = renderer.DocumentBuilder.GetImageRelationshipId(imagePart);

            // Calculate dimensions (default: max width of 6 inches)
            long widthEmus = 6 * 914400; // 6 inches in EMUs (English Metric Units)
            long heightEmus = 4 * 914400; // Default 4 inches, will be adjusted based on aspect ratio

            // Try to get actual dimensions (simplified - just use defaults for now)
            // In production, you'd use an image library to get actual dimensions

            // Create the image element
            var element = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmus, Cy = heightEmus },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = (uint)1, Name = link.Title ?? "Image" },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = link.Title ?? "Image" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U
                });

            // Add the image to the current paragraph
            var run = new WP.Run(element);
            paragraph.AppendChild(run);
        }
        catch
        {
            // Fallback to alt text if anything goes wrong
            InsertImageFallback(paragraph, link);
        }
    }

    private void InsertImageFallback(WP.Paragraph paragraph, LinkInline link)
    {
        var altText = link.Title ?? link.Url ?? "image";
        var run = new WP.Run(
            new WP.RunProperties(new WP.Italic()),
            new WP.Text($"[Image: {TextSanitizer.Sanitize(altText)}]") { Space = SpaceProcessingModeValues.Preserve }
        );
        paragraph.AppendChild(run);
    }

    private string GetContentTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            _ => "image/png"
        };
    }
}
