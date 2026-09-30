using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
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
    public async Task Swagger_DescribesOnlyTheFivePhase3BOperationsWithBearerSecurity()
    {
        using var client = _factory.CreateClient();

        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");

        Assert.Equal(5, paths.EnumerateObject().Count());
        Assert.True(paths.TryGetProperty("/api/templates", out _));
        Assert.True(paths.TryGetProperty("/api/templates/{templateId}", out _));
        Assert.True(paths.TryGetProperty(
            "/api/templates/{templateId}/current-version",
            out _));
        Assert.True(paths.TryGetProperty(
            "/api/template-versions/{templateVersionId}/placeholders",
            out _));
        Assert.True(paths.TryGetProperty("/api/documents", out _));

        var bearer = document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        Assert.Equal("http", bearer.GetProperty("type").GetString());
        Assert.Equal("bearer", bearer.GetProperty("scheme").GetString());
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
        DocumentRepository = new InMemoryDocumentRepository();
    }

    public Guid TemplateId { get; }

    public Guid TemplateVersionId { get; }

    public string TemplateContent { get; }

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

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    public List<Document> Documents { get; } = [];

    public Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        Documents.Add(document);
        return Task.CompletedTask;
    }
}

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string AuthenticationScheme = "Test";
    public static readonly Guid UserId = Guid.Parse("66c7e543-7a25-46fb-92bc-40d6464f9c3d");

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization)
            || !string.Equals(authorization, AuthenticationScheme, StringComparison.Ordinal))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, UserId.ToString()),
            new(ClaimTypes.Role, "User")
        ];
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
