using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class BillConfiguration : IEntityTypeConfiguration<Bill>
{
    public void Configure(EntityTypeBuilder<Bill> builder)
    {
        builder.HasKey(b => b.BillId);

        builder.Property(b => b.BillNo)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.BillDate)
            .HasColumnType("date");

        builder.Property(b => b.RateSnapshot22K).HasColumnType("decimal(18,2)");
        builder.Property(b => b.RateSnapshot24K).HasColumnType("decimal(18,2)");
        builder.Property(b => b.GoldValue).HasColumnType("decimal(18,2)");
        builder.Property(b => b.MakingAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.WastageAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.StoneCharge).HasColumnType("decimal(18,2)");
        builder.Property(b => b.SubTotal).HasColumnType("decimal(18,2)");
        builder.Property(b => b.DiscountAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.TaxableAmount).HasColumnType("decimal(18,2)");
        builder.Property(b => b.CGST).HasColumnType("decimal(18,2)");
        builder.Property(b => b.SGST).HasColumnType("decimal(18,2)");
        builder.Property(b => b.IGST).HasColumnType("decimal(18,2)");
        builder.Property(b => b.RoundOff).HasColumnType("decimal(18,2)");
        builder.Property(b => b.GrandTotal).HasColumnType("decimal(18,2)");
        builder.Property(b => b.ExchangeValue).HasColumnType("decimal(18,2)");
        builder.Property(b => b.AmountPaid).HasColumnType("decimal(18,2)");
        builder.Property(b => b.BalanceDue).HasColumnType("decimal(18,2)");

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.PaymentMode)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(b => b.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(b => b.BillNo)
            .IsUnique();

        builder.HasIndex(b => b.BillDate);
        builder.HasIndex(b => b.Status);

        builder.HasOne(b => b.Customer)
            .WithMany(c => c.Bills)
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Branch)
            .WithMany(br => br.Bills)
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Bills)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
