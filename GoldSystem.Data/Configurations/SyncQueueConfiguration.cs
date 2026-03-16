using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class SyncQueueConfiguration : IEntityTypeConfiguration<SyncQueue>
{
    public void Configure(EntityTypeBuilder<SyncQueue> builder)
    {
        builder.HasKey(s => s.QueueId);

        builder.Property(s => s.TableName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Operation)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(s => s.CreatedAt)
            .HasColumnType("datetime2");

        builder.Property(s => s.SyncedAt)
            .HasColumnType("datetime2");

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.CreatedAt);

        builder.HasOne(s => s.Branch)
            .WithMany(b => b.SyncQueues)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
