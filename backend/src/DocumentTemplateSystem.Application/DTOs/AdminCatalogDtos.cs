using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.DTOs;

public sealed record AdminCategoryDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record CreateCategoryRequestDto(string Name);

public sealed record UpdateCategoryRequestDto(string Name);

public sealed record AdminTemplateVersionSummaryDto(
    Guid Id,
    int VersionNumber,
    VersionStatus Status,
    bool IsCurrent,
    ContentFormat ContentFormat,
    int PlaceholderCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AdminTemplateSummaryDto(
    Guid Id,
    string Name,
    TemplateStatus Status,
    CategoryReferenceDto Category,
    DateTimeOffset CreatedAt,
    int VersionCount,
    int? CurrentVersionNumber);

public sealed record AdminTemplateDetailDto(
    Guid Id,
    string Name,
    TemplateStatus Status,
    CategoryReferenceDto Category,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AdminTemplateVersionSummaryDto> Versions);

public sealed record CreateTemplateRequestDto(string Name, Guid CategoryId);

public sealed record UpdateDraftTemplateRequestDto(string? Name, Guid? CategoryId);

public sealed record AdminTemplateVersionDetailDto(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    string Content,
    ContentFormat ContentFormat,
    VersionStatus Status,
    bool IsCurrent,
    int PlaceholderCount,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateDraftTemplateVersionRequestDto(string Content);

public sealed record CreatePlaceholderRequestDto(
    string Key,
    string Label,
    PlaceholderDataType DataType,
    bool IsRequired,
    string? DefaultValue);

public sealed record UpdatePlaceholderRequestDto(
    string Key,
    string Label,
    PlaceholderDataType DataType,
    bool IsRequired,
    string? DefaultValue);
