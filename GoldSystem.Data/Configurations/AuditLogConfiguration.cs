using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.LogId);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.TableName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.OldValueJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.NewValueJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(a => new { a.TableName, a.RecordId });
        builder.HasIndex(a => a.CreatedAt);

        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Branch)
            .WithMany(b => b.AuditLogs)
            .HasForeignKey(a => a.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // AuditLog is append-only; restrict modifications at the application level
        builder.ToTable(tb => tb.HasComment("Append-only audit log. No updates or deletes permitted."));
    }
}
