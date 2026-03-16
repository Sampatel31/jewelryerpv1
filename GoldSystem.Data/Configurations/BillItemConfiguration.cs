using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class BillItemConfiguration : IEntityTypeConfiguration<BillItem>
{
    public void Configure(EntityTypeBuilder<BillItem> builder)
    {
        builder.HasKey(bi => bi.BillItemId);

        builder.Property(bi => bi.ItemName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(bi => bi.Purity)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(bi => bi.GrossWeight).HasColumnType("decimal(10,3)");
        builder.Property(bi => bi.StoneWeight).HasColumnType("decimal(10,3)");
        builder.Property(bi => bi.NetWeight).HasColumnType("decimal(10,3)");
        builder.Property(bi => bi.WastagePercent).HasColumnType("decimal(5,2)");
        builder.Property(bi => bi.WastageWeight).HasColumnType("decimal(10,3)");
        builder.Property(bi => bi.BillableWeight).HasColumnType("decimal(10,3)");
        builder.Property(bi => bi.PureGoldWeight).HasColumnType("decimal(10,3)");
        builder.Property(bi => bi.RateUsed24K).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.GoldValue).HasColumnType("decimal(18,2)");

        builder.Property(bi => bi.MakingType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(bi => bi.MakingValue).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.MakingAmount).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.StoneCharge).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.TaxableAmount).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.CGST_Amount).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.SGST_Amount).HasColumnType("decimal(18,2)");
        builder.Property(bi => bi.LineTotal).HasColumnType("decimal(18,2)");

        builder.HasOne(bi => bi.Bill)
            .WithMany(b => b.BillItems)
            .HasForeignKey(bi => bi.BillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bi => bi.Item)
            .WithMany(i => i.BillItems)
            .HasForeignKey(bi => bi.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
