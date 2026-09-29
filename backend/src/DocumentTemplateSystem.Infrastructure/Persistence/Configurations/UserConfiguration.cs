using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username).IsRequired();
        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.FullName).IsRequired();
        builder.Property(user => user.Email).IsRequired();
        builder.Property(user => user.Role)
            .HasConversion<string>()
            .IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.Role);
    }
}
