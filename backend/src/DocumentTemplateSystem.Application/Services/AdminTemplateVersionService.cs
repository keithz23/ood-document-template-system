using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Strategy;

namespace DocumentTemplateSystem.Application.Services;

public sealed class AdminTemplateVersionService(
    IAdminCatalogRepository repository,
    ICurrentUserContext currentUser,
    PlaceholderValidator placeholderValidator)
{
    public async Task<IReadOnlyList<AdminTemplateVersionSummaryDto>> GetVersionsAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateId, "templateId");
        var entry = await GetTemplateEntryAsync(templateId, cancellationToken);
        return entry.Versions
            .OrderByDescending(version => version.VersionNumber)
            .Select(MapSummary)
            .ToArray();
    }

    public async Task<AdminTemplateVersionDetailDto> CreateDraftVersionAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(templateId, "templateId");
        var entry = await GetTemplateEntryAsync(templateId, cancellationToken);
        var source = entry.Versions.SingleOrDefault(version => version.IsCurrent)
            ?? entry.Versions.MaxBy(version => version.VersionNumber)
            ?? throw new InvalidOperationException(
                "The template does not have a version to copy.");

        var draft = entry.Template.AddVersion(
            source.Content,
            source.ContentFormat,
            currentUser.UserId);
        foreach (var placeholder in source.Placeholders.OrderBy(item => item.Key))
        {
            draft.AddPlaceholder(
                placeholder.Key,
                placeholder.Label,
                placeholder.DataType,
                placeholder.IsRequired,
                placeholder.DefaultValue);
        }

        await repository.AddTemplateVersionAsync(draft, cancellationToken);

        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "CreateTemplateVersion",
            nameof(TemplateVersion),
            draft.Id,
            $"Created Draft version {draft.VersionNumber} for template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);

        return MapDetail(draft);
    }

    public async Task<AdminTemplateVersionDetailDto> GetVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var (_, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        return MapDetail(version);
    }

    public async Task<AdminTemplateVersionDetailDto> UpdateDraftVersionAsync(
        Guid versionId,
        UpdateDraftTemplateVersionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Content is null)
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                "content",
                "REQUIRED",
                "content is required."));
        }

        var (entry, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        EnsureDraft(version);
        version.UpdateContent(request.Content, version.ContentFormat);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "UpdateTemplateVersion",
            nameof(TemplateVersion),
            version.Id,
            $"Updated Draft version {version.VersionNumber} for template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapDetail(version);
    }

    public async Task<AdminTemplateVersionDetailDto> PublishAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var (entry, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        EnsureDraft(version);

        var errors = version.Placeholders
            .Where(placeholder => placeholder.DefaultValue is not null)
            .Where(placeholder => !placeholderValidator.Validate(
                placeholder.DataType,
                placeholder.DefaultValue,
                true))
            .Select(placeholder => new ValidationErrorDto(
                $"placeholders.{placeholder.Key}.defaultValue",
                "INVALID_DEFAULT_VALUE",
                $"The default value for '{placeholder.Label}' is not a valid {placeholder.DataType}."))
            .ToArray();
        if (errors.Length > 0)
        {
            throw UseCaseException.Unprocessable(
                "INVALID_PLACEHOLDER_DEFAULTS",
                "One or more placeholder default values are invalid.",
                errors);
        }

        version.Publish(currentUser.UserId);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "PublishTemplateVersion",
            nameof(TemplateVersion),
            version.Id,
            $"Published version {version.VersionNumber} for template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapDetail(version);
    }

    public async Task<AdminTemplateVersionDetailDto> SetCurrentAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var (entry, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        if (version.Status != VersionStatus.Published)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_VERSION_NOT_PUBLISHED",
                "Only a Published template version may be Current.");
        }

        if (version.IsCurrent)
        {
            return MapDetail(version);
        }

        await repository.ExecuteInTransactionAsync(async transactionToken =>
        {
            entry.Template.ClearCurrentVersion();
            await repository.SaveChangesAsync(transactionToken);

            entry.Template.SetCurrentVersion(version.Id);
            repository.AddAuditLog(new AuditLog(
                currentUser.UserId,
                "SetCurrentTemplateVersion",
                nameof(TemplateVersion),
                version.Id,
                $"Set version {version.VersionNumber} as Current for template '{entry.Template.Name}'."));
            await repository.SaveChangesAsync(transactionToken);
        }, cancellationToken);

        return MapDetail(version);
    }

    public async Task<IReadOnlyList<PlaceholderDto>> GetPlaceholdersAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var (_, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        return version.Placeholders
            .OrderBy(placeholder => placeholder.Key)
            .Select(MapPlaceholder)
            .ToArray();
    }

    public async Task<PlaceholderDto> CreatePlaceholderAsync(
        Guid versionId,
        CreatePlaceholderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var (entry, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        EnsureDraft(version);
        ValidatePlaceholderRequest(
            version,
            null,
            request.Key,
            request.Label,
            request.DataType,
            request.DefaultValue);

        var placeholder = version.AddPlaceholder(
            request.Key,
            request.Label,
            request.DataType,
            request.IsRequired,
            request.DefaultValue);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "CreatePlaceholder",
            nameof(Placeholder),
            placeholder.Id,
            $"Added placeholder '{placeholder.Key}' to version {version.VersionNumber} of template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapPlaceholder(placeholder);
    }

    public async Task<PlaceholderDto> UpdatePlaceholderAsync(
        Guid versionId,
        Guid placeholderId,
        UpdatePlaceholderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(placeholderId, "placeholderId");
        var (entry, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        EnsureDraft(version);
        var existing = GetPlaceholder(version, placeholderId);
        ValidatePlaceholderRequest(
            version,
            existing.Id,
            request.Key,
            request.Label,
            request.DataType,
            request.DefaultValue);

        var placeholder = version.UpdatePlaceholder(
            existing.Id,
            request.Key,
            request.Label,
            request.DataType,
            request.IsRequired,
            request.DefaultValue);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "UpdatePlaceholder",
            nameof(Placeholder),
            placeholder.Id,
            $"Updated placeholder '{placeholder.Key}' on version {version.VersionNumber} of template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
        return MapPlaceholder(placeholder);
    }

    public async Task RemovePlaceholderAsync(
        Guid versionId,
        Guid placeholderId,
        CancellationToken cancellationToken = default)
    {
        EnsureIdentifier(placeholderId, "placeholderId");
        var (entry, version) = await GetVersionEntryAsync(versionId, cancellationToken);
        EnsureDraft(version);
        var existing = GetPlaceholder(version, placeholderId);
        var placeholder = version.RemovePlaceholder(existing.Id);
        repository.RemovePlaceholder(placeholder);
        repository.AddAuditLog(new AuditLog(
            currentUser.UserId,
            "RemovePlaceholder",
            nameof(Placeholder),
            placeholder.Id,
            $"Removed placeholder '{placeholder.Key}' from version {version.VersionNumber} of template '{entry.Template.Name}'."));
        await repository.SaveChangesAsync(cancellationToken);
    }

    private void ValidatePlaceholderRequest(
        TemplateVersion version,
        Guid? placeholderId,
        string? key,
        string? label,
        PlaceholderDataType dataType,
        string? defaultValue)
    {
        var errors = new List<ValidationErrorDto>();
        if (string.IsNullOrWhiteSpace(key))
        {
            errors.Add(new ValidationErrorDto("key", "REQUIRED", "key is required."));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            errors.Add(new ValidationErrorDto("label", "REQUIRED", "label is required."));
        }

        if (!Enum.IsDefined(dataType))
        {
            errors.Add(new ValidationErrorDto(
                "dataType",
                "INVALID_VALUE",
                "dataType must be Text, Number, Date, or Email."));
        }

        if (defaultValue is not null
            && Enum.IsDefined(dataType)
            && !placeholderValidator.Validate(dataType, defaultValue, true))
        {
            errors.Add(new ValidationErrorDto(
                "defaultValue",
                "INVALID_DEFAULT_VALUE",
                $"defaultValue is not a valid {dataType}."));
        }

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation(errors.ToArray());
        }

        if (version.Placeholders.Any(placeholder =>
                placeholder.Id != placeholderId
                && string.Equals(placeholder.Key, key!.Trim(), StringComparison.Ordinal)))
        {
            throw UseCaseException.Conflict(
                "PLACEHOLDER_KEY_CONFLICT",
                "Placeholder Key must be unique within the TemplateVersion.");
        }
    }

    private async Task<AdminTemplateEntry> GetTemplateEntryAsync(
        Guid templateId,
        CancellationToken cancellationToken) =>
        await repository.GetTemplateByIdAsync(templateId, cancellationToken)
        ?? throw UseCaseException.NotFound(
            "TEMPLATE_NOT_FOUND",
            "The requested template was not found.");

    private async Task<(AdminTemplateEntry Entry, TemplateVersion Version)> GetVersionEntryAsync(
        Guid versionId,
        CancellationToken cancellationToken)
    {
        EnsureIdentifier(versionId, "versionId");
        var entry = await repository.GetTemplateByVersionIdAsync(versionId, cancellationToken)
            ?? throw UseCaseException.NotFound(
                "TEMPLATE_VERSION_NOT_FOUND",
                "The requested template version was not found.");
        var version = entry.Versions.Single(candidate => candidate.Id == versionId);
        return (entry, version);
    }

    private static Placeholder GetPlaceholder(TemplateVersion version, Guid placeholderId) =>
        version.Placeholders.SingleOrDefault(placeholder => placeholder.Id == placeholderId)
        ?? throw UseCaseException.NotFound(
            "PLACEHOLDER_NOT_FOUND",
            "The requested placeholder was not found for this template version.");

    private static void EnsureDraft(TemplateVersion version)
    {
        if (version.Status != VersionStatus.Draft)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_VERSION_PUBLISHED",
                "Published template versions are immutable.");
        }
    }

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

    private static AdminTemplateVersionSummaryDto MapSummary(TemplateVersion version) =>
        new(
            version.Id,
            version.VersionNumber,
            version.Status,
            version.IsCurrent,
            version.ContentFormat,
            version.Placeholders.Count,
            version.CreatedAt,
            version.UpdatedAt);

    private static AdminTemplateVersionDetailDto MapDetail(TemplateVersion version) =>
        new(
            version.Id,
            version.TemplateId,
            version.VersionNumber,
            version.Content,
            version.ContentFormat,
            version.Status,
            version.IsCurrent,
            version.Placeholders.Count,
            version.PublishedAt,
            version.CreatedAt,
            version.UpdatedAt);

    private static PlaceholderDto MapPlaceholder(Placeholder placeholder) =>
        new(
            placeholder.Id,
            placeholder.TemplateVersionId,
            placeholder.Key,
            placeholder.Label,
            placeholder.DataType,
            placeholder.IsRequired,
            placeholder.DefaultValue);
}
