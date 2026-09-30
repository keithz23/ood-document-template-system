using DocumentTemplateSystem.Domain.Enums;

namespace DocumentTemplateSystem.Application.DTOs;

public sealed record LoginRequestDto(string Username, string Password);

public sealed record UserDto(
    Guid Id,
    string Username,
    string FullName,
    string Email,
    UserRole Role);

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserDto User);
