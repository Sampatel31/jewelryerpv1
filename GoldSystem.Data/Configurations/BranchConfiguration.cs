using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(b => b.BranchId);

        builder.Property(b => b.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.GSTIN)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(b => b.Phone)
            .IsRequired()
            .HasMaxLength(15);

        builder.Property(b => b.SqlConnectionString)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.LastSyncAt)
            .HasColumnType("datetime2");

        builder.HasIndex(b => b.Code)
            .IsUnique();
    }
}
