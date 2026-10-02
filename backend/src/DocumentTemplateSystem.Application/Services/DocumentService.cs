using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Strategy;

namespace DocumentTemplateSystem.Application.Services;

public sealed class DocumentService(
    ITemplateRepository templateRepository,
    IDocumentRepository documentRepository,
    ICurrentUserContext currentUserContext,
    PlaceholderValidator placeholderValidator,
    IDocumentRenderer documentRenderer,
    IHtmlContentSanitizer htmlSanitizer)
{
    public async Task<DocumentDetailDto> CreateDraftAsync(
        CreateDraftDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateCreateRequest(request);

        var source = await templateRepository.GetVersionByIdAsync(
            request.TemplateVersionId,
            cancellationToken);

        if (source is null)
        {
            throw UseCaseException.NotFound(
                "TEMPLATE_VERSION_NOT_FOUND",
                "The requested template version was not found.");
        }

        if (source.Template.Status != TemplateStatus.Active)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_INACTIVE",
                "The template is not active.");
        }

        if (!source.Version.IsCurrent || source.Version.Status != VersionStatus.Published)
        {
            throw UseCaseException.Conflict(
                "TEMPLATE_VERSION_NOT_CURRENT",
                "The requested template version is not the current published version.");
        }

        EnsureHtml(source.Version.ContentFormat);

        var document = source.Version.Clone(
            currentUserContext.UserId,
            request.Title.Trim());
        document.UpdateDraft(
            document.Title,
            htmlSanitizer.Sanitize(document.Content),
            document.UpdatedAt);

        await documentRepository.AddAsync(document, cancellationToken);

        return MapDocument(document, source.Template, source.Version);
    }

    public async Task<DocumentDetailDto> GetDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetOwnedDocumentAsync(documentId, cancellationToken);
        return MapDocument(entry);
    }

    public async Task<IReadOnlyList<DocumentSummaryDto>> GetHistoryAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await documentRepository.GetByOwnerAsync(
            currentUserContext.UserId,
            cancellationToken);

        return entries
            .OrderByDescending(entry => entry.Document.UpdatedAt)
            .Select(MapSummary)
            .ToArray();
    }

    public async Task<DocumentDetailDto> UpdateDraftAsync(
        Guid documentId,
        UpdateDraftDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePatchRequest(request);

        var entry = await GetOwnedDocumentAsync(documentId, cancellationToken);
        EnsureDraft(entry.Document);

        IReadOnlyList<ResolvedPlaceholderValue>? resolvedValues = null;
        if (request.PlaceholderValues is not null)
        {
            resolvedValues = ValidatePlaceholderValues(
                entry.Placeholders,
                request.PlaceholderValues,
                requireRequiredValues: false);
        }

        var timestamp = DateTimeOffset.UtcNow;
        entry.Document.UpdateDraft(
            request.Title?.Trim() ?? entry.Document.Title,
            request.Content is null
                ? entry.Document.Content
                : htmlSanitizer.Sanitize(request.Content),
            timestamp);

        if (resolvedValues is not null)
        {
            ReplacePlaceholderValues(entry.Document, resolvedValues, timestamp);
        }

        await documentRepository.SaveChangesAsync(cancellationToken);
        return MapDocument(entry);
    }

    public async Task<PreviewDocumentResponseDto> PreviewAsync(
        Guid documentId,
        PreviewDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await GetOwnedDocumentAsync(documentId, cancellationToken);
        EnsureHtml(entry.Version.ContentFormat);

        if (entry.Document.Status == DocumentStatus.Finalized)
        {
            var storedValues = entry.Document.PlaceholderValues
                .GroupBy(value => value.PlaceholderKeySnapshot, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last().Value,
                    StringComparer.Ordinal);

            return new PreviewDocumentResponseDto(
                entry.Document.Id,
                documentRenderer.Render(
                    htmlSanitizer.Sanitize(entry.Document.Content),
                    storedValues),
                ContentFormat.Html);
        }

        ValidatePreviewRequest(request);

        var resolvedValues = ValidatePlaceholderValues(
            entry.Placeholders,
            request.PlaceholderValues,
            requireRequiredValues: true);
        var rendered = documentRenderer.Render(
            htmlSanitizer.Sanitize(request.Content),
            BuildValueMap(entry.Placeholders, resolvedValues));

        return new PreviewDocumentResponseDto(
            entry.Document.Id,
            rendered,
            ContentFormat.Html);
    }

    public async Task<DocumentDetailDto> FinalizeAsync(
        Guid documentId,
        FinalizeDocumentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateFinalizeRequest(request);

        var entry = await GetOwnedDocumentAsync(documentId, cancellationToken);
        EnsureDraft(entry.Document);

        var resolvedValues = ValidatePlaceholderValues(
            entry.Placeholders,
            request.PlaceholderValues,
            requireRequiredValues: true);
        var timestamp = DateTimeOffset.UtcNow;

        entry.Document.UpdateDraft(
            request.Title.Trim(),
            htmlSanitizer.Sanitize(request.Content),
            timestamp);
        ReplacePlaceholderValues(entry.Document, resolvedValues, timestamp);
        entry.Document.Finalize(timestamp);

        await documentRepository.SaveChangesAsync(cancellationToken);
        return MapDocument(entry);
    }

    public async Task<DocumentDownloadDto> DownloadAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetOwnedDocumentAsync(documentId, cancellationToken);
        EnsureHtml(entry.Version.ContentFormat);

        var values = entry.Document.PlaceholderValues
            .GroupBy(value => value.PlaceholderKeySnapshot, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Last().Value,
                StringComparer.Ordinal);
        var content = documentRenderer.Render(
            htmlSanitizer.Sanitize(entry.Document.Content),
            values);

        return new DocumentDownloadDto(
            $"{CreateSafeFileName(entry.Document.Title, entry.Document.Id)}.html",
            "text/html; charset=utf-8",
            content);
    }

    private async Task<DocumentEntry> GetOwnedDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (documentId == Guid.Empty)
        {
            throw UseCaseException.Validation(new ValidationErrorDto(
                "documentId",
                "REQUIRED",
                "documentId is required."));
        }

        var entry = await documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (entry is null)
        {
            throw UseCaseException.NotFound(
                "DOCUMENT_NOT_FOUND",
                "The requested document was not found.");
        }

        if (entry.Document.CreatedBy != currentUserContext.UserId)
        {
            throw UseCaseException.Forbidden(
                "UNAUTHORIZED_DOCUMENT_ACCESS",
                "You do not have access to this document.");
        }

        return entry;
    }

    private IReadOnlyList<ResolvedPlaceholderValue> ValidatePlaceholderValues(
        IReadOnlyList<Placeholder> placeholders,
        IReadOnlyList<PlaceholderValueInputDto>? inputs,
        bool requireRequiredValues)
    {
        inputs ??= [];
        var placeholdersById = placeholders.ToDictionary(placeholder => placeholder.Id);
        var errors = new List<ValidationErrorDto>();
        var resolved = new List<ResolvedPlaceholderValue>();
        var seenIds = new HashSet<Guid>();
        var hasUnknown = false;

        foreach (var input in inputs)
        {
            if (!seenIds.Add(input.PlaceholderId))
            {
                errors.Add(new ValidationErrorDto(
                    "placeholderValues",
                    "DUPLICATE_PLACEHOLDER",
                    "Each placeholder may be supplied only once."));
                continue;
            }

            if (!placeholdersById.TryGetValue(input.PlaceholderId, out var placeholder))
            {
                hasUnknown = true;
                errors.Add(new ValidationErrorDto(
                    "placeholderValues",
                    "PLACEHOLDER_NOT_IN_SOURCE_VERSION",
                    "The placeholder does not belong to the document's source version."));
                continue;
            }

            var value = input.Value ?? string.Empty;
            var isRequired = requireRequiredValues && placeholder.IsRequired;
            if (!placeholderValidator.Validate(placeholder.DataType, value, isRequired))
            {
                var isMissing = isRequired && string.IsNullOrWhiteSpace(value);
                errors.Add(new ValidationErrorDto(
                    $"placeholderValues[{placeholder.Key}]",
                    isMissing
                        ? "MISSING_REQUIRED_PLACEHOLDER"
                        : $"INVALID_{placeholder.DataType.ToString().ToUpperInvariant()}",
                    isMissing
                        ? $"{placeholder.Label} is required."
                        : $"{placeholder.Label} must be a valid {placeholder.DataType.ToString().ToLowerInvariant()} value.",
                    placeholder.Key));
                continue;
            }

            resolved.Add(new ResolvedPlaceholderValue(placeholder, value));
        }

        if (requireRequiredValues)
        {
            foreach (var placeholder in placeholders.Where(candidate => candidate.IsRequired))
            {
                if (seenIds.Contains(placeholder.Id))
                {
                    continue;
                }

                errors.Add(new ValidationErrorDto(
                    $"placeholderValues[{placeholder.Key}]",
                    "MISSING_REQUIRED_PLACEHOLDER",
                    $"{placeholder.Label} is required.",
                    placeholder.Key));
            }
        }

        if (errors.Count > 0)
        {
            var hasMissing = errors.Any(error => error.Code == "MISSING_REQUIRED_PLACEHOLDER");
            throw UseCaseException.Unprocessable(
                hasUnknown
                    ? "PLACEHOLDER_NOT_IN_SOURCE_VERSION"
                    : hasMissing
                        ? "MISSING_REQUIRED_PLACEHOLDER"
                        : "INVALID_PLACEHOLDER_VALUE",
                hasMissing
                    ? "One or more required placeholder values are missing."
                    : "One or more placeholder values are invalid.",
                errors);
        }

        return resolved;
    }

    private void ReplacePlaceholderValues(
        Document document,
        IReadOnlyList<ResolvedPlaceholderValue> values,
        DateTimeOffset timestamp)
    {
        var retainedIds = values
            .Select(value => value.Placeholder.Id)
            .ToArray();
        var removed = document.RemovePlaceholderValuesExcept(retainedIds, timestamp);
        documentRepository.RemovePlaceholderValues(removed);
        foreach (var value in values)
        {
            document.SetPlaceholderValue(value.Placeholder, value.Value, timestamp);
        }
    }

    private static IReadOnlyDictionary<string, string> BuildValueMap(
        IReadOnlyList<Placeholder> placeholders,
        IReadOnlyList<ResolvedPlaceholderValue> values)
    {
        var suppliedValues = values.ToDictionary(
            value => value.Placeholder.Id,
            value => value.Value);

        return placeholders.ToDictionary(
            placeholder => placeholder.Key,
            placeholder => suppliedValues.GetValueOrDefault(placeholder.Id, string.Empty),
            StringComparer.Ordinal);
    }

    private static void ValidateCreateRequest(CreateDraftDocumentRequestDto request)
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

        ThrowValidation(errors);
    }

    private static void ValidatePatchRequest(UpdateDraftDocumentRequestDto request)
    {
        var errors = new List<ValidationErrorDto>();
        if (request.Title is null
            && request.Content is null
            && request.PlaceholderValues is null)
        {
            errors.Add(new ValidationErrorDto(
                "request",
                "EMPTY_PATCH",
                "Supply title, content, and/or placeholderValues."));
        }

        if (request.Title is not null && string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ValidationErrorDto(
                "title",
                "REQUIRED",
                "title cannot be empty when supplied."));
        }

        ThrowValidation(errors);
    }

    private static void ValidatePreviewRequest(PreviewDocumentRequestDto request)
    {
        var errors = new List<ValidationErrorDto>();
        if (request.Content is null)
        {
            errors.Add(new ValidationErrorDto("content", "REQUIRED", "content is required."));
        }

        if (request.PlaceholderValues is null)
        {
            errors.Add(new ValidationErrorDto(
                "placeholderValues",
                "REQUIRED",
                "placeholderValues is required."));
        }

        ThrowValidation(errors);
    }

    private static void ValidateFinalizeRequest(FinalizeDocumentRequestDto request)
    {
        var errors = new List<ValidationErrorDto>();
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add(new ValidationErrorDto("title", "REQUIRED", "title is required."));
        }

        if (request.Content is null)
        {
            errors.Add(new ValidationErrorDto("content", "REQUIRED", "content is required."));
        }

        if (request.PlaceholderValues is null)
        {
            errors.Add(new ValidationErrorDto(
                "placeholderValues",
                "REQUIRED",
                "placeholderValues is required."));
        }

        ThrowValidation(errors);
    }

    private static void ThrowValidation(IReadOnlyCollection<ValidationErrorDto> errors)
    {
        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }
    }

    private static void EnsureDraft(Document document)
    {
        if (document.Status != DocumentStatus.Draft)
        {
            throw UseCaseException.Conflict(
                "DOCUMENT_FINALIZED",
                "A finalized document is read-only.");
        }
    }

    private static void EnsureHtml(ContentFormat contentFormat)
    {
        if (contentFormat != ContentFormat.Html)
        {
            throw UseCaseException.Unprocessable(
                "UNSUPPORTED_CONTENT_FORMAT",
                "The document does not use the supported HTML content format.");
        }
    }

    private static string CreateSafeFileName(string title, Guid documentId)
    {
        var characters = title.Trim()
            .Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : char.IsWhiteSpace(character) ? '-' : '\0')
            .Where(character => character != '\0')
            .Take(100)
            .ToArray();
        var fileName = new string(characters).Trim('-', '.', '_');
        return string.IsNullOrWhiteSpace(fileName)
            ? $"document-{documentId:N}"
            : fileName;
    }

    private static DocumentDetailDto MapDocument(DocumentEntry entry) =>
        MapDocument(entry.Document, entry.Template, entry.Version);

    private static DocumentDetailDto MapDocument(
        Document document,
        Template template,
        TemplateVersion version) =>
        new(
            document.Id,
            document.Title,
            document.Status,
            MapSource(template, version),
            document.Content,
            version.ContentFormat,
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

    private static DocumentSummaryDto MapSummary(DocumentEntry entry) =>
        new(
            entry.Document.Id,
            entry.Document.Title,
            entry.Document.Status,
            MapSource(entry.Template, entry.Version),
            entry.Document.CreatedAt,
            entry.Document.UpdatedAt,
            entry.Document.FinalizedAt);

    private static DocumentSourceDto MapSource(
        Template template,
        TemplateVersion version) =>
        new(
            template.Id,
            template.Name,
            version.Id,
            version.VersionNumber);

    private sealed record ResolvedPlaceholderValue(
        Placeholder Placeholder,
        string Value);
}
