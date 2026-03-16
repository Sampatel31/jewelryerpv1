using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class OldGoldExchangeConfiguration : IEntityTypeConfiguration<OldGoldExchange>
{
    public void Configure(EntityTypeBuilder<OldGoldExchange> builder)
    {
        builder.HasKey(o => o.ExchangeId);

        builder.Property(o => o.Description)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.GrossWeight)
            .HasColumnType("decimal(10,3)");

        builder.Property(o => o.TestPurity)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(o => o.ExchangeRateApplied)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.ExchangeValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(o => o.Notes)
            .HasMaxLength(500);

        builder.Property(o => o.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasOne(o => o.Bill)
            .WithMany(b => b.OldGoldExchanges)
            .HasForeignKey(o => o.BillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
