using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Inventory;

// ============================================================================
// Product Category DTOs
// ============================================================================

public class ProductCategoryResponse
{
    public int ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductCategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class UpdateProductCategoryRequest
{
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

// ============================================================================
// Product DTOs
// ============================================================================

public class ProductResponse
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;

    // Inventory
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    // Pricing
    public decimal CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal RetailCommissionRate { get; set; }

    // Tracking
    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }

    // Status
    public bool IsActive { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsExpired { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProductListResponse
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int ProductCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public bool IsLowStock { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsActive { get; set; }
}

public class CreateProductRequest
{
    [Required]
    public int ProductCategoryId { get; set; }

    [Required, StringLength(100)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(20)]
    public string ProductType { get; set; } = "Supply"; // Retail, Supply, Consumable

    [Range(0, double.MaxValue)]
    public decimal ReorderLevel { get; set; }

    [Required, StringLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Required, Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }

    [Range(0, 1)]
    public decimal RetailCommissionRate { get; set; } = 0.10m;

    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
}

public class UpdateProductRequest
{
    [Required]
    public int ProductCategoryId { get; set; }

    [Required, StringLength(100)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, StringLength(20)]
    public string ProductType { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal ReorderLevel { get; set; }

    [Required, StringLength(20)]
    public string UnitOfMeasure { get; set; } = string.Empty;

    [Required, Range(0, double.MaxValue)]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }

    [Range(0, 1)]
    public decimal RetailCommissionRate { get; set; }

    public DateTime? ExpiryDate { get; set; }
    public string? Supplier { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductSearchRequest
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? ProductType { get; set; }
    public bool? IsLowStock { get; set; }
    public bool? IsExpiringSoon { get; set; }
    public bool? IsActive { get; set; }
    /// <summary>"low" = low stock (>0 but <= reorder), "out" = 0 stock, "ok" = above reorder level</summary>
    public string? StockFilter { get; set; }
    public string? SortBy { get; set; } = "name";
    public bool SortDescending { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============================================================================
// Stock Adjustment DTOs
// ============================================================================

public class StockAdjustmentResponse
{
    public int AdjustmentId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = string.Empty;
    public decimal QuantityBefore { get; set; }
    public decimal QuantityChange { get; set; }
    public decimal QuantityAfter { get; set; }
    public string? Reason { get; set; }
    public string? ReferenceNumber { get; set; }
    public int AdjustedBy { get; set; }
    public string AdjustedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateStockAdjustmentRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required, StringLength(50)]
    public string AdjustmentType { get; set; } = string.Empty; // Received, Sold, Damaged, Expired, Service Usage, Audit

    [Required]
    public decimal QuantityChange { get; set; } // Positive or negative

    [StringLength(500)]
    public string? Reason { get; set; }

    [StringLength(50)]
    public string? ReferenceNumber { get; set; }
}

public class StockAdjustmentHistoryRequest
{
    public int? ProductId { get; set; }
    public string? AdjustmentType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============================================================================
// Supplier DTOs
// ============================================================================

public class SupplierResponse
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? TaxIdentificationNumber { get; set; }
    public string? PaymentTerms { get; set; }
    public int TotalOrders { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSupplierRequest
{
    [Required, StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(50)]
    public string? TaxIdentificationNumber { get; set; }

    [StringLength(50)]
    public string? PaymentTerms { get; set; } // Net 30, Net 60, COD, etc.
}

public class UpdateSupplierRequest
{
    [Required, StringLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(50)]
    public string? TaxIdentificationNumber { get; set; }

    [StringLength(50)]
    public string? PaymentTerms { get; set; }

    public bool IsActive { get; set; } = true;
}

// ============================================================================
// Purchase Order DTOs
// ============================================================================

public class PurchaseOrderResponse
{
    public int PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ApprovedBy { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? Notes { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<PurchaseOrderItemResponse> Items { get; set; } = new();
}

public class PurchaseOrderListResponse
{
    public int PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class PurchaseOrderItemResponse
{
    public int PurchaseOrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityPending { get; set; }
    public bool IsFullyReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}

public class CreatePurchaseOrderRequest
{
    [Required]
    public int SupplierId { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    [Required, MinLength(1)]
    public List<CreatePurchaseOrderItemRequest> Items { get; set; } = new();
}

public class CreatePurchaseOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal Quantity { get; set; }

    [Required, Range(0.01, double.MaxValue)]
    public decimal UnitCost { get; set; }
}

public class PurchaseOrderSearchRequest
{
    public string? SearchTerm { get; set; }
    public int? SupplierId { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ============================================================================
// Inventory Dashboard DTOs
// ============================================================================

public class InventoryDashboardResponse
{
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public List<ProductListResponse> LowStockProducts { get; set; } = new();
    public List<ProductListResponse> ExpiringSoonProducts { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> ByProductType { get; set; } = new();
}

public class InventoryValuationResponse
{
    public DateTime AsOfDate { get; set; }
    public int TotalProducts { get; set; }
    public decimal TotalUnits { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialProfit { get; set; }
    public List<CategoryValuationResponse> ByCategory { get; set; } = new();
}

public class CategoryValuationResponse
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public decimal TotalUnits { get; set; }
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
}
