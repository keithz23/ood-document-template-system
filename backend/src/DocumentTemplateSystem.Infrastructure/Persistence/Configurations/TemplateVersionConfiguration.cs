using DocumentTemplateSystem.Domain.Entities;
using DocumentTemplateSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.ToTable(
            "TemplateVersions",
            table => table.HasCheckConstraint(
                "CK_TemplateVersions_CurrentRequiresPublished",
                $"\"IsCurrent\" = FALSE OR \"Status\" = '{VersionStatus.Published}'"));

        builder.HasKey(version => version.Id);

        builder.Property(version => version.VersionNumber).IsRequired();
        builder.Property(version => version.Content).IsRequired();
        builder.Property(version => version.ContentFormat)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(version => version.Status)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(version => version.IsCurrent).IsRequired();
        builder.Property(version => version.CreatedAt).IsRequired();
        builder.Property(version => version.UpdatedAt).IsRequired();

        builder.HasIndex(version => new { version.TemplateId, version.VersionNumber })
            .IsUnique();
        builder.HasIndex(version => version.TemplateId)
            .IsUnique()
            .HasFilter("\"IsCurrent\" = TRUE");
        builder.HasIndex(version => version.CreatedBy);
        builder.HasIndex(version => version.PublishedBy);
        builder.HasIndex(version => version.Status);

        builder.HasOne(version => version.Template)
            .WithMany(template => template.Versions)
            .HasForeignKey(version => version.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(version => version.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(version => version.PublishedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(version => version.Placeholders)
            .WithOne(placeholder => placeholder.TemplateVersion)
            .HasForeignKey(placeholder => placeholder.TemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(version => version.Documents)
            .WithOne(document => document.TemplateVersion)
            .HasForeignKey(document => document.TemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(version => version.Placeholders)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(version => version.Documents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
