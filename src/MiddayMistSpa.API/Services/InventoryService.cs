using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Inventory;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Inventory;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class InventoryService : IInventoryService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<InventoryService> _logger;
    private readonly IAccountingService _accountingService;
    private readonly INotificationService _notificationService;

    public InventoryService(SpaDbContext context, ILogger<InventoryService> logger, IAccountingService accountingService, INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _accountingService = accountingService;
        _notificationService = notificationService;
    }

    #region Product Categories

    public async Task<ProductCategoryResponse> CreateCategoryAsync(CreateProductCategoryRequest request)
    {
        var category = new ProductCategory
        {
            CategoryName = request.CategoryName,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductCategories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created product category: {CategoryName}", category.CategoryName);

        return new ProductCategoryResponse
        {
            ProductCategoryId = category.ProductCategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            ProductCount = 0,
            IsActive = category.IsActive
        };
    }

    public async Task<ProductCategoryResponse?> GetCategoryByIdAsync(int categoryId)
    {
        var category = await _context.ProductCategories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.ProductCategoryId == categoryId);

        if (category == null) return null;

        return MapToCategoryResponse(category);
    }

    public async Task<List<ProductCategoryResponse>> GetAllCategoriesAsync(bool includeInactive = false)
    {
        var query = _context.ProductCategories
            .AsNoTracking()
            .Include(c => c.Products)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.CategoryName)
            .Select(c => new ProductCategoryResponse
            {
                ProductCategoryId = c.ProductCategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                ProductCount = c.Products.Count(p => p.IsActive),
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<ProductCategoryResponse> UpdateCategoryAsync(int categoryId, UpdateProductCategoryRequest request)
    {
        var category = await _context.ProductCategories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.ProductCategoryId == categoryId)
            ?? throw new InvalidOperationException($"Category with ID {categoryId} not found");

        category.CategoryName = request.CategoryName;
        category.Description = request.Description;
        category.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return MapToCategoryResponse(category);
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.ProductCategories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.ProductCategoryId == categoryId);

        if (category == null) return false;

        if (category.Products.Any())
            throw new InvalidOperationException("Cannot delete category with existing products");

        _context.ProductCategories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Products

    public async Task<ProductResponse> CreateProductAsync(CreateProductRequest request)
    {
        var category = await _context.ProductCategories.FindAsync(request.ProductCategoryId)
            ?? throw new InvalidOperationException($"Category with ID {request.ProductCategoryId} not found");

        // Generate product code
        var lastProduct = await _context.Products.OrderByDescending(p => p.ProductId).FirstOrDefaultAsync();
        var nextNumber = (lastProduct?.ProductId ?? 0) + 1;
        var productCode = $"PRD-{nextNumber:D4}";

        var product = new Product
        {
            ProductCode = productCode,
            ProductCategoryId = request.ProductCategoryId,
            ProductName = request.ProductName,
            Brand = request.Brand,
            Description = request.Description,
            ProductType = request.ProductType,
            CurrentStock = 0,
            ReorderLevel = request.ReorderLevel,
            UnitOfMeasure = request.UnitOfMeasure,
            CostPrice = request.CostPrice,
            SellingPrice = request.SellingPrice,
            RetailCommissionRate = request.RetailCommissionRate,
            ExpiryDate = request.ExpiryDate,
            SupplierId = request.SupplierId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created product {ProductCode}: {ProductName}", product.ProductCode, product.ProductName);

        return MapToProductResponse(product, category.CategoryName);
    }

    public async Task<ProductResponse?> GetProductByIdAsync(int productId)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        return product == null ? null : MapToProductResponse(product, product.Category.CategoryName);
    }

    public async Task<ProductResponse?> GetProductByCodeAsync(string productCode)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductCode == productCode);

        return product == null ? null : MapToProductResponse(product, product.Category.CategoryName);
    }

    public async Task<PagedResponse<ProductListResponse>> SearchProductsAsync(ProductSearchRequest request)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(term) ||
                p.ProductCode.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.ProductCategoryId == request.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(request.ProductType))
            query = query.Where(p => p.ProductType == request.ProductType);

        if (request.IsLowStock.HasValue && request.IsLowStock.Value)
            query = query.Where(p => p.CurrentStock <= p.ReorderLevel);

        if (!string.IsNullOrWhiteSpace(request.StockFilter))
        {
            query = request.StockFilter.ToLower() switch
            {
                "out" => query.Where(p => p.CurrentStock == 0),
                "low" => query.Where(p => p.CurrentStock > 0 && p.ReorderLevel > 0 && p.CurrentStock <= p.ReorderLevel),
                "ok" => query.Where(p => p.CurrentStock > p.ReorderLevel || (p.ReorderLevel == 0 && p.CurrentStock > 0)),
                _ => query
            };
        }

        if (request.IsExpiringSoon.HasValue && request.IsExpiringSoon.Value)
            query = query.Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30));

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        query = request.SortBy?.ToLower() switch
        {
            "code" => request.SortDescending ? query.OrderByDescending(p => p.ProductCode) : query.OrderBy(p => p.ProductCode),
            "stock" => request.SortDescending ? query.OrderByDescending(p => p.CurrentStock) : query.OrderBy(p => p.CurrentStock),
            "category" => request.SortDescending ? query.OrderByDescending(p => p.Category.CategoryName) : query.OrderBy(p => p.Category.CategoryName),
            _ => request.SortDescending ? query.OrderByDescending(p => p.ProductName) : query.OrderBy(p => p.ProductName)
        };

        // When a stock filter is active and no explicit sort is chosen, sort ascending by stock level
        if (!string.IsNullOrWhiteSpace(request.StockFilter) && (request.SortBy == null || request.SortBy == "name"))
            query = query.OrderBy(p => p.CurrentStock).ThenBy(p => p.ProductName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => MapToProductListResponse(p))
            .ToListAsync();

        return new PagedResponse<ProductListResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<ProductListResponse>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.ProductCategoryId == categoryId && p.IsActive)
            .OrderBy(p => p.ProductName)
            .Select(p => MapToProductListResponse(p))
            .ToListAsync();
    }

    public async Task<List<ProductListResponse>> GetLowStockProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CurrentStock <= p.ReorderLevel)
            .OrderBy(p => p.CurrentStock)
            .Select(p => MapToProductListResponse(p))
            .ToListAsync();
    }

    public async Task<List<ProductListResponse>> GetExpiringSoonProductsAsync(int days = 30)
    {
        var expiryDate = DateTime.UtcNow.AddDays(days);
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ExpiryDate.HasValue && p.ExpiryDate.Value <= expiryDate && p.ExpiryDate.Value > DateTime.UtcNow)
            .OrderBy(p => p.ExpiryDate)
            .Select(p => MapToProductListResponse(p))
            .ToListAsync();
    }

    public async Task<List<ProductListResponse>> GetExpiredProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ExpiryDate.HasValue && p.ExpiryDate.Value < DateTime.UtcNow)
            .OrderBy(p => p.ExpiryDate)
            .Select(p => MapToProductListResponse(p))
            .ToListAsync();
    }

    public async Task<List<ProductListResponse>> GetRetailProductsAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.ProductType == "Retail" && p.CurrentStock > 0)
            .OrderBy(p => p.ProductName)
            .Select(p => MapToProductListResponse(p))
            .ToListAsync();
    }

    public async Task<ProductResponse> UpdateProductAsync(int productId, UpdateProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.ProductId == productId)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found");

        var category = await _context.ProductCategories.FindAsync(request.ProductCategoryId)
            ?? throw new InvalidOperationException($"Category with ID {request.ProductCategoryId} not found");

        product.ProductCategoryId = request.ProductCategoryId;
        product.ProductName = request.ProductName;
        product.Brand = request.Brand;
        product.Description = request.Description;
        product.ProductType = request.ProductType;
        product.ReorderLevel = request.ReorderLevel;
        product.UnitOfMeasure = request.UnitOfMeasure;
        product.CostPrice = request.CostPrice;
        product.SellingPrice = request.SellingPrice;
        product.RetailCommissionRate = request.RetailCommissionRate;
        product.ExpiryDate = request.ExpiryDate;
        product.SupplierId = request.SupplierId;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToProductResponse(product, category.CategoryName);
    }

    public async Task<bool> DeactivateProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReactivateProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return false;

        product.IsActive = true;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Stock Management

    public async Task<StockAdjustmentResponse> AdjustStockAsync(CreateStockAdjustmentRequest request, int adjustedBy)
    {
        if (!DomainConstants.StockAdjustmentTypes.IsValid(request.AdjustmentType))
            throw new InvalidOperationException($"Invalid adjustment type: '{request.AdjustmentType}'. Valid types: {string.Join(", ", DomainConstants.StockAdjustmentTypes.All)}");

        var product = await _context.Products.FindAsync(request.ProductId)
            ?? throw new InvalidOperationException($"Product with ID {request.ProductId} not found");

        var user = await _context.Users.FindAsync(adjustedBy)
            ?? throw new InvalidOperationException($"User with ID {adjustedBy} not found");

        var quantityBefore = product.CurrentStock;
        var quantityAfter = quantityBefore + request.QuantityChange;

        if (quantityAfter < 0)
            throw new InvalidOperationException("Insufficient stock for this adjustment");

        var adjustment = new StockAdjustment
        {
            ProductId = request.ProductId,
            AdjustmentType = request.AdjustmentType,
            QuantityBefore = quantityBefore,
            QuantityChange = request.QuantityChange,
            QuantityAfter = quantityAfter,
            Reason = request.Reason,
            ReferenceNumber = request.ReferenceNumber,
            AdjustedBy = adjustedBy,
            CreatedAt = DateTime.UtcNow
        };

        product.CurrentStock = quantityAfter;
        product.UpdatedAt = DateTime.UtcNow;

        _context.StockAdjustments.Add(adjustment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Stock adjusted for {ProductCode}: {Change} ({Type})",
            product.ProductCode, request.QuantityChange, request.AdjustmentType);

        // Fire low-stock / out-of-stock notification when quantity drops at or below reorder level
        if (quantityAfter <= product.ReorderLevel && request.QuantityChange < 0)
        {
            try
            {
                await _notificationService.SendLowStockAlertAsync(product.ProductId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send low-stock notification for product {ProductId}", product.ProductId);
            }
        }

        return MapToStockAdjustmentResponse(adjustment, product, user.Username);
    }

    public async Task<StockAdjustmentResponse> ReceiveStockAsync(int productId, decimal quantity, string? referenceNumber, int adjustedBy)
    {
        return await AdjustStockAsync(new CreateStockAdjustmentRequest
        {
            ProductId = productId,
            AdjustmentType = "Received",
            QuantityChange = quantity,
            ReferenceNumber = referenceNumber,
            Reason = "Stock received"
        }, adjustedBy);
    }

    public async Task<StockAdjustmentResponse> DeductStockForServiceAsync(int productId, decimal quantity, int appointmentId, int adjustedBy)
    {
        return await AdjustStockAsync(new CreateStockAdjustmentRequest
        {
            ProductId = productId,
            AdjustmentType = "Service Usage",
            QuantityChange = -quantity,
            ReferenceNumber = $"APT-{appointmentId}",
            Reason = "Used during service"
        }, adjustedBy);
    }

    public async Task<StockAdjustmentResponse> DeductStockForSaleAsync(int productId, decimal quantity, int transactionId, int adjustedBy)
    {
        return await AdjustStockAsync(new CreateStockAdjustmentRequest
        {
            ProductId = productId,
            AdjustmentType = "Sold",
            QuantityChange = -quantity,
            ReferenceNumber = $"TXN-{transactionId}",
            Reason = "Retail sale"
        }, adjustedBy);
    }

    public async Task<StockAdjustmentResponse> WriteOffExpiredAsync(int productId, int adjustedBy)
    {
        var product = await _context.Products.FindAsync(productId)
            ?? throw new InvalidOperationException($"Product with ID {productId} not found");

        return await AdjustStockAsync(new CreateStockAdjustmentRequest
        {
            ProductId = productId,
            AdjustmentType = "Expired",
            QuantityChange = -product.CurrentStock,
            Reason = $"Expired on {product.ExpiryDate?.ToShortDateString()}"
        }, adjustedBy);
    }

    public async Task<StockAdjustmentResponse> WriteOffDamagedAsync(int productId, decimal quantity, string reason, int adjustedBy)
    {
        return await AdjustStockAsync(new CreateStockAdjustmentRequest
        {
            ProductId = productId,
            AdjustmentType = "Damaged",
            QuantityChange = -quantity,
            Reason = reason
        }, adjustedBy);
    }

    public async Task<PagedResponse<StockAdjustmentResponse>> GetStockAdjustmentHistoryAsync(StockAdjustmentHistoryRequest request)
    {
        var query = _context.StockAdjustments
            .AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.AdjustedByUser)
            .AsQueryable();

        if (request.ProductId.HasValue)
            query = query.Where(a => a.ProductId == request.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(request.AdjustmentType))
            query = query.Where(a => a.AdjustmentType == request.AdjustmentType);

        if (request.FromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(a => a.CreatedAt <= request.ToDate.Value.AddDays(1));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new StockAdjustmentResponse
            {
                AdjustmentId = a.AdjustmentId,
                ProductId = a.ProductId,
                ProductCode = a.Product.ProductCode,
                ProductName = a.Product.ProductName,
                AdjustmentType = a.AdjustmentType,
                QuantityBefore = a.QuantityBefore,
                QuantityChange = a.QuantityChange,
                QuantityAfter = a.QuantityAfter,
                Reason = a.Reason,
                ReferenceNumber = a.ReferenceNumber,
                AdjustedBy = a.AdjustedBy,
                AdjustedByName = a.AdjustedByUser.Username,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return new PagedResponse<StockAdjustmentResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<StockAdjustmentResponse>> GetProductAdjustmentHistoryAsync(int productId)
    {
        return await _context.StockAdjustments
            .AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.AdjustedByUser)
            .Where(a => a.ProductId == productId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new StockAdjustmentResponse
            {
                AdjustmentId = a.AdjustmentId,
                ProductId = a.ProductId,
                ProductCode = a.Product.ProductCode,
                ProductName = a.Product.ProductName,
                AdjustmentType = a.AdjustmentType,
                QuantityBefore = a.QuantityBefore,
                QuantityChange = a.QuantityChange,
                QuantityAfter = a.QuantityAfter,
                Reason = a.Reason,
                ReferenceNumber = a.ReferenceNumber,
                AdjustedBy = a.AdjustedBy,
                AdjustedByName = a.AdjustedByUser.Username,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();
    }

    #endregion

    #region Suppliers

    public async Task<SupplierResponse> CreateSupplierAsync(CreateSupplierRequest request)
    {
        var lastSupplier = await _context.Suppliers.OrderByDescending(s => s.SupplierId).FirstOrDefaultAsync();
        var nextNumber = (lastSupplier?.SupplierId ?? 0) + 1;
        var supplierCode = $"SUP-{nextNumber:D4}";

        var supplier = new Supplier
        {
            SupplierCode = supplierCode,
            SupplierName = request.SupplierName,
            ContactPerson = request.ContactPerson,
            Email = request.Email,
            PhoneNumber = request.Phone,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            TINNumber = request.TaxIdentificationNumber,
            PaymentTerms = request.PaymentTerms,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created supplier {SupplierCode}: {SupplierName}", supplier.SupplierCode, supplier.SupplierName);

        return await GetSupplierByIdAsync(supplier.SupplierId)
            ?? throw new InvalidOperationException("Failed to retrieve created supplier");
    }

    public async Task<SupplierResponse?> GetSupplierByIdAsync(int supplierId)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Include(s => s.PurchaseOrders)
            .FirstOrDefaultAsync(s => s.SupplierId == supplierId);

        return supplier == null ? null : MapToSupplierResponse(supplier);
    }

    public async Task<List<SupplierResponse>> GetAllSuppliersAsync(bool includeInactive = false)
    {
        var query = _context.Suppliers
            .AsNoTracking()
            .Include(s => s.PurchaseOrders)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.SupplierName)
            .Select(s => new SupplierResponse
            {
                SupplierId = s.SupplierId,
                SupplierCode = s.SupplierCode,
                SupplierName = s.SupplierName,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.PhoneNumber,
                Address = s.Address,
                City = s.City,
                Province = s.Province,
                TaxIdentificationNumber = s.TINNumber,
                PaymentTerms = s.PaymentTerms,
                TotalOrders = s.PurchaseOrders.Count,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SupplierResponse> UpdateSupplierAsync(int supplierId, UpdateSupplierRequest request)
    {
        var supplier = await _context.Suppliers.FindAsync(supplierId)
            ?? throw new InvalidOperationException($"Supplier with ID {supplierId} not found");

        supplier.SupplierName = request.SupplierName;
        supplier.ContactPerson = request.ContactPerson;
        supplier.Email = request.Email;
        supplier.PhoneNumber = request.Phone;
        supplier.Address = request.Address;
        supplier.City = request.City;
        supplier.Province = request.Province;
        supplier.TINNumber = request.TaxIdentificationNumber;
        supplier.PaymentTerms = request.PaymentTerms;
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetSupplierByIdAsync(supplierId)
            ?? throw new InvalidOperationException("Failed to retrieve updated supplier");
    }

    public async Task<bool> DeactivateSupplierAsync(int supplierId)
    {
        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null) return false;

        supplier.IsActive = false;
        supplier.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Purchase Orders

    public async Task<PurchaseOrderResponse> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, int createdBy)
    {
        var supplier = await _context.Suppliers.FindAsync(request.SupplierId)
            ?? throw new InvalidOperationException($"Supplier with ID {request.SupplierId} not found");

        // Generate PO number
        var today = PhilippineTime.Today;
        var todayCount = await _context.PurchaseOrders
            .CountAsync(po => po.CreatedAt.Date == today);
        var poNumber = $"PO-{today:yyyyMMdd}-{(todayCount + 1):D3}";

        var purchaseOrder = new PurchaseOrder
        {
            PONumber = poNumber,
            SupplierId = request.SupplierId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Status = "Pending",
            Notes = request.Notes,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;
        foreach (var item in request.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId)
                ?? throw new InvalidOperationException($"Product with ID {item.ProductId} not found");

            var poItem = new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitCost,
                TotalPrice = item.Quantity * item.UnitCost,
                CreatedAt = DateTime.UtcNow
            };

            purchaseOrder.Items.Add(poItem);
            totalAmount += poItem.TotalPrice;
        }

        purchaseOrder.TotalAmount = totalAmount;

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created purchase order {PONumber} for supplier {SupplierName}",
            poNumber, supplier.SupplierName);

        return await GetPurchaseOrderByIdAsync(purchaseOrder.PurchaseOrderId)
            ?? throw new InvalidOperationException("Failed to retrieve created purchase order");
    }

    public async Task<PurchaseOrderResponse?> GetPurchaseOrderByIdAsync(int purchaseOrderId)
    {
        var po = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.CreatedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

        return po == null ? null : MapToPurchaseOrderResponse(po);
    }

    public async Task<PurchaseOrderResponse?> GetPurchaseOrderByNumberAsync(string poNumber)
    {
        var po = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.CreatedByUser)
            .Include(p => p.ApprovedByUser)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.PONumber == poNumber);

        return po == null ? null : MapToPurchaseOrderResponse(po);
    }

    public async Task<PagedResponse<PurchaseOrderListResponse>> SearchPurchaseOrdersAsync(PurchaseOrderSearchRequest request)
    {
        var query = _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.PONumber.ToLower().Contains(term) ||
                p.Supplier.SupplierName.ToLower().Contains(term));
        }

        if (request.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(p => p.Status == request.Status);

        if (request.FromDate.HasValue)
            query = query.Where(p => p.OrderDate >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(p => p.OrderDate <= request.ToDate.Value.AddDays(1));

        query = request.SortBy?.ToLower() switch
        {
            "supplier" => request.SortDescending ? query.OrderByDescending(p => p.Supplier.SupplierName) : query.OrderBy(p => p.Supplier.SupplierName),
            "amount" => request.SortDescending ? query.OrderByDescending(p => p.TotalAmount) : query.OrderBy(p => p.TotalAmount),
            "status" => request.SortDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            _ => request.SortDescending ? query.OrderByDescending(p => p.OrderDate) : query.OrderBy(p => p.OrderDate)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PurchaseOrderListResponse
            {
                PurchaseOrderId = p.PurchaseOrderId,
                PONumber = p.PONumber,
                SupplierName = p.Supplier.SupplierName,
                OrderDate = p.OrderDate,
                ExpectedDeliveryDate = p.ExpectedDeliveryDate,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                ItemCount = p.Items.Count
            })
            .ToListAsync();

        return new PagedResponse<PurchaseOrderListResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<PurchaseOrderListResponse>> GetPendingPurchaseOrdersAsync()
    {
        return await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .Where(p => p.Status == "Pending")
            .OrderBy(p => p.OrderDate)
            .Select(p => new PurchaseOrderListResponse
            {
                PurchaseOrderId = p.PurchaseOrderId,
                PONumber = p.PONumber,
                SupplierName = p.Supplier.SupplierName,
                OrderDate = p.OrderDate,
                ExpectedDeliveryDate = p.ExpectedDeliveryDate,
                TotalAmount = p.TotalAmount,
                Status = p.Status,
                ItemCount = p.Items.Count
            })
            .ToListAsync();
    }

    public async Task<PurchaseOrderResponse> ApprovePurchaseOrderAsync(int purchaseOrderId, int approvedBy)
    {
        var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId)
            ?? throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found");

        if (po.Status != "Pending")
            throw new InvalidOperationException($"Cannot approve purchase order with status: {po.Status}");

        po.Status = "Approved";
        po.ApprovedBy = approvedBy;
        po.ApprovedAt = DateTime.UtcNow;
        po.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Approved purchase order {PONumber}", po.PONumber);

        return await GetPurchaseOrderByIdAsync(purchaseOrderId)
            ?? throw new InvalidOperationException("Failed to retrieve updated purchase order");
    }

    public async Task<PurchaseOrderResponse> ReceivePurchaseOrderAsync(int purchaseOrderId, int receivedBy)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId)
            ?? throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found");

        if (po.Status != "Approved" && po.Status != "PartiallyReceived")
            throw new InvalidOperationException($"Cannot receive purchase order with status: {po.Status}. Must be Approved or PartiallyReceived.");

        var user = await _context.Users.FindAsync(receivedBy)
            ?? throw new InvalidOperationException($"User with ID {receivedBy} not found");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            bool allFullyReceived = true;
            bool anyReceived = false;

            foreach (var item in po.Items)
            {
                var quantityToReceive = item.Quantity - item.QuantityReceived;
                if (quantityToReceive <= 0)
                    continue; // Already fully received

                anyReceived = true;

                // 1. Update product stock
                var quantityBefore = item.Product.CurrentStock;
                item.Product.CurrentStock += quantityToReceive;
                item.Product.UpdatedAt = DateTime.UtcNow;

                // 2. Update product cost price (weighted average)
                var totalExistingValue = quantityBefore * item.Product.CostPrice;
                var newBatchValue = quantityToReceive * item.UnitPrice;
                var totalQuantity = quantityBefore + quantityToReceive;
                if (totalQuantity > 0)
                {
                    item.Product.CostPrice = (totalExistingValue + newBatchValue) / totalQuantity;
                }

                // 3. Create ProductBatch for FIFO tracking
                var batchNumber = $"{po.PONumber}-{item.POItemId}";
                _context.ProductBatches.Add(new ProductBatch
                {
                    ProductId = item.ProductId,
                    BatchNumber = batchNumber,
                    CostPrice = item.UnitPrice,
                    QuantityReceived = quantityToReceive,
                    QuantityRemaining = quantityToReceive,
                    ExpiryDate = item.Product.ExpiryDate,
                    ReceivedDate = DateTime.UtcNow,
                    PurchaseOrderItemId = item.POItemId,
                    SupplierId = po.SupplierId,
                    Notes = $"Received from PO {po.PONumber}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                // 4. Create StockAdjustment audit record
                _context.StockAdjustments.Add(new StockAdjustment
                {
                    ProductId = item.ProductId,
                    AdjustmentType = DomainConstants.StockAdjustmentTypes.Received,
                    QuantityBefore = quantityBefore,
                    QuantityChange = quantityToReceive,
                    QuantityAfter = item.Product.CurrentStock,
                    Reason = $"Received from PO {po.PONumber}",
                    ReferenceNumber = po.PONumber,
                    AdjustedBy = receivedBy,
                    CreatedAt = DateTime.UtcNow
                });

                // 5. Mark PO item as fully received
                item.QuantityReceived = item.Quantity;
            }

            if (!anyReceived)
                throw new InvalidOperationException("All items in this purchase order have already been received");

            // Check if all items are fully received
            allFullyReceived = po.Items.All(i => i.QuantityReceived >= i.Quantity);

            po.Status = allFullyReceived ? "Delivered" : "PartiallyReceived";
            po.ReceivedDate = allFullyReceived ? DateTime.UtcNow : po.ReceivedDate;
            po.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Received purchase order {PONumber} - Status: {Status}", po.PONumber, po.Status);

            // 6. Try to create accounting journal entry (DR Inventory, CR Accounts Payable)
            try
            {
                await _accountingService.CreatePurchaseOrderJournalEntryAsync(purchaseOrderId, receivedBy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create accounting JE for PO {PONumber}. Inventory was still received.", po.PONumber);
            }

            return await GetPurchaseOrderByIdAsync(purchaseOrderId)
                ?? throw new InvalidOperationException("Failed to retrieve updated purchase order");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<PurchaseOrderResponse> CancelPurchaseOrderAsync(int purchaseOrderId)
    {
        var po = await _context.PurchaseOrders.FindAsync(purchaseOrderId)
            ?? throw new InvalidOperationException($"Purchase order with ID {purchaseOrderId} not found");

        if (po.Status == "Delivered")
            throw new InvalidOperationException("Cannot cancel a delivered purchase order");

        po.Status = "Cancelled";
        po.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cancelled purchase order {PONumber}", po.PONumber);

        return await GetPurchaseOrderByIdAsync(purchaseOrderId)
            ?? throw new InvalidOperationException("Failed to retrieve updated purchase order");
    }

    #endregion

    #region Dashboard & Reports

    public async Task<InventoryDashboardResponse> GetDashboardAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync();

        var lowStock = products.Where(p => p.CurrentStock <= p.ReorderLevel).ToList();
        var expiringSoon = products.Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30) && p.ExpiryDate.Value > DateTime.UtcNow).ToList();
        var expired = products.Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value < DateTime.UtcNow).ToList();

        return new InventoryDashboardResponse
        {
            TotalProducts = await _context.Products.CountAsync(),
            ActiveProducts = products.Count,
            LowStockCount = lowStock.Count,
            ExpiringSoonCount = expiringSoon.Count,
            ExpiredCount = expired.Count,
            TotalInventoryValue = products.Sum(p => p.CurrentStock * p.CostPrice),
            TotalRetailValue = products.Where(p => p.SellingPrice.HasValue).Sum(p => p.CurrentStock * p.SellingPrice!.Value),
            LowStockProducts = lowStock.Take(10).Select(p => MapToProductListResponse(p)).ToList(),
            ExpiringSoonProducts = expiringSoon.Take(10).Select(p => MapToProductListResponse(p)).ToList(),
            ByCategory = products.GroupBy(p => p.Category.CategoryName).ToDictionary(g => g.Key, g => g.Count()),
            ByProductType = products.GroupBy(p => p.ProductType).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<InventoryValuationResponse> GetInventoryValuationAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .ToListAsync();

        var byCategory = products
            .GroupBy(p => new { p.ProductCategoryId, p.Category.CategoryName })
            .Select(g => new CategoryValuationResponse
            {
                CategoryId = g.Key.ProductCategoryId,
                CategoryName = g.Key.CategoryName,
                ProductCount = g.Count(),
                TotalUnits = g.Sum(p => p.CurrentStock),
                TotalCostValue = g.Sum(p => p.CurrentStock * p.CostPrice),
                TotalRetailValue = g.Where(p => p.SellingPrice.HasValue).Sum(p => p.CurrentStock * p.SellingPrice!.Value)
            })
            .ToList();

        return new InventoryValuationResponse
        {
            AsOfDate = DateTime.UtcNow,
            TotalProducts = products.Count,
            TotalUnits = products.Sum(p => p.CurrentStock),
            TotalCostValue = products.Sum(p => p.CurrentStock * p.CostPrice),
            TotalRetailValue = products.Where(p => p.SellingPrice.HasValue).Sum(p => p.CurrentStock * p.SellingPrice!.Value),
            PotentialProfit = products.Where(p => p.SellingPrice.HasValue).Sum(p => p.CurrentStock * (p.SellingPrice!.Value - p.CostPrice)),
            ByCategory = byCategory
        };
    }

    public async Task<decimal> GetTotalInventoryValueAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .SumAsync(p => p.CurrentStock * p.CostPrice);
    }

    #endregion

    #region Private Helpers

    private static ProductCategoryResponse MapToCategoryResponse(ProductCategory category) => new()
    {
        ProductCategoryId = category.ProductCategoryId,
        CategoryName = category.CategoryName,
        Description = category.Description,
        ProductCount = category.Products?.Count(p => p.IsActive) ?? 0,
        IsActive = category.IsActive
    };

    private static ProductResponse MapToProductResponse(Product product, string categoryName) => new()
    {
        ProductId = product.ProductId,
        ProductCode = product.ProductCode,
        ProductName = product.ProductName,
        Brand = product.Brand,
        Description = product.Description,
        ProductCategoryId = product.ProductCategoryId,
        CategoryName = categoryName,
        ProductType = product.ProductType,
        CurrentStock = product.CurrentStock,
        ReorderLevel = product.ReorderLevel,
        UnitOfMeasure = product.UnitOfMeasure,
        CostPrice = product.CostPrice,
        SellingPrice = product.SellingPrice,
        RetailCommissionRate = product.RetailCommissionRate,
        ExpiryDate = product.ExpiryDate,
        SupplierId = product.SupplierId,
        SupplierName = product.Supplier?.SupplierName,
        IsActive = product.IsActive,
        IsLowStock = product.IsLowStock,
        IsExpiringSoon = product.IsExpiringSoon,
        IsExpired = product.IsExpired,
        CreatedAt = product.CreatedAt,
        UpdatedAt = product.UpdatedAt
    };

    private static ProductListResponse MapToProductListResponse(Product product) => new()
    {
        ProductId = product.ProductId,
        ProductCode = product.ProductCode,
        ProductName = product.ProductName,
        Brand = product.Brand,
        ProductCategoryId = product.ProductCategoryId,
        CategoryName = product.Category?.CategoryName ?? "Unknown",
        ProductType = product.ProductType,
        CurrentStock = product.CurrentStock,
        ReorderLevel = product.ReorderLevel,
        UnitOfMeasure = product.UnitOfMeasure,
        CostPrice = product.CostPrice,
        SellingPrice = product.SellingPrice,
        SupplierId = product.SupplierId,
        SupplierName = product.Supplier?.SupplierName,
        IsLowStock = product.IsLowStock,
        IsExpiringSoon = product.IsExpiringSoon,
        IsActive = product.IsActive
    };

    private static StockAdjustmentResponse MapToStockAdjustmentResponse(StockAdjustment adjustment, Product product, string adjustedByName) => new()
    {
        AdjustmentId = adjustment.AdjustmentId,
        ProductId = adjustment.ProductId,
        ProductCode = product.ProductCode,
        ProductName = product.ProductName,
        AdjustmentType = adjustment.AdjustmentType,
        QuantityBefore = adjustment.QuantityBefore,
        QuantityChange = adjustment.QuantityChange,
        QuantityAfter = adjustment.QuantityAfter,
        Reason = adjustment.Reason,
        ReferenceNumber = adjustment.ReferenceNumber,
        AdjustedBy = adjustment.AdjustedBy,
        AdjustedByName = adjustedByName,
        CreatedAt = adjustment.CreatedAt
    };

    private static SupplierResponse MapToSupplierResponse(Supplier supplier) => new()
    {
        SupplierId = supplier.SupplierId,
        SupplierCode = supplier.SupplierCode,
        SupplierName = supplier.SupplierName,
        ContactPerson = supplier.ContactPerson,
        Email = supplier.Email,
        Phone = supplier.PhoneNumber,
        Address = supplier.Address,
        City = supplier.City,
        Province = supplier.Province,
        TaxIdentificationNumber = supplier.TINNumber,
        PaymentTerms = supplier.PaymentTerms,
        TotalOrders = supplier.PurchaseOrders?.Count ?? 0,
        IsActive = supplier.IsActive,
        CreatedAt = supplier.CreatedAt
    };

    private static PurchaseOrderResponse MapToPurchaseOrderResponse(PurchaseOrder po) => new()
    {
        PurchaseOrderId = po.PurchaseOrderId,
        PONumber = po.PONumber,
        SupplierId = po.SupplierId,
        SupplierName = po.Supplier.SupplierName,
        OrderDate = po.OrderDate,
        ExpectedDeliveryDate = po.ExpectedDeliveryDate,
        TotalAmount = po.TotalAmount,
        Status = po.Status,
        ApprovedBy = po.ApprovedBy,
        ApprovedByName = po.ApprovedByUser?.Username,
        ApprovedAt = po.ApprovedAt,
        ReceivedDate = po.ReceivedDate,
        Notes = po.Notes,
        CreatedBy = po.CreatedBy,
        CreatedByName = po.CreatedByUser.Username,
        CreatedAt = po.CreatedAt,
        Items = po.Items.Select(i => new PurchaseOrderItemResponse
        {
            PurchaseOrderItemId = i.POItemId,
            ProductId = i.ProductId,
            ProductCode = i.Product.ProductCode,
            ProductName = i.Product.ProductName,
            Quantity = i.Quantity,
            QuantityReceived = i.QuantityReceived,
            QuantityPending = i.QuantityPending,
            IsFullyReceived = i.IsFullyReceived,
            UnitCost = i.UnitPrice,
            TotalCost = i.TotalPrice
        }).ToList()
    };

    #endregion
}
