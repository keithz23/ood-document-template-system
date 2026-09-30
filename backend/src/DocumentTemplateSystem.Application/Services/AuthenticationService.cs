using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;
using DocumentTemplateSystem.Application.Interfaces;

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
            new UserDto(
                user.Id,
                user.Username,
                user.FullName,
                user.Email,
                user.Role));
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

        return new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Role);
    }
}
