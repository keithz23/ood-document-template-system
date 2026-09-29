using System.Net;

namespace DocumentTemplateSystem.Domain.Patterns.Composite;

public sealed class ImageComponent(string source, string altText) : DocumentComponent
{
    public string Source { get; private set; } =
        string.IsNullOrWhiteSpace(source)
            ? throw new ArgumentException("Image source is required.", nameof(source))
            : source;

    public string AltText { get; private set; } =
        altText ?? throw new ArgumentNullException(nameof(altText));

    public void Update(string source, string altText)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Image source is required.", nameof(source));
        }

        Source = source;
        AltText = altText ?? throw new ArgumentNullException(nameof(altText));
    }

    public override string Render()
    {
        return $"<img src=\"{WebUtility.HtmlEncode(Source)}\" alt=\"{WebUtility.HtmlEncode(AltText)}\" />";
    }

    public override DocumentComponent Clone() => new ImageComponent(Source, AltText);
}
