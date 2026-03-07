using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Common;
using MiddayMistSpa.API.DTOs.Customer;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IClusteringService _clusteringService;
    private readonly IExportService _exportService;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ICustomerService customerService,
        IClusteringService clusteringService,
        IExportService exportService,
        ILogger<CustomersController> logger)
    {
        _customerService = customerService;
        _clusteringService = clusteringService;
        _exportService = exportService;
        _logger = logger;
    }

    #region Customer CRUD

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Permission:customers.create")]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var customer = await _customerService.CreateCustomerAsync(request);
            return CreatedAtAction(nameof(GetCustomer), new { id = customer.CustomerId }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("stats")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerStatsResponse>> GetCustomerStats()
    {
        var result = await _customerService.GetCustomerStatsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id);
        if (customer == null)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return Ok(customer);
    }

    /// <summary>
    /// Get customer by code
    /// </summary>
    [HttpGet("code/{code}")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerResponse>> GetCustomerByCode(string code)
    {
        var customer = await _customerService.GetCustomerByCodeAsync(code);
        if (customer == null)
            return NotFound(new { message = $"Customer with code {code} not found" });

        return Ok(customer);
    }

    /// <summary>
    /// Get customer by phone number
    /// </summary>
    [HttpGet("phone/{phone}")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerResponse>> GetCustomerByPhone(string phone)
    {
        var customer = await _customerService.GetCustomerByPhoneAsync(phone);
        if (customer == null)
            return NotFound(new { message = $"Customer with phone {phone} not found" });

        return Ok(customer);
    }

    /// <summary>
    /// Search customers with filters and pagination
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<PagedResponse<CustomerListResponse>>> SearchCustomers([FromQuery] CustomerSearchRequest request)
    {
        var result = await _customerService.SearchCustomersAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Get recently active customers
    /// </summary>
    [HttpGet("recent")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<CustomerListResponse>>> GetRecentCustomers([FromQuery] int count = 10)
    {
        var customers = await _customerService.GetRecentCustomersAsync(count);
        return Ok(customers);
    }

    /// <summary>
    /// Get customers as lookup items for dropdowns
    /// </summary>
    [HttpGet("lookup")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<LookupItemDto>>> GetCustomerLookup([FromQuery] string? search = null)
    {
        var request = new CustomerSearchRequest { PageSize = 500, SearchTerm = search };
        var result = await _customerService.SearchCustomersAsync(request);
        var lookup = result.Items.Select(c => new LookupItemDto
        {
            Id = c.CustomerId,
            Name = c.FullName,
            Code = c.CustomerCode
        }).ToList();
        return Ok(lookup);
    }

    /// <summary>
    /// Update customer
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<CustomerResponse>> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            var customer = await _customerService.UpdateCustomerAsync(id, request);
            return Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivate customer
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Policy = "Permission:customers.delete")]
    public async Task<ActionResult> DeactivateCustomer(int id)
    {
        var result = await _customerService.DeactivateCustomerAsync(id);
        if (!result)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return Ok(new { message = "Customer deactivated successfully" });
    }

    /// <summary>
    /// Reactivate customer
    /// </summary>
    [HttpPost("{id:int}/reactivate")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult> ReactivateCustomer(int id)
    {
        var result = await _customerService.ReactivateCustomerAsync(id);
        if (!result)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return Ok(new { message = "Customer reactivated successfully" });
    }

    #endregion

    #region Preferences

    /// <summary>
    /// Get customer preferences (important for service delivery)
    /// </summary>
    [HttpGet("{id:int}/preferences")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerPreferencesResponse>> GetCustomerPreferences(int id)
    {
        var preferences = await _customerService.GetCustomerPreferencesAsync(id);
        if (preferences == null)
            return NotFound(new { message = $"Customer with ID {id} not found" });

        return Ok(preferences);
    }

    /// <summary>
    /// Update customer preferences
    /// </summary>
    [HttpPut("{id:int}/preferences")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<CustomerPreferencesResponse>> UpdateCustomerPreferences(
        int id,
        [FromBody] CustomerPreferencesResponse preferences)
    {
        try
        {
            var result = await _customerService.UpdateCustomerPreferencesAsync(id, preferences);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Loyalty Program

    /// <summary>
    /// Get loyalty points balance
    /// </summary>
    [HttpGet("{id:int}/loyalty/balance")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<int>> GetLoyaltyBalance(int id)
    {
        try
        {
            var balance = await _customerService.GetLoyaltyPointsBalanceAsync(id);
            return Ok(new { customerId = id, balance });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get loyalty transaction history
    /// </summary>
    [HttpGet("{id:int}/loyalty/history")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<LoyaltyPointHistoryResponse>>> GetLoyaltyHistory(int id, [FromQuery] int count = 50)
    {
        try
        {
            var history = await _customerService.GetLoyaltyTransactionHistoryAsync(id, count);
            return Ok(history);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add loyalty points
    /// </summary>
    [HttpPost("{id:int}/loyalty/add")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<LoyaltyTransactionResponse>> AddLoyaltyPoints(int id, [FromBody] AddLoyaltyPointsRequest request)
    {
        try
        {
            var result = await _customerService.AddLoyaltyPointsAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Redeem loyalty points
    /// </summary>
    [HttpPost("{id:int}/loyalty/redeem")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<LoyaltyTransactionResponse>> RedeemLoyaltyPoints(int id, [FromBody] RedeemLoyaltyPointsRequest request)
    {
        try
        {
            var result = await _customerService.RedeemLoyaltyPointsAsync(id, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Visit History

    /// <summary>
    /// Get customer visit history
    /// </summary>
    [HttpGet("{id:int}/visits")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<CustomerVisitHistoryResponse>>> GetVisitHistory(int id, [FromQuery] int count = 10)
    {
        var history = await _customerService.GetCustomerVisitHistoryAsync(id, count);
        return Ok(history);
    }

    #endregion

    #region Segments

    /// <summary>
    /// Get all customer segments
    /// </summary>
    [HttpGet("segments")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<CustomerSegmentResponse>>> GetAllSegments()
    {
        var segments = await _customerService.GetAllSegmentsAsync();
        return Ok(segments);
    }

    /// <summary>
    /// Get customers by segment
    /// </summary>
    [HttpGet("segments/{segmentName}/customers")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<CustomerListResponse>>> GetCustomersBySegment(string segmentName)
    {
        var customers = await _customerService.GetCustomersBySegmentAsync(segmentName);
        return Ok(customers);
    }

    /// <summary>
    /// Assign customer to segment
    /// </summary>
    [HttpPost("{id:int}/segment")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult> AssignToSegment(int id, [FromBody] string segmentName)
    {
        try
        {
            await _customerService.AssignCustomerToSegmentAsync(id, segmentName);
            return Ok(new { message = $"Customer assigned to segment {segmentName}" });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    #endregion

    #region Membership

    /// <summary>
    /// Upgrade customer membership
    /// </summary>
    [HttpPost("{id:int}/membership/upgrade")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<CustomerResponse>> UpgradeMembership(
        int id,
        [FromQuery] string membershipType,
        [FromQuery] DateTime? expiryDate)
    {
        try
        {
            var customer = await _customerService.UpgradeMembershipAsync(id, membershipType, expiryDate);
            return Ok(customer);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get customers with expiring memberships
    /// </summary>
    [HttpGet("membership/expiring")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<List<CustomerListResponse>>> GetExpiringMemberships([FromQuery] int daysAhead = 30)
    {
        var customers = await _customerService.GetExpiringMembershipsAsync(daysAhead);
        return Ok(customers);
    }

    #endregion

    #region DBSCAN Clustering

    /// <summary>
    /// Run DBSCAN clustering analysis on all customers
    /// </summary>
    [HttpPost("segments/analyze")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult<ClusteringResultResponse>> RunClusteringAnalysis([FromBody] DbscanParametersRequest? parameters)
    {
        _logger.LogInformation("DBSCAN clustering analysis requested");
        var result = await _clusteringService.RunDbscanAnalysisAsync(parameters ?? new DbscanParametersRequest());
        return Ok(result);
    }

    /// <summary>
    /// Get current clustering status and summary
    /// </summary>
    [HttpGet("segments/status")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<ClusteringStatusResponse>> GetClusteringStatus()
    {
        var status = await _clusteringService.GetClusteringStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Get detailed information about a specific segment
    /// </summary>
    [HttpGet("segments/{segmentId:int}/details")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<SegmentDetailResponse>> GetSegmentDetails(int segmentId)
    {
        var details = await _clusteringService.GetSegmentDetailsAsync(segmentId);
        if (details == null)
            return NotFound(new { message = "Segment not found" });
        return Ok(details);
    }

    /// <summary>
    /// Get RFM metrics for a specific customer
    /// </summary>
    [HttpGet("{id:int}/rfm")]
    [Authorize(Policy = "Permission:customers.view")]
    public async Task<ActionResult<CustomerRfmMetricsResponse>> GetCustomerRfmMetrics(int id)
    {
        var metrics = await _clusteringService.GetCustomerRfmMetricsAsync(id);
        if (metrics == null)
            return NotFound(new { message = "Customer not found" });
        return Ok(metrics);
    }

    /// <summary>
    /// Recalculate segment statistics based on current customer data
    /// </summary>
    [HttpPost("segments/recalculate")]
    [Authorize(Policy = "Permission:customers.edit")]
    public async Task<ActionResult> RecalculateSegmentStats()
    {
        await _clusteringService.RecalculateSegmentStatsAsync();
        return Ok(new { message = "Segment statistics recalculated successfully" });
    }

    /// <summary>
    /// Export customer segmentation report as PDF or Excel
    /// </summary>
    [HttpPost("segments/export")]
    [Authorize(Policy = "Permission:reports.export")]
    public async Task<IActionResult> ExportSegmentation([FromBody] SegmentExportRequest request)
    {
        try
        {
            var segments = await _customerService.GetAllSegmentsAsync();
            if (!string.IsNullOrWhiteSpace(request.SegmentName))
                segments = segments.Where(s => s.SegmentName.Equals(request.SegmentName, StringComparison.OrdinalIgnoreCase)).ToList();

            var segmentCustomers = new List<(string, List<CustomerListResponse>)>();
            foreach (var seg in segments)
            {
                var customers = await _customerService.GetCustomersBySegmentAsync(seg.SegmentName);
                segmentCustomers.Add((seg.SegmentName, customers));
            }

            var generatedBy = User.Identity?.Name ?? "System";
            var result = await _exportService.ExportSegmentationAsync(segments, segmentCustomers, request.Format, generatedBy: generatedBy);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting segmentation report");
            return BadRequest(new { message = ex.Message });
        }
    }

    #endregion
}
