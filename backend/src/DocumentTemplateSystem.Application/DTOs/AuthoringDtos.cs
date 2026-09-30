using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.DTOs;

public sealed record CategoryReferenceDto(Guid Id, string Name);

public sealed record CurrentTemplateVersionSummaryDto(
    Guid Id,
    int VersionNumber,
    VersionStatus Status,
    bool IsCurrent,
    ContentFormat ContentFormat,
    DateTimeOffset UpdatedAt,
    int PlaceholderCount);

public sealed record TemplateGalleryItemDto(
    Guid Id,
    string Name,
    TemplateStatus Status,
    CategoryReferenceDto Category,
    CurrentTemplateVersionSummaryDto CurrentVersion);

public sealed record TemplateDetailDto(
    Guid Id,
    string Name,
    TemplateStatus Status,
    CategoryReferenceDto Category,
    DateTimeOffset CreatedAt,
    CurrentTemplateVersionSummaryDto CurrentVersion);

public sealed record TemplateVersionDetailDto(
    Guid Id,
    Guid TemplateId,
    int VersionNumber,
    string Content,
    ContentFormat ContentFormat,
    VersionStatus Status,
    bool IsCurrent,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PlaceholderDto(
    Guid Id,
    Guid TemplateVersionId,
    string Key,
    string Label,
    PlaceholderDataType DataType,
    bool IsRequired,
    string? DefaultValue);

public sealed record CreateDraftDocumentRequestDto(
    Guid TemplateVersionId,
    string Title);

public sealed record PlaceholderValueInputDto(
    Guid PlaceholderId,
    string Value);

public sealed record UpdateDraftDocumentRequestDto(
    string? Title,
    string? Content,
    IReadOnlyList<PlaceholderValueInputDto>? PlaceholderValues);

public sealed record PreviewDocumentRequestDto(
    string Content,
    IReadOnlyList<PlaceholderValueInputDto> PlaceholderValues);

public sealed record FinalizeDocumentRequestDto(
    string Title,
    string Content,
    IReadOnlyList<PlaceholderValueInputDto> PlaceholderValues);

public sealed record DocumentSourceDto(
    Guid TemplateId,
    string TemplateName,
    Guid TemplateVersionId,
    int VersionNumber);

public sealed record DocumentPlaceholderValueDto(
    Guid Id,
    Guid? PlaceholderId,
    string PlaceholderKeySnapshot,
    string LabelSnapshot,
    PlaceholderDataType DataTypeSnapshot,
    string Value);

public sealed record DocumentDetailDto(
    Guid Id,
    string Title,
    DocumentStatus Status,
    DocumentSourceDto Source,
    string Content,
    ContentFormat ContentFormat,
    IReadOnlyList<DocumentPlaceholderValueDto> PlaceholderValues,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FinalizedAt);

public sealed record DocumentSummaryDto(
    Guid Id,
    string Title,
    DocumentStatus Status,
    DocumentSourceDto Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FinalizedAt);

public sealed record PreviewDocumentResponseDto(
    Guid DocumentId,
    string RenderedContent,
    ContentFormat ContentFormat);

public sealed record DocumentDownloadDto(
    string FileName,
    string ContentType,
    string Content);

public sealed record ValidationErrorDto(
    string Field,
    string Code,
    string Message,
    string? PlaceholderKey = null);

public sealed record ErrorResponseDto(
    int Status,
    string Code,
    string Title,
    string? Detail,
    string TraceId,
    IReadOnlyList<ValidationErrorDto>? Errors = null);
