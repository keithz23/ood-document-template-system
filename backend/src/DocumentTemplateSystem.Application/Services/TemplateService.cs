using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Strategy;

namespace DocumentTemplateSystem.Application.Services;

public sealed class TemplateService(
    ITemplateRepository templateRepository,
    PlaceholderValidator placeholderValidator)
{
    public async Task<IReadOnlyList<TemplateGalleryItemDto>> GetActiveTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await templateRepository.GetActiveAsync(cancellationToken);

        return entries
            .Select(entry => new TemplateGalleryItemDto(
                entry.Template.Id,
                entry.Template.Name,
                entry.Template.Status,
                MapCategory(entry.Category),
                MapCurrentVersion(GetCatalogCurrentVersion(entry))))
            .ToArray();
    }

    public async Task<TemplateDetailDto> GetTemplateDetailAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateId, "templateId");
        var entry = await templateRepository.GetByIdAsync(templateId, cancellationToken);

        if (entry is null || entry.Template.Status != TemplateStatus.Active)
        {
            throw UseCaseException.NotFound(
                "TEMPLATE_NOT_FOUND",
                "The requested active template was not found.");
        }

        var currentVersion = GetCurrentPublishedVersion(entry);

        return new TemplateDetailDto(
            entry.Template.Id,
            entry.Template.Name,
            entry.Template.Status,
            MapCategory(entry.Category),
            entry.Template.CreatedAt,
            MapCurrentVersion(currentVersion));
    }

    public async Task<TemplateVersionDetailDto> GetCurrentVersionAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateId, "templateId");
        var entry = await templateRepository.GetByIdAsync(templateId, cancellationToken);

        if (entry is null || entry.Template.Status != TemplateStatus.Active)
        {
            throw UseCaseException.NotFound(
                "TEMPLATE_NOT_FOUND",
                "The requested active template was not found.");
        }

        var version = GetCurrentPublishedVersion(entry);

        if (version.ContentFormat != ContentFormat.Html)
        {
            throw UseCaseException.Unprocessable(
                "UNSUPPORTED_CONTENT_FORMAT",
                "The current template version does not use a supported content format.");
        }

        return new TemplateVersionDetailDto(
            version.Id,
            version.TemplateId,
            version.VersionNumber,
            version.Content,
            version.ContentFormat,
            version.Status,
            version.IsCurrent,
            version.PublishedAt,
            version.CreatedAt,
            version.UpdatedAt);
    }

    public async Task<IReadOnlyList<PlaceholderDto>> GetCurrentVersionPlaceholdersAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateVersionId, "templateVersionId");
        var entry = await templateRepository.GetVersionByIdAsync(
            templateVersionId,
            cancellationToken);

        if (entry is null)
        {
            throw UseCaseException.NotFound(
                "TEMPLATE_VERSION_NOT_FOUND",
                "The requested template version was not found.");
        }

        if (!entry.Version.IsCurrent || entry.Version.Status != VersionStatus.Published)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_VERSION_NOT_CURRENT",
                "The requested template version is not the current published version.");
        }

        if (entry.Template.Status != TemplateStatus.Active)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_INACTIVE",
                "The template is not active.");
        }

        foreach (var placeholder in entry.Placeholders)
        {
            EnsureValidDefaultValue(placeholder);
        }

        return entry.Placeholders
            .Select(placeholder => new PlaceholderDto(
                placeholder.Id,
                placeholder.TemplateVersionId,
                placeholder.Key,
                placeholder.Label,
                placeholder.DataType,
                placeholder.IsRequired,
                placeholder.DefaultValue))
            .ToArray();
    }

    private static TemplateVersion GetCatalogCurrentVersion(TemplateCatalogEntry entry)
    {
        var currentVersions = entry.Versions.Where(version => version.IsCurrent).ToArray();

        if (currentVersions.Length != 1
            || currentVersions[0].Status != VersionStatus.Published)
        {
            throw new InvalidOperationException(
                $"Active template '{entry.Template.Id}' does not have exactly one current published version.");
        }

        return currentVersions[0];
    }

    private static TemplateVersion GetCurrentPublishedVersion(TemplateCatalogEntry entry)
    {
        var version = entry.Versions.SingleOrDefault(candidate =>
            candidate.IsCurrent && candidate.Status == VersionStatus.Published);

        return version ?? throw UseCaseException.NotFound(
            "CURRENT_VERSION_NOT_FOUND",
            "A current published template version was not found.");
    }

    private void EnsureValidDefaultValue(Placeholder placeholder)
    {
        if (!string.IsNullOrWhiteSpace(placeholder.DefaultValue)
            && !placeholderValidator.Validate(
                placeholder.DataType,
                placeholder.DefaultValue,
                false))
        {
            throw new InvalidOperationException(
                $"Placeholder '{placeholder.Id}' has an invalid default value.");
        }
    }

    private static CategoryReferenceDto MapCategory(Category category) =>
        new(category.Id, category.Name);

    private static CurrentTemplateVersionSummaryDto MapCurrentVersion(
        TemplateVersion version) =>
        new(
            version.Id,
            version.VersionNumber,
            version.Status,
            version.IsCurrent,
            version.ContentFormat,
            version.UpdatedAt,
            version.Placeholders.Count);

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
}
