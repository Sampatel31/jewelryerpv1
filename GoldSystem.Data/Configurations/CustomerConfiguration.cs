using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.CustomerId);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Phone)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.GSTIN)
            .HasMaxLength(15);

        builder.Property(c => c.LoyaltyPoints)
            .HasDefaultValue(0);

        builder.Property(c => c.TotalPurchased)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(c => c.CreditLimit)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(0m);

        builder.Property(c => c.CreatedAt)
            .HasColumnType("datetime2");

        builder.HasIndex(c => c.Phone)
            .IsUnique();

        builder.HasOne(c => c.Branch)
            .WithMany(b => b.Customers)
            .HasForeignKey(c => c.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
