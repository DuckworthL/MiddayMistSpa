using MiddayMistSpa.Core;
using MiddayMistSpa.Web.Models;

namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Dashboard data service
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummary?> GetDashboardSummaryAsync();
    Task<List<AppointmentSummary>> GetTodayAppointmentsAsync();
    Task<List<DailyRevenue>> GetRevenueAsync(DateTime startDate, DateTime endDate);
}

public class DashboardService : IDashboardService
{
    private readonly IApiClient _apiClient;

    public DashboardService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<DashboardSummary?> GetDashboardSummaryAsync()
    {
        return await _apiClient.GetAsync<DashboardSummary>("api/reports/dashboard");
    }

    public async Task<List<AppointmentSummary>> GetTodayAppointmentsAsync()
    {
        var result = await _apiClient.GetAsync<List<AppointmentSummary>>("api/appointments/today");
        return result ?? new List<AppointmentSummary>();
    }

    public async Task<List<DailyRevenue>> GetRevenueAsync(DateTime startDate, DateTime endDate)
    {
        var result = await _apiClient.GetAsync<List<DailyRevenue>>($"api/reports/revenue/daily?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
        return result ?? new List<DailyRevenue>();
    }
}

/// <summary>
/// Employee API service
/// </summary>
public interface IEmployeeApiService
{
    Task<PagedResponse<EmployeeResponse>> GetEmployeesAsync(int page = 1, int pageSize = 20, string? search = null, bool? activeOnly = null, string? department = null);
    Task<EmployeeResponse?> GetEmployeeByIdAsync(int id);
    Task<EmployeeResponse?> CreateEmployeeAsync(CreateEmployeeRequest request);
    Task<(EmployeeResponse? Result, string? ErrorMessage)> CreateEmployeeWithErrorAsync(CreateEmployeeRequest request);
    Task<EmployeeResponse?> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request);
    Task<(EmployeeResponse? Result, string? ErrorMessage)> UpdateEmployeeWithErrorAsync(int id, UpdateEmployeeRequest request);
    Task<bool> ArchiveEmployeeAsync(int id);
    Task<bool> ReactivateEmployeeAsync(int id);
    Task<List<LookupItem>> GetTherapistsAsync();
}

public class EmployeeApiService : IEmployeeApiService
{
    private readonly IApiClient _apiClient;

    public EmployeeApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<EmployeeResponse>> GetEmployeesAsync(int page = 1, int pageSize = 20, string? search = null, bool? activeOnly = null, string? department = null)
    {
        var url = $"api/employees?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={Uri.EscapeDataString(search)}";
        if (activeOnly.HasValue) url += $"&activeOnly={activeOnly.Value}";
        if (!string.IsNullOrEmpty(department)) url += $"&department={Uri.EscapeDataString(department)}";

        var result = await _apiClient.GetAsync<PagedResponse<EmployeeResponse>>(url);
        return result ?? new PagedResponse<EmployeeResponse>();
    }

    public async Task<EmployeeResponse?> GetEmployeeByIdAsync(int id)
    {
        return await _apiClient.GetAsync<EmployeeResponse>($"api/employees/{id}");
    }

    public async Task<EmployeeResponse?> CreateEmployeeAsync(CreateEmployeeRequest request)
    {
        return await _apiClient.PostAsync<CreateEmployeeRequest, EmployeeResponse>("api/employees", request);
    }

    public async Task<(EmployeeResponse? Result, string? ErrorMessage)> CreateEmployeeWithErrorAsync(CreateEmployeeRequest request)
    {
        return await _apiClient.PostWithErrorAsync<CreateEmployeeRequest, EmployeeResponse>("api/employees", request);
    }

    public async Task<EmployeeResponse?> UpdateEmployeeAsync(int id, UpdateEmployeeRequest request)
    {
        return await _apiClient.PutAsync<UpdateEmployeeRequest, EmployeeResponse>($"api/employees/{id}", request);
    }

    public async Task<(EmployeeResponse? Result, string? ErrorMessage)> UpdateEmployeeWithErrorAsync(int id, UpdateEmployeeRequest request)
    {
        return await _apiClient.PutWithErrorAsync<UpdateEmployeeRequest, EmployeeResponse>($"api/employees/{id}", request);
    }

    public async Task<bool> ArchiveEmployeeAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/employees/{id}/deactivate", new { });
        return result != null;
    }

    public async Task<bool> ReactivateEmployeeAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/employees/{id}/reactivate", new { });
        return result != null;
    }

    public async Task<List<LookupItem>> GetTherapistsAsync()
    {
        var result = await _apiClient.GetAsync<List<LookupItem>>("api/employees/therapists");
        return result ?? new List<LookupItem>();
    }
}

/// <summary>
/// Customer API service
/// </summary>
public interface ICustomerApiService
{
    Task<PagedResponse<CustomerResponse>> GetCustomersAsync(int page = 1, int pageSize = 20, string? search = null, string? membershipType = null);
    Task<CustomerResponse?> GetCustomerByIdAsync(int id);
    Task<CustomerResponse?> CreateCustomerAsync(CreateCustomerRequest request);
    Task<(CustomerResponse? Result, string? ErrorMessage)> CreateCustomerWithErrorAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> UpdateCustomerAsync(int id, UpdateCustomerRequest request);
    Task<(CustomerResponse? Result, string? ErrorMessage)> UpdateCustomerWithErrorAsync(int id, UpdateCustomerRequest request);
    Task<bool> DeleteCustomerAsync(int id);
    Task<List<LookupItem>> SearchCustomersAsync(string query);
    Task<List<LookupItem>> GetCustomerLookupAsync();
    Task<CustomerStatsResponse?> GetCustomerStatsAsync();
}

public class CustomerApiService : ICustomerApiService
{
    private readonly IApiClient _apiClient;

    public CustomerApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<CustomerResponse>> GetCustomersAsync(int page = 1, int pageSize = 20, string? search = null, string? membershipType = null)
    {
        var url = $"api/customers?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={Uri.EscapeDataString(search)}";
        if (!string.IsNullOrEmpty(membershipType)) url += $"&membershipType={Uri.EscapeDataString(membershipType)}";

        var result = await _apiClient.GetAsync<PagedResponse<CustomerResponse>>(url);
        return result ?? new PagedResponse<CustomerResponse>();
    }

    public async Task<CustomerResponse?> GetCustomerByIdAsync(int id)
    {
        return await _apiClient.GetAsync<CustomerResponse>($"api/customers/{id}");
    }

    public async Task<CustomerResponse?> CreateCustomerAsync(CreateCustomerRequest request)
    {
        return await _apiClient.PostAsync<CreateCustomerRequest, CustomerResponse>("api/customers", request);
    }

    public async Task<(CustomerResponse? Result, string? ErrorMessage)> CreateCustomerWithErrorAsync(CreateCustomerRequest request)
    {
        return await _apiClient.PostWithErrorAsync<CreateCustomerRequest, CustomerResponse>("api/customers", request);
    }

    public async Task<CustomerResponse?> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
    {
        return await _apiClient.PutAsync<UpdateCustomerRequest, CustomerResponse>($"api/customers/{id}", request);
    }

    public async Task<(CustomerResponse? Result, string? ErrorMessage)> UpdateCustomerWithErrorAsync(int id, UpdateCustomerRequest request)
    {
        return await _apiClient.PutWithErrorAsync<UpdateCustomerRequest, CustomerResponse>($"api/customers/{id}", request);
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        return await _apiClient.DeleteAsync($"api/customers/{id}");
    }

    public async Task<List<LookupItem>> SearchCustomersAsync(string query)
    {
        // Use the main customers endpoint with search parameter and map to LookupItem
        var result = await _apiClient.GetAsync<PagedResponse<CustomerResponse>>($"api/customers?searchTerm={Uri.EscapeDataString(query)}&pageSize=20");
        return result?.Items?.Select(c => new LookupItem { Id = c.CustomerId, Name = $"{c.FirstName} {c.LastName}" }).ToList() ?? new List<LookupItem>();
    }

    public async Task<List<LookupItem>> GetCustomerLookupAsync()
    {
        var result = await _apiClient.GetAsync<List<LookupItem>>("api/customers/lookup");
        return result ?? new List<LookupItem>();
    }

    public async Task<CustomerStatsResponse?> GetCustomerStatsAsync()
    {
        return await _apiClient.GetAsync<CustomerStatsResponse>("api/customers/stats");
    }
}

/// <summary>
/// Appointment API service
/// </summary>
public interface IAppointmentApiService
{
    Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(int page = 1, int pageSize = 20, DateTime? date = null, string? status = null, int? customerId = null, string? searchTerm = null, bool includeArchived = false);
    Task<List<AppointmentResponse>> GetAppointmentsByDateRangeAsync(DateTime start, DateTime end);
    Task<AppointmentResponse?> GetAppointmentByIdAsync(int id);
    Task<AppointmentResponse?> CreateAppointmentAsync(CreateAppointmentRequest request);
    Task<(AppointmentResponse? Result, string? ErrorMessage)> CreateAppointmentWithErrorAsync(CreateAppointmentRequest request);
    Task<AppointmentResponse?> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request);
    Task<bool> CancelAppointmentAsync(int id, string? reason = null);
    Task<bool> ArchiveAppointmentAsync(int id);
    Task<List<TimeSpan>> GetAvailableSlotsAsync(int serviceId, DateTime date, int? therapistId = null);
    Task<List<AvailableSlotResponse>> GetAvailableSlotsDetailedAsync(int serviceId, DateTime date, int? therapistId = null);
    Task<AppointmentResponse?> CheckInAppointmentAsync(int id);
    Task<(AppointmentResponse? Result, string? ErrorMessage)> UpdateAppointmentWithErrorAsync(int id, UpdateAppointmentRequest request);
    Task<AppointmentResponse?> StartServiceAsync(int id);
    Task<AppointmentResponse?> CompleteServiceAsync(int id);
    Task<AppointmentResponse?> AddServiceToAppointmentAsync(int appointmentId, AddServiceToAppointmentRequest request);
    Task<AppointmentResponse?> RemoveServiceFromAppointmentAsync(int appointmentId, int serviceItemId);
    Task<List<RoomResponse>> GetRoomsAsync();
}

