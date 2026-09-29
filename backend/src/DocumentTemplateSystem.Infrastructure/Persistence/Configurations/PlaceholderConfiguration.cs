using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class PlaceholderConfiguration : IEntityTypeConfiguration<Placeholder>
{
    public void Configure(EntityTypeBuilder<Placeholder> builder)
    {
        builder.ToTable("Placeholders");
        builder.HasKey(placeholder => placeholder.Id);

        builder.Property(placeholder => placeholder.Key).IsRequired();
        builder.Property(placeholder => placeholder.Label).IsRequired();
        builder.Property(placeholder => placeholder.DataType)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(placeholder => placeholder.IsRequired).IsRequired();

        builder.HasIndex(placeholder => new
            {
                placeholder.TemplateVersionId,
                placeholder.Key
            })
            .IsUnique();
        builder.HasIndex(placeholder => placeholder.TemplateVersionId);

        builder.HasOne(placeholder => placeholder.TemplateVersion)
            .WithMany(version => version.Placeholders)
            .HasForeignKey(placeholder => placeholder.TemplateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(placeholder => placeholder.DocumentValues)
            .WithOne(value => value.Placeholder)
            .HasForeignKey(value => value.PlaceholderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(placeholder => placeholder.DocumentValues)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
