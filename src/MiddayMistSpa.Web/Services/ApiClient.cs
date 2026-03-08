using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Base API client for communicating with the backend
/// </summary>
public interface IApiClient
{
    event Action? OnUnauthorized;
    Task<T?> GetAsync<T>(string endpoint);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<(TResponse? Result, string? ErrorMessage)> PostWithErrorAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<(TResponse? Result, string? ErrorMessage)> PutWithErrorAsync<TRequest, TResponse>(string endpoint, TRequest data);
    Task<bool> DeleteAsync(string endpoint);
    Task<(bool Success, string? ErrorMessage)> DeleteWithErrorAsync(string endpoint);
    Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> PostForFileAsync<TRequest>(string endpoint, TRequest data);
    void SetAuthToken(string token);
    void ClearAuthToken();
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _authToken;

    public event Action? OnUnauthorized;

    public ApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("SpaApi");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void SetAuthToken(string token)
    {
        _authToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearAuthToken()
    {
        _authToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            Console.WriteLine($"API GET: {endpoint} (Auth token: {(_authToken != null ? "SET" : "NOT SET")})");
            var response = await _httpClient.GetAsync(endpoint);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
                Console.WriteLine($"API GET Success: {endpoint} (Result null: {result == null})");
                return result;
            }

            Console.WriteLine($"API GET Error [{response.StatusCode}]: {endpoint}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("API: Unauthorized - triggering OnUnauthorized event");
                OnUnauthorized?.Invoke();
            }

            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API GET Exception: {endpoint} - {ex.Message}");
            return default;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }

            // Log the error response for debugging
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API POST Error [{response.StatusCode}] {endpoint}: {errorContent}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                Console.WriteLine("API: Unauthorized - triggering OnUnauthorized event");
                OnUnauthorized?.Invoke();
            }

            return default;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API POST Exception {endpoint}: {ex.Message}");
            return default;
        }
    }

    public async Task<(TResponse? Result, string? ErrorMessage)> PostWithErrorAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                return (result, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API POST Error [{response.StatusCode}] {endpoint}: {errorContent}");

            // Try to deserialize error response body as TResponse (for fields like RemainingAttempts, LockoutEnd)
            TResponse? errorResult = default;
            try
            {
                errorResult = System.Text.Json.JsonSerializer.Deserialize<TResponse>(errorContent, _jsonOptions);
            }
            catch { /* non-deserializable, ignore */ }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                var unauthMsg = TryExtractErrorMessage(errorContent) ?? "Unauthorized";
                return (errorResult, unauthMsg);
            }

            var errorMessage = TryExtractErrorMessage(errorContent) ?? $"Request failed ({(int)response.StatusCode})";
            return (errorResult, errorMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API POST Exception {endpoint}: {ex.Message}");
            return (default, ex.Message);
        }
    }

    private static string? TryExtractErrorMessage(string content)
    {
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
                return errorProp.GetString();
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                return msgProp.GetString();
            if (doc.RootElement.TryGetProperty("title", out var titleProp))
                return titleProp.GetString();
        }
        catch { /* non-JSON response, ignore */ }
        return content.Length > 200 ? content[..200] : content;
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
            }

            return default;
        }
        catch (Exception)
        {
            return default;
        }
    }

    public async Task<(TResponse? Result, string? ErrorMessage)> PutWithErrorAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                return (result, null);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API PUT Error [{response.StatusCode}] {endpoint}: {errorContent}");

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                return (default, "Unauthorized");
            }

            var errorMessage = TryExtractErrorMessage(errorContent) ?? $"Request failed ({(int)response.StatusCode})";
            return (default, errorMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API PUT Exception {endpoint}: {ex.Message}");
            return (default, ex.Message);
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
            }

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteWithErrorAsync(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                return (false, "Unauthorized");
            }

            if (response.IsSuccessStatusCode)
                return (true, null);

            // Try to read error message from response body
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    return (false, msgProp.GetString());
            }
            catch { }

            return (false, $"Request failed with status {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> PostForFileAsync<TRequest>(string endpoint, TRequest data)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                OnUnauthorized?.Invoke();
                return (null, null, null, "Unauthorized");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return (null, null, null, $"Export failed: {response.StatusCode} — {errorBody}");
            }

            var fileBytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType;
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? response.Content.Headers.ContentDisposition?.FileNameStar
                ?? "export";

            return (fileBytes, fileName, contentType, null);
        }
        catch (Exception ex)
        {
            return (null, null, null, $"Export request failed: {ex.Message}");
        }
    }
}