public class AppointmentApiService : IAppointmentApiService
{
    private readonly IApiClient _apiClient;

    public AppointmentApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(int page = 1, int pageSize = 20, DateTime? date = null, string? status = null, int? customerId = null, string? searchTerm = null, bool includeArchived = false)
    {
        var url = $"api/appointments?page={page}&pageSize={pageSize}";
        // API uses DateFrom/DateTo, not date - for a single date, set both to same value
        if (date.HasValue)
        {
            url += $"&dateFrom={date.Value:yyyy-MM-dd}&dateTo={date.Value:yyyy-MM-dd}";
        }
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";
        if (!string.IsNullOrWhiteSpace(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        if (includeArchived) url += "&includeArchived=true";

        var result = await _apiClient.GetAsync<PagedResponse<AppointmentResponse>>(url);
        return result ?? new PagedResponse<AppointmentResponse>();
    }

    public async Task<bool> ArchiveAppointmentAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/appointments/{id}/archive", new { });
        return result != null;
    }

    public async Task<List<AppointmentResponse>> GetAppointmentsByDateRangeAsync(DateTime start, DateTime end)
    {
        // Use the main appointments endpoint with dateFrom/dateTo filter
        var result = await _apiClient.GetAsync<PagedResponse<AppointmentResponse>>($"api/appointments?dateFrom={start:yyyy-MM-dd}&dateTo={end:yyyy-MM-dd}&pageSize=1000");
        return result?.Items?.ToList() ?? new List<AppointmentResponse>();
    }

    public async Task<AppointmentResponse?> GetAppointmentByIdAsync(int id)
    {
        return await _apiClient.GetAsync<AppointmentResponse>($"api/appointments/{id}");
    }

    public async Task<AppointmentResponse?> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        return await _apiClient.PostAsync<CreateAppointmentRequest, AppointmentResponse>("api/appointments", request);
    }

    public async Task<(AppointmentResponse? Result, string? ErrorMessage)> CreateAppointmentWithErrorAsync(CreateAppointmentRequest request)
    {
        return await _apiClient.PostWithErrorAsync<CreateAppointmentRequest, AppointmentResponse>("api/appointments", request);
    }

    public async Task<AppointmentResponse?> UpdateAppointmentAsync(int id, UpdateAppointmentRequest request)
    {
        return await _apiClient.PutAsync<UpdateAppointmentRequest, AppointmentResponse>($"api/appointments/{id}", request);
    }

    public async Task<(AppointmentResponse? Result, string? ErrorMessage)> UpdateAppointmentWithErrorAsync(int id, UpdateAppointmentRequest request)
    {
        return await _apiClient.PutWithErrorAsync<UpdateAppointmentRequest, AppointmentResponse>($"api/appointments/{id}", request);
    }

    public async Task<bool> CancelAppointmentAsync(int id, string? reason = null)
    {
        var request = new { Reason = reason ?? "Cancelled by user", NotifyCustomer = true };
        var result = await _apiClient.PostAsync<object, object>($"api/appointments/{id}/cancel", request);
        return result != null;
    }

    public async Task<List<TimeSpan>> GetAvailableSlotsAsync(int serviceId, DateTime date, int? therapistId = null)
    {
        var url = $"api/appointments/available-slots?serviceId={serviceId}&date={date:yyyy-MM-dd}";
        if (therapistId.HasValue) url += $"&therapistId={therapistId}";

        var slots = await _apiClient.GetAsync<List<AvailableSlotResponse>>(url);
        if (slots == null) return new List<TimeSpan>();

        // Return only slots that have at least one available therapist
        return slots
            .Where(s => s.AvailableTherapists.Any())
            .Select(s => s.StartTime)
            .ToList();
    }

    public async Task<List<AvailableSlotResponse>> GetAvailableSlotsDetailedAsync(int serviceId, DateTime date, int? therapistId = null)
    {
        var url = $"api/appointments/available-slots?serviceId={serviceId}&date={date:yyyy-MM-dd}";
        if (therapistId.HasValue) url += $"&therapistId={therapistId}";

        var slots = await _apiClient.GetAsync<List<AvailableSlotResponse>>(url);
        return slots ?? new List<AvailableSlotResponse>();
    }

    public async Task<AppointmentResponse?> CheckInAppointmentAsync(int id)
    {
        return await _apiClient.PostAsync<object, AppointmentResponse>($"api/appointments/{id}/check-in", new { });
    }

    public async Task<AppointmentResponse?> StartServiceAsync(int id)
    {
        return await _apiClient.PostAsync<object, AppointmentResponse>($"api/appointments/{id}/start", new { });
    }

    public async Task<AppointmentResponse?> CompleteServiceAsync(int id)
    {
        return await _apiClient.PostAsync<object, AppointmentResponse>($"api/appointments/{id}/complete", new { });
    }

    public async Task<AppointmentResponse?> AddServiceToAppointmentAsync(int appointmentId, AddServiceToAppointmentRequest request)
    {
        return await _apiClient.PostAsync<AddServiceToAppointmentRequest, AppointmentResponse>($"api/appointments/{appointmentId}/services", request);
    }

    public async Task<AppointmentResponse?> RemoveServiceFromAppointmentAsync(int appointmentId, int serviceItemId)
    {
        var success = await _apiClient.DeleteAsync($"api/appointments/{appointmentId}/services/{serviceItemId}");
        if (success)
            return await GetAppointmentByIdAsync(appointmentId);
        return null;
    }

    public async Task<List<RoomResponse>> GetRoomsAsync()
    {
        var result = await _apiClient.GetAsync<List<RoomResponse>>("api/appointments/rooms");
        return result ?? new List<RoomResponse>();
    }
}

/// <summary>
/// Service/Treatment API service
/// </summary>
public interface IServiceApiService
{
    Task<PagedResponse<ServiceResponse>> GetServicesAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, bool? isActive = null);
    Task<ServiceResponse?> GetServiceByIdAsync(int id);
    Task<ServiceResponse?> CreateServiceAsync(CreateServiceRequest request);
    Task<(ServiceResponse? Result, string? ErrorMessage)> CreateServiceWithErrorAsync(CreateServiceRequest request);
    Task<ServiceResponse?> UpdateServiceAsync(int id, UpdateServiceRequest request);
    Task<(ServiceResponse? Result, string? ErrorMessage)> UpdateServiceWithErrorAsync(int id, UpdateServiceRequest request);
    Task<bool> ArchiveServiceAsync(int id);
    Task<bool> ReactivateServiceAsync(int id);
    Task<List<ServiceCategoryResponse>> GetCategoriesAsync();
    Task<bool> SaveCategoryAsync(CreateCategoryRequest request, int? id = null);
    Task<List<LookupItem>> GetServiceLookupAsync();
}

public class ServiceApiService : IServiceApiService
{
    private readonly IApiClient _apiClient;

    public ServiceApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<ServiceResponse>> GetServicesAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, bool? isActive = null)
    {
        var url = $"api/services?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (isActive.HasValue) url += $"&isActive={isActive}";

        var result = await _apiClient.GetAsync<PagedResponse<ServiceResponse>>(url);
        return result ?? new PagedResponse<ServiceResponse>();
    }

    public async Task<ServiceResponse?> GetServiceByIdAsync(int id)
    {
        return await _apiClient.GetAsync<ServiceResponse>($"api/services/{id}");
    }

    public async Task<ServiceResponse?> CreateServiceAsync(CreateServiceRequest request)
    {
        return await _apiClient.PostAsync<CreateServiceRequest, ServiceResponse>("api/services", request);
    }

    public async Task<(ServiceResponse? Result, string? ErrorMessage)> CreateServiceWithErrorAsync(CreateServiceRequest request)
    {
        return await _apiClient.PostWithErrorAsync<CreateServiceRequest, ServiceResponse>("api/services", request);
    }

    public async Task<ServiceResponse?> UpdateServiceAsync(int id, UpdateServiceRequest request)
    {
        return await _apiClient.PutAsync<UpdateServiceRequest, ServiceResponse>($"api/services/{id}", request);
    }

    public async Task<(ServiceResponse? Result, string? ErrorMessage)> UpdateServiceWithErrorAsync(int id, UpdateServiceRequest request)
    {
        return await _apiClient.PutWithErrorAsync<UpdateServiceRequest, ServiceResponse>($"api/services/{id}", request);
    }

    public async Task<bool> ArchiveServiceAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/services/{id}/deactivate", new { });
        return result != null;
    }

    public async Task<bool> ReactivateServiceAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/services/{id}/reactivate", new { });
        return result != null;
    }

    public async Task<List<ServiceCategoryResponse>> GetCategoriesAsync()
    {
        var result = await _apiClient.GetAsync<List<ServiceCategoryResponse>>("api/services/categories");
        return result ?? new List<ServiceCategoryResponse>();
    }

    public async Task<bool> SaveCategoryAsync(CreateCategoryRequest request, int? id = null)
    {
        if (id.HasValue)
        {
            var result = await _apiClient.PutAsync<CreateCategoryRequest, ServiceCategoryResponse>($"api/services/categories/{id}", request);
            return result != null;
        }
        else
        {
            var result = await _apiClient.PostAsync<CreateCategoryRequest, ServiceCategoryResponse>("api/services/categories", request);
            return result != null;
        }
    }

    public async Task<List<LookupItem>> GetServiceLookupAsync()
    {
        var result = await _apiClient.GetAsync<List<LookupItem>>("api/services/lookup");
        return result ?? new List<LookupItem>();
    }
}

