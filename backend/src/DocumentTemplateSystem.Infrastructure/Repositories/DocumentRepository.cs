using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

public sealed class DocumentRepository(AppDbContext context) : IDocumentRepository
{
    public async Task AddAsync(
        Document document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        context.Documents.Add(document);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentEntry?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await DocumentQuery()
            .SingleOrDefaultAsync(candidate => candidate.Id == documentId, cancellationToken);

        return document is null ? null : CreateEntry(document);
    }

    public async Task<IReadOnlyList<DocumentEntry>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var documents = await DocumentQuery()
            .Where(document => document.CreatedBy == ownerId)
            .OrderByDescending(document => document.UpdatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(CreateEntry).ToArray();
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public void RemovePlaceholderValues(
        IReadOnlyCollection<DocumentPlaceholderValue> placeholderValues)
    {
        context.DocumentPlaceholderValues.RemoveRange(placeholderValues);
    }

    private IQueryable<Document> DocumentQuery() =>
        context.Documents
            .Include(document => document.PlaceholderValues)
            .Include(document => document.TemplateVersion)
                .ThenInclude(version => version!.Template)
            .Include(document => document.TemplateVersion)
                .ThenInclude(version => version!.Placeholders)
            .AsSplitQuery();

    private static DocumentEntry CreateEntry(Document document)
    {
        var version = document.TemplateVersion
            ?? throw new InvalidOperationException("Document source version was not loaded.");
        var template = version.Template
            ?? throw new InvalidOperationException("Document source template was not loaded.");
        return new DocumentEntry(
            document,
            template,
            version,
            version.Placeholders.ToArray());
    }
}
