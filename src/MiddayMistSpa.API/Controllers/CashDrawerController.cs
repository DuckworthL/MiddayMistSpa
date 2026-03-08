using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.DTOs.Transaction;
using MiddayMistSpa.API.Services;
using System.Security.Claims;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/cash-drawer")]
[Authorize]
public class CashDrawerController : ControllerBase
{
    private readonly ICashDrawerService _cashDrawerService;

    public CashDrawerController(ICashDrawerService cashDrawerService)
    {
        _cashDrawerService = cashDrawerService;
    }

    [HttpGet("active")]
    [Authorize(Policy = "Permission:transactions.view")]
    public async Task<ActionResult<CashDrawerSessionResponse>> GetActiveSession()
    {
        var result = await _cashDrawerService.GetActiveSessionAsync();
        if (result == null) return NoContent();
        return Ok(result);
    }

    [HttpPost("open")]
    [Authorize(Policy = "Permission:transactions.manage")]
    public async Task<ActionResult<CashDrawerSessionResponse>> OpenDrawer([FromBody] OpenDrawerRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var result = await _cashDrawerService.OpenDrawerAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("close")]
    [Authorize(Policy = "Permission:transactions.manage")]
    public async Task<ActionResult<CashDrawerSessionResponse>> CloseDrawer([FromBody] CloseDrawerRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            var result = await _cashDrawerService.CloseDrawerAsync(request, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("history")]
    [Authorize(Policy = "Permission:transactions.view")]
    public async Task<ActionResult<List<CashDrawerSessionResponse>>> GetHistory([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var result = await _cashDrawerService.GetSessionHistoryAsync(startDate, endDate);
        return Ok(result);
    }
}
