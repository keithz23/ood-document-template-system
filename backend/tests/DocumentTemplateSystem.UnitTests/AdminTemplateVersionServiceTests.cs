using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Application.Services;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Domain.Patterns.Strategy;

namespace DocumentTemplateSystem.UnitTests;

public sealed class AdminTemplateVersionServiceTests
{
    [Fact]
    public async Task CreateDraft_CopiesCurrentVersionContentFormatAndPlaceholders()
    {
        var fixture = Fixture.Create();

        var draft = await fixture.Service.CreateDraftVersionAsync(fixture.Template.Id);

        Assert.Equal(VersionStatus.Draft, draft.Status);
        Assert.False(draft.IsCurrent);
        Assert.Equal(fixture.Current.Content, draft.Content);
        Assert.Equal(fixture.Current.ContentFormat, draft.ContentFormat);
        Assert.Equal(fixture.Current.Placeholders.Count, draft.PlaceholderCount);
        Assert.Contains(
            fixture.Repository.AddedTemplateVersions,
            version => version.Id == draft.Id);
        var copied = fixture.Template.Versions.Single(version => version.Id == draft.Id)
            .Placeholders.Single();
        Assert.NotEqual(fixture.Current.Placeholders.Single().Id, copied.Id);
    }

    [Fact]
    public async Task CreateDraft_WithoutCurrentVersion_CopiesLatestVersion()
    {
        var adminId = Guid.NewGuid();
        var category = new Category("Business", adminId);
        var template = new Template(
            "Agreement",
            category.Id,
            adminId,
            "<p>First</p>");
        var latest = template.AddVersion(
            "<p>Latest</p>",
            ContentFormat.Html,
            adminId);
        latest.AddPlaceholder(
            "latest_value",
            "Latest value",
            PlaceholderDataType.Text,
            false);
        var repository = new RepositoryStub(category, template);
        var service = new AdminTemplateVersionService(
            repository,
            new CurrentUserContextStub(adminId),
            new PlaceholderValidator());

        var draft = await service.CreateDraftVersionAsync(template.Id);

        Assert.Equal(3, draft.VersionNumber);
        Assert.Equal(latest.Content, draft.Content);
        Assert.Equal(1, draft.PlaceholderCount);
    }

    [Fact]
    public async Task UpdateDraft_ChangesContent()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();

        var result = await fixture.Service.UpdateDraftVersionAsync(
            draft.Id,
            new UpdateDraftTemplateVersionRequestDto("<p>Changed</p>"));

