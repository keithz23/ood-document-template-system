using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using DocumentTemplateSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocumentTemplateSystem.IntegrationTests;

public sealed class AdminUserApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Admin_CanCreateUserWithHashedPasswordAndAuditLog()
    {
        await using var factory = new AdminUserApiFactory();
        using var client = CreateClient(
            factory,
            TestAuthenticationHandler.AdminAuthenticationScheme,
            factory.Admin.Id);
        const string password = "CreatedUser123!";

        var response = await client.PostAsJsonAsync(
            "/api/admin/users",
            new CreateAdminUserRequestDto(
                "created.user",
                "Created User",
                "created.user@example.test",
                password,
                UserRole.User),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<AdminUserDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created.IsActive);
        Assert.Equal(UserRole.User, created.Role);
        var stored = factory.Repository.Users.Single(user => user.Id == created.Id);
        Assert.NotEqual(password, stored.PasswordHash);
        Assert.Single(factory.Repository.AuditLogs, log =>
            log.ActionType == "CreateUser" && log.EntityId == created.Id);

        using var publicClient = factory.CreateClient();
        var loginResponse = await publicClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("created.user", password),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Theory]
    [InlineData("author", "unique-admin-create@example.test", "USERNAME_ALREADY_EXISTS")]
    [InlineData("unique-admin-create", "author@example.test", "EMAIL_ALREADY_EXISTS")]
    public async Task AdminCreate_WithDuplicateIdentity_ReturnsConflict(
        string username,
        string email,
        string expectedCode)
    {
        await using var factory = new AdminUserApiFactory();
        using var client = CreateClient(
            factory,
            TestAuthenticationHandler.AdminAuthenticationScheme,
            factory.Admin.Id);

        var response = await client.PostAsJsonAsync(
            "/api/admin/users",
            new CreateAdminUserRequestDto(
                username,
                "Duplicate User",
                email,
                "CreatedUser123!",
                UserRole.User),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal(expectedCode, error?.Code);
    }

    [Fact]
    public async Task Admin_CanListViewChangeRoleAndManageUserActivity()
    {
        await using var factory = new AdminUserApiFactory();
        using var client = CreateClient(
            factory,
            TestAuthenticationHandler.AdminAuthenticationScheme,
            factory.Admin.Id);

        var users = await client.GetFromJsonAsync<AdminUserDto[]>(
            "/api/admin/users",
            JsonOptions);
        Assert.Equal(2, users?.Length);

        var detail = await client.GetFromJsonAsync<AdminUserDto>(
            $"/api/admin/users/{factory.Author.Id}",
            JsonOptions);
        Assert.Equal(factory.Author.Id, detail?.Id);
        Assert.Equal(UserRole.User, detail?.Role);

        using var roleRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/admin/users/{factory.Author.Id}/role")
        {
            Content = JsonContent.Create(
                new UpdateUserRoleRequestDto(UserRole.Admin),
                options: JsonOptions)
        };
        var roleResponse = await client.SendAsync(roleRequest);
        Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
        Assert.Equal(UserRole.Admin, factory.Author.Role);

        var deactivateResponse = await client.PostAsync(
            $"/api/admin/users/{factory.Author.Id}/deactivate",
            null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);
        Assert.False(factory.Author.IsActive);

        using var publicClient = factory.CreateClient();
        var loginResponse = await publicClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", AdminUserApiFactory.AuthorPassword),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        var loginError = await loginResponse.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("INVALID_CREDENTIALS", loginError?.Code);

        var activateResponse = await client.PostAsync(
            $"/api/admin/users/{factory.Author.Id}/activate",
            null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
        Assert.True(factory.Author.IsActive);
    }

    [Fact]
    public async Task Admin_CannotDeactivateSelfOrChangeOwnRole()
    {
        await using var factory = new AdminUserApiFactory();
        using var client = CreateClient(
            factory,
            TestAuthenticationHandler.AdminAuthenticationScheme,
            factory.Admin.Id);

        var deactivateResponse = await client.PostAsync(
            $"/api/admin/users/{factory.Admin.Id}/deactivate",
            null);
        Assert.Equal(HttpStatusCode.Conflict, deactivateResponse.StatusCode);
        var deactivateError = await deactivateResponse.Content.ReadFromJsonAsync<ErrorResponseDto>(
            JsonOptions);
        Assert.Equal("SELF_DEACTIVATION_NOT_ALLOWED", deactivateError?.Code);

        using var roleRequest = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/admin/users/{factory.Admin.Id}/role")
        {
            Content = JsonContent.Create(
                new UpdateUserRoleRequestDto(UserRole.User),
                options: JsonOptions)
        };
        var roleResponse = await client.SendAsync(roleRequest);
        Assert.Equal(HttpStatusCode.Conflict, roleResponse.StatusCode);
        var roleError = await roleResponse.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("SELF_ROLE_CHANGE_NOT_ALLOWED", roleError?.Code);
    }

    [Fact]
    public async Task AuditLog_ReturnsExpectedRecordsAndHasNoMutationEndpoint()
    {
        await using var factory = new AdminUserApiFactory();
        using var client = CreateClient(
            factory,
            TestAuthenticationHandler.AdminAuthenticationScheme,
            factory.Admin.Id);

        await client.PostAsync($"/api/admin/users/{factory.Author.Id}/deactivate", null);

        var logs = await client.GetFromJsonAsync<AdminAuditLogDto[]>(
            "/api/admin/audit-logs",
            JsonOptions);
        var log = Assert.Single(logs!);
        Assert.Equal(factory.Admin.Id, log.PerformedBy.Id);
        Assert.Equal("admin", log.PerformedBy.Username);
        Assert.Equal("DeactivateUser", log.ActionType);
        Assert.Equal("User", log.EntityType);
        Assert.Equal(factory.Author.Id, log.EntityId);
        Assert.Contains("author", log.Description);

        var mutationResponse = await client.PostAsJsonAsync(
            "/api/admin/audit-logs",
            new { description = "not allowed" });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, mutationResponse.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("POST", "/api/admin/users")]
    [InlineData("GET", "/api/admin/users/{authorId}")]
    [InlineData("POST", "/api/admin/users/{authorId}/activate")]
    [InlineData("POST", "/api/admin/users/{authorId}/deactivate")]
    [InlineData("PATCH", "/api/admin/users/{authorId}/role")]
    [InlineData("GET", "/api/admin/audit-logs")]
    public async Task NormalUser_AdminUserAndAuditEndpoints_ReturnForbidden(
        string method,
        string path)
    {
        await using var factory = new AdminUserApiFactory();
        using var client = CreateClient(
            factory,
            TestAuthenticationHandler.AuthenticationScheme,
            factory.Author.Id);
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            path.Replace("{authorId}", factory.Author.Id.ToString()));
        if (method == "PATCH")
        {
            request.Content = JsonContent.Create(
                new UpdateUserRoleRequestDto(UserRole.Admin),
                options: JsonOptions);
        }
        else if (method == "POST" && path == "/api/admin/users")
        {
            request.Content = JsonContent.Create(
                new CreateAdminUserRequestDto(
                    "forbidden.user",
                    "Forbidden User",
                    "forbidden.user@example.test",
                    "Password123!",
                    UserRole.User),
                options: JsonOptions);
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static HttpClient CreateClient(
        AdminUserApiFactory factory,
        string authenticationScheme,
        Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(authenticationScheme);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}

public sealed class AdminUserApiFactory : WebApplicationFactory<Program>
{
    public const string AuthorPassword = "User123!";
    private const string JwtKey =
        "admin-user-integration-signing-key-with-at-least-32-bytes";

    public AdminUserApiFactory()
    {
        Admin = CreateUser(
            "admin",
            "Development Admin",
            "admin@example.test",
            UserRole.Admin,
            "Admin123!");
        Author = CreateUser(
            "author",
            "Development Author",
            "author@example.test",
            UserRole.User,
            AuthorPassword);
        Repository = new InMemoryAdminUserRepository([Admin, Author]);
    }

    public User Admin { get; }

    public User Author { get; }

    public InMemoryAdminUserRepository Repository { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SeedData:Enabled", "false");
        builder.UseSetting("Jwt:Key", JwtKey);
        builder.UseSetting("Jwt:Issuer", "DocumentTemplateSystem.Tests");
        builder.UseSetting("Jwt:Audience", "DocumentTemplateSystem.Tests.Client");
        builder.UseSetting("Jwt:ExpiresMinutes", "30");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAdminUserRepository>();
            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IAdminUserRepository>(Repository);
            services.AddSingleton<IUserRepository>(Repository);
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

    private static User CreateUser(
        string username,
        string fullName,
        string email,
        UserRole role,
        string password)
    {
        var passwordHashService = new AspNetPasswordHashService();
        var hashSubject = new User(username, "pending-hash", fullName, email, role);
        return new User(
            username,
            passwordHashService.HashPassword(hashSubject, password),
            fullName,
            email,
            role);
    }
}

public sealed class InMemoryAdminUserRepository(IReadOnlyList<User> seedUsers)
    : IAdminUserRepository, IUserRepository
{
    public List<User> Users { get; } = [.. seedUsers];

    public List<AuditLog> AuditLogs { get; } = [];

    public Task<IReadOnlyList<User>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>(
            Users.OrderBy(user => user.Username).ToArray());

    public Task<User?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));

    public Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));

    public Task<User?> FindByUsernameOrEmailAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user =>
            user.Username == usernameOrEmail || user.Email == usernameOrEmail));

    public Task<bool> UsernameExistsAsync(
        string username,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Any(user => user.Username == username));

    public Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Any(user => user.Email == email));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        AddUser(user);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AdminAuditLogEntry>> GetAuditLogsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdminAuditLogEntry>>(
            AuditLogs
                .OrderByDescending(log => log.CreatedAt)
                .Select(log => new AdminAuditLogEntry(
                    log,
                    Users.Single(user => user.Id == log.PerformedBy)))
                .ToArray());

    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);

    public void AddUser(User user) => Users.Add(user);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
