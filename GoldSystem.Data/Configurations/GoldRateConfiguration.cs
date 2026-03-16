using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class GoldRateConfiguration : IEntityTypeConfiguration<GoldRate>
{
    public void Configure(EntityTypeBuilder<GoldRate> builder)
    {
        builder.HasKey(r => r.RateId);

        builder.Property(r => r.RateDate)
            .HasColumnType("date");

        builder.Property(r => r.RateTime)
            .HasColumnType("time");

        builder.Property(r => r.Rate24K)
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.Rate22K)
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.Rate18K)
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.Source)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.OverrideNote)
            .HasMaxLength(200);

        builder.Property(r => r.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasOne(r => r.Branch)
            .WithMany(b => b.GoldRates)
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.CreatedByUser)
            .WithMany(u => u.GoldRates)
            .HasForeignKey(r => r.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(r => r.RateDate);
        builder.HasIndex(r => new { r.BranchId, r.RateDate });
    }
}