/// <summary>
/// Inventory/Product API service
/// </summary>
public interface IInventoryApiService
{
    Task<PagedResponse<ProductResponse>> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, bool? lowStockOnly = null, string? stockFilter = null, bool? isActive = null);
    Task<ProductResponse?> GetProductByIdAsync(int id);
    Task<ProductResponse?> CreateProductAsync(CreateProductRequest request);
    Task<(ProductResponse? Result, string? ErrorMessage)> CreateProductWithErrorAsync(CreateProductRequest request);
    Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<(ProductResponse? Result, string? ErrorMessage)> UpdateProductWithErrorAsync(int id, UpdateProductRequest request);
    Task<bool> ArchiveProductAsync(int id);
    Task<bool> ReactivateProductAsync(int id);
    Task<List<ProductCategoryResponse>> GetCategoriesAsync();
    Task<bool> SaveCategoryAsync(CreateCategoryRequest request, int? id = null);
    Task<bool> AdjustStockAsync(StockAdjustmentRequest request);
    Task<(bool Success, string? ErrorMessage)> AdjustStockWithErrorAsync(StockAdjustmentRequest request);
    Task<List<ProductResponse>> GetLowStockProductsAsync();
    Task<List<LookupItem>> GetProductLookupAsync();
}

public class InventoryApiService : IInventoryApiService
{
    private readonly IApiClient _apiClient;

    public InventoryApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<ProductResponse>> GetProductsAsync(int page = 1, int pageSize = 20, string? search = null, int? categoryId = null, bool? lowStockOnly = null, string? stockFilter = null, bool? isActive = null)
    {
        var url = $"api/inventory/products?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={Uri.EscapeDataString(search)}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId}";
        if (lowStockOnly.HasValue) url += $"&lowStockOnly={lowStockOnly}";
        if (!string.IsNullOrEmpty(stockFilter)) url += $"&stockFilter={stockFilter}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value}";

        var result = await _apiClient.GetAsync<PagedResponse<ProductResponse>>(url);
        return result ?? new PagedResponse<ProductResponse>();
    }

    public async Task<ProductResponse?> GetProductByIdAsync(int id)
    {
        return await _apiClient.GetAsync<ProductResponse>($"api/inventory/products/{id}");
    }

    public async Task<ProductResponse?> CreateProductAsync(CreateProductRequest request)
    {
        return await _apiClient.PostAsync<CreateProductRequest, ProductResponse>("api/inventory/products", request);
    }

    public async Task<(ProductResponse? Result, string? ErrorMessage)> CreateProductWithErrorAsync(CreateProductRequest request)
    {
        return await _apiClient.PostWithErrorAsync<CreateProductRequest, ProductResponse>("api/inventory/products", request);
    }

    public async Task<ProductResponse?> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        return await _apiClient.PutAsync<UpdateProductRequest, ProductResponse>($"api/inventory/products/{id}", request);
    }

    public async Task<(ProductResponse? Result, string? ErrorMessage)> UpdateProductWithErrorAsync(int id, UpdateProductRequest request)
    {
        return await _apiClient.PutWithErrorAsync<UpdateProductRequest, ProductResponse>($"api/inventory/products/{id}", request);
    }

    public async Task<bool> ArchiveProductAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/inventory/products/{id}/deactivate", new { });
        return result != null;
    }

    public async Task<bool> ReactivateProductAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/inventory/products/{id}/reactivate", new { });
        return result != null;
    }

    public async Task<List<ProductCategoryResponse>> GetCategoriesAsync()
    {
        var result = await _apiClient.GetAsync<List<ProductCategoryResponse>>("api/inventory/categories");
        return result ?? new List<ProductCategoryResponse>();
    }

    public async Task<bool> SaveCategoryAsync(CreateCategoryRequest request, int? id = null)
    {
        if (id.HasValue)
        {
            var result = await _apiClient.PutAsync<CreateCategoryRequest, ProductCategoryResponse>($"api/inventory/categories/{id}", request);
            return result != null;
        }
        else
        {
            var result = await _apiClient.PostAsync<CreateCategoryRequest, ProductCategoryResponse>("api/inventory/categories", request);
            return result != null;
        }
    }

    public async Task<bool> AdjustStockAsync(StockAdjustmentRequest request)
    {
        var result = await _apiClient.PostAsync<StockAdjustmentRequest, object>("api/inventory/stock/adjust", request);
        return result != null;
    }

    public async Task<(bool Success, string? ErrorMessage)> AdjustStockWithErrorAsync(StockAdjustmentRequest request)
    {
        var (result, error) = await _apiClient.PostWithErrorAsync<StockAdjustmentRequest, object>("api/inventory/stock/adjust", request);
        return (result != null, error);
    }

    public async Task<List<ProductResponse>> GetLowStockProductsAsync()
    {
        var result = await _apiClient.GetAsync<List<ProductResponse>>("api/inventory/products/low-stock");
        return result ?? new List<ProductResponse>();
    }

    public async Task<List<LookupItem>> GetProductLookupAsync()
    {
        var result = await _apiClient.GetAsync<List<LookupItem>>("api/inventory/lookup");
        return result ?? new List<LookupItem>();
    }
}

/// <summary>
/// Transaction API service
/// </summary>
public interface ITransactionApiService
{
    Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(int page = 1, int pageSize = 20, DateTime? date = null, string? search = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null);
    Task<PagedResponse<TransactionResponse>> GetCustomerTransactionsAsync(int customerId, int page = 1, int pageSize = 100);
    Task<TransactionResponse?> GetTransactionByIdAsync(int id);
    Task<TransactionResponse?> CreateTransactionAsync(CreateTransactionRequest request);
    Task<TransactionResponse?> CreatePendingTransactionAsync(int appointmentId);
    Task<TransactionResponse?> GetPendingTransactionByAppointmentAsync(int appointmentId);
    Task<TransactionResponse?> FinalizePendingTransactionAsync(int transactionId, FinalizePendingTransactionRequest request);
    Task<bool> RefundTransactionAsync(int id);
    Task<decimal> GetTodayRevenueAsync();
    Task<TransactionStatsResponse?> GetTransactionStatsAsync();
    Task<TransactionSalesReportResponse?> GetTransactionSalesReportAsync(DateTime startDate, DateTime endDate);
}

public class TransactionApiService : ITransactionApiService
{
    private readonly IApiClient _apiClient;

    public TransactionApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(int page = 1, int pageSize = 20, DateTime? date = null, string? search = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null)
    {
        var url = $"api/transactions?page={page}&pageSize={pageSize}";
        if (date.HasValue) url += $"&dateFrom={date.Value:yyyy-MM-dd}&dateTo={date.Value:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(search)) url += $"&searchTerm={Uri.EscapeDataString(search)}";
        if (startDate.HasValue) url += $"&dateFrom={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&dateTo={endDate.Value:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(status)) url += $"&paymentStatus={status}";

        var result = await _apiClient.GetAsync<PagedResponse<TransactionResponse>>(url);
        return result ?? new PagedResponse<TransactionResponse>();
    }

    public async Task<PagedResponse<TransactionResponse>> GetCustomerTransactionsAsync(int customerId, int page = 1, int pageSize = 100)
    {
        var result = await _apiClient.GetAsync<PagedResponse<TransactionResponse>>($"api/transactions/customer/{customerId}?page={page}&pageSize={pageSize}");
        return result ?? new PagedResponse<TransactionResponse>();
    }

    public async Task<TransactionResponse?> GetTransactionByIdAsync(int id)
    {
        return await _apiClient.GetAsync<TransactionResponse>($"api/transactions/{id}");
    }

    public async Task<TransactionResponse?> CreateTransactionAsync(CreateTransactionRequest request)
    {
        var (result, error) = await _apiClient.PostWithErrorAsync<CreateTransactionRequest, TransactionResponse>("api/transactions", request);
        if (result == null && error != null)
            throw new InvalidOperationException(error);
        return result;
    }

    public async Task<TransactionResponse?> CreatePendingTransactionAsync(int appointmentId)
    {
        var (result, error) = await _apiClient.PostWithErrorAsync<object, TransactionResponse>($"api/transactions/{appointmentId}/pending", new { });
        if (result == null && error != null)
            throw new InvalidOperationException(error);
        return result;
    }

    public async Task<TransactionResponse?> GetPendingTransactionByAppointmentAsync(int appointmentId)
    {
        return await _apiClient.GetAsync<TransactionResponse>($"api/transactions/by-appointment/{appointmentId}/pending");
    }

    public async Task<TransactionResponse?> FinalizePendingTransactionAsync(int transactionId, FinalizePendingTransactionRequest request)
    {
        var (result, error) = await _apiClient.PostWithErrorAsync<FinalizePendingTransactionRequest, TransactionResponse>($"api/transactions/{transactionId}/finalize", request);
        if (result == null && error != null)
            throw new InvalidOperationException(error);
        return result;
    }

    public async Task<bool> RefundTransactionAsync(int id)
    {
        // First fetch the transaction to get the correct amount and payment method
        var transaction = await _apiClient.GetAsync<TransactionResponse>($"api/transactions/{id}");
        if (transaction == null) return false;

        var refundMethod = transaction.PaymentMethod switch
        {
            "Card" => "Card Reversal",
            _ => "Cash"
        };

        var request = new
        {
            RefundAmount = transaction.TotalAmount,
            RefundType = "Full",
            RefundMethod = refundMethod,
            Reason = "Customer request"
        };
        var result = await _apiClient.PostAsync<object, object>($"api/transactions/{id}/refund", request);
        return result != null;
    }

    public async Task<decimal> GetTodayRevenueAsync()
    {
        // Use transactions list and sum completed transactions for today
        var result = await _apiClient.GetAsync<PagedResponse<TransactionResponse>>($"api/transactions?startDate={PhilippineTime.Today:yyyy-MM-dd}&endDate={PhilippineTime.Today:yyyy-MM-dd}&pageSize=1000");
        return result?.Items?.Where(t => t.PaymentStatus == "Completed").Sum(t => t.TotalAmount) ?? 0;
    }

