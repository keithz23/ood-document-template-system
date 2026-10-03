using DocumentTemplateSystem.Application.Interfaces;
using AngleSharp.Dom;
using Ganss.Xss;

namespace DocumentTemplateSystem.Infrastructure.Rendering;

public sealed class AllowlistHtmlContentSanitizer : IHtmlContentSanitizer
{
    private static readonly string[] Tags =
    [
        "a", "blockquote", "br", "code", "col", "colgroup", "em", "h1", "h2",
        "h3", "hr", "img", "li", "ol", "p", "pre", "s", "strong", "table",
        "tbody", "td", "th", "thead", "tr", "u", "ul"
    ];

    private static readonly string[] Attributes =
    [
        "alt", "colspan", "href", "rel", "rowspan", "src", "style", "target",
        "title"
    ];

    private readonly HtmlSanitizer _sanitizer = CreateSanitizer();

    public string Sanitize(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        return _sanitizer.Sanitize(html);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.UnionWith(Tags);
        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.UnionWith(Attributes);
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedCssProperties.UnionWith(["text-align", "width", "min-width"]);
        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.UnionWith(["http", "https", "mailto"]);
        sanitizer.PostProcessNode += (_, args) =>
        {
            if (args.Node is not IElement element
                || !string.Equals(element.LocalName, "img", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var source = element.GetAttribute("src");
            if (!Uri.TryCreate(source, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                element.RemoveAttribute("src");
            }
        };
        return sanitizer;
    }
}
