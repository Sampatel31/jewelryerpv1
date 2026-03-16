using GoldSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GoldSystem.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.CategoryId);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.DefaultMakingType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.DefaultMakingValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.DefaultWastagePercent)
            .HasColumnType("decimal(5,2)");

        builder.Property(c => c.DefaultPurity)
            .IsRequired()
            .HasMaxLength(5);

        builder.HasIndex(c => c.SortOrder);
    }
}