    public async Task<TransactionStatsResponse?> GetTransactionStatsAsync()
    {
        return await _apiClient.GetAsync<TransactionStatsResponse>("api/transactions/stats");
    }

    public async Task<TransactionSalesReportResponse?> GetTransactionSalesReportAsync(DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<TransactionSalesReportResponse>($"api/transactions/reports/sales?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }
}

// =============================================================================
// Payroll API Service
// =============================================================================

public interface IPayrollApiService
{
    // Payroll Periods
    Task<PagedResponse<PayrollPeriodResponse>> GetPayrollPeriodsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<PayrollPeriodResponse?> GetPayrollPeriodByIdAsync(int id);
    Task<PayrollPeriodResponse?> GetPayrollPeriodByDateAsync(DateTime startDate, DateTime endDate);
    Task<PayrollPeriodResponse?> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request);
    Task<PayrollPeriodResponse?> FinalizePayrollPeriodAsync(int id);
    Task<(PayrollPeriodResponse? Result, string? ErrorMessage)> FinalizePayrollPeriodWithErrorAsync(int id);
    Task<PayrollPeriodResponse?> ReopenPayrollPeriodAsync(int id);

    // Payroll Records
    Task<PagedResponse<PayrollRecordResponse>> GetPayrollRecordsAsync(int periodId, int page = 1, int pageSize = 50);
    Task<PagedResponse<PayrollRecordResponse>> SearchPayrollRecordsAsync(int? periodId = null, int? employeeId = null, int page = 1, int pageSize = 50);
    Task<PayrollRecordResponse?> CreatePayrollRecordAsync(CreatePayrollRecordRequest request);
    Task<PayrollRecordResponse?> UpdatePayrollRecordAsync(int id, UpdatePayrollRecordRequest request);
    Task<bool> GeneratePayrollRecordsAsync(int periodId);
    Task<(bool Success, string? ErrorMessage)> GeneratePayrollRecordsWithErrorAsync(int periodId, List<int>? employeeIds = null);

    // Bonuses & Adjustments
    Task<List<BonusResponse>> GetEmployeeBonusesAsync(int employeeId);
    Task<BonusResponse?> AddBonusAsync(CreateBonusRequest request);
    Task<(BonusResponse? Result, string? ErrorMessage)> AddBonusWithErrorAsync(CreateBonusRequest request);

    // Payroll Preview
    Task<List<PayrollRecordResponse>> PreviewPayrollAsync(DateTime startDate, DateTime endDate);
}

public class PayrollApiService : IPayrollApiService
{
    private readonly IApiClient _apiClient;

    public PayrollApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PagedResponse<PayrollPeriodResponse>> GetPayrollPeriodsAsync(int page = 1, int pageSize = 20, string? status = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var url = $"api/payroll/periods?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        var result = await _apiClient.GetAsync<PagedResponse<PayrollPeriodResponse>>(url);
        return result ?? new PagedResponse<PayrollPeriodResponse>();
    }

    public async Task<PayrollPeriodResponse?> GetPayrollPeriodByIdAsync(int id)
    {
        return await _apiClient.GetAsync<PayrollPeriodResponse>($"api/payroll/periods/{id}");
    }

    public async Task<PayrollPeriodResponse?> GetPayrollPeriodByDateAsync(DateTime startDate, DateTime endDate)
    {
        // Get periods that overlap with the given date range
        var periods = await GetPayrollPeriodsAsync(1, 10, null, startDate, endDate);
        return periods.Items.FirstOrDefault();
    }

    public async Task<PayrollPeriodResponse?> CreatePayrollPeriodAsync(CreatePayrollPeriodRequest request)
    {
        return await _apiClient.PostAsync<CreatePayrollPeriodRequest, PayrollPeriodResponse>("api/payroll/periods", request);
    }

    public async Task<PayrollPeriodResponse?> FinalizePayrollPeriodAsync(int id)
    {
        return await _apiClient.PostAsync<object, PayrollPeriodResponse>($"api/payroll/periods/{id}/finalize", new { });
    }

    public async Task<(PayrollPeriodResponse? Result, string? ErrorMessage)> FinalizePayrollPeriodWithErrorAsync(int id)
    {
        return await _apiClient.PostWithErrorAsync<object, PayrollPeriodResponse>($"api/payroll/periods/{id}/finalize", new { });
    }

    public async Task<PayrollPeriodResponse?> ReopenPayrollPeriodAsync(int id)
    {
        return await _apiClient.PostAsync<object, PayrollPeriodResponse>($"api/payroll/periods/{id}/reopen", new { });
    }

    public async Task<PagedResponse<PayrollRecordResponse>> GetPayrollRecordsAsync(int periodId, int page = 1, int pageSize = 50)
    {
        var result = await _apiClient.GetAsync<PagedResponse<PayrollRecordResponse>>($"api/payroll/periods/{periodId}/records?page={page}&pageSize={pageSize}");
        return result ?? new PagedResponse<PayrollRecordResponse>();
    }

    public async Task<PagedResponse<PayrollRecordResponse>> SearchPayrollRecordsAsync(int? periodId = null, int? employeeId = null, int page = 1, int pageSize = 50)
    {
        var url = $"api/payroll/records?page={page}&pageSize={pageSize}";
        if (periodId.HasValue) url += $"&payrollPeriodId={periodId}";
        if (employeeId.HasValue) url += $"&employeeId={employeeId}";
        var result = await _apiClient.GetAsync<PagedResponse<PayrollRecordResponse>>(url);
        return result ?? new PagedResponse<PayrollRecordResponse>();
    }

    public async Task<PayrollRecordResponse?> CreatePayrollRecordAsync(CreatePayrollRecordRequest request)
    {
        return await _apiClient.PostAsync<CreatePayrollRecordRequest, PayrollRecordResponse>("api/payroll/records", request);
    }

    public async Task<PayrollRecordResponse?> UpdatePayrollRecordAsync(int id, UpdatePayrollRecordRequest request)
    {
        return await _apiClient.PutAsync<UpdatePayrollRecordRequest, PayrollRecordResponse>($"api/payroll/records/{id}", request);
    }

    public async Task<bool> GeneratePayrollRecordsAsync(int periodId)
    {
        var request = new { PayrollPeriodId = periodId };
        var result = await _apiClient.PostAsync<object, object>("api/payroll/generate", request);
        return result != null;
    }

    public async Task<(bool Success, string? ErrorMessage)> GeneratePayrollRecordsWithErrorAsync(int periodId, List<int>? employeeIds = null)
    {
        var request = new { PayrollPeriodId = periodId, EmployeeIds = employeeIds };
        var (result, error) = await _apiClient.PostWithErrorAsync<object, object>("api/payroll/generate", request);
        return (result != null, error);
    }

    public async Task<List<BonusResponse>> GetEmployeeBonusesAsync(int employeeId)
    {
        var result = await _apiClient.GetAsync<List<BonusResponse>>($"api/employees/{employeeId}/bonuses");
        return result ?? new List<BonusResponse>();
    }

    public async Task<BonusResponse?> AddBonusAsync(CreateBonusRequest request)
    {
        return await _apiClient.PostAsync<CreateBonusRequest, BonusResponse>("api/payroll/bonuses", request);
    }

    public async Task<(BonusResponse? Result, string? ErrorMessage)> AddBonusWithErrorAsync(CreateBonusRequest request)
    {
        return await _apiClient.PostWithErrorAsync<CreateBonusRequest, BonusResponse>("api/payroll/bonuses", request);
    }

    public async Task<List<PayrollRecordResponse>> PreviewPayrollAsync(DateTime startDate, DateTime endDate)
    {
        var url = $"api/payroll/preview?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        var result = await _apiClient.GetAsync<List<PayrollRecordResponse>>(url);
        return result ?? new List<PayrollRecordResponse>();
    }
}

// =============================================================================
// Time & Attendance API Service
// =============================================================================

public interface ITimeAttendanceApiService
{
    // Schedules
    Task<List<ScheduleResponse>> GetEmployeeSchedulesAsync(int employeeId, DateTime? effectiveDate = null);
    Task<WeeklyScheduleResponse?> GetWeeklyScheduleAsync(int employeeId, DateTime? asOfDate = null);
    Task<ScheduleResponse?> CreateScheduleAsync(CreateScheduleRequest request);

    // Attendance
    Task<List<AttendanceRecordResponse>> GetAttendanceAsync(int? employeeId = null, DateTime? date = null);
    Task<AttendanceRecordResponse?> ClockInAsync(int employeeId);
    Task<(AttendanceRecordResponse? Result, string? ErrorMessage)> ClockInWithErrorAsync(int employeeId);
    Task<AttendanceRecordResponse?> ClockOutAsync(int employeeId);
    Task<(AttendanceRecordResponse? Result, string? ErrorMessage)> ClockOutWithErrorAsync(int employeeId);
    Task<AttendanceRecordResponse?> StartBreakAsync(int employeeId);
    Task<AttendanceRecordResponse?> EndBreakAsync(int employeeId);
    Task<AttendanceSummaryResponse?> GetAttendanceSummaryAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task<AttendanceRecordResponse?> ApproveAttendanceAsync(int attendanceId);

    // Time Off / Leave
    Task<PagedResponse<TimeOffResponse>> GetTimeOffRequestsAsync(int? employeeId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<TimeOffResponse?> CreateTimeOffRequestAsync(CreateTimeOffRequest request);
    Task<TimeOffResponse?> ApproveTimeOffAsync(int id);
    Task<TimeOffResponse?> RejectTimeOffAsync(int id, string reason);

    // Live Status
    Task<List<LiveAttendanceStatus>> GetLiveStatusAsync();

    // Manual Entry
    Task<AttendanceRecordResponse?> CreateManualEntryAsync(CreateManualEntryRequest request);
}

public class TimeAttendanceApiService : ITimeAttendanceApiService
{
    private readonly IApiClient _apiClient;

