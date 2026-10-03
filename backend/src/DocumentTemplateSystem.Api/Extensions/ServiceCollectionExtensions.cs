using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using DocumentTemplateSystem.Api.Authentication;
using DocumentTemplateSystem.Api.Middleware;
using DocumentTemplateSystem.Api.OpenApi;
using DocumentTemplateSystem.Application.Authorization;
using DocumentTemplateSystem.Application.DTOs;
using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Application.Services;
using DocumentTemplateSystem.Domain.Patterns.Strategy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace DocumentTemplateSystem.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string FrontendCorsPolicy = "Frontend";

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
        services.AddHealthChecks();
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

        return services;
    }
}
