using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Text.Json;

namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Manages authentication state for the Blazor application
/// </summary>
public interface IAuthStateService
{
    event Action? OnAuthStateChanged;
    bool IsAuthenticated { get; }
    string? CurrentToken { get; }
    Models.UserInfo? CurrentUser { get; }
    Task<LoginResult> LoginAsync(string username, string password, string? captchaToken = null);
    Task<bool> ValidateTwoFactorAsync(string twoFactorToken, string code, string? recoveryCode = null);
    Task LogoutAsync();
    Task<bool> TryRestoreSessionAsync();
}

/// <summary>
/// Result of a login attempt
/// </summary>
public class LoginResult
{
    public bool Success { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? TwoFactorToken { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AuthStateService : IAuthStateService
{
    private readonly IApiClient _apiClient;
    private readonly ProtectedLocalStorage _localStorage;
    private readonly IRolePermissionService _rolePermissionService;
    private string? _token;
    private Models.UserInfo? _currentUser;

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
    public string? CurrentToken => _token;
    public Models.UserInfo? CurrentUser => _currentUser;

    public AuthStateService(IApiClient apiClient, ProtectedLocalStorage localStorage, IRolePermissionService rolePermissionService)
    {
        _apiClient = apiClient;
        _localStorage = localStorage;
        _rolePermissionService = rolePermissionService;
    }

    public async Task<LoginResult> LoginAsync(string username, string password, string? captchaToken = null)
    {
        var loginRequest = new Models.LoginRequest
        {
            Username = username,
            Password = password,
            CaptchaToken = captchaToken
        };

        var (response, errorMessage) = await _apiClient.PostWithErrorAsync<Models.LoginRequest, Models.LoginResponse>("api/auth/login", loginRequest);

        // Check if 2FA is required
        if (response?.RequiresTwoFactor == true && !string.IsNullOrEmpty(response.TwoFactorToken))
        {
            return new LoginResult
            {
                Success = false,
                RequiresTwoFactor = true,
                TwoFactorToken = response.TwoFactorToken
            };
        }

        if (response?.Success == true && !string.IsNullOrEmpty(response.Token?.AccessToken))
        {
            _token = response.Token.AccessToken;
            _currentUser = response.User;
            _apiClient.SetAuthToken(_token);

            // Store token in browser storage
            await _localStorage.SetAsync("authToken", _token);
            if (_currentUser != null)
            {
                await _localStorage.SetAsync("currentUser", _currentUser);
            }

            // Load permissions from API after successful login
            await _rolePermissionService.LoadPermissionsAsync();

            OnAuthStateChanged?.Invoke();
            return new LoginResult { Success = true };
        }

        return new LoginResult
        {
            Success = false,
            ErrorMessage = response?.Message ?? errorMessage ?? "Invalid username or password"
        };
    }

    public async Task<bool> ValidateTwoFactorAsync(string twoFactorToken, string code, string? recoveryCode = null)
    {
        var request = new Models.TwoFactorValidateRequest
        {
            TwoFactorToken = twoFactorToken,
            Code = string.IsNullOrEmpty(recoveryCode) ? code : null,
            RecoveryCode = recoveryCode
        };

        var response = await _apiClient.PostAsync<Models.TwoFactorValidateRequest, Models.LoginResponse>("api/auth/2fa/validate", request);

        if (response?.Success == true && !string.IsNullOrEmpty(response.Token?.AccessToken))
        {
            _token = response.Token.AccessToken;
            _currentUser = response.User;
            _apiClient.SetAuthToken(_token);

            await _localStorage.SetAsync("authToken", _token);
            if (_currentUser != null)
            {
                await _localStorage.SetAsync("currentUser", _currentUser);
            }

            // Load permissions from API after successful 2FA
            await _rolePermissionService.LoadPermissionsAsync();

            OnAuthStateChanged?.Invoke();
            return true;
        }

        return false;
    }

    public async Task LogoutAsync()
    {
        _token = null;
        _currentUser = null;
        _apiClient.ClearAuthToken();
        _rolePermissionService.ClearPermissions();

        try
        {
            await _localStorage.DeleteAsync("authToken");
            await _localStorage.DeleteAsync("currentUser");
        }
        catch
        {
            // Ignore storage errors during logout
        }

        OnAuthStateChanged?.Invoke();
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        try
        {
            var tokenResult = await _localStorage.GetAsync<string>("authToken");
            var userResult = await _localStorage.GetAsync<Models.UserInfo>("currentUser");

            if (tokenResult.Success && !string.IsNullOrEmpty(tokenResult.Value))
            {
                // Validate token expiration before using it
                if (!IsTokenValid(tokenResult.Value))
                {
                    Console.WriteLine("AuthStateService: Token expired, clearing session");
                    await LogoutAsync();
                    return false;
                }

                _token = tokenResult.Value;
                _currentUser = userResult.Success ? userResult.Value : null;
                _apiClient.SetAuthToken(_token);

                // Load permissions from API after session restore
                await _rolePermissionService.LoadPermissionsAsync();

                OnAuthStateChanged?.Invoke();
                return true;
            }
        }
        catch
        {
            // Session restoration failed
        }

        return false;
    }

    /// <summary>
    /// Validates if a JWT token is still valid (not expired)
    /// </summary>
    private bool IsTokenValid(string token)
    {
        try
        {
            // JWT format: header.payload.signature
            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            // Decode the payload (second part)
            var payload = parts[1];

            // Add padding if needed for base64
            var paddedPayload = payload.Length % 4 == 0 ? payload :
                payload + new string('=', 4 - payload.Length % 4);

            // Replace URL-safe characters
            paddedPayload = paddedPayload.Replace('-', '+').Replace('_', '/');

            var payloadBytes = Convert.FromBase64String(paddedPayload);
            var payloadJson = System.Text.Encoding.UTF8.GetString(payloadBytes);

            // Parse the JSON to get expiration
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("exp", out var expElement))
            {
                var expUnix = expElement.GetInt64();
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

                // Add 30-second grace period for clock differences
                var isValid = DateTime.UtcNow < expDateTime.AddSeconds(30);
                Console.WriteLine($"AuthStateService: Token exp={expDateTime:u}, now={DateTime.UtcNow:u}, valid={isValid}");
                return isValid;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AuthStateService: Token validation error: {ex.Message}");
            return false;
        }
    }
}
