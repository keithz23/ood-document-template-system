using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record TemplateCatalogEntry(
    Template Template,
    Category Category,
    IReadOnlyList<TemplateVersion> Versions);

public sealed record TemplateVersionEntry(
    TemplateVersion Version,
    Template Template,
    IReadOnlyList<Placeholder> Placeholders);

public interface ITemplateRepository
{
    Task<IReadOnlyList<TemplateCatalogEntry>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task<TemplateCatalogEntry?> GetByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<TemplateVersionEntry?> GetVersionByIdAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken = default);
}