    public TimeAttendanceApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<ScheduleResponse>> GetEmployeeSchedulesAsync(int employeeId, DateTime? effectiveDate = null)
    {
        var url = $"api/time-attendance/employees/{employeeId}/schedules";
        if (effectiveDate.HasValue) url += $"?effectiveDate={effectiveDate.Value:yyyy-MM-dd}";
        var result = await _apiClient.GetAsync<List<ScheduleResponse>>(url);
        return result ?? new List<ScheduleResponse>();
    }

    public async Task<WeeklyScheduleResponse?> GetWeeklyScheduleAsync(int employeeId, DateTime? asOfDate = null)
    {
        var url = $"api/time-attendance/employees/{employeeId}/weekly-schedule";
        if (asOfDate.HasValue) url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
        return await _apiClient.GetAsync<WeeklyScheduleResponse>(url);
    }

    public async Task<ScheduleResponse?> CreateScheduleAsync(CreateScheduleRequest request)
    {
        return await _apiClient.PostAsync<CreateScheduleRequest, ScheduleResponse>("api/time-attendance/schedules", request);
    }

    public async Task<List<AttendanceRecordResponse>> GetAttendanceAsync(int? employeeId = null, DateTime? date = null)
    {
        var url = "api/time-attendance/attendance";
        var queryParams = new List<string>();
        if (employeeId.HasValue) queryParams.Add($"employeeId={employeeId}");
        if (date.HasValue) queryParams.Add($"date={date.Value:yyyy-MM-dd}");
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

        var result = await _apiClient.GetAsync<List<AttendanceRecordResponse>>(url);
        return result ?? new List<AttendanceRecordResponse>();
    }

    public async Task<AttendanceRecordResponse?> ClockInAsync(int employeeId)
    {
        return await _apiClient.PostAsync<object, AttendanceRecordResponse>($"api/time-attendance/employees/{employeeId}/clock-in", new { });
    }

    public async Task<(AttendanceRecordResponse? Result, string? ErrorMessage)> ClockInWithErrorAsync(int employeeId)
    {
        return await _apiClient.PostWithErrorAsync<object, AttendanceRecordResponse>($"api/time-attendance/employees/{employeeId}/clock-in", new { });
    }

    public async Task<AttendanceRecordResponse?> ClockOutAsync(int employeeId)
    {
        return await _apiClient.PostAsync<object, AttendanceRecordResponse>($"api/time-attendance/employees/{employeeId}/clock-out", new { });
    }

    public async Task<(AttendanceRecordResponse? Result, string? ErrorMessage)> ClockOutWithErrorAsync(int employeeId)
    {
        return await _apiClient.PostWithErrorAsync<object, AttendanceRecordResponse>($"api/time-attendance/employees/{employeeId}/clock-out", new { });
    }

    public async Task<AttendanceRecordResponse?> StartBreakAsync(int employeeId)
    {
        return await _apiClient.PostAsync<object, AttendanceRecordResponse>($"api/time-attendance/employees/{employeeId}/start-break", new { });
    }

    public async Task<AttendanceRecordResponse?> EndBreakAsync(int employeeId)
    {
        return await _apiClient.PostAsync<object, AttendanceRecordResponse>($"api/time-attendance/employees/{employeeId}/end-break", new { });
    }

    public async Task<AttendanceSummaryResponse?> GetAttendanceSummaryAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<AttendanceSummaryResponse>($"api/time-attendance/employees/{employeeId}/attendance-summary?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }

    public async Task<PagedResponse<TimeOffResponse>> GetTimeOffRequestsAsync(int? employeeId = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var url = $"api/time-attendance/time-off?page={page}&pageSize={pageSize}";
        if (employeeId.HasValue) url += $"&employeeId={employeeId}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={status}";
        var result = await _apiClient.GetAsync<PagedResponse<TimeOffResponse>>(url);
        return result ?? new PagedResponse<TimeOffResponse>();
    }

    public async Task<TimeOffResponse?> CreateTimeOffRequestAsync(CreateTimeOffRequest request)
    {
        return await _apiClient.PostAsync<CreateTimeOffRequest, TimeOffResponse>("api/time-attendance/time-off", request);
    }

    public async Task<AttendanceRecordResponse?> ApproveAttendanceAsync(int attendanceId)
    {
        return await _apiClient.PostAsync<object, AttendanceRecordResponse>($"api/time-attendance/attendance/{attendanceId}/approve", new { });
    }

    public async Task<TimeOffResponse?> ApproveTimeOffAsync(int id)
    {
        return await _apiClient.PostAsync<object, TimeOffResponse>($"api/time-attendance/time-off/{id}/approve", new { });
    }

    public async Task<TimeOffResponse?> RejectTimeOffAsync(int id, string reason)
    {
        return await _apiClient.PostAsync<object, TimeOffResponse>($"api/time-attendance/time-off/{id}/reject", new { reason });
    }

    public async Task<List<LiveAttendanceStatus>> GetLiveStatusAsync()
    {
        var result = await _apiClient.GetAsync<List<LiveAttendanceStatus>>("api/time-attendance/live-status");
        return result ?? new List<LiveAttendanceStatus>();
    }

    public async Task<AttendanceRecordResponse?> CreateManualEntryAsync(CreateManualEntryRequest request)
    {
        return await _apiClient.PostAsync<CreateManualEntryRequest, AttendanceRecordResponse>("api/time-attendance/manual-entry", request);
    }
}

// =============================================================================
// Reports API Service
// =============================================================================

public interface IReportsApiService
{
    Task<DashboardReportResponse?> GetDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<QuickStatsResponse?> GetQuickStatsAsync();
    Task<SalesReportResponse?> GetSalesReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null);
    Task<ServicePerformanceResponse?> GetServicePerformanceAsync(DateTime startDate, DateTime endDate);
    Task<EmployeePerformanceResponse?> GetEmployeePerformanceAsync(DateTime startDate, DateTime endDate);
    Task<CustomerReportResponse?> GetCustomerReportAsync(DateTime startDate, DateTime endDate);
    Task<InventoryReportResponse?> GetInventoryReportAsync();
    Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> ExportReportAsync(string reportType, string format, DateTime startDate, DateTime endDate);
}

public class ReportsApiService : IReportsApiService
{
    private readonly IApiClient _apiClient;

    public ReportsApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<DashboardReportResponse?> GetDashboardAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var url = "api/reports/dashboard";
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

        return await _apiClient.GetAsync<DashboardReportResponse>(url);
    }

    public async Task<QuickStatsResponse?> GetQuickStatsAsync()
    {
        return await _apiClient.GetAsync<QuickStatsResponse>("api/reports/quick-stats");
    }

    public async Task<SalesReportResponse?> GetSalesReportAsync(DateTime startDate, DateTime endDate, string? groupBy = null)
    {
        var url = $"api/reports/sales?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(groupBy)) url += $"&groupBy={groupBy}";
        return await _apiClient.GetAsync<SalesReportResponse>(url);
    }

