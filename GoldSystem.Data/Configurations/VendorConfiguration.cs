using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(v => v.VendorId);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Phone)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(v => v.Address)
            .HasMaxLength(500);

        builder.Property(v => v.GSTIN)
            .HasMaxLength(15);

        builder.Property(v => v.OpeningBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(v => v.CreatedAt)
            .HasColumnType("datetime2");
    }
}