        Assert.Equal("<p>Changed</p>", result.Content);
    }

    [Fact]
    public async Task UpdatePublishedVersion_IsRejected()
    {
        var fixture = Fixture.Create();

        var exception = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.UpdateDraftVersionAsync(
                fixture.Current.Id,
                new UpdateDraftTemplateVersionRequestDto("<p>Changed</p>")));

        Assert.Equal("TEMPLATE_VERSION_PUBLISHED", exception.Code);
    }

    [Fact]
    public async Task DraftPlaceholder_CanBeAddedUpdatedAndRemoved()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();

        var created = await fixture.Service.CreatePlaceholderAsync(
            draft.Id,
            new CreatePlaceholderRequestDto(
                "amount",
                "Amount",
                PlaceholderDataType.Number,
                true,
                "12.5"));
        var updated = await fixture.Service.UpdatePlaceholderAsync(
            draft.Id,
            created.Id,
            new UpdatePlaceholderRequestDto(
                "total",
                "Total",
                PlaceholderDataType.Number,
                false,
                null));
        await fixture.Service.RemovePlaceholderAsync(draft.Id, created.Id);

        Assert.Equal("total", updated.Key);
        Assert.DoesNotContain(
            fixture.Template.Versions.Single(version => version.Id == draft.Id).Placeholders,
            placeholder => placeholder.Id == created.Id);
    }

    [Fact]
    public async Task PublishedPlaceholderMutation_IsRejected()
    {
        var fixture = Fixture.Create();
        var existing = fixture.Current.Placeholders.Single();

        var createException = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.CreatePlaceholderAsync(
                fixture.Current.Id,
                new CreatePlaceholderRequestDto(
                    "amount",
                    "Amount",
                    PlaceholderDataType.Number,
                    false,
                    null)));
        var updateException = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.UpdatePlaceholderAsync(
                fixture.Current.Id,
                existing.Id,
                new UpdatePlaceholderRequestDto(
                    existing.Key,
                    existing.Label,
                    existing.DataType,
                    existing.IsRequired,
                    existing.DefaultValue)));
        var removeException = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.RemovePlaceholderAsync(fixture.Current.Id, existing.Id));

        Assert.Equal("TEMPLATE_VERSION_PUBLISHED", createException.Code);
        Assert.Equal("TEMPLATE_VERSION_PUBLISHED", updateException.Code);
        Assert.Equal("TEMPLATE_VERSION_PUBLISHED", removeException.Code);
    }

    [Fact]
    public async Task PlaceholderDefaults_UseStrategyWhileRequiredMayOmitDefault()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();

        var invalid = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.CreatePlaceholderAsync(
                draft.Id,
                new CreatePlaceholderRequestDto(
                    "amount",
                    "Amount",
                    PlaceholderDataType.Number,
                    false,
                    "not-a-number")));
        var required = await fixture.Service.CreatePlaceholderAsync(
            draft.Id,
            new CreatePlaceholderRequestDto(
                "contact_email",
                "Contact email",
                PlaceholderDataType.Email,
                true,
                null));

        Assert.Equal("VALIDATION_FAILED", invalid.Code);
        Assert.Contains(invalid.Errors ?? [], error => error.Code == "INVALID_DEFAULT_VALUE");
        Assert.True(required.IsRequired);
        Assert.Null(required.DefaultValue);
    }

    [Fact]
    public async Task DuplicatePlaceholderKey_IsRejected()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();

        var exception = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.CreatePlaceholderAsync(
                draft.Id,
                new CreatePlaceholderRequestDto(
                    "client_name",
                    "Another client name",
                    PlaceholderDataType.Text,
                    false,
                    null)));

        Assert.Equal("PLACEHOLDER_KEY_CONFLICT", exception.Code);
    }

    [Fact]
    public async Task PublishDraft_DoesNotMakeItCurrent()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();

        var published = await fixture.Service.PublishAsync(draft.Id);

        Assert.Equal(VersionStatus.Published, published.Status);
        Assert.False(published.IsCurrent);
        Assert.True(fixture.Current.IsCurrent);
    }

    [Fact]
    public async Task SetCurrent_ClearsPreviousCurrentAtomically()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();
        await fixture.Service.PublishAsync(draft.Id);

        var current = await fixture.Service.SetCurrentAsync(draft.Id);

        Assert.True(current.IsCurrent);
        Assert.False(fixture.Current.IsCurrent);
        Assert.Single(fixture.Template.Versions, version => version.IsCurrent);
        Assert.Equal(1, fixture.Repository.TransactionCount);
    }

    [Fact]
    public async Task DraftCannotBecomeCurrent()
    {
        var fixture = Fixture.Create();
        var draft = await fixture.CreateDraftAsync();

        var exception = await Assert.ThrowsAsync<UseCaseException>(() =>
            fixture.Service.SetCurrentAsync(draft.Id));

        Assert.Equal("TEMPLATE_VERSION_NOT_PUBLISHED", exception.Code);
    }

    private sealed record Fixture(
        Template Template,
        TemplateVersion Current,
        RepositoryStub Repository,
        AdminTemplateVersionService Service)
    {
        public static Fixture Create()
        {
            var adminId = Guid.NewGuid();
            var category = new Category("Business", adminId);
            var template = new Template(
                "Agreement",
                category.Id,
                adminId,
                "<p>{{client_name}}</p>");
            var current = template.Versions.Single();
            current.AddPlaceholder(
                "client_name",
                "Client name",
                PlaceholderDataType.Text,
                true);
            current.Publish(adminId);
            template.SetCurrentVersion(current.Id);
            var repository = new RepositoryStub(category, template);
            var service = new AdminTemplateVersionService(
                repository,
                new CurrentUserContextStub(adminId),
                new PlaceholderValidator());
            return new Fixture(template, current, repository, service);
        }

        public Task<AdminTemplateVersionDetailDto> CreateDraftAsync() =>
            Service.CreateDraftVersionAsync(Template.Id);
    }

    private sealed class RepositoryStub(Category category, Template template)
        : IAdminCatalogRepository
    {
        public List<AuditLog> AuditLogs { get; } = [];

        public int TransactionCount { get; private set; }

        public List<TemplateVersion> AddedTemplateVersions { get; } = [];

        public Task<IReadOnlyList<Category>> GetCategoriesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>([category]);

        public Task<Category?> GetCategoryByIdAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Category?>(category.Id == categoryId ? category : null);

        public Task<IReadOnlyList<AdminTemplateEntry>> GetTemplatesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminTemplateEntry>>([MapTemplate()]);

        public Task<AdminTemplateEntry?> GetTemplateByIdAsync(
            Guid templateId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminTemplateEntry?>(template.Id == templateId ? MapTemplate() : null);

        public Task<AdminTemplateEntry?> GetTemplateByVersionIdAsync(
            Guid templateVersionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminTemplateEntry?>(
                template.Versions.Any(version => version.Id == templateVersionId)
                    ? MapTemplate()
                    : null);

        public Task AddCategoryAsync(
            Category newCategory,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddTemplateAsync(
            Template newTemplate,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task AddTemplateVersionAsync(
            TemplateVersion templateVersion,
            CancellationToken cancellationToken = default)
        {
            AddedTemplateVersions.Add(templateVersion);
            return Task.CompletedTask;
        }

        public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

        public void RemovePlaceholder(Placeholder placeholder)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken = default)
        {
            TransactionCount++;
            await operation(cancellationToken);
        }

        private AdminTemplateEntry MapTemplate() =>
            new(template, category, template.Versions.ToArray());
    }

    private sealed record CurrentUserContextStub(Guid UserId) : ICurrentUserContext;
}
