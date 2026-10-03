using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DocumentTemplateSystem.Api.Authentication;
using DocumentTemplateSystem.Api.Health;
using DocumentTemplateSystem.Api.Middleware;
using DocumentTemplateSystem.Api.OpenApi;
using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Application.Services;
using DocumentTemplateSystem.Domain.Patterns.Strategy;
using DocumentTemplateSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace DocumentTemplateSystem.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";
    public const string PublicAuthenticationRateLimitPolicy = "PublicAuthentication";

    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter()));
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .SelectMany(entry => entry.Value?.Errors.Select(error =>
                        new ValidationErrorDto(
                            entry.Key,
                            "INVALID_VALUE",
                            string.IsNullOrWhiteSpace(error.ErrorMessage)
                                ? "The supplied value is invalid."
                                : error.ErrorMessage)) ?? [])
                    .ToArray();

                return new BadRequestObjectResult(new ErrorResponseDto(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_FAILED",
                    "One or more request values are invalid.",
                    "Correct the reported values and try again.",
                    context.HttpContext.TraceIdentifier,
                    errors));
            };
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Document Template System API",
                Version = "v1",
                Description = "Author document workflows and Phase 6 administration."
            });
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter a JWT bearer token."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
            options.OperationFilter<AllowAnonymousOperationFilter>();
        });
        services
            .AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>(
                "database",
                tags: ["ready"]);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<AdminCatalogService>();
        services.AddScoped<AdminTemplateVersionService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<AccountService>();
        services.AddScoped<PasswordRecoveryService>();
        services.AddSingleton<IDocumentRenderer, HtmlDocumentRenderer>();
        services.AddSingleton<PlaceholderValidator>();
        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Issuer)
                    && !string.IsNullOrWhiteSpace(settings.Audience)
                    && !string.IsNullOrWhiteSpace(settings.Key)
                    && Encoding.UTF8.GetByteCount(settings.Key) >= 32
                    && settings.ExpiresMinutes > 0,
                "Jwt configuration requires an issuer, audience, positive expiry, and a key of at least 32 bytes.")
            .ValidateOnStart();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var signingKey = configuration["Jwt:Key"];

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    NameClaimType = "unique_name",
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    IssuerSigningKey = string.IsNullOrWhiteSpace(signingKey)
                        ? null
                        : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        if (context.Response.HasStarted)
                        {
                            return;
                        }

                        context.HandleResponse();
                        var invalidToken = context.AuthenticateFailure is not null;
                        await ApiExceptionMiddleware.WriteErrorAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            invalidToken ? "INVALID_TOKEN" : "AUTHENTICATION_REQUIRED",
                            invalidToken
                                ? "The bearer token is invalid or expired."
                                : "Authentication is required.");
                    },
                    OnForbidden = context => ApiExceptionMiddleware.WriteErrorAsync(
                        context.HttpContext,
                        StatusCodes.Status403Forbidden,
                        "FORBIDDEN",
                        "The authenticated user is not permitted to perform this action.")
                };
            });
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(
                    permission,
                    policy => policy.RequireClaim(Permissions.ClaimType, permission));
            }
        });

        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .WithExposedHeaders("Content-Disposition");
            });
        });

        var permitLimit = Math.Max(
            1,
            configuration.GetValue(
                "RateLimiting:PublicAuthentication:PermitLimit",
                20));
        var windowSeconds = Math.Max(
            1,
            configuration.GetValue(
                "RateLimiting:PublicAuthentication:WindowSeconds",
                60));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(
                    MetadataName.RetryAfter,
                    out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(
                            retryAfter.TotalSeconds)
                        .ToString(CultureInfo.InvariantCulture);
                }

                await ApiExceptionMiddleware.WriteErrorAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "RATE_LIMIT_EXCEEDED",
                    "Too many authentication requests.",
                    "Wait before trying again.");
            };
            options.AddPolicy(
                PublicAuthenticationRateLimitPolicy,
                context => RateLimitPartition.GetFixedWindowLimiter(
                    $"{context.Connection.RemoteIpAddress}:{context.Request.Path.Value?.ToLowerInvariant()}",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        QueueLimit = 0,
                        Window = TimeSpan.FromSeconds(windowSeconds)
                    }));
        });

        return services;
    }
}
