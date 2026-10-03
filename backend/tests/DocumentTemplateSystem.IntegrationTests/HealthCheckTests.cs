using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace DocumentTemplateSystem.IntegrationTests;

public sealed class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(builder =>
                builder.UseSetting("SeedData:Enabled", "false"))
            .CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task LivenessEndpoint_DoesNotDependOnDatabaseReadiness()
    {
        var response = await _client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ReadinessEndpoint_ReturnsUnavailableWhenDatabaseCannotConnect()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("SeedData:Enabled", "false");
                builder.UseSetting(
                    "ConnectionStrings:DefaultConnection",
                    "Host=127.0.0.1;Port=1;Database=unavailable;Username=none;Password=none;Timeout=1;Command Timeout=1");
            });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task ApiResponses_IncludeSecurityHeaders()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers
            .GetValues("Content-Security-Policy")
            .Single());
        Assert.Contains("img-src http: https:", response.Headers
            .GetValues("Content-Security-Policy")
            .Single());
        Assert.Contains("camera=()", response.Headers
            .GetValues("Permissions-Policy")
            .Single());
    }

    [Fact]
    public void ApiStartup_WithInvalidJwtConfiguration_FailsValidation()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("SeedData:Enabled", "false");
                builder.UseSetting("Jwt:Key", "too-short");
            });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            }));

        Assert.Contains("key of at least 32 bytes", exception.Message);
    }

    [Fact]
    public async Task SwaggerDocument_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CorsPolicy_AllowsConfiguredFrontendOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        Assert.Equal(
            "http://localhost:3000",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }
}
