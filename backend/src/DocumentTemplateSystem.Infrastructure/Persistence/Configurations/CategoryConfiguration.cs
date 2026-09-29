using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();
        builder.Property(category => category.CreatedAt).IsRequired();

        builder.HasIndex(category => category.CreatedBy);
        builder.HasIndex(category => category.IsActive);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(category => category.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(category => category.Templates)
            .WithOne(template => template.Category)
            .HasForeignKey(template => template.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(category => category.Templates)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
