using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Seeding;

public static class DevelopmentDataSeeder
{
    private const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEMk0+ncQn/We9Q3wXpGkvnIF2jxr9rC127s0NF30RP9TSPHpOibwkAoKdrmHT39tOA==";

    private const string UserPasswordHash =
        "AQAAAAIAAYagAAAAEHWwMFFzx+OV3jpNHT6n9tvXljGbE0d5n7geelvozhm4QsQRRfB+v3MalV249xjcbA==";

    public static async Task SeedDevelopmentDataAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await HasExistingDataAsync(context, cancellationToken))
        {
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken);

        var createdAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var admin = new User(
            "admin",
            AdminPasswordHash,
            "Development Admin",
            "admin@example.test",
            UserRole.Admin,
            createdAt);
        var user = new User(
            "author",
            UserPasswordHash,
            "Development Author",
            "author@example.test",
            UserRole.User,
            createdAt);

        var business = new Category("Business", admin.Id, createdAt);
        var finance = new Category("Finance", admin.Id, createdAt);
        var humanResources = new Category("Human resources", admin.Id, createdAt);
        var operations = new Category("Operations", admin.Id, createdAt);

        var agreement = CreatePublishedTemplate(
            "Professional services agreement",
            business.Id,
            admin.Id,
            "<h1>Professional services agreement</h1><p>This agreement is made on {{effective_date}} between {{provider_name}} and {{client_name}}.</p>",
            createdAt,
            version =>
            {
                version.AddPlaceholder(
                    "effective_date",
                    "Effective date",
                    PlaceholderDataType.Date,
                    true);
                version.AddPlaceholder(
                    "provider_name",
                    "Provider name",
                    PlaceholderDataType.Text,
                    true);
                version.AddPlaceholder(
                    "client_name",
                    "Client name",
                    PlaceholderDataType.Text,
                    true);
                version.AddPlaceholder(
                    "client_email",
                    "Client email",
                    PlaceholderDataType.Email,
                    false);
            });

        var invoice = CreatePublishedTemplate(
            "Service invoice",
            finance.Id,
            admin.Id,
            "<h1>Invoice {{invoice_number}}</h1><p>Issued {{issue_date}} to {{customer_name}} for {{amount}}.</p>",
            createdAt,
            version =>
            {
                version.AddPlaceholder(
                    "invoice_number",
                    "Invoice number",
                    PlaceholderDataType.Text,
                    true);
                version.AddPlaceholder(
                    "issue_date",
                    "Issue date",
                    PlaceholderDataType.Date,
                    true);
                version.AddPlaceholder(
                    "customer_name",
                    "Customer name",
                    PlaceholderDataType.Text,
                    true);
                version.AddPlaceholder(
                    "amount",
                    "Amount",
                    PlaceholderDataType.Number,
                    true);
            });

        var curriculumVitae = CreatePublishedTemplate(
            "Curriculum vitae",
            humanResources.Id,
            admin.Id,
            "<h1>{{full_name}}</h1><p>{{email}}</p><section>{{professional_summary}}</section>",
            createdAt,
            version =>
            {
                version.AddPlaceholder(
                    "full_name",
                    "Full name",
                    PlaceholderDataType.Text,
                    true);
                version.AddPlaceholder(
                    "email",
                    "Email",
                    PlaceholderDataType.Email,
                    true);
                version.AddPlaceholder(
                    "professional_summary",
                    "Professional summary",
                    PlaceholderDataType.Text,
                    false);
            });

        var report = CreatePublishedTemplate(
            "Project status report",
            operations.Id,
            admin.Id,
            "<h1>{{project_name}} status report</h1><p>Reporting date: {{reporting_date}}</p><section>{{summary}}</section>",
            createdAt,
            version =>
            {
                version.AddPlaceholder(
                    "project_name",
                    "Project name",
                    PlaceholderDataType.Text,
                    true);
                version.AddPlaceholder(
                    "reporting_date",
                    "Reporting date",
                    PlaceholderDataType.Date,
                    true);
                version.AddPlaceholder(
                    "summary",
                    "Summary",
                    PlaceholderDataType.Text,
                    true);
            });

        context.Users.AddRange(admin, user);
        context.Categories.AddRange(business, finance, humanResources, operations);
        context.Templates.AddRange(agreement, invoice, curriculumVitae, report);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> HasExistingDataAsync(
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        return await context.Users.AnyAsync(cancellationToken)
            || await context.Categories.AnyAsync(cancellationToken)
            || await context.Templates.AnyAsync(cancellationToken);
    }

    private static Template CreatePublishedTemplate(
        string name,
        Guid categoryId,
        Guid createdBy,
        string content,
        DateTimeOffset createdAt,
        Action<TemplateVersion> configurePlaceholders)
    {
        var template = new Template(
            name,
            categoryId,
            createdBy,
            content,
            ContentFormat.Html,
            createdAt);
        var version = template.Versions.Single();

        configurePlaceholders(version);
        version.Publish(createdBy, createdAt);
        template.SetCurrentVersion(version.Id);
        template.Activate();

        return template;
    }
}
