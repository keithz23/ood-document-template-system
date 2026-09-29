using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("Templates");
        builder.HasKey(template => template.Id);

        builder.Property(template => template.Name).IsRequired();
        builder.Property(template => template.Status)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(template => template.CreatedAt).IsRequired();

        builder.HasIndex(template => template.CategoryId);
        builder.HasIndex(template => template.CreatedBy);
        builder.HasIndex(template => template.Status);

        builder.HasOne(template => template.Category)
            .WithMany(category => category.Templates)
            .HasForeignKey(template => template.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(template => template.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(template => template.Versions)
            .WithOne(version => version.Template)
            .HasForeignKey(version => version.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(template => template.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
