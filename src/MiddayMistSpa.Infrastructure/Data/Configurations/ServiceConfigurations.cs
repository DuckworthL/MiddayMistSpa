using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Service;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class ServiceCategoryConfiguration : IEntityTypeConfiguration<ServiceCategory>
{
    public void Configure(EntityTypeBuilder<ServiceCategory> builder)
    {
        builder.ToTable("ServiceCategories");
        builder.HasKey(c => c.CategoryId);

        builder.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.CategoryName).IsUnique();
    }
}

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.ServiceId);

        builder.Property(s => s.ServiceCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.ServiceName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.TherapistCommissionRate).HasPrecision(5, 4);

        builder.HasIndex(s => s.ServiceCode).IsUnique();

        builder.HasOne(s => s.Category)
            .WithMany(c => c.Services)
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ServiceProductRequirementConfiguration : IEntityTypeConfiguration<ServiceProductRequirement>
{
    public void Configure(EntityTypeBuilder<ServiceProductRequirement> builder)
    {
        builder.ToTable("ServiceProductRequirements");
        builder.HasKey(r => r.RequirementId);

        builder.HasIndex(r => new { r.ServiceId, r.ProductId }).IsUnique();

        builder.HasOne(r => r.Service)
            .WithMany(s => s.ProductRequirements)
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Product)
            .WithMany(p => p.ServiceRequirements)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
