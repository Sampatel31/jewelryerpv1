using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(i => i.ItemId);

        builder.Property(i => i.HUID)
            .HasMaxLength(6);

        builder.Property(i => i.TagNo)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Purity)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(i => i.GrossWeight)
            .HasColumnType("decimal(10,3)");

        builder.Property(i => i.StoneWeight)
            .HasColumnType("decimal(10,3)");

        builder.Property(i => i.NetWeight)
            .HasColumnType("decimal(10,3)");

        builder.Property(i => i.PureGoldWeight)
            .HasColumnType("decimal(10,3)");

        builder.Property(i => i.MakingType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.MakingValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.WastagePercent)
            .HasColumnType("decimal(5,2)");

        builder.Property(i => i.PurchaseRate24K)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.CostPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.PurchaseDate)
            .HasColumnType("date");

        builder.Property(i => i.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(i => i.HUID)
            .IsUnique()
            .HasFilter("[HUID] IS NOT NULL");

        builder.HasIndex(i => i.TagNo);
        builder.HasIndex(i => i.Status);

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Branch)
            .WithMany(b => b.Items)
            .HasForeignKey(i => i.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Vendor)
            .WithMany(v => v.Items)
            .HasForeignKey(i => i.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.SoldBill)
            .WithMany(b => b.SoldItems)
            .HasForeignKey(i => i.SoldBillId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
