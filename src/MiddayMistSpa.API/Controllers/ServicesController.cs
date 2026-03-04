using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Common;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Service;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly ISpaServiceService _serviceService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(ISpaServiceService serviceService, ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _logger = logger;
    }

    #region Categories

    [HttpPost("categories")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<CategoryResponse>> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var category = await _serviceService.CreateCategoryAsync(request);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("categories/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryResponse>> GetCategoryById(int id)
    {
        var category = await _serviceService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        return Ok(category);
    }

    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryResponse>>> GetAllCategories([FromQuery] bool includeInactive = false)
    {
        var categories = await _serviceService.GetAllCategoriesAsync(includeInactive);
        return Ok(categories);
    }

    [HttpPut("categories/{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var category = await _serviceService.UpdateCategoryAsync(id, request);
            return Ok(category);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("categories/{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var deleted = await _serviceService.DeleteCategoryAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    #endregion

    #region Services

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<ServiceResponse>> CreateService([FromBody] CreateServiceRequest request)
    {
        try
        {
            var service = await _serviceService.CreateServiceAsync(request);
            return CreatedAtAction(nameof(GetServiceById), new { id = service.ServiceId }, service);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse>> GetServiceById(int id)
    {
        var service = await _serviceService.GetServiceByIdAsync(id);
        if (service == null) return NotFound();
        return Ok(service);
    }

    [HttpGet("by-code/{code}")]
    [AllowAnonymous]
    public async Task<ActionResult<ServiceResponse>> GetServiceByCode(string code)
    {
        var service = await _serviceService.GetServiceByCodeAsync(code);
        if (service == null) return NotFound();
        return Ok(service);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResponse<ServiceListResponse>>> SearchServices([FromQuery] ServiceSearchRequest request)
    {
        var result = await _serviceService.SearchServicesAsync(request);
        return Ok(result);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceListResponse>>> GetActiveServices()
    {
        var services = await _serviceService.GetActiveServicesAsync();
        return Ok(services);
    }

    [HttpGet("by-category/{categoryId}")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceListResponse>>> GetServicesByCategory(int categoryId)
    {
        var services = await _serviceService.GetServicesByCategoryAsync(categoryId);
        return Ok(services);
    }

    [HttpGet("menu")]
    [AllowAnonymous]
    public async Task<ActionResult<List<ServiceMenuResponse>>> GetServiceMenu()
    {
        var menu = await _serviceService.GetServiceMenuAsync();
        return Ok(menu);
    }

    /// <summary>
    /// Get services as lookup items for dropdowns
    /// </summary>
    [HttpGet("lookup")]
    [Authorize(Policy = "AllStaff")]
    public async Task<ActionResult<List<LookupItemDto>>> GetServiceLookup()
    {
        var services = await _serviceService.GetActiveServicesAsync();
        var lookup = services.Select(s => new LookupItemDto
        {
            Id = s.ServiceId,
            Name = s.ServiceName,
            Code = s.ServiceCode,
            Price = s.RegularPrice
        }).ToList();
        return Ok(lookup);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<ServiceResponse>> UpdateService(int id, [FromBody] UpdateServiceRequest request)
    {
        try
        {
            var service = await _serviceService.UpdateServiceAsync(id, request);
            return Ok(service);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service {ServiceId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id}/pricing")]
    [Authorize(Roles = "SuperAdmin,Admin,Accountant")]
    public async Task<ActionResult<ServiceResponse>> UpdatePricing(int id, [FromBody] UpdatePricingRequest request)
    {
        try
        {
            var service = await _serviceService.UpdatePricingAsync(id, request);
            return Ok(service);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("bulk-price-adjustment")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<object>> ApplyBulkPriceAdjustment([FromBody] BulkPriceAdjustmentRequest request)
    {
        try
        {
            var count = await _serviceService.ApplyBulkPriceAdjustmentAsync(request);
            return Ok(new { message = $"Price adjustment applied to {count} services", affectedCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying bulk price adjustment");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> DeactivateService(int id)
    {
        var result = await _serviceService.DeactivateServiceAsync(id);
        if (!result) return NotFound();
        return Ok(new { message = "Service deactivated successfully" });
    }

    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> ReactivateService(int id)
    {
        var result = await _serviceService.ReactivateServiceAsync(id);
        if (!result) return NotFound();
        return Ok(new { message = "Service reactivated successfully" });
    }

    #endregion

    #region Product Requirements

    [HttpPost("{serviceId}/product-requirements")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<ProductRequirementResponse>> AddProductRequirement(
        int serviceId, [FromBody] AddProductRequirementRequest request)
    {
        try
        {
            var requirement = await _serviceService.AddProductRequirementAsync(serviceId, request);
            return CreatedAtAction(nameof(GetServiceProductRequirements), new { serviceId }, requirement);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{serviceId}/product-requirements")]
    public async Task<ActionResult<List<ProductRequirementResponse>>> GetServiceProductRequirements(int serviceId)
    {
        var requirements = await _serviceService.GetServiceProductRequirementsAsync(serviceId);
        return Ok(requirements);
    }

    [HttpPut("product-requirements/{requirementId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<ActionResult<ProductRequirementResponse>> UpdateProductRequirement(
        int requirementId, [FromBody] UpdateProductRequirementRequest request)
    {
        try
        {
            var requirement = await _serviceService.UpdateProductRequirementAsync(requirementId, request.QuantityRequired);
            return Ok(requirement);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("product-requirements/{requirementId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Inventory")]
    public async Task<IActionResult> RemoveProductRequirement(int requirementId)
    {
        var result = await _serviceService.RemoveProductRequirementAsync(requirementId);
        if (!result) return NotFound();
        return NoContent();
    }

    #endregion

    #region Pricing Helpers

    [HttpGet("{serviceId}/price")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetPriceForCustomer(int serviceId, [FromQuery] string? membershipType)
    {
        try
        {
            var price = await _serviceService.GetPriceForCustomerAsync(serviceId, membershipType ?? "Regular");
            return Ok(new { serviceId, membershipType = membershipType ?? "Regular", price });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    #endregion
}

public class UpdateProductRequirementRequest
{
    public decimal QuantityRequired { get; set; }
}
