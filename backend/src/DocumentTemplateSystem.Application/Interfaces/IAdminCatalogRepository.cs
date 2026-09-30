using DocumentTemplateSystem.Domain.Entities;

namespace DocumentTemplateSystem.Application.Interfaces;

public sealed record AdminTemplateEntry(
    Template Template,
    Category Category,
    IReadOnlyList<TemplateVersion> Versions);

public interface IAdminCatalogRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<Category?> GetCategoryByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminTemplateEntry>> GetTemplatesAsync(
        CancellationToken cancellationToken = default);

    Task<AdminTemplateEntry?> GetTemplateByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    Task<AdminTemplateEntry?> GetTemplateByVersionIdAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken = default);

    Task AddCategoryAsync(
        Category category,
        CancellationToken cancellationToken = default);

    Task AddTemplateAsync(
        Template template,
        CancellationToken cancellationToken = default);

    Task AddTemplateVersionAsync(
        TemplateVersion templateVersion,
        CancellationToken cancellationToken = default);

    void AddAuditLog(AuditLog auditLog);

    void RemovePlaceholder(Placeholder placeholder);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
