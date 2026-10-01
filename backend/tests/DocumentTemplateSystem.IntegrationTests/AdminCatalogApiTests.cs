using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocumentTemplateSystem.IntegrationTests;

public sealed class AdminCatalogApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Admin_CategoryWorkflow_PersistsSoftStateChangesAndAuditLogs()
    {
        await using var factory = new AdminCatalogApiFactory();
        using var client = CreateClient(factory, TestAuthenticationHandler.AdminAuthenticationScheme);

        var listResponse = await client.GetAsync("/api/admin/categories");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/categories",
            new CreateCategoryRequestDto("Legal"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminCategoryDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.IsActive);

        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/admin/categories/{created.Id}")
        {
            Content = JsonContent.Create(
                new UpdateCategoryRequestDto("Legal and compliance"),
                options: JsonOptions)
        };
        var updateResponse = await client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deactivateResponse = await client.PostAsync(
            $"/api/admin/categories/{created.Id}/deactivate",
            null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var inactive = await deactivateResponse.Content.ReadFromJsonAsync<AdminCategoryDto>(JsonOptions);
        Assert.False(inactive?.IsActive);

        var activateResponse = await client.PostAsync(
            $"/api/admin/categories/{created.Id}/activate",
            null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        Assert.Equal(4, factory.Repository.AuditLogs.Count);
    }

    [Fact]
    public async Task Admin_TemplateWorkflow_CreatesInitialDraftAndEnforcesActivationInvariant()
    {
        await using var factory = new AdminCatalogApiFactory();
        using var client = CreateClient(factory, TestAuthenticationHandler.AdminAuthenticationScheme);

        var createResponse = await client.PostAsJsonAsync(
            "/api/admin/templates",
            new CreateTemplateRequestDto("Statement of work", factory.BusinessCategoryId),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<AdminTemplateDetailDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(TemplateStatus.Draft, created.Status);
        var initialVersion = Assert.Single(created.Versions);
        Assert.Equal(VersionStatus.Draft, initialVersion.Status);
        Assert.Equal(ContentFormat.Html, initialVersion.ContentFormat);

        using var updateRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/admin/templates/{created.Id}")
        {
            Content = JsonContent.Create(
                new UpdateDraftTemplateRequestDto("Standard statement of work", null),
                options: JsonOptions)
        };
        var updateResponse = await client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var activateResponse = await client.PostAsync(
            $"/api/admin/templates/{created.Id}/activate",
            null);
        Assert.Equal(HttpStatusCode.Conflict, activateResponse.StatusCode);
        var error = await activateResponse.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("TEMPLATE_ACTIVATION_REQUIRES_CURRENT_VERSION", error?.Code);

        var deactivateResponse = await client.PostAsync(
            $"/api/admin/templates/{factory.ActiveTemplateId}/deactivate",
            null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<AdminTemplateDetailDto>(JsonOptions);
        Assert.Equal(TemplateStatus.Inactive, deactivated?.Status);

        var reactivateResponse = await client.PostAsync(
            $"/api/admin/templates/{factory.ActiveTemplateId}/activate",
            null);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
    }

    [Fact]
    public async Task User_AdminEndpoint_ReturnsForbidden()
    {
        await using var factory = new AdminCatalogApiFactory();
        using var client = CreateClient(factory, TestAuthenticationHandler.AuthenticationScheme);

        var response = await client.GetAsync(
            $"/api/admin/templates/{factory.ActiveTemplateId}/versions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_TemplateVersionWorkflow_ManagesDraftPlaceholdersAndCurrentVersion()
    {
        await using var factory = new AdminCatalogApiFactory();
        using var client = CreateClient(factory, TestAuthenticationHandler.AdminAuthenticationScheme);

        var createDraftResponse = await client.PostAsync(
            $"/api/admin/templates/{factory.ActiveTemplateId}/versions",
            null);
        Assert.Equal(HttpStatusCode.Created, createDraftResponse.StatusCode);
        var draft = await createDraftResponse.Content.ReadFromJsonAsync<AdminTemplateVersionDetailDto>(
            JsonOptions);
        Assert.NotNull(draft);
        Assert.Equal(VersionStatus.Draft, draft.Status);
        Assert.False(draft.IsCurrent);
        Assert.Equal("<p>{{client_name}}</p>", draft.Content);
        Assert.Equal(1, draft.PlaceholderCount);

        using var updateVersionRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/admin/template-versions/{draft.Id}")
        {
            Content = JsonContent.Create(
                new UpdateDraftTemplateVersionRequestDto("<p>Updated {{client_name}}</p>"),
                options: JsonOptions)
        };
        var updateVersionResponse = await client.SendAsync(updateVersionRequest);
        Assert.Equal(HttpStatusCode.OK, updateVersionResponse.StatusCode);

        var createPlaceholderRequest = new CreatePlaceholderRequestDto(
            "project_total",
            "Project total",
            PlaceholderDataType.Number,
            true,
            "10.50");
        var createPlaceholderResponse = await client.PostAsJsonAsync(
            $"/api/admin/template-versions/{draft.Id}/placeholders",
            createPlaceholderRequest,
            JsonOptions);
        Assert.Equal(HttpStatusCode.Created, createPlaceholderResponse.StatusCode);
        var placeholder = await createPlaceholderResponse.Content.ReadFromJsonAsync<PlaceholderDto>(
            JsonOptions);
        Assert.NotNull(placeholder);

        var duplicateResponse = await client.PostAsJsonAsync(
            $"/api/admin/template-versions/{draft.Id}/placeholders",
            createPlaceholderRequest,
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var updatePlaceholderRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/admin/template-versions/{draft.Id}/placeholders/{placeholder.Id}")
        {
            Content = JsonContent.Create(
                new UpdatePlaceholderRequestDto(
                    "project_value",
                    "Project value",
                    PlaceholderDataType.Number,
                    false,
                    "25"),
                options: JsonOptions)
        };
        var updatePlaceholderResponse = await client.SendAsync(updatePlaceholderRequest);
        Assert.Equal(HttpStatusCode.OK, updatePlaceholderResponse.StatusCode);

        var removePlaceholderResponse = await client.DeleteAsync(
            $"/api/admin/template-versions/{draft.Id}/placeholders/{placeholder.Id}");
        Assert.Equal(HttpStatusCode.NoContent, removePlaceholderResponse.StatusCode);

        var oldCurrent = factory.Repository.Templates.Single().Versions.Single(version => version.IsCurrent);
        var publishResponse = await client.PostAsync(
            $"/api/admin/template-versions/{draft.Id}/publish",
            null);
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);
        var published = await publishResponse.Content.ReadFromJsonAsync<AdminTemplateVersionDetailDto>(
            JsonOptions);
        Assert.False(published?.IsCurrent);

        var publishedMutationResponse = await client.PostAsJsonAsync(
            $"/api/admin/template-versions/{draft.Id}/placeholders",
            createPlaceholderRequest,
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, publishedMutationResponse.StatusCode);

        var setCurrentResponse = await client.PostAsync(
            $"/api/admin/template-versions/{draft.Id}/set-current",
            null);
        Assert.Equal(HttpStatusCode.OK, setCurrentResponse.StatusCode);
        Assert.False(oldCurrent.IsCurrent);
        Assert.True(factory.Repository.Templates.Single().Versions.Single(
            version => version.Id == draft.Id).IsCurrent);
    }

    private static HttpClient CreateClient(
        AdminCatalogApiFactory factory,
        string authenticationScheme)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(authenticationScheme);
        return client;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class AdminCatalogApiFactory : WebApplicationFactory<Program>
{
    public AdminCatalogApiFactory()
    {
        Repository = new InMemoryAdminCatalogRepository();
        BusinessCategoryId = Repository.Categories.Single().Id;
        ActiveTemplateId = Repository.Templates.Single().Id;
    }

    public InMemoryAdminCatalogRepository Repository { get; }

    public Guid BusinessCategoryId { get; }

    public Guid ActiveTemplateId { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SeedData:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminCatalogRepository>();
            services.AddSingleton<IAdminCatalogRepository>(Repository);
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultForbidScheme =
                        TestAuthenticationHandler.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.AuthenticationScheme,
                    _ => { });
        });
    }
}

public sealed class InMemoryAdminCatalogRepository : IAdminCatalogRepository
{
    public InMemoryAdminCatalogRepository()
    {
        var creator = TestAuthenticationHandler.AdminId;
        var category = new Category("Business", creator);
        Categories.Add(category);
        var template = new Template(
            "Agreement",
            category.Id,
            creator,
            "<p>{{client_name}}</p>");
        var version = template.Versions.Single();
        version.AddPlaceholder(
            "client_name",
            "Client name",
            PlaceholderDataType.Text,
            true);
        version.Publish(creator);
        template.SetCurrentVersion(version.Id);
        template.Activate();
        Templates.Add(template);
    }

    public List<Category> Categories { get; } = [];

    public List<Template> Templates { get; } = [];

    public List<AuditLog> AuditLogs { get; } = [];

    public Task<IReadOnlyList<Category>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Category>>(Categories.OrderBy(item => item.Name).ToArray());

    public Task<Category?> GetCategoryByIdAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Categories.SingleOrDefault(item => item.Id == categoryId));

    public Task<IReadOnlyList<AdminTemplateEntry>> GetTemplatesAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminTemplateEntry>>(
            Templates.Select(MapTemplate).ToArray());

    public Task<AdminTemplateEntry?> GetTemplateByIdAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = Templates.SingleOrDefault(item => item.Id == templateId);
        return Task.FromResult(template is null ? null : MapTemplate(template));
    }

    public Task<AdminTemplateEntry?> GetTemplateByVersionIdAsync(
        Guid templateVersionId,
        CancellationToken cancellationToken = default)
    {
        var template = Templates.SingleOrDefault(item =>
            item.Versions.Any(version => version.Id == templateVersionId));
        return Task.FromResult(template is null ? null : MapTemplate(template));
    }

    public Task AddCategoryAsync(
        Category category,
        CancellationToken cancellationToken = default)
    {
        Categories.Add(category);
        return Task.CompletedTask;
    }

    public Task AddTemplateAsync(
        Template template,
        CancellationToken cancellationToken = default)
    {
        Templates.Add(template);
        return Task.CompletedTask;
    }

    public Task AddTemplateVersionAsync(
        TemplateVersion templateVersion,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

    public void RemovePlaceholder(Placeholder placeholder)
    {
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) =>
        operation(cancellationToken);

    private AdminTemplateEntry MapTemplate(Template template) =>
        new(
            template,
            Categories.Single(category => category.Id == template.CategoryId),
            template.Versions.ToArray());
}
