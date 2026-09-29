using System.Net;

namespace DocumentTemplateSystem.Domain.Patterns.Composite;

public sealed class TextComponent(string text) : DocumentComponent
{
    public string Text { get; private set; } =
        text ?? throw new ArgumentNullException(nameof(text));

    public void UpdateText(string text)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public override string Render() => WebUtility.HtmlEncode(Text);

    public override DocumentComponent Clone() => new TextComponent(Text);
}
