using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Repositories;

public sealed class TemplateRepository(AppDbContext context) : ITemplateRepository
{
    public async Task<IReadOnlyList<TemplateCatalogEntry>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var templates = await context.Templates
            .AsNoTracking()
            .AsSplitQuery()
            .Where(template => template.Status == TemplateStatus.Active)
            .Include(template => template.Category)
            .Include(template => template.Versions)
                .ThenInclude(version => version.Placeholders)
            .ToListAsync(cancellationToken);

        return templates
            .Select(template => new TemplateCatalogEntry(
                template,
                template.Category ?? throw new InvalidOperationException(
                    $"Template '{template.Id}' does not have a category."),
                template.Versions.ToArray()))
            .ToArray();
    }

    public async Task<TemplateCatalogEntry?> GetByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await context.Templates
            .AsNoTracking()
            .AsSplitQuery()
            .Include(candidate => candidate.Category)
            .Include(candidate => candidate.Versions)
                .ThenInclude(version => version.Placeholders)
            .SingleOrDefaultAsync(candidate => candidate.Id == templateId, cancellationToken);

        return template is null
            ? null
            : new TemplateCatalogEntry(
                template,
                template.Category ?? throw new InvalidOperationException(
                    $"Template '{template.Id}' does not have a category."),
                template.Versions.ToArray());
    }

    public async Task<TemplateVersionEntry?> GetVersionByIdAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken = default)
    {
        var version = await context.TemplateVersions
            .AsNoTracking()
            .Include(candidate => candidate.Template)
            .Include(candidate => candidate.Placeholders)
            .SingleOrDefaultAsync(candidate => candidate.Id == templateVersionId, cancellationToken);

        return version is null
            ? null
            : new TemplateVersionEntry(
                version,
                version.Template ?? throw new InvalidOperationException(
                    $"Template version '{version.Id}' does not have a template."),
                version.Placeholders.ToArray());
    }
}
