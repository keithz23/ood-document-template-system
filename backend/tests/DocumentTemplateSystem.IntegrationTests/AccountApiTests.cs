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

public sealed class AccountApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task User_CanUpdateOwnProfileWithoutChangingUsername()
    {
        await using var factory = new AccountApiFactory();
        using var client = CreateAuthenticatedClient(factory, factory.Author.Id);
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me")
        {
            Content = JsonContent.Create(
                new UpdateOwnProfileRequestDto(
                    "Updated Author",
                    "updated.author@example.test"),
                options: JsonOptions)
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        Assert.Equal("author", user?.Username);
        Assert.Equal("Updated Author", user?.FullName);
        Assert.Equal("updated.author@example.test", user?.Email);
        Assert.Single(factory.Repository.AuditLogs, log =>
            log.ActionType == "ProfileUpdate" && log.EntityId == factory.Author.Id);
    }

    [Fact]
    public async Task ProfileUpdate_WithDuplicateEmail_ReturnsConflict()
    {
        await using var factory = new AccountApiFactory();
        using var client = CreateAuthenticatedClient(factory, factory.Author.Id);
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me")
        {
            Content = JsonContent.Create(
                new UpdateOwnProfileRequestDto("Author", factory.Admin.Email),
                options: JsonOptions)
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Equal("EMAIL_ALREADY_EXISTS", error?.Code);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_PersistsAndAudits()
    {
        await using var factory = new AccountApiFactory();
        using var client = CreateAuthenticatedClient(factory, factory.Author.Id);
        const string newPassword = "Updated123!";

        var response = await client.PostAsJsonAsync(
            "/api/users/me/change-password",
            new ChangePasswordRequestDto(AccountApiFactory.AuthorPassword, newPassword),
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Single(factory.Repository.AuditLogs, log =>
            log.ActionType == "ChangePassword" && log.EntityId == factory.Author.Id);
        using var publicClient = factory.CreateClient();
        var oldLogin = await publicClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", AccountApiFactory.AuthorPassword),
            JsonOptions);
        var newLogin = await publicClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", newPassword),
            JsonOptions);
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_DoesNotModifyPassword()
    {
        await using var factory = new AccountApiFactory();
        using var client = CreateAuthenticatedClient(factory, factory.Author.Id);

        var response = await client.PostAsJsonAsync(
            "/api/users/me/change-password",
            new ChangePasswordRequestDto("wrong-password", "Updated123!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
        Assert.Contains(error?.Errors ?? [], item =>
            item.Code == "CURRENT_PASSWORD_INVALID" && item.Field == "currentPassword");
        Assert.Empty(factory.Repository.AuditLogs);
        using var publicClient = factory.CreateClient();
        var login = await publicClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", AccountApiFactory.AuthorPassword),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsSameResponseForKnownUnknownAndInactiveAccounts()
    {
        await using var factory = new AccountApiFactory();
        using var client = factory.CreateClient();

        var known = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequestDto(factory.Author.Email),
            JsonOptions);
        var unknown = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequestDto("missing@example.test"),
            JsonOptions);
        var inactive = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequestDto(factory.Inactive.Email),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Accepted, known.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, inactive.StatusCode);
        Assert.Equal(
            await known.Content.ReadAsStringAsync(),
            await unknown.Content.ReadAsStringAsync());
        Assert.Equal(
            await known.Content.ReadAsStringAsync(),
            await inactive.Content.ReadAsStringAsync());
        Assert.Single(factory.Repository.ResetTokens);
        Assert.Single(factory.EmailService.Messages);
        Assert.DoesNotContain(
            factory.EmailService.Messages[0].RawToken,
            factory.Repository.ResetTokens[0].TokenHash,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetPassword_IsOneTimeAndAllowsLoginWithNewPassword()
    {
        await using var factory = new AccountApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequestDto(factory.Author.Email),
            JsonOptions);
        var rawToken = Assert.Single(factory.EmailService.Messages).RawToken;
        const string newPassword = "Recovered123!";

        var first = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequestDto(rawToken, newPassword),
            JsonOptions);
        var second = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordRequestDto(rawToken, "Second123!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.Single(factory.Repository.AuditLogs, log => log.ActionType == "ResetPassword");
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto("author", newPassword),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ConcurrentResetAttempts_OnlyOneCanConsumeToken()
    {
        await using var factory = new AccountApiFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequestDto(factory.Author.Email),
            JsonOptions);
        var rawToken = Assert.Single(factory.EmailService.Messages).RawToken;

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new ResetPasswordRequestDto(rawToken, "ConcurrentOne123!"),
                JsonOptions),
            client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new ResetPasswordRequestDto(rawToken, "ConcurrentTwo123!"),
                JsonOptions));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, response =>
            response.StatusCode == HttpStatusCode.UnprocessableEntity);
        Assert.Single(factory.Repository.AuditLogs, log => log.ActionType == "ResetPassword");
    }

    [Fact]
    public async Task ResetPassword_RejectsInvalidExpiredAndInactiveTokens()
    {
        await using var factory = new AccountApiFactory();
        using var client = factory.CreateClient();
        var expiredRaw = "expired-token";
        factory.Repository.ResetTokens.Add(new PasswordResetToken(
            factory.Author.Id,
            factory.TokenService.Hash(expiredRaw),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow.AddMinutes(-2)));
        var inactiveRaw = "inactive-token";
        factory.Repository.ResetTokens.Add(new PasswordResetToken(
            factory.Inactive.Id,
            factory.TokenService.Hash(inactiveRaw),
            DateTimeOffset.UtcNow.AddMinutes(30)));

        foreach (var token in new[] { "invalid-token", expiredRaw, inactiveRaw })
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/reset-password",
                new ResetPasswordRequestDto(token, "Recovered123!"),
                JsonOptions);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ErrorResponseDto>(JsonOptions);
            Assert.Equal("INVALID_RESET_TOKEN", error?.Code);
        }
    }

    private static HttpClient CreateAuthenticatedClient(AccountApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(TestAuthenticationHandler.AuthenticationScheme);
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

public sealed class AccountApiFactory : WebApplicationFactory<Program>
{
    public const string AuthorPassword = "User123!";

    public AccountApiFactory()
    {
        Admin = CreateUser("admin", "Admin", "admin@example.test", UserRole.Admin, "Admin123!");
        Author = CreateUser("author", "Author", "author@example.test", UserRole.User, AuthorPassword);
        Inactive = CreateUser(
            "inactive",
            "Inactive",
            "inactive@example.test",
            UserRole.User,
            "Inactive123!");
        Inactive.Deactivate();
        Repository = new InMemoryAccountRepository([Admin, Author, Inactive]);
    }

    public User Admin { get; }
    public User Author { get; }
    public User Inactive { get; }
    public InMemoryAccountRepository Repository { get; }
    public RecordingEmailService EmailService { get; } = new();
    public CryptographicPasswordResetTokenService TokenService { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("SeedData:Enabled", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAccountRepository>();
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IEmailService>();
            services.RemoveAll<IPasswordResetTokenService>();
            services.AddSingleton<IAccountRepository>(Repository);
            services.AddSingleton<IUserRepository>(Repository);
            services.AddSingleton<IEmailService>(EmailService);
            services.AddSingleton<IPasswordResetTokenService>(TokenService);
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.AuthenticationScheme;
                    options.DefaultForbidScheme = TestAuthenticationHandler.AuthenticationScheme;
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
        var hasher = new AspNetPasswordHashService();
        var hashSubject = new User(username, "pending-hash", fullName, email, role);
        return new User(
            username,
            hasher.HashPassword(hashSubject, password),
            fullName,
            email,
            role);
    }
}

public sealed class InMemoryAccountRepository(IReadOnlyList<User> seedUsers)
    : IAccountRepository, IUserRepository
{
    private readonly object _gate = new();
    public List<User> Users { get; } = [.. seedUsers];
    public List<PasswordResetToken> ResetTokens { get; } = [];
    public List<AuditLog> AuditLogs { get; } = [];

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Id == userId));

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        GetUserByIdAsync(userId, cancellationToken);

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Email == email));

    public Task<User?> FindByUsernameOrEmailAsync(
        string usernameOrEmail,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.SingleOrDefault(user =>
            user.Username == usernameOrEmail || user.Email == usernameOrEmail));

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Any(user => user.Username == username));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Any(user => user.Email == email));

    public Task<bool> EmailExistsAsync(
        string email,
        Guid excludingUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.Any(user => user.Email == email && user.Id != excludingUserId));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }

    public Task<PasswordResetCandidate?> FindPasswordResetCandidateAsync(
        string tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var token = ResetTokens.SingleOrDefault(candidate =>
            candidate.TokenHash == tokenHash && candidate.CanBeUsedAt(now));
        var result = token is null
            ? null
            : new PasswordResetCandidate(
                token.Id,
                token.TokenHash,
                Users.Single(user => user.Id == token.UserId));
        return Task.FromResult(result);
    }

    public void AddPasswordResetToken(PasswordResetToken token) => ResetTokens.Add(token);
    public void AddAuditLog(AuditLog auditLog) => AuditLogs.Add(auditLog);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> TryCompletePasswordResetAsync(
        Guid tokenId,
        string tokenHash,
        Guid userId,
        string passwordHash,
        DateTimeOffset usedAt,
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var token = ResetTokens.SingleOrDefault(candidate =>
                candidate.Id == tokenId
                && candidate.TokenHash == tokenHash
                && candidate.UserId == userId
                && candidate.CanBeUsedAt(usedAt));
            var user = Users.SingleOrDefault(candidate => candidate.Id == userId && candidate.IsActive);
            if (token is null || user is null)
            {
                return Task.FromResult(false);
            }

            token.MarkUsed(usedAt);
            user.ChangePasswordHash(passwordHash);
            AuditLogs.Add(auditLog);
            return Task.FromResult(true);
        }
    }
}

public sealed class RecordingEmailService : IEmailService
{
    public List<(string RecipientEmail, string RawToken)> Messages { get; } = [];

    public Task SendPasswordResetAsync(
        string recipientEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        Messages.Add((recipientEmail, rawToken));
        return Task.CompletedTask;
    }
}
