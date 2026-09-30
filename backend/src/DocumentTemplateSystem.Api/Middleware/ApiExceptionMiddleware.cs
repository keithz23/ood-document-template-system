using System.Diagnostics;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Exceptions;

namespace DocumentTemplateSystem.Api.Middleware;

public sealed class ApiExceptionMiddleware(
    RequestDelegate next,
    ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UseCaseException exception)
        {
            await WriteErrorAsync(
                context,
                MapStatusCode(exception.Kind),
                exception.Code,
                exception.Title,
                exception.Detail,
                exception.Errors);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled API error for {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "INTERNAL_SERVER_ERROR",
                "An unexpected error occurred.",
                "Try the request again later.");
        }
    }

    public static Task WriteErrorAsync(
        HttpContext context,
        int status,
        string code,
        string title,
        string? detail = null,
        IReadOnlyList<ValidationErrorDto>? errors = null)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsJsonAsync(new ErrorResponseDto(
            status,
            code,
            title,
            detail,
            Activity.Current?.Id ?? context.TraceIdentifier,
            errors));
    }

    private static int MapStatusCode(UseCaseErrorKind kind) => kind switch
    {
        UseCaseErrorKind.Validation => StatusCodes.Status400BadRequest,
        UseCaseErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        UseCaseErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        UseCaseErrorKind.NotFound => StatusCodes.Status404NotFound,
        UseCaseErrorKind.Conflict => StatusCodes.Status409Conflict,
        UseCaseErrorKind.UnprocessableEntity => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };
}
