using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocumentTemplateSystem.IntegrationTests;

public sealed class AuthenticationApiTests : IClassFixture<AuthenticationApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly AuthenticationApiFactory _factory;

    public AuthenticationApiTests(AuthenticationApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("author")]
    [InlineData("author@example.test")]
    public async Task Login_WithUsernameOrEmail_ReturnsJwtAndUser(string identifier)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(identifier, AuthenticationApiFactory.UserPassword),
            JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = await response.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal("Bearer", login.TokenType);
        Assert.True(login.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal("author", login.User.Username);
        Assert.Equal("author@example.test", login.User.Email);
        Assert.Equal(UserRole.User, login.User.Role);
        Assert.Equal(Permissions.ForRole(UserRole.User), login.User.Permissions);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsGenericUnauthorizedError()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", "incorrect-password"),
            JsonOptions);

        await AssertInvalidCredentialsAsync(response);
    }

    [Fact]
    public async Task Login_WithUnknownUser_ReturnsGenericUnauthorizedError()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("missing@example.test", "incorrect-password"),
            JsonOptions);

        await AssertInvalidCredentialsAsync(response);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ReturnsGenericUnauthorizedError()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("inactive", AuthenticationApiFactory.InactivePassword),
            JsonOptions);

        await AssertInvalidCredentialsAsync(response);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/templates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("AUTHENTICATION_REQUIRED", error?.Code);
    }

    [Fact]
    public async Task ValidToken_CanAccessUserAuthorizedEndpoint()
    {
        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("admin", AuthenticationApiFactory.AdminPassword),
            JsonOptions);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync("/api/templates");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsAuthenticatedUser()
    {
        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", AuthenticationApiFactory.UserPassword),
            JsonOptions);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var user = await client.GetFromJsonAsync<UserDto>("/api/auth/me", JsonOptions);

        Assert.NotNull(user);
        Assert.Equal(login.User.Id, user.Id);
        Assert.Equal("author", user.Username);
        Assert.Equal(UserRole.User, user.Role);
        Assert.Equal(Permissions.ForRole(UserRole.User), user.Permissions);
    }

    [Fact]
    public async Task Register_CreatesActiveUserWithHashedPasswordAndCanLogin()
    {
        using var client = _factory.CreateClient();
        var suffix = Guid.NewGuid().ToString("N");
        var username = $"new-{suffix}";
        var email = $"new-{suffix}@example.test";
        const string password = "Registration123!";

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto(username, "New Author", email, password),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        Assert.NotNull(user);
        Assert.Equal(UserRole.User, user.Role);
        Assert.Equal(Permissions.ForRole(UserRole.User), user.Permissions);
        var responseBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, responseBody, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", responseBody, StringComparison.OrdinalIgnoreCase);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto(username, password),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto(
                "author",
                "Another Author",
                $"unique-{Guid.NewGuid():N}@example.test",
                "Registration123!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("USERNAME_ALREADY_EXISTS", error?.Code);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequestDto(
                $"unique-{Guid.NewGuid():N}",
                "Another Author",
                "author@example.test",
                "Registration123!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("EMAIL_ALREADY_EXISTS", error?.Code);
    }

    [Fact]
    public async Task UserToken_WithoutAdminPermission_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", AuthenticationApiFactory.UserPassword),
            JsonOptions);
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        Assert.NotNull(login);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PublicAuthenticationEndpoint_ExceedingLimit_Returns429()
    {
        await using var factory = new AuthenticationApiFactory(rateLimitPermitLimit: 2);
        using var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", "incorrect-password"),
            JsonOptions);
        var second = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", "incorrect-password"),
            JsonOptions);
        var rejected = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", "incorrect-password"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        var error = await rejected.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("RATE_LIMIT_EXCEEDED", error?.Code);
    }

    private static async Task AssertInvalidCredentialsAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("INVALID_CREDENTIALS", error?.Code);
        Assert.DoesNotContain("inactive", error?.Title ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class AuthenticationApiFactory : WebApplicationFactory<Program>
{
    public const string AdminPassword = "Admin123!";
    public const string UserPassword = "User123!";
    public const string InactivePassword = "Inactive123!";
    private const string JwtKey =
        "integration-test-signing-key-with-at-least-32-bytes";

    private readonly IReadOnlyList<User> _users;
    private readonly int? _rateLimitPermitLimit;

    public AuthenticationApiFactory()
        : this(null)
    {
    }

    internal AuthenticationApiFactory(int? rateLimitPermitLimit)
    {
        _rateLimitPermitLimit = rateLimitPermitLimit;
        _users =
        [
            CreateUser(
                "admin",
                "Development Admin",
                "admin@example.test",
                UserRole.Admin,
                AdminPassword),
            CreateUser(
                "author",
                "Development Author",
                "author@example.test",
                UserRole.User,
                UserPassword),
            CreateUser(
                "inactive",
                "Inactive User",
                "inactive@example.test",
                UserRole.User,
                InactivePassword,
                isActive: false)
        ];
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SeedData:Enabled", "false");
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", "DocumentTemplateSystem.Tests");
        builder.UseSetting("Jwt:Audience", "DocumentTemplateSystem.Tests.Client");
        builder.UseSetting("Jwt:ExpiresMinutes", "30");
        if (_rateLimitPermitLimit is not null)
        {
            builder.UseSetting(
                "RateLimiting:PublicAuthentication:PermitLimit",
                _rateLimitPermitLimit.Value.ToString());
        }
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<ITemplateRepository>();
            services.AddSingleton<IUserRepository>(new InMemoryUserRepository(_users));
            services.AddSingleton<ITemplateRepository>(new EmptyTemplateRepository());
        });
    }

    private static User CreateUser(
        string username,
        string fullName,
        string email,
        UserRole role,
        string password,
        bool isActive = true)
    {
        var passwordHashService = new AspNetPasswordHashService();
        var hashSubject = new User(username, "pending-hash", fullName, email, role);
        var user = new User(
            username,
            passwordHashService.HashPassword(hashSubject, password),
            fullName,
            email,
            role);

        if (!isActive)
        {
            user.Deactivate();
        }

        return user;
    }

    private sealed class InMemoryUserRepository(IReadOnlyList<User> seedUsers)
        : IUserRepository
    {
        private readonly List<User> _users = [.. seedUsers];

        public Task<User?> GetByIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.SingleOrDefault(user => user.Id == userId));
        }

        public Task<User?> FindByUsernameOrEmailAsync(
            string usernameOrEmail,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.SingleOrDefault(user =>
                user.Username == usernameOrEmail || user.Email == usernameOrEmail));
        }

        public Task<bool> UsernameExistsAsync(
            string username,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(user => user.Username == username));

        public Task<bool> EmailExistsAsync(
            string email,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.Any(user => user.Email == email));

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyTemplateRepository : ITemplateRepository
    {
        public Task<IReadOnlyList<TemplateCatalogEntry>> GetActiveAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TemplateCatalogEntry>>([]);
        }

        public Task<TemplateCatalogEntry?> GetByIdAsync(
            Guid templateId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TemplateCatalogEntry?>(null);
        }

        public Task<TemplateVersionEntry?> GetVersionByIdAsync(
            Guid templateVersionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TemplateVersionEntry?>(null);
        }
    }
}