    public async Task<ServicePerformanceResponse?> GetServicePerformanceAsync(DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<ServicePerformanceResponse>($"api/reports/services/performance?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }

    public async Task<EmployeePerformanceResponse?> GetEmployeePerformanceAsync(DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<EmployeePerformanceResponse>($"api/reports/employees/performance?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }

    public async Task<CustomerReportResponse?> GetCustomerReportAsync(DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<CustomerReportResponse>($"api/reports/customers/analytics?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }

    public async Task<InventoryReportResponse?> GetInventoryReportAsync()
    {
        return await _apiClient.GetAsync<InventoryReportResponse>("api/reports/inventory");
    }

    public async Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> ExportReportAsync(string reportType, string format, DateTime startDate, DateTime endDate)
    {
        var request = new { ReportType = reportType, Format = format, StartDate = startDate, EndDate = endDate };
        return await _apiClient.PostForFileAsync("api/reports/export", request);
    }
}

/// <summary>
/// Notification API service - connects to the API notification endpoints
/// </summary>
public interface INotificationApiService
{
    Task<List<NotificationResponse>> GetMyNotificationsAsync(bool unreadOnly = false);
    Task<int> GetUnreadCountAsync();
    Task<NotificationResponse?> MarkAsReadAsync(int notificationId);
    Task<int> MarkAllAsReadAsync();
    Task<bool> DeleteNotificationAsync(int notificationId);
    Task<NotificationResponse?> CreateNotificationAsync(CreateNotificationRequest request);
}

public class NotificationApiService : INotificationApiService
{
    private readonly IApiClient _apiClient;

    public NotificationApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<NotificationResponse>> GetMyNotificationsAsync(bool unreadOnly = false)
    {
        var url = $"api/notifications/my-notifications?unreadOnly={unreadOnly}";
        var result = await _apiClient.GetAsync<List<NotificationResponse>>(url);
        return result ?? new List<NotificationResponse>();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var result = await _apiClient.GetAsync<int>("api/notifications/unread-count");
        return result;
    }

    public async Task<NotificationResponse?> MarkAsReadAsync(int notificationId)
    {
        return await _apiClient.PutAsync<object, NotificationResponse>($"api/notifications/{notificationId}/read", new { });
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        var result = await _apiClient.PutAsync<object, MarkAllReadResponse>($"api/notifications/read-all", new { });
        return result?.MarkedAsRead ?? 0;
    }

    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        return await _apiClient.DeleteAsync($"api/notifications/{notificationId}");
    }

    public async Task<NotificationResponse?> CreateNotificationAsync(CreateNotificationRequest request)
    {
        return await _apiClient.PostAsync<CreateNotificationRequest, NotificationResponse>("api/notifications", request);
    }
}

// Helper class for MarkAllAsRead response
public class MarkAllReadResponse
{
    public int MarkedAsRead { get; set; }
}

// =============================================================================
// Accounting API Service
// =============================================================================

public interface IAccountingApiService
{
    // Chart of Accounts
    Task<PagedResponse<ChartOfAccountResponse>> GetAccountsAsync(string? searchTerm = null, string? accountType = null, bool? isActive = null, int page = 1, int pageSize = 50);
    Task<List<ChartOfAccountResponse>> GetAccountsHierarchyAsync();
    Task<ChartOfAccountResponse?> GetAccountByIdAsync(int id);
    Task<ChartOfAccountResponse?> CreateAccountAsync(CreateAccountRequest request);
    Task<ChartOfAccountResponse?> UpdateAccountAsync(int id, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(int id);

    // Journal Entries
    Task<PagedResponse<JournalEntryResponse>> GetJournalEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null, int? accountId = null, string? searchTerm = null, int page = 1, int pageSize = 20);
    Task<JournalEntryResponse?> GetJournalEntryByIdAsync(int id);
    Task<JournalEntryResponse?> CreateJournalEntryAsync(CreateJournalEntryRequest request);
    Task<bool> VoidJournalEntryAsync(int id);

    // Financial Reports
    Task<TrialBalanceResponse?> GetTrialBalanceAsync(DateTime? asOfDate = null);
    Task<IncomeStatementResponse?> GetIncomeStatementAsync(DateTime startDate, DateTime endDate);
    Task<BalanceSheetResponse?> GetBalanceSheetAsync(DateTime? asOfDate = null);
    Task<AccountLedgerResponse?> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate);

    // Expenses
    Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
    Task<ExpenseResponse?> CreateExpenseAsync(CreateExpenseRequest request);

    // Income
    Task<PagedResponse<IncomeRecordResponse>> GetIncomeRecordsAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20);
    Task<IncomeRecordResponse?> CreateIncomeAsync(CreateIncomeRequest request);

    // Invoices
    Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(string? status = null, int? customerId = null, int page = 1, int pageSize = 20);
    Task<InvoiceResponse?> GetInvoiceByIdAsync(int id);
    Task<InvoiceResponse?> CreateInvoiceAsync(CreateInvoiceRequest request);
    Task<InvoiceResponse?> UpdateInvoiceStatusAsync(int id, string status);
    Task<InvoiceResponse?> RecordInvoicePaymentAsync(int id, decimal amount);

    // Summary
    Task<AccountingSummaryResponse?> GetAccountingSummaryAsync(DateTime? startDate = null, DateTime? endDate = null);

    // Sub-page Summaries
    Task<ExpenseSummaryResponse?> GetExpenseSummaryAsync();
    Task<IncomeSummaryResponse?> GetIncomeSummaryAsync();
    Task<JournalSummaryResponse?> GetJournalSummaryAsync();
}

public class AccountingApiService : IAccountingApiService
{
    private readonly IApiClient _apiClient;

    public AccountingApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Chart of Accounts
    public async Task<PagedResponse<ChartOfAccountResponse>> GetAccountsAsync(string? searchTerm = null, string? accountType = null, bool? isActive = null, int page = 1, int pageSize = 50)
    {
        var url = $"api/accounting/accounts?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        if (!string.IsNullOrEmpty(accountType)) url += $"&accountType={Uri.EscapeDataString(accountType)}";
        if (isActive.HasValue) url += $"&isActive={isActive.Value}";

        var result = await _apiClient.GetAsync<PagedResponse<ChartOfAccountResponse>>(url);
        return result ?? new PagedResponse<ChartOfAccountResponse>();
    }

    public async Task<List<ChartOfAccountResponse>> GetAccountsHierarchyAsync()
    {
        var result = await _apiClient.GetAsync<List<ChartOfAccountResponse>>("api/accounting/accounts/hierarchy");
        return result ?? new List<ChartOfAccountResponse>();
    }

    public async Task<ChartOfAccountResponse?> GetAccountByIdAsync(int id)
    {
        return await _apiClient.GetAsync<ChartOfAccountResponse>($"api/accounting/accounts/{id}");
    }

    public async Task<ChartOfAccountResponse?> CreateAccountAsync(CreateAccountRequest request)
    {
        return await _apiClient.PostAsync<CreateAccountRequest, ChartOfAccountResponse>("api/accounting/accounts", request);
    }

    public async Task<ChartOfAccountResponse?> UpdateAccountAsync(int id, UpdateAccountRequest request)
    {
        return await _apiClient.PutAsync<UpdateAccountRequest, ChartOfAccountResponse>($"api/accounting/accounts/{id}", request);
    }

    public async Task<bool> DeleteAccountAsync(int id)
    {
        return await _apiClient.DeleteAsync($"api/accounting/accounts/{id}");
    }

    // Journal Entries
    public async Task<PagedResponse<JournalEntryResponse>> GetJournalEntriesAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null, int? accountId = null, string? searchTerm = null, int page = 1, int pageSize = 20)
    {
        var url = $"api/accounting/journal-entries?pageNumber={page}&pageSize={pageSize}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (accountId.HasValue) url += $"&accountId={accountId.Value}";
        if (!string.IsNullOrEmpty(searchTerm)) url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";

        var result = await _apiClient.GetAsync<PagedResponse<JournalEntryResponse>>(url);
        return result ?? new PagedResponse<JournalEntryResponse>();
    }

    public async Task<JournalEntryResponse?> GetJournalEntryByIdAsync(int id)
    {
        return await _apiClient.GetAsync<JournalEntryResponse>($"api/accounting/journal-entries/{id}");
    }

    public async Task<JournalEntryResponse?> CreateJournalEntryAsync(CreateJournalEntryRequest request)
    {
        return await _apiClient.PostAsync<CreateJournalEntryRequest, JournalEntryResponse>("api/accounting/journal-entries", request);
    }

    public async Task<bool> VoidJournalEntryAsync(int id)
    {
        var result = await _apiClient.PostAsync<object, object>($"api/accounting/journal-entries/{id}/void", new { });
        return result != null;
    }

    // Financial Reports
    public async Task<TrialBalanceResponse?> GetTrialBalanceAsync(DateTime? asOfDate = null)
    {
        var url = "api/accounting/reports/trial-balance";
        if (asOfDate.HasValue) url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
        return await _apiClient.GetAsync<TrialBalanceResponse>(url);
    }

    public async Task<IncomeStatementResponse?> GetIncomeStatementAsync(DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<IncomeStatementResponse>($"api/accounting/reports/income-statement?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }

    public async Task<BalanceSheetResponse?> GetBalanceSheetAsync(DateTime? asOfDate = null)
    {
        var url = "api/accounting/reports/balance-sheet";
        if (asOfDate.HasValue) url += $"?asOfDate={asOfDate.Value:yyyy-MM-dd}";
        return await _apiClient.GetAsync<BalanceSheetResponse>(url);
    }

    public async Task<AccountLedgerResponse?> GetAccountLedgerAsync(int accountId, DateTime startDate, DateTime endDate)
    {
        return await _apiClient.GetAsync<AccountLedgerResponse>($"api/accounting/accounts/{accountId}/ledger?startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}");
    }

    // Expenses
    public async Task<PagedResponse<ExpenseResponse>> GetExpensesAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
    {
        var url = $"api/accounting/expenses?pageNumber={page}&pageSize={pageSize}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";

        var result = await _apiClient.GetAsync<PagedResponse<ExpenseResponse>>(url);
        return result ?? new PagedResponse<ExpenseResponse>();
    }

    public async Task<ExpenseResponse?> CreateExpenseAsync(CreateExpenseRequest request)
    {
        return await _apiClient.PostAsync<CreateExpenseRequest, ExpenseResponse>("api/accounting/expenses", request);
    }

    // Income
    public async Task<PagedResponse<IncomeRecordResponse>> GetIncomeRecordsAsync(DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
    {
        var url = $"api/accounting/income?pageNumber={page}&pageSize={pageSize}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";

        var result = await _apiClient.GetAsync<PagedResponse<IncomeRecordResponse>>(url);
        return result ?? new PagedResponse<IncomeRecordResponse>();
    }

    public async Task<IncomeRecordResponse?> CreateIncomeAsync(CreateIncomeRequest request)
    {
        return await _apiClient.PostAsync<CreateIncomeRequest, IncomeRecordResponse>("api/accounting/income", request);
    }

    // Invoices
    public async Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(string? status = null, int? customerId = null, int page = 1, int pageSize = 20)
    {
        var url = $"api/accounting/invoices?pageNumber={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (customerId.HasValue) url += $"&customerId={customerId.Value}";

        var result = await _apiClient.GetAsync<PagedResponse<InvoiceResponse>>(url);
        return result ?? new PagedResponse<InvoiceResponse>();
    }

    public async Task<InvoiceResponse?> GetInvoiceByIdAsync(int id)
    {
        return await _apiClient.GetAsync<InvoiceResponse>($"api/accounting/invoices/{id}");
    }

    public async Task<InvoiceResponse?> CreateInvoiceAsync(CreateInvoiceRequest request)
    {
        return await _apiClient.PostAsync<CreateInvoiceRequest, InvoiceResponse>("api/accounting/invoices", request);
    }

    public async Task<InvoiceResponse?> UpdateInvoiceStatusAsync(int id, string status)
    {
        return await _apiClient.PostAsync<object, InvoiceResponse>($"api/accounting/invoices/{id}/status", new { Status = status });
    }

    public async Task<InvoiceResponse?> RecordInvoicePaymentAsync(int id, decimal amount)
    {
        return await _apiClient.PostAsync<object, InvoiceResponse>($"api/accounting/invoices/{id}/payment", new { Amount = amount });
    }

    // Summary
    public async Task<AccountingSummaryResponse?> GetAccountingSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var url = "api/accounting/summary";
        var queryParams = new List<string>();
        if (startDate.HasValue) queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
        if (endDate.HasValue) queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);
        return await _apiClient.GetAsync<AccountingSummaryResponse>(url);
    }

    // Sub-page Summaries
    public async Task<ExpenseSummaryResponse?> GetExpenseSummaryAsync()
    {
        return await _apiClient.GetAsync<ExpenseSummaryResponse>("api/accounting/expenses/summary");
    }

    public async Task<IncomeSummaryResponse?> GetIncomeSummaryAsync()
    {
        return await _apiClient.GetAsync<IncomeSummaryResponse>("api/accounting/income/summary");
    }

    public async Task<JournalSummaryResponse?> GetJournalSummaryAsync()
    {
        return await _apiClient.GetAsync<JournalSummaryResponse>("api/accounting/journal-entries/summary");
    }
}

/// <summary>
/// Customer Segmentation API Service for DBSCAN clustering
/// </summary>
public interface ICustomerSegmentationService
{
    Task<ClusteringResultResponse?> RunDbscanAnalysisAsync(DbscanParametersRequest parameters);
    Task<ClusteringStatusResponse?> GetClusteringStatusAsync();
    Task<List<CustomerSegmentResponse>> GetAllSegmentsAsync();
    Task<SegmentDetailResponse?> GetSegmentDetailsAsync(int segmentId);
    Task<List<CustomerListItem>> GetCustomersBySegmentAsync(string segmentName);
    Task<CustomerRfmMetricsResponse?> GetCustomerRfmMetricsAsync(int customerId);
    Task RecalculateSegmentStatsAsync();
    Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> ExportSegmentationAsync(string format, string? segmentName = null);
}

public class CustomerSegmentationService : ICustomerSegmentationService
{
    private readonly IApiClient _apiClient;

    public CustomerSegmentationService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ClusteringResultResponse?> RunDbscanAnalysisAsync(DbscanParametersRequest parameters)
    {
        var (result, error) = await _apiClient.PostWithErrorAsync<DbscanParametersRequest, ClusteringResultResponse>(
            "api/customers/segments/analyze", parameters);
        if (result == null && error != null)
            return new ClusteringResultResponse { Success = false, Message = $"Analysis failed: {error}" };
        return result;
    }

    public async Task<ClusteringStatusResponse?> GetClusteringStatusAsync()
    {
        return await _apiClient.GetAsync<ClusteringStatusResponse>("api/customers/segments/status");
    }

    public async Task<List<CustomerSegmentResponse>> GetAllSegmentsAsync()
    {
        var result = await _apiClient.GetAsync<List<CustomerSegmentResponse>>("api/customers/segments");
        return result ?? new List<CustomerSegmentResponse>();
    }

    public async Task<SegmentDetailResponse?> GetSegmentDetailsAsync(int segmentId)
    {
        return await _apiClient.GetAsync<SegmentDetailResponse>($"api/customers/segments/{segmentId}/details");
    }

    public async Task<List<CustomerListItem>> GetCustomersBySegmentAsync(string segmentName)
    {
        var result = await _apiClient.GetAsync<List<CustomerListItem>>(
            $"api/customers/segments/{Uri.EscapeDataString(segmentName)}/customers");
        return result ?? new List<CustomerListItem>();
    }

    public async Task<CustomerRfmMetricsResponse?> GetCustomerRfmMetricsAsync(int customerId)
    {
        return await _apiClient.GetAsync<CustomerRfmMetricsResponse>($"api/customers/{customerId}/rfm");
    }

    public async Task RecalculateSegmentStatsAsync()
    {
        await _apiClient.PostAsync<object, object>("api/customers/segments/recalculate", new { });
    }

    public async Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> ExportSegmentationAsync(string format, string? segmentName = null)
    {
        return await _apiClient.PostForFileAsync("api/customers/segments/export", new { Format = format, SegmentName = segmentName });
    }
}

/// <summary>
/// Profile API Service for user profile operations
/// </summary>
public interface IProfileApiService
{
    Task<ChangePasswordResponse?> ChangePasswordAsync(ChangePasswordRequest request);
    Task<UpdateProfileResponse?> UpdateProfileAsync(UpdateProfileRequest request);
}

public class ProfileApiService : IProfileApiService
{
    private readonly IApiClient _apiClient;

    public ProfileApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ChangePasswordResponse?> ChangePasswordAsync(ChangePasswordRequest request)
    {
        return await _apiClient.PostAsync<ChangePasswordRequest, ChangePasswordResponse>(
            "api/auth/change-password", request);
    }

    public async Task<UpdateProfileResponse?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        return await _apiClient.PutAsync<UpdateProfileRequest, UpdateProfileResponse>(
            "api/auth/profile", request);
    }
}

/// <summary>
/// Shift Management API Service
/// </summary>
public interface IShiftApiService
{
    // Shifts
    Task<ShiftResponse?> CreateShiftAsync(CreateShiftRequest request);
    Task<List<ShiftResponse>> GetEmployeeShiftsAsync(int employeeId);
    Task<WeeklyShiftScheduleResponse?> GetWeeklyScheduleAsync(int employeeId);
    Task<ShiftResponse?> UpdateShiftAsync(int shiftId, UpdateShiftRequest request);
    Task<bool> DeleteShiftAsync(int shiftId);
    Task<List<ShiftResponse>> SetBulkShiftsAsync(int employeeId, BulkShiftRequest request);

    // Exceptions
    Task<ShiftExceptionResponse?> CreateShiftExceptionAsync(CreateShiftExceptionRequest request);
    Task<List<ShiftExceptionResponse>> GetEmployeeExceptionsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ShiftExceptionResponse>> GetExceptionsByDateAsync(DateTime date);
    Task<ShiftExceptionResponse?> UpdateShiftExceptionAsync(int exceptionId, UpdateShiftExceptionRequest request);
    Task<bool> DeleteShiftExceptionAsync(int exceptionId);

    // Availability
    Task<EmployeeAvailabilityResponse?> GetEmployeeAvailabilityAsync(int employeeId, DateTime date);
    Task<List<EmployeeAvailabilityResponse>> GetAvailableStaffAsync(DateTime date, TimeSpan? startTime = null, TimeSpan? endTime = null, bool therapistsOnly = false);
}

public class ShiftApiService : IShiftApiService
{
    private readonly IApiClient _apiClient;

    public ShiftApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // Shifts
    public async Task<ShiftResponse?> CreateShiftAsync(CreateShiftRequest request)
    {
        return await _apiClient.PostAsync<CreateShiftRequest, ShiftResponse>(
            $"api/shifts/employee/{request.EmployeeId}", request);
    }

    public async Task<List<ShiftResponse>> GetEmployeeShiftsAsync(int employeeId)
    {
        var result = await _apiClient.GetAsync<List<ShiftResponse>>($"api/shifts/employee/{employeeId}");
        return result ?? new List<ShiftResponse>();
    }

    public async Task<WeeklyShiftScheduleResponse?> GetWeeklyScheduleAsync(int employeeId)
    {
        return await _apiClient.GetAsync<WeeklyShiftScheduleResponse>($"api/shifts/employee/{employeeId}/weekly");
    }

    public async Task<ShiftResponse?> UpdateShiftAsync(int shiftId, UpdateShiftRequest request)
    {
        return await _apiClient.PutAsync<UpdateShiftRequest, ShiftResponse>($"api/shifts/{shiftId}", request);
    }

    public async Task<bool> DeleteShiftAsync(int shiftId)
    {
        return await _apiClient.DeleteAsync($"api/shifts/{shiftId}");
    }

    public async Task<List<ShiftResponse>> SetBulkShiftsAsync(int employeeId, BulkShiftRequest request)
    {
        var result = await _apiClient.PostAsync<BulkShiftRequest, List<ShiftResponse>>(
            $"api/shifts/employee/{employeeId}/bulk", request);
        return result ?? new List<ShiftResponse>();
    }

    // Exceptions
    public async Task<ShiftExceptionResponse?> CreateShiftExceptionAsync(CreateShiftExceptionRequest request)
    {
        return await _apiClient.PostAsync<CreateShiftExceptionRequest, ShiftExceptionResponse>(
            "api/shifts/exceptions", request);
    }

    public async Task<List<ShiftExceptionResponse>> GetEmployeeExceptionsAsync(int employeeId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var url = $"api/shifts/exceptions/employee/{employeeId}";
        if (fromDate.HasValue) url += $"?fromDate={fromDate.Value:yyyy-MM-dd}";
        if (toDate.HasValue) url += $"{(fromDate.HasValue ? "&" : "?")}toDate={toDate.Value:yyyy-MM-dd}";

        var result = await _apiClient.GetAsync<List<ShiftExceptionResponse>>(url);
        return result ?? new List<ShiftExceptionResponse>();
    }

    public async Task<List<ShiftExceptionResponse>> GetExceptionsByDateAsync(DateTime date)
    {
        var result = await _apiClient.GetAsync<List<ShiftExceptionResponse>>(
            $"api/shifts/exceptions/date/{date:yyyy-MM-dd}");
        return result ?? new List<ShiftExceptionResponse>();
    }

    public async Task<ShiftExceptionResponse?> UpdateShiftExceptionAsync(int exceptionId, UpdateShiftExceptionRequest request)
    {
        return await _apiClient.PutAsync<UpdateShiftExceptionRequest, ShiftExceptionResponse>(
            $"api/shifts/exceptions/{exceptionId}", request);
    }

    public async Task<bool> DeleteShiftExceptionAsync(int exceptionId)
    {
        return await _apiClient.DeleteAsync($"api/shifts/exceptions/{exceptionId}");
    }

    // Availability
    public async Task<EmployeeAvailabilityResponse?> GetEmployeeAvailabilityAsync(int employeeId, DateTime date)
    {
        return await _apiClient.GetAsync<EmployeeAvailabilityResponse>(
            $"api/shifts/availability/{employeeId}/{date:yyyy-MM-dd}");
    }

    public async Task<List<EmployeeAvailabilityResponse>> GetAvailableStaffAsync(
        DateTime date, TimeSpan? startTime = null, TimeSpan? endTime = null, bool therapistsOnly = false)
    {
        var url = $"api/shifts/available-staff?date={date:yyyy-MM-dd}&therapistsOnly={therapistsOnly}";
        if (startTime.HasValue) url += $"&startTime={startTime.Value}";
        if (endTime.HasValue) url += $"&endTime={endTime.Value}";

        var result = await _apiClient.GetAsync<List<EmployeeAvailabilityResponse>>(url);
        return result ?? new List<EmployeeAvailabilityResponse>();
    }
}

// ============================================================================
// Currency API Service
// ============================================================================

public interface ICurrencyApiService
{
    Task<AllRatesResponse?> GetAllRatesAsync();
    Task<CurrencyRateResponse?> GetRateAsync(string currency);
    Task<ConvertCurrencyResponse?> ConvertAsync(decimal amount, string from, string to);
    Task<DetectedClientInfoResponse?> DetectClientAsync();
    Task<SupportedCurrenciesResponse?> GetSupportedCurrenciesAsync();
}

public class CurrencyApiService : ICurrencyApiService
{
    private readonly IApiClient _apiClient;

    public CurrencyApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<AllRatesResponse?> GetAllRatesAsync()
    {
        return await _apiClient.GetAsync<AllRatesResponse>("api/currency/rates");
    }

    public async Task<CurrencyRateResponse?> GetRateAsync(string currency)
    {
        return await _apiClient.GetAsync<CurrencyRateResponse>($"api/currency/rates/{currency}");
    }

    public async Task<ConvertCurrencyResponse?> ConvertAsync(decimal amount, string from, string to)
    {
        var request = new ConvertCurrencyRequest
        {
            Amount = amount,
            FromCurrency = from,
            ToCurrency = to
        };
        return await _apiClient.PostAsync<ConvertCurrencyRequest, ConvertCurrencyResponse>("api/currency/convert", request);
    }

    public async Task<DetectedClientInfoResponse?> DetectClientAsync()
    {
        return await _apiClient.GetAsync<DetectedClientInfoResponse>("api/currency/detect");
    }

    public async Task<SupportedCurrenciesResponse?> GetSupportedCurrenciesAsync()
    {
        return await _apiClient.GetAsync<SupportedCurrenciesResponse>("api/currency/supported");
    }
}

/// <summary>
/// Two-Factor Authentication API service
/// </summary>
public interface ITwoFactorApiService
{
    Task<TwoFactorSetupResponse?> SetupAsync();
    Task<TwoFactorVerifySetupResponse?> VerifySetupAsync(string code);
    Task<bool> DisableAsync(string code);
    Task<TwoFactorStatusResponse?> GetStatusAsync();
}

public class TwoFactorApiService : ITwoFactorApiService
{
    private readonly IApiClient _apiClient;

    public TwoFactorApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<TwoFactorSetupResponse?> SetupAsync()
    {
        return await _apiClient.PostAsync<object, TwoFactorSetupResponse>("api/auth/2fa/setup", new { });
    }

    public async Task<TwoFactorVerifySetupResponse?> VerifySetupAsync(string code)
    {
        var request = new TwoFactorVerifySetupRequest { Code = code };
        return await _apiClient.PostAsync<TwoFactorVerifySetupRequest, TwoFactorVerifySetupResponse>("api/auth/2fa/verify-setup", request);
    }

    public async Task<bool> DisableAsync(string code)
    {
        var request = new TwoFactorDisableRequest { Code = code };
        var result = await _apiClient.PostAsync<TwoFactorDisableRequest, object>("api/auth/2fa/disable", request);
        return result != null;
    }

    public async Task<TwoFactorStatusResponse?> GetStatusAsync()
    {
        return await _apiClient.GetAsync<TwoFactorStatusResponse>("api/auth/2fa/status");
    }
}

/// <summary>
/// Captcha API service for managing reCAPTCHA v2 settings
/// </summary>
public interface ICaptchaApiService
{
    Task<CaptchaSettingsResponse?> GetSettingsAsync();
    Task<bool> UpdateSettingsAsync(UpdateCaptchaSettingsRequest request);
}

public class CaptchaApiService : ICaptchaApiService
{
    private readonly IApiClient _apiClient;

    public CaptchaApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<CaptchaSettingsResponse?> GetSettingsAsync()
    {
        return await _apiClient.GetAsync<CaptchaSettingsResponse>("api/captcha/settings");
    }

    public async Task<bool> UpdateSettingsAsync(UpdateCaptchaSettingsRequest request)
    {
        var result = await _apiClient.PutAsync<UpdateCaptchaSettingsRequest, object>("api/captcha/settings", request);
        return result != null;
    }
}
// =========================================================================
// Settings API Service
// =========================================================================

public interface ISettingsApiService
{
    // General Settings
    Task<GeneralSettingsDto?> GetGeneralSettingsAsync();
    Task<GeneralSettingsDto?> SaveGeneralSettingsAsync(GeneralSettingsDto dto);

    // Business Info
    Task<BusinessInfoDto?> GetBusinessInfoAsync();
    Task<BusinessInfoDto?> SaveBusinessInfoAsync(BusinessInfoDto dto);

    // Notification Settings
    Task<NotificationSettingsDto?> GetNotificationSettingsAsync();
    Task<NotificationSettingsDto?> SaveNotificationSettingsAsync(NotificationSettingsDto dto);

    // User Management
    Task<List<SettingsUserListResponse>> GetUsersAsync();
    Task<(SettingsUserListResponse? Result, string? Error)> CreateUserAsync(CreateUserRequest request);
    Task<(SettingsUserListResponse? Result, string? Error)> UpdateUserAsync(int userId, UpdateUserRequest request);
    Task<SettingsUserListResponse?> ToggleUserStatusAsync(int userId);
    Task<bool> ResetUserPasswordAsync(int userId, ResetPasswordRequest request);

    // Role Management
    Task<List<SettingsRoleListResponse>> GetRolesAsync();
    Task<(SettingsRoleListResponse? Result, string? Error)> CreateRoleAsync(CreateRoleRequest request);
    Task<SettingsRoleListResponse?> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request);
    Task<(bool Success, string? Error)> DeleteRoleAsync(int roleId);
}

public class SettingsApiService : ISettingsApiService
{
    private readonly IApiClient _apiClient;

