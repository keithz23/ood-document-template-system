using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

public sealed class AdminCatalogRepository(AppDbContext context) : IAdminCatalogRepository
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await context.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToArrayAsync(cancellationToken);

    public Task<Category?> GetCategoryByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        context.Categories.SingleOrDefaultAsync(
            category => category.Id == categoryId,
            cancellationToken);

    public async Task<IReadOnlyList<AdminTemplateEntry>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var templates = await TemplateQuery()
            .AsNoTracking()
            .OrderByDescending(template => template.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return templates.Select(MapTemplate).ToArray();
    }

    public async Task<AdminTemplateEntry?> GetTemplateByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await TemplateQuery().SingleOrDefaultAsync(
            candidate => candidate.Id == templateId,
            cancellationToken);
        return template is null ? null : MapTemplate(template);
    }

    public Task AddCategoryAsync(
        Category category,
        CancellationToken cancellationToken = default) =>
        context.Categories.AddAsync(category, cancellationToken).AsTask();

    public Task AddTemplateAsync(
        Template template,
        CancellationToken cancellationToken = default) =>
        context.Templates.AddAsync(template, cancellationToken).AsTask();

    public void AddAuditLog(AuditLog auditLog) => context.AuditLogs.Add(auditLog);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    private IQueryable<Template> TemplateQuery() => context.Templates
        .AsSplitQuery()
        .Include(template => template.Category)
        .Include(template => template.Versions)
            .ThenInclude(version => version.Placeholders);

    private static AdminTemplateEntry MapTemplate(Template template) =>
        new(
            template,
            template.Category ?? throw new InvalidOperationException(
                $"Template '{template.Id}' does not have a category."),
            template.Versions.ToArray());
}
