using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Common;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Inventory;
using MiddayMistSpa.API.Services;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #region Product Categories

    [HttpPost("categories")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<ProductCategoryResponse>> CreateCategory([FromBody] CreateProductCategoryRequest request)
    {
        try
        {
            var category = await _inventoryService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.ProductCategoryId }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product category");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("categories/{id}")]
    public async Task<ActionResult<ProductCategoryResponse>> GetCategoryById(int id)
    {
        var category = await _inventoryService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<ProductCategoryResponse>>> GetAllCategories([FromQuery] bool includeInactive = false)
    {
        var categories = await _inventoryService.GetAllCategoriesAsync(includeInactive);
        return Ok(categories);
    }

    [HttpPut("categories/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<ProductCategoryResponse>> UpdateCategory(int id, [FromBody] UpdateProductCategoryRequest request)
    {
        try
        {
            var category = await _inventoryService.UpdateCategoryAsync(id, request);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var deleted = await _inventoryService.DeleteCategoryAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Products

    [HttpPost("products")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        try
        {
            var product = await _inventoryService.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("products/{id}")]
    public async Task<ActionResult<ProductResponse>> GetProductById(int id)
    {
        var product = await _inventoryService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet("products/by-code/{code}")]
    public async Task<ActionResult<ProductResponse>> GetProductByCode(string code)
    {
        var product = await _inventoryService.GetProductByCodeAsync(code);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpGet("products")]
    public async Task<ActionResult<PagedResponse<ProductListResponse>>> SearchProducts([FromQuery] ProductSearchRequest request)
    {
        var result = await _inventoryService.SearchProductsAsync(request);
        return Ok(result);
    }

    [HttpGet("products/by-category/{categoryId}")]
    public async Task<ActionResult<List<ProductListResponse>>> GetProductsByCategory(int categoryId)
    {
        var products = await _inventoryService.GetProductsByCategoryAsync(categoryId);
        return Ok(products);
    }

    [HttpGet("products/low-stock")]
    public async Task<ActionResult<List<ProductListResponse>>> GetLowStockProducts()
    {
        var products = await _inventoryService.GetLowStockProductsAsync();
        return Ok(products);
    }

    [HttpGet("products/expiring-soon")]
    public async Task<ActionResult<List<ProductListResponse>>> GetExpiringSoonProducts([FromQuery] int days = 30)
    {
        var products = await _inventoryService.GetExpiringSoonProductsAsync(days);
        return Ok(products);
    }

    [HttpGet("products/expired")]
    public async Task<ActionResult<List<ProductListResponse>>> GetExpiredProducts()
    {
        var products = await _inventoryService.GetExpiredProductsAsync();
        return Ok(products);
    }

    [HttpGet("products/retail")]
    public async Task<ActionResult<List<ProductListResponse>>> GetRetailProducts()
    {
        var products = await _inventoryService.GetRetailProductsAsync();
        return Ok(products);
    }

    /// <summary>
    /// Get products as lookup items for dropdowns
    /// </summary>
    [HttpGet("lookup")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<LookupItemDto>>> GetProductLookup()
    {
        var products = await _inventoryService.GetRetailProductsAsync();
        var lookup = products.Select(p => new LookupItemDto
        {
            Id = p.ProductId,
            Name = p.ProductName,
            Code = p.ProductCode,
            Price = p.SellingPrice,
            Stock = (int)p.CurrentStock
        }).ToList();
        return Ok(lookup);
    }

    [HttpPut("products/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var product = await _inventoryService.UpdateProductAsync(id, request);
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("products/{id}/deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<IActionResult> DeactivateProduct(int id)
    {
        var result = await _inventoryService.DeactivateProductAsync(id);
        if (!result) return NotFound();
        return Ok(new { message = "Product deactivated successfully" });
    }

    [HttpPost("products/{id}/reactivate")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<IActionResult> ReactivateProduct(int id)
    {
        var result = await _inventoryService.ReactivateProductAsync(id);
        if (!result) return NotFound();
        return Ok(new { message = "Product reactivated successfully" });
    }

    #endregion

    #region Stock Management

    [HttpPost("stock/adjust")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<StockAdjustmentResponse>> AdjustStock([FromBody] CreateStockAdjustmentRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var adjustment = await _inventoryService.AdjustStockAsync(request, userId.Value);
            return Ok(adjustment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("stock/receive")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<StockAdjustmentResponse>> ReceiveStock(
        [FromQuery] int productId,
        [FromQuery] decimal quantity,
        [FromQuery] string? referenceNumber = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var adjustment = await _inventoryService.ReceiveStockAsync(productId, quantity, referenceNumber, userId.Value);
            return Ok(adjustment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("stock/write-off-expired/{productId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<StockAdjustmentResponse>> WriteOffExpired(int productId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var adjustment = await _inventoryService.WriteOffExpiredAsync(productId, userId.Value);
            return Ok(adjustment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("stock/write-off-damaged")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<StockAdjustmentResponse>> WriteOffDamaged(
        [FromQuery] int productId,
        [FromQuery] decimal quantity,
        [FromQuery] string reason)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var adjustment = await _inventoryService.WriteOffDamagedAsync(productId, quantity, reason, userId.Value);
            return Ok(adjustment);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("stock/history")]
    public async Task<ActionResult<PagedResponse<StockAdjustmentResponse>>> GetStockHistory([FromQuery] StockAdjustmentHistoryRequest request)
    {
        var result = await _inventoryService.GetStockAdjustmentHistoryAsync(request);
        return Ok(result);
    }

    [HttpGet("stock/history/{productId}")]
    public async Task<ActionResult<List<StockAdjustmentResponse>>> GetProductStockHistory(int productId)
    {
        var history = await _inventoryService.GetProductAdjustmentHistoryAsync(productId);
        return Ok(history);
    }

    #endregion

    #region Suppliers

    [HttpPost("suppliers")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<SupplierResponse>> CreateSupplier([FromBody] CreateSupplierRequest request)
    {
        try
        {
            var supplier = await _inventoryService.CreateSupplierAsync(request);
            return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.SupplierId }, supplier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating supplier");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("suppliers/{id}")]
    public async Task<ActionResult<SupplierResponse>> GetSupplierById(int id)
    {
        var supplier = await _inventoryService.GetSupplierByIdAsync(id);
        if (supplier == null) return NotFound();
        return Ok(supplier);
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<List<SupplierResponse>>> GetAllSuppliers([FromQuery] bool includeInactive = false)
    {
        var suppliers = await _inventoryService.GetAllSuppliersAsync(includeInactive);
        return Ok(suppliers);
    }

    [HttpPut("suppliers/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<SupplierResponse>> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
    {
        try
        {
            var supplier = await _inventoryService.UpdateSupplierAsync(id, request);
            return Ok(supplier);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("suppliers/{id}/deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<IActionResult> DeactivateSupplier(int id)
    {
        var result = await _inventoryService.DeactivateSupplierAsync(id);
        if (!result) return NotFound();
        return Ok(new { message = "Supplier deactivated successfully" });
    }

    #endregion

    #region Purchase Orders

    [HttpPost("purchase-orders")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<PurchaseOrderResponse>> CreatePurchaseOrder([FromBody] CreatePurchaseOrderRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var po = await _inventoryService.CreatePurchaseOrderAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetPurchaseOrderById), new { id = po.PurchaseOrderId }, po);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("purchase-orders/{id}")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetPurchaseOrderById(int id)
    {
        var po = await _inventoryService.GetPurchaseOrderByIdAsync(id);
        if (po == null) return NotFound();
        return Ok(po);
    }

    [HttpGet("purchase-orders/by-number/{poNumber}")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetPurchaseOrderByNumber(string poNumber)
    {
        var po = await _inventoryService.GetPurchaseOrderByNumberAsync(poNumber);
        if (po == null) return NotFound();
        return Ok(po);
    }

    [HttpGet("purchase-orders")]
    public async Task<ActionResult<PagedResponse<PurchaseOrderListResponse>>> SearchPurchaseOrders([FromQuery] PurchaseOrderSearchRequest request)
    {
        var result = await _inventoryService.SearchPurchaseOrdersAsync(request);
        return Ok(result);
    }

    [HttpGet("purchase-orders/pending")]
    public async Task<ActionResult<List<PurchaseOrderListResponse>>> GetPendingPurchaseOrders()
    {
        var orders = await _inventoryService.GetPendingPurchaseOrdersAsync();
        return Ok(orders);
    }

    [HttpPost("purchase-orders/{id}/approve")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> ApprovePurchaseOrder(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var po = await _inventoryService.ApprovePurchaseOrderAsync(id, userId.Value);
            return Ok(po);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("purchase-orders/{id}/receive")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<PurchaseOrderResponse>> ReceivePurchaseOrder(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue) return Unauthorized();

        try
        {
            var po = await _inventoryService.ReceivePurchaseOrderAsync(id, userId.Value);
            return Ok(po);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("purchase-orders/{id}/cancel")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<PurchaseOrderResponse>> CancelPurchaseOrder(int id)
    {
        try
        {
            var po = await _inventoryService.CancelPurchaseOrderAsync(id);
            return Ok(po);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Dashboard & Reports

    [HttpGet("dashboard")]
    public async Task<ActionResult<InventoryDashboardResponse>> GetDashboard()
    {
        var dashboard = await _inventoryService.GetDashboardAsync();
        return Ok(dashboard);
    }

    [HttpGet("valuation")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<InventoryValuationResponse>> GetInventoryValuation()
    {
        var valuation = await _inventoryService.GetInventoryValuationAsync();
        return Ok(valuation);
    }

    [HttpGet("total-value")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<object>> GetTotalInventoryValue()
    {
        var value = await _inventoryService.GetTotalInventoryValueAsync();
        return Ok(new { totalValue = value });
    }

    #endregion
}
