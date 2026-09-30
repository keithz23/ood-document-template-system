using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);
}
