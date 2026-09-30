using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.Services;

public sealed class DocumentService(
    ITemplateRepository templateRepository,
    IDocumentRepository documentRepository,
    ICurrentUserContext currentUserContext)
{
    public async Task<DocumentDetailDto> CreateDraftAsync(
        CreateDraftDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request);

        var entry = await templateRepository.GetVersionByIdAsync(
            request.TemplateVersionId,
            cancellationToken);

        if (entry is null)
        {
            throw UseCaseException.NotFound(
                "TEMPLATE_VERSION_NOT_FOUND",
                "The requested template version was not found.");
        }

        if (entry.Template.Status != TemplateStatus.Active)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_INACTIVE",
                "The template is not active.");
        }

        if (!entry.Version.IsCurrent || entry.Version.Status != VersionStatus.Published)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_VERSION_NOT_CURRENT",
                "The requested template version is not the current published version.");
        }

        if (entry.Version.ContentFormat != ContentFormat.Html)
        {
            throw UseCaseException.Unprocessable(
                "UNSUPPORTED_CONTENT_FORMAT",
                "The selected template version does not use a supported content format.");
        }

        var document = entry.Version.Clone(
            currentUserContext.UserId,
            request.Title.Trim());

        await documentRepository.AddAsync(document, cancellationToken);

        return MapDocument(document, entry);
    }

    private static void ValidateRequest(CreateDraftDocumentRequestDto request)
    {
        var errors = new List<ValidationErrorDto>();

        if (request.TemplateVersionId == Guid.Empty)
        {
            errors.Add(new ValidationErrorDto(
                "templateVersionId",
                "REQUIRED",
                "templateVersionId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ValidationErrorDto(
                "title",
                "REQUIRED",
                "title is required."));
        }

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }
    }

    private static DocumentDetailDto MapDocument(
        Document document,
        TemplateVersionEntry source) =>
        new(
            document.Id,
            document.Title,
            document.Status,
            new DocumentSourceDto(
                source.Template.Id,
                source.Template.Name,
                source.Version.Id,
                source.Version.VersionNumber),
            document.Content,
            source.Version.ContentFormat,
            document.PlaceholderValues
                .Select(value => new DocumentPlaceholderValueDto(
                    value.Id,
                    value.PlaceholderId,
                    value.PlaceholderKeySnapshot,
                    value.LabelSnapshot,
                    value.DataTypeSnapshot,
                    value.Value))
                .ToArray(),
            document.CreatedAt,
            document.UpdatedAt,
            document.FinalizedAt);
}
