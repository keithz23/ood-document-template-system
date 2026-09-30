using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.Services;

public sealed class AdminCatalogService(
    IAdminCatalogRepository repository,
    ICurrentUserContext currentUser)
{
    public async Task<IReadOnlyList<AdminCategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetCategoriesAsync(cancellationToken);
        return categories.Select(MapCategory).ToArray();
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(
        CreateCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureName(request.Name, "name");

        var category = new Category(request.Name, currentUser.UserId);
        await repository.AddCategoryAsync(category, cancellationToken);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "CreateCategory",
            nameof(Category),
            category.Id,
            $"Created category '{category.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);

        return MapCategory(category);
    }

    public async Task<AdminCategoryDto> UpdateCategoryAsync(
        Guid categoryId,
        UpdateCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(categoryId, "categoryId");
        EnsureName(request.Name, "name");

        var category = await GetCategoryAsync(categoryId, cancellationToken);
        category.Rename(request.Name);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "UpdateCategory",
            nameof(Category),
            category.Id,
            $"Updated category metadata for '{category.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);

        return MapCategory(category);
    }

    public Task<AdminCategoryDto> ActivateCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        SetCategoryStateAsync(categoryId, true, cancellationToken);

    public Task<AdminCategoryDto> DeactivateCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        SetCategoryStateAsync(categoryId, false, cancellationToken);

    public async Task<IReadOnlyList<AdminTemplateSummaryDto>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await repository.GetTemplatesAsync(cancellationToken);
        return entries.Select(MapTemplateSummary).ToArray();
    }

    public async Task<AdminTemplateDetailDto> GetTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateId, "templateId");
        return MapTemplateDetail(await GetTemplateEntryAsync(templateId, cancellationToken));
    }

    public async Task<AdminTemplateDetailDto> CreateTemplateAsync(
        CreateTemplateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureName(request.Name, "name");
        EnsureIdentifier(request.CategoryId, "categoryId");

        var category = await GetCategoryAsync(request.CategoryId, cancellationToken);
        var template = new Template(request.Name, category.Id, currentUser.UserId);
        await repository.AddTemplateAsync(template, cancellationToken);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "CreateTemplate",
            nameof(Template),
            template.Id,
            $"Created Draft template '{template.Name}' with its initial version."));
        await repository.SaveChangesAsync(cancellationToken);

        return MapTemplateDetail(new AdminTemplateEntry(
            template,
            category,
            template.Versions.ToArray()));
    }

    public async Task<AdminTemplateDetailDto> UpdateTemplateAsync(
        Guid templateId,
        UpdateDraftTemplateRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateId, "templateId");
        if (request.Name is null && request.CategoryId is null)
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                "request",
                "REQUIRED",
                "At least one template metadata field is required."));
        }

        var entry = await GetTemplateEntryAsync(templateId, cancellationToken);
        if (entry.Template.Status != TemplateStatus.Draft)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_NOT_DRAFT",
                "Only a Draft template may be updated.");
        }

        var name = request.Name ?? entry.Template.Name;
        EnsureName(name, "name");
        var categoryId = request.CategoryId ?? entry.Template.CategoryId;
        EnsureIdentifier(categoryId, "categoryId");
        var category = categoryId == entry.Category.Id
            ? entry.Category
            : await GetCategoryAsync(categoryId, cancellationToken);

        entry.Template.UpdateDraftMetadata(name, category.Id);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "UpdateTemplate",
            nameof(Template),
            entry.Template.Id,
            $"Updated Draft template metadata for '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);

        return MapTemplateDetail(new AdminTemplateEntry(
            entry.Template,
            category,
            entry.Versions));
    }

    public Task<AdminTemplateDetailDto> ActivateTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default) =>
        SetTemplateStateAsync(templateId, true, cancellationToken);

    public Task<AdminTemplateDetailDto> DeactivateTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default) =>
        SetTemplateStateAsync(templateId, false, cancellationToken);

    private async Task<AdminCategoryDto> SetCategoryStateAsync(
        Guid categoryId,
        bool activate,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(categoryId, "categoryId");
        var category = await GetCategoryAsync(categoryId, cancellationToken);

        if (activate)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            activate ? "ActivateCategory" : "DeactivateCategory",
            nameof(Category),
            category.Id,
            $"{(activate ? "Activated" : "Deactivated")} category '{category.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    private async Task<AdminTemplateDetailDto> SetTemplateStateAsync(
        Guid templateId,
        bool activate,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(templateId, "templateId");
        var entry = await GetTemplateEntryAsync(templateId, cancellationToken);

        if (activate)
        {
            var canActivate = entry.Versions.Any(version =>
                version.IsCurrent && version.Status == VersionStatus.Published);
            if (!canActivate)
            {
                throw UseCaseException.Conflict(
                    "TEMPLATE_ACTIVATION_REQUIRES_CURRENT_VERSION",
                    "A template requires a current Published version before activation.");
            }

            entry.Template.Activate();
        }
        else
        {
            entry.Template.Deactivate();
        }

        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            activate ? "ActivateTemplate" : "DeactivateTemplate",
            nameof(Template),
            entry.Template.Id,
            $"{(activate ? "Activated" : "Deactivated")} template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapTemplateDetail(entry);
    }

    private async Task<Category> GetCategoryAsync(
        Guid categoryId,
        CancellationToken cancellationToken) =>
        await repository.GetCategoryByIdAsync(categoryId, cancellationToken)
        ?? throw UseCaseException.NotFound(
            "CATEGORY_NOT_FOUND",
            "The requested category was not found.");

    private async Task<AdminTemplateEntry> GetTemplateEntryAsync(
        Guid templateId,
        CancellationToken cancellationToken) =>
        await repository.GetTemplateByIdAsync(templateId, cancellationToken)
        ?? throw UseCaseException.NotFound(
            "TEMPLATE_NOT_FOUND",
            "The requested template was not found.");

    private static AdminCategoryDto MapCategory(Category category) =>
        new(category.Id, category.Name, category.IsActive, category.CreatedAt);

    private static AdminTemplateSummaryDto MapTemplateSummary(AdminTemplateEntry entry) =>
        new(
            entry.Template.Id,
            entry.Template.Name,
            entry.Template.Status,
            new CategoryReferenceDto(entry.Category.Id, entry.Category.Name),
            entry.Template.CreatedAt,
            entry.Versions.Count,
            entry.Versions.SingleOrDefault(version => version.IsCurrent)?.VersionNumber);

    private static AdminTemplateDetailDto MapTemplateDetail(AdminTemplateEntry entry) =>
        new(
            entry.Template.Id,
            entry.Template.Name,
            entry.Template.Status,
            new CategoryReferenceDto(entry.Category.Id, entry.Category.Name),
            entry.Template.CreatedAt,
            entry.Versions
                .OrderByDescending(version => version.VersionNumber)
                .Select(version => new AdminTemplateVersionSummaryDto(
                    version.Id,
                    version.VersionNumber,
                    version.Status,
                    version.IsCurrent,
                    version.ContentFormat,
                    version.Placeholders.Count,
                    version.CreatedAt,
                    version.UpdatedAt))
                .ToArray());

    private static void EnsureIdentifier(Guid id, string field)
    {
        if (id == Guid.Empty)
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                field,
                "REQUIRED",
                $"{field} is required."));
        }
    }

    private static void EnsureName(string? name, string field)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                field,
                "REQUIRED",
                $"{field} is required."));
        }
    }
}
