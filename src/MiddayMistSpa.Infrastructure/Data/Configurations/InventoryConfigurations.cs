using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiddayMistSpa.Core.Entities.Inventory;

namespace MiddayMistSpa.Infrastructure.Data.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(c => c.ProductCategoryId);

        builder.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.CategoryName).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.ProductId);

        builder.Property(p => p.ProductCode).HasMaxLength(50).IsRequired();
        builder.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.ProductType).HasMaxLength(50).IsRequired();
        builder.Property(p => p.UnitOfMeasure).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Supplier).HasMaxLength(200);
        builder.Property(p => p.RetailCommissionRate).HasPrecision(5, 4);
        builder.Property(p => p.CurrentStock).HasPrecision(18, 3);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 3);

        builder.HasIndex(p => p.ProductCode).IsUnique();
        builder.HasIndex(p => p.CurrentStock);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.ProductCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(p => p.IsLowStock);
        builder.Ignore(p => p.IsExpiringSoon);
        builder.Ignore(p => p.IsExpired);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers");
        builder.HasKey(s => s.SupplierId);

        builder.Property(s => s.SupplierCode).HasMaxLength(50).IsRequired();
        builder.Property(s => s.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.ContactPerson).HasMaxLength(100);
        builder.Property(s => s.Email).HasMaxLength(256);
        builder.Property(s => s.PhoneNumber).HasMaxLength(50);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.Province).HasMaxLength(100);
        builder.Property(s => s.TINNumber).HasMaxLength(20);
        builder.Property(s => s.PaymentTerms).HasMaxLength(100);

        builder.HasIndex(s => s.SupplierCode).IsUnique();
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("PurchaseOrders");
        builder.HasKey(p => p.PurchaseOrderId);

        builder.Property(p => p.PONumber).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasIndex(p => p.PONumber).IsUnique();
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Supplier)
            .WithMany(s => s.PurchaseOrders)
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ApprovedByUser)
            .WithMany()
            .HasForeignKey(p => p.ApprovedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.CreatedByUser)
            .WithMany()
            .HasForeignKey(p => p.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
    {
        builder.ToTable("PurchaseOrderItems");
        builder.HasKey(i => i.POItemId);

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.QuantityReceived).HasPrecision(18, 3);

        builder.HasOne(i => i.PurchaseOrder)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(i => i.QuantityPending);
        builder.Ignore(i => i.IsFullyReceived);
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");
        builder.HasKey(a => a.AdjustmentId);

        builder.Property(a => a.AdjustmentType).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.Property(a => a.ReferenceNumber).HasMaxLength(100);
        builder.Property(a => a.QuantityBefore).HasPrecision(18, 3);
        builder.Property(a => a.QuantityChange).HasPrecision(18, 3);
        builder.Property(a => a.QuantityAfter).HasPrecision(18, 3);

        builder.HasOne(a => a.Product)
            .WithMany(p => p.StockAdjustments)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AdjustedByUser)
            .WithMany()
            .HasForeignKey(a => a.AdjustedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductBatchConfiguration : IEntityTypeConfiguration<ProductBatch>
{
    public void Configure(EntityTypeBuilder<ProductBatch> builder)
    {
        builder.ToTable("ProductBatches");
        builder.HasKey(b => b.ProductBatchId);

        builder.Property(b => b.BatchNumber).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Notes).HasMaxLength(500);
        builder.Property(b => b.QuantityReceived).HasPrecision(18, 3);
        builder.Property(b => b.QuantityRemaining).HasPrecision(18, 3);

        builder.HasIndex(b => b.ProductId);
        builder.HasIndex(b => b.ExpiryDate);
        builder.HasIndex(b => b.BatchNumber);

        builder.HasOne(b => b.Product)
            .WithMany(p => p.Batches)
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.PurchaseOrderItem)
            .WithMany()
            .HasForeignKey(b => b.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(b => b.Supplier)
            .WithMany()
            .HasForeignKey(b => b.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(b => b.IsExpired);
        builder.Ignore(b => b.IsExpiringSoon);
        builder.Ignore(b => b.IsFullyConsumed);
    }
}
