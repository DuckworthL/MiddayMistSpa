using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Inventory;

namespace MiddayMistSpa.API.Services;

public interface IInventoryService
{
    // ========================================================================
    // Product Categories
    // ========================================================================
    Task<ProductCategoryResponse> CreateCategoryAsync(CreateProductCategoryRequest request);
    Task<ProductCategoryResponse?> GetCategoryByIdAsync(int categoryId);
    Task<List<ProductCategoryResponse>> GetAllCategoriesAsync(bool includeInactive = false);
    Task<ProductCategoryResponse> UpdateCategoryAsync(int categoryId, UpdateProductCategoryRequest request);
    Task<bool> DeleteCategoryAsync(int categoryId);

    // ========================================================================
    // Products
    // ========================================================================
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request);
    Task<ProductResponse?> GetProductByIdAsync(int productId);
    Task<ProductResponse?> GetProductByCodeAsync(string productCode);
    Task<PagedResponse<ProductListResponse>> SearchProductsAsync(ProductSearchRequest request);
    Task<List<ProductListResponse>> GetProductsByCategoryAsync(int categoryId);
    Task<List<ProductListResponse>> GetLowStockProductsAsync();
    Task<List<ProductListResponse>> GetExpiringSoonProductsAsync(int days = 30);
    Task<List<ProductListResponse>> GetExpiredProductsAsync();
    Task<List<ProductListResponse>> GetRetailProductsAsync();
    Task<ProductResponse> UpdateProductAsync(int productId, UpdateProductRequest request);
    Task<bool> DeactivateProductAsync(int productId);
    Task<bool> ReactivateProductAsync(int productId);

    // ========================================================================
    // Stock Management
    // ========================================================================
    Task<StockAdjustmentResponse> AdjustStockAsync(CreateStockAdjustmentRequest request, int adjustedBy);
    Task<StockAdjustmentResponse> ReceiveStockAsync(int productId, decimal quantity, string? referenceNumber, int adjustedBy);
    Task<StockAdjustmentResponse> DeductStockForServiceAsync(int productId, decimal quantity, int appointmentId, int adjustedBy);
    Task<StockAdjustmentResponse> DeductStockForSaleAsync(int productId, decimal quantity, int transactionId, int adjustedBy);
    Task<StockAdjustmentResponse> WriteOffExpiredAsync(int productId, int adjustedBy);
    Task<StockAdjustmentResponse> WriteOffDamagedAsync(int productId, decimal quantity, string reason, int adjustedBy);
    Task<PagedResponse<StockAdjustmentResponse>> GetStockAdjustmentHistoryAsync(StockAdjustmentHistoryRequest request);
    Task<List<StockAdjustmentResponse>> GetProductAdjustmentHistoryAsync(int productId);

    // ========================================================================
    // Suppliers
    // ========================================================================
    Task<SupplierResponse> CreateSupplierAsync(CreateSupplierRequest request);
    Task<SupplierResponse?> GetSupplierByIdAsync(int supplierId);
    Task<List<SupplierResponse>> GetAllSuppliersAsync(bool includeInactive = false);
    Task<SupplierResponse> UpdateSupplierAsync(int supplierId, UpdateSupplierRequest request);
    Task<bool> DeactivateSupplierAsync(int supplierId);

    // ========================================================================
    // Purchase Orders
    // ========================================================================
    Task<PurchaseOrderResponse> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, int createdBy);
    Task<PurchaseOrderResponse?> GetPurchaseOrderByIdAsync(int purchaseOrderId);
    Task<PurchaseOrderResponse?> GetPurchaseOrderByNumberAsync(string poNumber);
    Task<PagedResponse<PurchaseOrderListResponse>> SearchPurchaseOrdersAsync(PurchaseOrderSearchRequest request);
    Task<List<PurchaseOrderListResponse>> GetPendingPurchaseOrdersAsync();
    Task<PurchaseOrderResponse> ApprovePurchaseOrderAsync(int purchaseOrderId, int approvedBy);
    Task<PurchaseOrderResponse> ReceivePurchaseOrderAsync(int purchaseOrderId, int receivedBy);
    Task<PurchaseOrderResponse> CancelPurchaseOrderAsync(int purchaseOrderId);

    // ========================================================================
    // Dashboard & Reports
    // ========================================================================
    Task<InventoryDashboardResponse> GetDashboardAsync();
    Task<InventoryValuationResponse> GetInventoryValuationAsync();
    Task<decimal> GetTotalInventoryValueAsync();
}
