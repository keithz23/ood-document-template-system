using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record DocumentEntry(
    Document Document,
    Template Template,
    TemplateVersion Version,
    IReadOnlyList<Placeholder> Placeholders);

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<DocumentEntry?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentEntry>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    void RemovePlaceholderValues(
        IReadOnlyCollection<DocumentPlaceholderValue> placeholderValues);
}
