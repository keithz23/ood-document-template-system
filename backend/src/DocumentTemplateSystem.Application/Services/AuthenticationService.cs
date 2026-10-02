using System.Net.Mail;
using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.Services;

public sealed class AuthenticationService(
    IUserRepository userRepository,
    IPasswordHashService passwordHashService,
    IAccessTokenGenerator accessTokenGenerator,
    ICurrentUserContext currentUserContext)
{
    public async Task<LoginResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var usernameOrEmail = request.Username?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var validationErrors = new List<ValidationErrorDto>();

        if (usernameOrEmail.Length == 0)
        {
            validationErrors.Add(new ValidationErrorDto(
                "username",
                "REQUIRED",
                "Username or email is required."));
        }

        if (password.Length == 0)
        {
            validationErrors.Add(new ValidationErrorDto(
                "password",
                "REQUIRED",
                "Password is required."));
        }

        if (validationErrors.Count > 0)
        {
            throw UseCaseException.Validation([.. validationErrors]);
        }

        var user = await userRepository.FindByUsernameOrEmailAsync(
            usernameOrEmail,
            cancellationToken);

        if (user is null
            || !user.IsActive
            || !passwordHashService.VerifyPassword(user, password))
        {
            throw UseCaseException.InvalidCredentials();
        }

        var token = accessTokenGenerator.Generate(user);

        return new LoginResponseDto(
            token.AccessToken,
            "Bearer",
            token.ExpiresAt,
            MapUser(user));
    }

    public async Task<UserDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var username = request.Username?.Trim() ?? string.Empty;
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var errors = new List<ValidationErrorDto>();

        AddRequiredError(errors, "username", username);
        AddRequiredError(errors, "fullName", fullName);
        AddRequiredError(errors, "email", email);
        AddRequiredError(errors, "password", password);

        if (email.Length > 0 && !MailAddress.TryCreate(email, out _))
        {
            errors.Add(new ValidationErrorDto(
                "email",
                "INVALID_EMAIL",
                "Email must be a valid email address."));
        }

        if (errors.Count > 0)
        {
            throw UseCaseException.Validation([.. errors]);
        }

        if (await userRepository.UsernameExistsAsync(username, cancellationToken))
        {
            throw UseCaseException.Conflict(
                "USERNAME_ALREADY_EXISTS",
                "That username is already in use.");
        }

        if (await userRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw UseCaseException.Conflict(
                "EMAIL_ALREADY_EXISTS",
                "That email address is already in use.");
        }

        var hashSubject = new User(
            username,
            "pending-password-hash",
            fullName,
            email,
            UserRole.User);
        var passwordHash = passwordHashService.HashPassword(hashSubject, password);
        var user = new User(
            username,
            passwordHash,
            fullName,
            email,
            UserRole.User);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        return MapUser(user);
    }

    public async Task<UserDto> GetCurrentUserAsync(
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(
            currentUserContext.UserId,
            cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw UseCaseException.AuthenticationRequired();
        }

        return MapUser(user);
    }

    private static UserDto MapUser(User user) =>
        new(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role,
            Permissions.ForRole(user.Role));

    private static void AddRequiredError(
        ICollection<ValidationErrorDto> errors,
        string field,
        string value)
    {
        if (value.Length == 0)
        {
            errors.Add(new ValidationErrorDto(
                field,
                "REQUIRED",
                $"{field} is required."));
        }
    }
}
