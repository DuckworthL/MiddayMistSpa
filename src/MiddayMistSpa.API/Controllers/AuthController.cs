using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MiddayMistSpa.API.DTOs.Auth;
using MiddayMistSpa.API.Services;

namespace MiddayMistSpa.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[EnableRateLimiting("api")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ICaptchaService _captchaService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ITwoFactorService twoFactorService, ICaptchaService captchaService, IPermissionService permissionService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _twoFactorService = twoFactorService;
        _captchaService = captchaService;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and receive JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user info on success</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // Verify reCAPTCHA if enabled
        var captchaValid = await _captchaService.VerifyTokenAsync(request.CaptchaToken);
        if (!captchaValid)
        {
            return BadRequest(new LoginResponse
            {
                Success = false,
                Message = "CAPTCHA verification failed. Please complete the CAPTCHA and try again."
            });
        }

        // Capture client info
        request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        request.UserAgent = Request.Headers["User-Agent"].ToString();

        var result = await _authService.LoginAsync(request);

        if (result.RequiresTwoFactor)
        {
            // Password was valid but 2FA is required — return 200 with the 2FA token
            return Ok(result);
        }

        if (!result.Success)
        {
            if (result.LockoutEnd.HasValue)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, result);
            }
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout user and invalidate session
    /// </summary>
    /// <param name="request">Optional logout options</param>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        await _authService.LogoutAsync(userId.Value, request);
        return Ok(new { Message = "Logged out successfully" });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>New JWT token pair</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        if (!result.Success)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="request">Current and new password</param>
    /// <returns>Success status and validation errors if any</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ChangePasswordResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ChangePasswordResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.ChangePasswordAsync(userId.Value, request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get current user's active sessions
    /// </summary>
    /// <returns>List of active sessions</returns>
    [HttpGet("sessions")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserSessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserSessionDto>>> GetSessions()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var sessions = await _authService.GetActiveSessionsAsync(userId.Value);
        return Ok(sessions);
    }

    /// <summary>
    /// Terminate a specific session
    /// </summary>
    /// <param name="sessionId">Session ID to terminate</param>
    [HttpDelete("sessions/{sessionId:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TerminateSession(int sessionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _authService.TerminateSessionAsync(sessionId, userId.Value);

        if (!result)
        {
            return NotFound(new { Message = "Session not found" });
        }

        return Ok(new { Message = "Session terminated successfully" });
    }

    /// <summary>
    /// Get current user info from token
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<UserInfo> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var userInfo = new UserInfo
        {
            UserId = userId.Value,
            Username = User.Identity?.Name ?? "",
            Email = User.FindFirstValue(ClaimTypes.Email) ?? "",
            FirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? "",
            LastName = User.FindFirstValue(ClaimTypes.Surname) ?? "",
            RoleName = User.FindFirstValue(ClaimTypes.Role) ?? "",
            EmployeeId = null
        };

        return Ok(userInfo);
    }

    /// <summary>
    /// Get current user's permissions based on their role
    /// </summary>
    [HttpGet("me/permissions")]
    [Authorize]
    public async Task<ActionResult<HashSet<string>>> GetCurrentUserPermissions()
    {
        var roleName = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrEmpty(roleName))
            return Ok(new HashSet<string>());

        var permissions = await _permissionService.GetPermissionsAsync(roleName);
        return Ok(permissions);
    }

    /// <summary>
    /// Validate password against policy (without changing it)
    /// </summary>
    /// <param name="password">Password to validate</param>
    /// <returns>Validation result with errors if any</returns>
    [HttpPost("validate-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ValidatePassword([FromBody] string password)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var (isValid, errors) = await _authService.ValidatePasswordPolicyAsync(password, userId.Value);

        return Ok(new
        {
            IsValid = isValid,
            Errors = errors
        });
    }

    // =========================================================================
    // TWO-FACTOR AUTHENTICATION ENDPOINTS
    // =========================================================================

    /// <summary>
    /// Initiate 2FA setup — returns QR code and manual entry key
    /// </summary>
    [HttpPost("2fa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorSetupResponse>> Setup2FA()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _twoFactorService.SetupAsync(userId.Value);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Verify the TOTP code to confirm 2FA setup — returns recovery codes
    /// </summary>
    [HttpPost("2fa/verify-setup")]
    [Authorize]
    [ProducesResponseType(typeof(TwoFactorVerifySetupResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorVerifySetupResponse>> VerifySetup2FA([FromBody] TwoFactorVerifySetupRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _twoFactorService.VerifySetupAsync(userId.Value, request.Code);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Validate a TOTP code during login (second step)
    /// </summary>
    [HttpPost("2fa/validate")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Validate2FA([FromBody] TwoFactorValidateRequest request)
    {
        // Resolve userId from the temporary 2FA token
        var userId = _twoFactorService.ValidateTwoFactorToken(request.TwoFactorToken);
        if (userId == null)
        {
            return Unauthorized(new LoginResponse { Success = false, Message = "Invalid or expired two-factor token. Please login again." });
        }

        bool isValid;
        if (!string.IsNullOrEmpty(request.Code))
        {
            isValid = await _twoFactorService.ValidateCodeAsync(userId.Value, request.Code);
        }
        else if (!string.IsNullOrEmpty(request.RecoveryCode))
        {
            isValid = await _twoFactorService.ValidateRecoveryCodeAsync(userId.Value, request.RecoveryCode);
        }
        else
        {
            return BadRequest(new LoginResponse { Success = false, Message = "Please provide a verification code or recovery code" });
        }

        if (!isValid)
        {
            return Unauthorized(new LoginResponse { Success = false, Message = "Invalid verification code" });
        }

        // 2FA passed — complete the login
        var result = await _authService.CompleteTwoFactorLoginAsync(userId.Value);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    /// <summary>
    /// Disable 2FA for the current user
    /// </summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Disable2FA([FromBody] TwoFactorDisableRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var (success, message) = await _twoFactorService.DisableAsync(userId.Value, request.Code);
        return success ? Ok(new { Message = message }) : BadRequest(new { Message = message });
    }

    /// <summary>
    /// Get 2FA status for the current user
    /// </summary>
    [HttpGet("2fa/status")]
    [Authorize]
    [ProducesResponseType(typeof(TwoFactorStatusResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TwoFactorStatusResponse>> Get2FAStatus()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _twoFactorService.GetStatusAsync(userId.Value);
        return Ok(result);
    }

    #region Private Methods

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    #endregion

    // =========================================================================
    // PROFILE UPDATE ENDPOINT
    // =========================================================================

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UpdateProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _authService.UpdateProfileAsync(userId.Value, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // =========================================================================
    // PASSWORD RESET ENDPOINTS
    // =========================================================================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<PasswordResetResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.RequestPasswordResetAsync(request.Email);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<PasswordResetResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
