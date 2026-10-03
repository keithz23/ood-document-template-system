namespace DocumentTemplateSystem.Api.Middleware;

public sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IHostEnvironment environment)
{
    private const string ContentSecurityPolicy =
        "default-src 'none'; base-uri 'none'; form-action 'none'; "
        + "frame-ancestors 'none'; img-src http: https:; style-src 'unsafe-inline'";

    public Task InvokeAsync(HttpContext context)
    {
        if (environment.IsDevelopment()
            && context.Request.Path.StartsWithSegments("/swagger"))
        {
            return next(context);
        }

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("Content-Security-Policy", ContentSecurityPolicy);
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd(
                "Permissions-Policy",
                "camera=(), geolocation=(), microphone=(), payment=(), usb=()");
            return Task.CompletedTask;
        });

        return next(context);
    }
}
