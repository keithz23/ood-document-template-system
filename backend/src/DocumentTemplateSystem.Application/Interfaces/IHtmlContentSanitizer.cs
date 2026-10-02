namespace DocumentTemplateSystem.Application.Interfaces;

public interface IHtmlContentSanitizer
{
    string Sanitize(string html);
}
