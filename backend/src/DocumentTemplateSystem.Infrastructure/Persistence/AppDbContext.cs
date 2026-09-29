using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentTemplateSystem.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Template> Templates => Set<Template>();

    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();

    public DbSet<Placeholder> Placeholders => Set<Placeholder>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentPlaceholderValue> DocumentPlaceholderValues =>
        Set<DocumentPlaceholderValue>();

    public DbSet<User> Users => Set<User>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