    public SettingsApiService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    // General Settings
    public async Task<GeneralSettingsDto?> GetGeneralSettingsAsync()
        => await _apiClient.GetAsync<GeneralSettingsDto>("api/settings/general");

    public async Task<GeneralSettingsDto?> SaveGeneralSettingsAsync(GeneralSettingsDto dto)
        => await _apiClient.PutAsync<GeneralSettingsDto, GeneralSettingsDto>("api/settings/general", dto);

    // Business Info
    public async Task<BusinessInfoDto?> GetBusinessInfoAsync()
        => await _apiClient.GetAsync<BusinessInfoDto>("api/settings/business");

    public async Task<BusinessInfoDto?> SaveBusinessInfoAsync(BusinessInfoDto dto)
        => await _apiClient.PutAsync<BusinessInfoDto, BusinessInfoDto>("api/settings/business", dto);

    // Notification Settings
    public async Task<NotificationSettingsDto?> GetNotificationSettingsAsync()
        => await _apiClient.GetAsync<NotificationSettingsDto>("api/settings/notifications");

    public async Task<NotificationSettingsDto?> SaveNotificationSettingsAsync(NotificationSettingsDto dto)
        => await _apiClient.PutAsync<NotificationSettingsDto, NotificationSettingsDto>("api/settings/notifications", dto);

