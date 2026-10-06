using DocumentTemplateSystem.Application.Interfaces;
using DocumentTemplateSystem.Infrastructure.Authentication;
using DocumentTemplateSystem.Infrastructure.Persistence;
using DocumentTemplateSystem.Infrastructure.Repositories;
using DocumentTemplateSystem.Infrastructure.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentTemplateSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured.");
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminCatalogRepository, AdminCatalogRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddSingleton<IPasswordHashService, AspNetPasswordHashService>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IPasswordResetTokenService, CryptographicPasswordResetTokenService>();
        var emailOptions = configuration.GetSection(EmailOptions.SectionName);
        services.Configure<EmailOptions>(emailOptions);
        var emailProvider = emailOptions.Get<EmailOptions>()?.Provider ?? EmailOptions.DevelopmentProvider;

        if (string.Equals(emailProvider, EmailOptions.SmtpProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IEmailService, SmtpEmailService>();
        }
        else if (string.Equals(emailProvider, EmailOptions.DevelopmentProvider, StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IEmailService, DevelopmentEmailService>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Email:Provider must be '{EmailOptions.DevelopmentProvider}' or '{EmailOptions.SmtpProvider}'.");
        }
        services.AddScoped<IHtmlContentSanitizer, AllowlistHtmlContentSanitizer>();
        services.AddSingleton(TimeProvider.System);
        services.Configure<DevelopmentEmailOptions>(
            configuration.GetSection(DevelopmentEmailOptions.SectionName));

        return services;
    }
}
