using System.Text.RegularExpressions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Patterns.Composite;

namespace DocumentTemplateSystem.Application.Services;

public sealed class HtmlDocumentRenderer : IDocumentRenderer
{
    private static readonly Regex PlaceholderToken = new(
        @"\{\{\s*([A-Za-z0-9_.-]+)\s*\}\}",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string Render(
        string content,
        IReadOnlyDictionary<string, string> placeholderValues)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(placeholderValues);

        var root = new SectionComponent();
        var cursor = 0;

        foreach (Match match in PlaceholderToken.Matches(content))
        {
            if (match.Index > cursor)
            {
                root.Add(new HtmlFragmentComponent(
                    content[cursor..match.Index]));
            }

            var key = match.Groups[1].Value;
            root.Add(placeholderValues.TryGetValue(key, out var value)
                ? new TextComponent(value)
                : new HtmlFragmentComponent(match.Value));
            cursor = match.Index + match.Length;
        }

        if (cursor < content.Length)
        {
            root.Add(new HtmlFragmentComponent(content[cursor..]));
        }

        return root.RenderChildren();
    }

    private sealed class HtmlFragmentComponent(string html) : DocumentComponent
    {
        private string Html { get; } =
            html ?? throw new ArgumentNullException(nameof(html));

        public override string Render() => Html;

        public override DocumentComponent Clone() => new HtmlFragmentComponent(Html);
    }
}
