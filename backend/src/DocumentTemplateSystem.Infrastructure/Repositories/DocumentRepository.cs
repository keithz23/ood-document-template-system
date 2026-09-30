using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;

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
}
