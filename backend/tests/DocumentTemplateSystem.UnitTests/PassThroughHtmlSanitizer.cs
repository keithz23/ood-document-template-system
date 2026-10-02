using DocumentTemplateSystem.Application.Interfaces;

namespace DocumentTemplateSystem.UnitTests;

internal sealed class PassThroughHtmlSanitizer : IHtmlContentSanitizer
{
    public string Sanitize(string html) => html;
}
