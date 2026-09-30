using DocumentTemplateSystem.Application.DTOs;

namespace DocumentTemplateSystem.Application.Exceptions;

public enum UseCaseErrorKind
{
    Validation,
    Unauthorized,
    NotFound,
    Conflict,
    UnprocessableEntity
}

public sealed class UseCaseException : Exception
{
    public UseCaseException(
        UseCaseErrorKind kind,
        string code,
        string title,
        string? detail = null,
        IReadOnlyList<ValidationErrorDto>? errors = null)
        : base(title)
    {
        Kind = kind;
        Code = code;
        Title = title;
        Detail = detail;
        Errors = errors;
    }

    public UseCaseErrorKind Kind { get; }

    public string Code { get; }

    public string Title { get; }

    public string? Detail { get; }

    public IReadOnlyList<ValidationErrorDto>? Errors { get; }

    public static UseCaseException Validation(params ValidationErrorDto[] errors) =>
        new(
            UseCaseErrorKind.Validation,
            "VALIDATION_FAILED",
            "One or more request values are invalid.",
            "Correct the reported values and try again.",
            errors);

    public static UseCaseException NotFound(string code, string title) =>
        new(UseCaseErrorKind.NotFound, code, title);

    public static UseCaseException Conflict(string code, string title) =>
        new(UseCaseErrorKind.Conflict, code, title);

    public static UseCaseException Unprocessable(string code, string title) =>
        new(UseCaseErrorKind.UnprocessableEntity, code, title);

    public static UseCaseException AuthenticationRequired() =>
        new(
            UseCaseErrorKind.Unauthorized,
            "AUTHENTICATION_REQUIRED",
            "Authentication is required.");

    public static UseCaseException InvalidCredentials() =>
        new(
            UseCaseErrorKind.Unauthorized,
            "INVALID_CREDENTIALS",
            "The supplied credentials are invalid.");
}
