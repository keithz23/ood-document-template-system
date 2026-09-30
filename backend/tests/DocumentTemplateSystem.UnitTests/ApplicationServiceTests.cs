using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Application.Services;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Strategy;

namespace DocumentTemplateSystem.UnitTests;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task GetActiveTemplates_MapsCurrentPublishedVersionAndPlaceholderCount()
    {
        var data = AuthoringData.Create();
        var repository = new TemplateRepositoryStub(data);
        var service = new TemplateService(repository, new PlaceholderValidator());

        var result = await service.GetActiveTemplatesAsync();

        var template = Assert.Single(result);
        Assert.Equal(data.Template.Id, template.Id);
        Assert.Equal(TemplateStatus.Active, template.Status);
        Assert.Equal(data.Category.Name, template.Category.Name);
        Assert.Equal(data.Version.Id, template.CurrentVersion.Id);
        Assert.Equal(2, template.CurrentVersion.PlaceholderCount);
    }

    [Fact]
    public async Task GetTemplateDetail_TreatsInactiveTemplateAsNotFound()
    {
        var data = AuthoringData.Create();
        data.Template.Deactivate();
        var service = new TemplateService(
            new TemplateRepositoryStub(data),
            new PlaceholderValidator());

        var exception = await Assert.ThrowsAsync<UseCaseException>(() =>
            service.GetTemplateDetailAsync(data.Template.Id));

        Assert.Equal(UseCaseErrorKind.NotFound, exception.Kind);
        Assert.Equal("TEMPLATE_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task GetPlaceholders_UsesStrategyToRejectInvalidTypedDefault()
    {
        var data = AuthoringData.Create(includeInvalidDefault: true);
        var service = new TemplateService(
            new TemplateRepositoryStub(data),
            new PlaceholderValidator());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCurrentVersionPlaceholdersAsync(data.Version.Id));
    }

    [Fact]
    public async Task CreateDraft_UsesPrototypeAndPersistsIndependentDocument()
    {
        var data = AuthoringData.Create();
        var documentRepository = new DocumentRepositoryStub();
        var currentUser = new CurrentUserContextStub(Guid.NewGuid());
        var service = new DocumentService(
            new TemplateRepositoryStub(data),
            documentRepository,
            currentUser);
        var sourceContent = data.Version.Content;

        var result = await service.CreateDraftAsync(new CreateDraftDocumentRequestDto(
            data.Version.Id,
            "  Client agreement  "));

        var document = Assert.Single(documentRepository.Documents);
        Assert.NotEqual(data.Version.Id, document.Id);
        Assert.Equal(data.Version.Id, document.TemplateVersionId);
        Assert.Equal(currentUser.UserId, document.CreatedBy);
        Assert.Equal("Client agreement", document.Title);
        Assert.Equal(sourceContent, document.Content);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Equal(sourceContent, data.Version.Content);
        Assert.Equal(document.Id, result.Id);
        Assert.Empty(result.PlaceholderValues);
    }

    [Fact]
    public async Task CreateDraft_RejectsNonCurrentVersion()
    {
        var data = AuthoringData.Create();
        var draft = data.Template.AddVersion(
            "<p>Later draft</p>",
            ContentFormat.Html,
            Guid.NewGuid());
        var repository = new TemplateRepositoryStub(data, draft);
        var service = new DocumentService(
            repository,
            new DocumentRepositoryStub(),
            new CurrentUserContextStub(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<UseCaseException>(() =>
            service.CreateDraftAsync(new CreateDraftDocumentRequestDto(
                draft.Id,
                "Invalid source")));

        Assert.Equal(UseCaseErrorKind.Conflict, exception.Kind);
        Assert.Equal("TEMPLATE_VERSION_NOT_CURRENT", exception.Code);
    }

    [Fact]
    public async Task CreateDraft_ReportsAllRequiredRequestFields()
    {
        var data = AuthoringData.Create();
        var service = new DocumentService(
            new TemplateRepositoryStub(data),
            new DocumentRepositoryStub(),
            new CurrentUserContextStub(Guid.NewGuid()));

        var exception = await Assert.ThrowsAsync<UseCaseException>(() =>
            service.CreateDraftAsync(new CreateDraftDocumentRequestDto(Guid.Empty, " ")));

        Assert.Equal("VALIDATION_FAILED", exception.Code);
        Assert.Equal(2, exception.Errors?.Count);
    }

    private sealed record AuthoringData(
        Category Category,
        Template Template,
        TemplateVersion Version)
    {
        public static AuthoringData Create(bool includeInvalidDefault = false)
        {
            var creator = Guid.NewGuid();
            var category = new Category("Business", creator);
            var template = new Template(
                "Professional services agreement",
                category.Id,
                creator,
                "<h1>Agreement</h1><p>{{client_name}}</p>",
                ContentFormat.Html);
            var version = template.Versions.Single();
            version.AddPlaceholder(
                "client_name",
                "Client name",
                PlaceholderDataType.Text,
                true);
            version.AddPlaceholder(
                "client_email",
                "Client email",
                PlaceholderDataType.Email,
                false,
                includeInvalidDefault ? "not-an-email" : "client@example.test");
            version.Publish(creator);
            template.SetCurrentVersion(version.Id);
            template.Activate();
            return new AuthoringData(category, template, version);
        }
    }

    private sealed class TemplateRepositoryStub : ITemplateRepository
    {
        private readonly AuthoringData _data;
        private readonly Dictionary<Guid, TemplateVersionEntry> _versions;

        public TemplateRepositoryStub(
            AuthoringData data,
            TemplateVersion? additionalVersion = null)
        {
            _data = data;
            _versions = new Dictionary<Guid, TemplateVersionEntry>
            {
                [data.Version.Id] = CreateVersionEntry(data.Version)
            };

            if (additionalVersion is not null)
            {
                _versions[additionalVersion.Id] = CreateVersionEntry(additionalVersion);
            }
        }

        public Task<IReadOnlyList<TemplateCatalogEntry>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TemplateCatalogEntry> result =
            [
                new(
                    _data.Template,
                    _data.Category,
                    _data.Template.Versions.ToArray())
            ];
            return Task.FromResult(result);
        }

        public Task<TemplateCatalogEntry?> GetByIdAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            TemplateCatalogEntry? result = templateId == _data.Template.Id
                ? new(
                    _data.Template,
                    _data.Category,
                    _data.Template.Versions.ToArray())
                : null;
            return Task.FromResult(result);
        }

        public Task<TemplateVersionEntry?> GetVersionByIdAsync(
            Guid templateVersionId,
            CancellationToken cancellationToken = default)
        {
            _versions.TryGetValue(templateVersionId, out var result);
            return Task.FromResult(result);
        }

        private TemplateVersionEntry CreateVersionEntry(TemplateVersion version) =>
            new(version, _data.Template, version.Placeholders.ToArray());
    }

    private sealed class DocumentRepositoryStub : IDocumentRepository
    {
        public List<Document> Documents { get; } = [];

        public Task AddAsync(
            Document document,
            CancellationToken cancellationToken = default)
        {
            Documents.Add(document);
            return Task.CompletedTask;
        }
    }

    private sealed record CurrentUserContextStub(Guid UserId) : ICurrentUserContext;
}
