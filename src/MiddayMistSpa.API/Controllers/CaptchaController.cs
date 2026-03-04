using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;
    private readonly ILogger<CaptchaController> _logger;

    public CaptchaController(ICaptchaService captchaService, ILogger<CaptchaController> logger)
    {
        _captchaService = captchaService;
        _logger = logger;
    }

    /// <summary>
    /// Get captcha settings (public — needed by login page to show/hide widget)
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CaptchaSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CaptchaSettingsDto>> GetSettings()
    {
        var settings = await _captchaService.GetSettingsAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Update captcha settings (admin only)
    /// </summary>
    [HttpPut("settings")]
    [Authorize(Policy = "AdminOrAbove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateCaptchaSettingsDto settings)
    {
        var success = await _captchaService.UpdateSettingsAsync(settings);
        if (!success)
        {
            return BadRequest(new { Message = "Failed to update captcha settings" });
        }

        return Ok(new { Message = "Captcha settings updated successfully" });
    }
}
