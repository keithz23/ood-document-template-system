using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class DocumentPlaceholderValueConfiguration
    : IEntityTypeConfiguration<DocumentPlaceholderValue>
{
    public void Configure(EntityTypeBuilder<DocumentPlaceholderValue> builder)
    {
        builder.ToTable("DocumentPlaceholderValues");
        builder.HasKey(value => value.Id);

        builder.Property(value => value.PlaceholderKeySnapshot).IsRequired();
        builder.Property(value => value.LabelSnapshot).IsRequired();
        builder.Property(value => value.DataTypeSnapshot)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(value => value.Value).IsRequired();

        builder.HasIndex(value => value.DocumentId);
        builder.HasIndex(value => value.PlaceholderId);

        builder.HasOne(value => value.Document)
            .WithMany(document => document.PlaceholderValues)
            .HasForeignKey(value => value.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(value => value.Placeholder)
            .WithMany(placeholder => placeholder.DocumentValues)
            .HasForeignKey(value => value.PlaceholderId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
