using DocumentTemplateSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentTemplateSystem.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.ActionType).IsRequired();
        builder.Property(log => log.EntityType).IsRequired();
        builder.Property(log => log.Description).IsRequired();
        builder.Property(log => log.CreatedAt).IsRequired();

        builder.HasIndex(log => log.PerformedBy);
        builder.HasIndex(log => new { log.EntityType, log.EntityId });
        builder.HasIndex(log => log.CreatedAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(log => log.PerformedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