    // User Management
    public async Task<List<SettingsUserListResponse>> GetUsersAsync()
        => await _apiClient.GetAsync<List<SettingsUserListResponse>>("api/settings/users") ?? new();

    public async Task<(SettingsUserListResponse? Result, string? Error)> CreateUserAsync(CreateUserRequest request)
        => await _apiClient.PostWithErrorAsync<CreateUserRequest, SettingsUserListResponse>("api/settings/users", request);

    public async Task<(SettingsUserListResponse? Result, string? Error)> UpdateUserAsync(int userId, UpdateUserRequest request)
        => await _apiClient.PutWithErrorAsync<UpdateUserRequest, SettingsUserListResponse>($"api/settings/users/{userId}", request);

    public async Task<SettingsUserListResponse?> ToggleUserStatusAsync(int userId)
        => await _apiClient.PutAsync<object, SettingsUserListResponse>($"api/settings/users/{userId}/toggle-status", new { });

    public async Task<bool> ResetUserPasswordAsync(int userId, ResetPasswordRequest request)
    {
        var result = await _apiClient.PostAsync<ResetPasswordRequest, object>($"api/settings/users/{userId}/reset-password", request);
        return result != null;
    }

    // Role Management
    public async Task<List<SettingsRoleListResponse>> GetRolesAsync()
        => await _apiClient.GetAsync<List<SettingsRoleListResponse>>("api/settings/roles") ?? new();

    public async Task<(SettingsRoleListResponse? Result, string? Error)> CreateRoleAsync(CreateRoleRequest request)
        => await _apiClient.PostWithErrorAsync<CreateRoleRequest, SettingsRoleListResponse>("api/settings/roles", request);

    public async Task<SettingsRoleListResponse?> UpdateRolePermissionsAsync(int roleId, UpdateRolePermissionsRequest request)
        => await _apiClient.PutAsync<UpdateRolePermissionsRequest, SettingsRoleListResponse>($"api/settings/roles/{roleId}/permissions", request);

    public async Task<(bool Success, string? Error)> DeleteRoleAsync(int roleId)
        => await _apiClient.DeleteWithErrorAsync($"api/settings/roles/{roleId}");
}