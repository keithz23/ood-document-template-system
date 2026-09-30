namespace DocumentTemplateSystem.Application.Interfaces;

public interface IDocumentRenderer
{
    string Render(
        string content,
        IReadOnlyDictionary<string, string> placeholderValues);
}
