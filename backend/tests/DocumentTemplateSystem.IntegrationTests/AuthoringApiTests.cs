using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using DocumentTemplateSystem.Application.Authorization;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentTemplateSystem.IntegrationTests;

public sealed class AuthoringApiTests : IClassFixture<AuthoringApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly AuthoringApiFactory _factory;

    public AuthoringApiTests(AuthoringApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutBearerToken_ReturnsContractError()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.UseSetting("SeedData:Enabled", "false"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/templates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("AUTHENTICATION_REQUIRED", error?.Code);
        Assert.False(string.IsNullOrWhiteSpace(error?.TraceId));
    }

    [Fact]
    public async Task TemplateRoutes_ReturnCurrentPublishedAuthoringData()
    {
        using var client = CreateAuthenticatedClient();

        var gallery = await client.GetFromJsonAsync<TemplateGalleryItemDto[]>(
            "/api/templates",
            JsonOptions);
        var galleryItem = Assert.Single(gallery!);
        Assert.Equal(_factory.TemplateId, galleryItem.Id);
        Assert.Equal(_factory.TemplateVersionId, galleryItem.CurrentVersion.Id);
        Assert.Equal(2, galleryItem.CurrentVersion.PlaceholderCount);

        var detail = await client.GetFromJsonAsync<TemplateDetailDto>(
            $"/api/templates/{_factory.TemplateId}",
            JsonOptions);
        Assert.Equal(_factory.TemplateId, detail?.Id);
        Assert.Equal("Business", detail?.Category.Name);

        var version = await client.GetFromJsonAsync<TemplateVersionDetailDto>(
            $"/api/templates/{_factory.TemplateId}/current-version",
            JsonOptions);
        Assert.Equal(_factory.TemplateVersionId, version?.Id);
        Assert.Equal(VersionStatus.Published, version?.Status);
        Assert.True(version?.IsCurrent);

        var placeholders = await client.GetFromJsonAsync<PlaceholderDto[]>(
            $"/api/template-versions/{_factory.TemplateVersionId}/placeholders",
            JsonOptions);
        Assert.Equal(2, placeholders?.Length);
        Assert.Contains(placeholders!, placeholder =>
            placeholder.Key == "effective_date"
            && placeholder.DataType == PlaceholderDataType.Date);
    }

    [Fact]
    public async Task CreateDraft_Returns201LocationAndPrototypeCopy()
    {
        _factory.DocumentRepository.Reset();
        using var client = CreateAuthenticatedClient();
        var request = new CreateDraftDocumentRequestDto(
            _factory.TemplateVersionId,
            "  Acme agreement  ");

        var response = await client.PostAsJsonAsync("/api/documents", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var document = await response.Content.ReadFromJsonAsync<DocumentDetailDto>(JsonOptions);
        Assert.NotNull(document);
        Assert.Equal($"/api/documents/{document.Id}", response.Headers.Location?.ToString());
        Assert.Equal("Acme agreement", document.Title);
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Equal(_factory.TemplateVersionId, document.Source.TemplateVersionId);
        Assert.Equal(_factory.TemplateContent, document.Content);
        Assert.Equal(ContentFormat.Html, document.ContentFormat);
        Assert.Empty(document.PlaceholderValues);

        var stored = Assert.Single(_factory.DocumentRepository.Documents);
        Assert.Equal(TestAuthenticationHandler.UserId, stored.CreatedBy);
        Assert.Equal(_factory.TemplateContent, stored.Content);
    }

    [Fact]
    public async Task CreateDraft_WithMissingFields_ReturnsMachineReadableValidationErrors()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync(
            "/api/documents",
            new CreateDraftDocumentRequestDto(Guid.Empty, " "),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("VALIDATION_FAILED", error?.Code);
        Assert.Equal(2, error?.Errors?.Count);
    }

    [Fact]
    public async Task Swagger_DescribesApprovedOperationsWithBearerSecurity()
    {
        using var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");

        Assert.Equal(32, paths.EnumerateObject().Count());
        Assert.True(paths.TryGetProperty("/api/auth/login", out var loginPath));
        Assert.True(paths.TryGetProperty("/api/auth/register", out var registerPath));
        Assert.True(paths.TryGetProperty("/api/auth/me", out _));
        Assert.True(paths.TryGetProperty("/api/templates", out _));
        Assert.True(paths.TryGetProperty("/api/templates/{templateId}", out _));
        Assert.True(paths.TryGetProperty(
            "/api/templates/{templateId}/current-version",
            out _));
        Assert.True(paths.TryGetProperty(
            "/api/template-versions/{templateVersionId}/placeholders",
            out _));
        Assert.True(paths.TryGetProperty("/api/documents", out _));
        Assert.True(paths.TryGetProperty("/api/documents/{documentId}", out var documentPath));
        Assert.True(documentPath.TryGetProperty("patch", out _));
        Assert.True(paths.TryGetProperty("/api/documents/{documentId}/preview", out _));
        Assert.True(paths.TryGetProperty("/api/documents/{documentId}/finalize", out _));
        Assert.True(paths.TryGetProperty("/api/documents/{documentId}/download", out _));
        Assert.True(paths.TryGetProperty("/api/admin/categories", out _));
        Assert.True(paths.TryGetProperty("/api/admin/categories/{categoryId}", out _));
        Assert.True(paths.TryGetProperty("/api/admin/templates", out _));
        Assert.True(paths.TryGetProperty("/api/admin/templates/{templateId}", out _));
        Assert.True(paths.TryGetProperty(
            "/api/admin/templates/{templateId}/versions",
            out var versionsPath));
        Assert.True(versionsPath.TryGetProperty("get", out _));
        Assert.True(versionsPath.TryGetProperty("post", out _));
        Assert.True(paths.TryGetProperty(
            "/api/admin/template-versions/{versionId}",
            out var versionPath));
        Assert.True(versionPath.TryGetProperty("get", out _));
        Assert.True(versionPath.TryGetProperty("patch", out _));
        Assert.True(paths.TryGetProperty(
            "/api/admin/template-versions/{versionId}/publish",
            out _));
        Assert.True(paths.TryGetProperty(
            "/api/admin/template-versions/{versionId}/set-current",
            out _));
        Assert.True(paths.TryGetProperty(
            "/api/admin/template-versions/{versionId}/placeholders",
            out var placeholdersPath));
        Assert.True(placeholdersPath.TryGetProperty("get", out _));
        Assert.True(placeholdersPath.TryGetProperty("post", out _));
        Assert.True(paths.TryGetProperty(
            "/api/admin/template-versions/{versionId}/placeholders/{placeholderId}",
            out var placeholderPath));
        Assert.True(paths.TryGetProperty("/api/admin/users", out var usersPath));
        Assert.True(usersPath.TryGetProperty("post", out _));
        Assert.True(paths.TryGetProperty("/api/admin/users/{userId}", out _));
        Assert.True(paths.TryGetProperty("/api/admin/users/{userId}/role", out _));
        Assert.True(paths.TryGetProperty("/api/admin/audit-logs", out var auditLogsPath));
        Assert.True(auditLogsPath.TryGetProperty("get", out _));
        Assert.False(auditLogsPath.TryGetProperty("post", out _));
        Assert.True(placeholderPath.TryGetProperty("patch", out _));
        Assert.True(placeholderPath.TryGetProperty("delete", out _));

        var bearer = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
        Assert.Empty(loginPath.GetProperty("post").GetProperty("security").EnumerateArray());
        Assert.Empty(registerPath.GetProperty("post").GetProperty("security").EnumerateArray());
    }

    [Fact]
    public async Task DocumentWorkflow_LoadsUpdatesPreviewsFinalizesAndDownloadsOwnDocument()
    {
        _factory.DocumentRepository.Reset();
        using var client = CreateAuthenticatedClient();
        var document = await CreateDraftAsync(client, "Workflow agreement");

        var loaded = await client.GetFromJsonAsync<DocumentDetailDto>(
            $"/api/documents/{document.Id}",
            JsonOptions);
        Assert.Equal(document.Id, loaded?.Id);
        Assert.Equal(TestAuthenticationHandler.UserId, _factory.DocumentRepository.Documents.Single().CreatedBy);

        var placeholders = _factory.Placeholders;
        var update = new UpdateDraftDocumentRequestDto(
            "Updated agreement",
            "<h1>Updated</h1><p>{{effective_date}} for {{client_name}}</p>",
            [
                new PlaceholderValueInputDto(placeholders[0].Id, "2026-10-01"),
                new PlaceholderValueInputDto(placeholders[1].Id, "Acme & Partners")
            ]);
        using var patchRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/documents/{document.Id}")
        {
            Content = JsonContent.Create(update, options: JsonOptions)
        };
        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updated = await patchResponse.Content.ReadFromJsonAsync<DocumentDetailDto>(JsonOptions);
        Assert.Equal("Updated agreement", updated?.Title);
        Assert.Equal(2, updated?.PlaceholderValues.Count);

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/preview",
            new PreviewDocumentRequestDto(update.Content!, update.PlaceholderValues!),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<PreviewDocumentResponseDto>(JsonOptions);
        Assert.Contains("2026-10-01", preview?.RenderedContent);
        Assert.Contains("Acme &amp; Partners", preview?.RenderedContent);

        var finalizeResponse = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/finalize",
            new FinalizeDocumentRequestDto(
                update.Title!,
                update.Content!,
                update.PlaceholderValues!),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, finalizeResponse.StatusCode);
        var finalized = await finalizeResponse.Content.ReadFromJsonAsync<DocumentDetailDto>(JsonOptions);
        Assert.Equal(DocumentStatus.Finalized, finalized?.Status);
        Assert.NotNull(finalized?.FinalizedAt);

        var finalizedPreviewResponse = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/preview",
            new PreviewDocumentRequestDto(
                "<p>Attempted finalized change</p>",
                [
                    new PlaceholderValueInputDto(placeholders[0].Id, "2099-01-01"),
                    new PlaceholderValueInputDto(placeholders[1].Id, "Changed")
                ]),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, finalizedPreviewResponse.StatusCode);
        var finalizedPreview = await finalizedPreviewResponse.Content
            .ReadFromJsonAsync<PreviewDocumentResponseDto>(JsonOptions);
        Assert.Contains("2026-10-01", finalizedPreview?.RenderedContent);
        Assert.Contains("Acme &amp; Partners", finalizedPreview?.RenderedContent);
        Assert.DoesNotContain("Attempted finalized change", finalizedPreview?.RenderedContent);

        var history = await client.GetFromJsonAsync<DocumentSummaryDto[]>(
            "/api/documents",
            JsonOptions);
        var historyItem = Assert.Single(history!);
        Assert.Equal(document.Id, historyItem.Id);
        Assert.Equal(DocumentStatus.Finalized, historyItem.Status);

        var downloadResponse = await client.GetAsync($"/api/documents/{document.Id}/download");
        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal("text/html", downloadResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("utf-8", downloadResponse.Content.Headers.ContentType?.CharSet);
        Assert.Equal("Updated-agreement.html", downloadResponse.Content.Headers.ContentDisposition?.FileNameStar);
        Assert.Contains("Acme &amp; Partners", await downloadResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetDocument_WhenOwnedByAnotherUser_ReturnsForbidden()
    {
        _factory.DocumentRepository.Reset();
        using var ownerClient = CreateAuthenticatedClient();
        var document = await CreateDraftAsync(ownerClient, "Private document");
        using var otherClient = _factory.CreateClient();
        otherClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthenticationHandler.OtherAuthenticationScheme);

        var response = await otherClient.GetAsync($"/api/documents/{document.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("UNAUTHORIZED_DOCUMENT_ACCESS", error?.Code);
    }

    [Fact]
    public async Task Preview_WithMissingRequiredValues_ReturnsUnprocessableEntity()
    {
        _factory.DocumentRepository.Reset();
        using var client = CreateAuthenticatedClient();
        var document = await CreateDraftAsync(client, "Incomplete agreement");

        var response = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/preview",
            new PreviewDocumentRequestDto(document.Content, []),
            JsonOptions);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("MISSING_REQUIRED_PLACEHOLDER", error?.Code);
        Assert.Equal(2, error?.Errors?.Count);
    }

    [Fact]
    public async Task UpdateAndDownload_SanitizeUnsafeHtml()
    {
        _factory.DocumentRepository.Reset();
        using var client = CreateAuthenticatedClient();
        var document = await CreateDraftAsync(client, "Sanitized agreement");

        using var patchRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/documents/{document.Id}")
        {
            Content = JsonContent.Create(
                new UpdateDraftDocumentRequestDto(
                    null,
                    "<h1>Safe</h1><script>alert('unsafe')</script><p onclick=\"alert(1)\">Body</p>",
                    null),
                options: JsonOptions)
        };
        var patchResponse = await client.SendAsync(patchRequest);

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updated = await patchResponse.Content.ReadFromJsonAsync<DocumentDetailDto>(JsonOptions);
        Assert.Contains("<h1>Safe</h1>", updated?.Content);
        Assert.DoesNotContain("<script", updated?.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", updated?.Content, StringComparison.OrdinalIgnoreCase);

        var download = await client.GetStringAsync($"/api/documents/{document.Id}/download");
        Assert.DoesNotContain("<script", download, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", download, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FinalizedDocument_RejectsFurtherUpdatesAndFinalization()
    {
        _factory.DocumentRepository.Reset();
        using var client = CreateAuthenticatedClient();
        var document = await CreateDraftAsync(client, "Final agreement");
        var values = new[]
        {
            new PlaceholderValueInputDto(_factory.Placeholders[0].Id, "2026-10-01"),
            new PlaceholderValueInputDto(_factory.Placeholders[1].Id, "Acme")
        };
        var finalized = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/finalize",
            new FinalizeDocumentRequestDto(document.Title, document.Content, values),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, finalized.StatusCode);

        using var patchRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/documents/{document.Id}")
        {
            Content = JsonContent.Create(
                new UpdateDraftDocumentRequestDto("Changed", null, null),
                options: JsonOptions)
        };
        var patchResponse = await client.SendAsync(patchRequest);
        Assert.Equal(HttpStatusCode.Conflict, patchResponse.StatusCode);

        var secondFinalize = await client.PostAsJsonAsync(
            $"/api/documents/{document.Id}/finalize",
            new FinalizeDocumentRequestDto("Changed", document.Content, values),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Conflict, secondFinalize.StatusCode);
        var error = await secondFinalize.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("DOCUMENT_FINALIZED", error?.Code);
    }

    [Fact]
    public async Task Download_AllowsDraftDocuments()
    {
        _factory.DocumentRepository.Reset();
        using var client = CreateAuthenticatedClient();
        var document = await CreateDraftAsync(client, "Draft export");

        var response = await client.GetAsync($"/api/documents/{document.Id}/download");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<DocumentDetailDto> CreateDraftAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync(
            "/api/documents",
            new CreateDraftDocumentRequestDto(_factory.TemplateVersionId, title),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DocumentDetailDto>(JsonOptions))!;
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
        return client;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class AuthoringApiFactory : WebApplicationFactory<Program>
{
    private readonly TemplateCatalogEntry _catalogEntry;
    private readonly TemplateVersionEntry _versionEntry;

    public AuthoringApiFactory()
    {
        var creator = Guid.NewGuid();
        var category = new Category("Business", creator);
        var template = new Template(
            "Professional services agreement",
            category.Id,
            creator,
            "<h1>Agreement</h1><p>{{effective_date}} for {{client_name}}</p>",
            ContentFormat.Html);
        var version = template.Versions.Single();
        version.AddPlaceholder(
            "effective_date",
            "Effective date",
            PlaceholderDataType.Date,
            true);
        version.AddPlaceholder(
            "client_name",
            "Client name",
            PlaceholderDataType.Text,
            true);
        version.Publish(creator);
        template.SetCurrentVersion(version.Id);
        template.Activate();

        TemplateId = template.Id;
        TemplateVersionId = version.Id;
        TemplateContent = version.Content;
        _catalogEntry = new TemplateCatalogEntry(
            template,
            category,
            template.Versions.ToArray());
        _versionEntry = new TemplateVersionEntry(
            version,
            template,
            version.Placeholders.ToArray());
        Placeholders = version.Placeholders.ToArray();
        DocumentRepository = new InMemoryDocumentRepository(template, version);
    }

    public Guid TemplateId { get; }

    public Guid TemplateVersionId { get; }

    public string TemplateContent { get; }

    public IReadOnlyList<Placeholder> Placeholders { get; }

    public InMemoryDocumentRepository DocumentRepository { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SeedData:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ITemplateRepository>();
            services.RemoveAll<IDocumentRepository>();
            services.AddSingleton<ITemplateRepository>(
                new InMemoryTemplateRepository(_catalogEntry, _versionEntry));
            services.AddSingleton<IDocumentRepository>(DocumentRepository);
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

    private sealed class InMemoryTemplateRepository(
        TemplateCatalogEntry catalogEntry,
        TemplateVersionEntry versionEntry) : ITemplateRepository
    {
        public Task<IReadOnlyList<TemplateCatalogEntry>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TemplateCatalogEntry> result = [catalogEntry];
            return Task.FromResult(result);
        }

        public Task<TemplateCatalogEntry?> GetByIdAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TemplateCatalogEntry?>(
                templateId == catalogEntry.Template.Id ? catalogEntry : null);
        }

        public Task<TemplateVersionEntry?> GetVersionByIdAsync(
            Guid templateVersionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TemplateVersionEntry?>(
                templateVersionId == versionEntry.Version.Id ? versionEntry : null);
        }
    }
}

public sealed class InMemoryDocumentRepository(
    Template template,
    TemplateVersion version) : IDocumentRepository
{
    public List<Document> Documents { get; } = [];

    public void Reset() => Documents.Clear();

    public Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        Documents.Add(document);
        return Task.CompletedTask;
    }

    public Task<DocumentEntry?> GetByIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = Documents.SingleOrDefault(candidate => candidate.Id == documentId);
        return Task.FromResult(document is null
            ? null
            : new DocumentEntry(document, template, version, version.Placeholders.ToArray()));
    }

    public Task<IReadOnlyList<DocumentEntry>> GetByOwnerAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentEntry> entries = Documents
            .Where(document => document.CreatedBy == ownerId)
            .Select(document => new DocumentEntry(
                document,
                template,
                version,
                version.Placeholders.ToArray()))
            .ToArray();
        return Task.FromResult(entries);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public void RemovePlaceholderValues(
        IReadOnlyCollection<DocumentPlaceholderValue> placeholderValues)
    {
    }
}

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";
    public const string OtherAuthenticationScheme = "Other";
    public const string AdminAuthenticationScheme = "Admin";
    public static readonly Guid UserId = Guid.Parse("66c7e543-7a25-46fb-92bc-40d6464f9c3d");
    public static readonly Guid AdminId = Guid.Parse("c3d358ab-35bb-4018-8988-346381f6422c");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization)
            || (authorization != AuthenticationScheme
                && authorization != OtherAuthenticationScheme
                && authorization != AdminAuthenticationScheme))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var authenticationValue = authorization.ToString();
        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var customUserId)
            && Guid.TryParse(customUserId, out var parsedUserId)
            ? parsedUserId
            : authenticationValue switch
        {
            OtherAuthenticationScheme =>
                Guid.Parse("15dbaf8b-cc8c-447c-b0a8-6dc076fb8e7c"),
            AdminAuthenticationScheme => AdminId,
            _ => UserId
        };
        var role = authenticationValue == AdminAuthenticationScheme ? "Admin" : "User";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        claims.AddRange(Permissions.ForRole(
                role == "Admin" ? UserRole.Admin : UserRole.User)
            .Select(permission => new Claim(Permissions.ClaimType, permission)));
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
