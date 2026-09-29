using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");
        builder.HasKey(document => document.Id);

        builder.Property(document => document.Title).IsRequired();
        builder.Property(document => document.Content).IsRequired();
        builder.Property(document => document.Status)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(document => document.CreatedAt).IsRequired();
        builder.Property(document => document.UpdatedAt).IsRequired();

        builder.HasIndex(document => document.TemplateVersionId);
        builder.HasIndex(document => document.CreatedBy);
        builder.HasIndex(document => document.Status);
        builder.HasIndex(document => document.UpdatedAt);

        builder.HasOne(document => document.TemplateVersion)
            .WithMany(version => version.Documents)
            .HasForeignKey(document => document.TemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => document.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(document => document.PlaceholderValues)
            .WithOne(value => value.Document)
            .HasForeignKey(value => value.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(document => document.PlaceholderValues)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
