using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Service;

namespace MiddayMistSpa.API.Services;

public interface ISpaServiceService
{
    #region Categories

    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request);
    Task<CategoryResponse?> GetCategoryByIdAsync(int categoryId);
    Task<List<CategoryResponse>> GetAllCategoriesAsync(bool includeInactive = false);
    Task<CategoryResponse> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request);
    Task<bool> DeleteCategoryAsync(int categoryId);

    #endregion

    #region Services

    Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request);
    Task<ServiceResponse?> GetServiceByIdAsync(int serviceId);
    Task<ServiceResponse?> GetServiceByCodeAsync(string serviceCode);
    Task<PagedResponse<ServiceListResponse>> SearchServicesAsync(ServiceSearchRequest request);
    Task<List<ServiceListResponse>> GetServicesByCategoryAsync(int categoryId);
    Task<List<ServiceListResponse>> GetActiveServicesAsync();
    Task<List<ServiceMenuResponse>> GetServiceMenuAsync();
    Task<ServiceResponse> UpdateServiceAsync(int serviceId, UpdateServiceRequest request);
    Task<ServiceResponse> UpdatePricingAsync(int serviceId, UpdatePricingRequest request);
    Task<int> ApplyBulkPriceAdjustmentAsync(BulkPriceAdjustmentRequest request);
    Task<bool> DeactivateServiceAsync(int serviceId);
    Task<bool> ReactivateServiceAsync(int serviceId);

    #endregion

    #region Product Requirements

    Task<ProductRequirementResponse> AddProductRequirementAsync(int serviceId, AddProductRequirementRequest request);
    Task<List<ProductRequirementResponse>> GetServiceProductRequirementsAsync(int serviceId);
    Task<bool> RemoveProductRequirementAsync(int requirementId);
    Task<ProductRequirementResponse> UpdateProductRequirementAsync(int requirementId, decimal quantityRequired);

    #endregion

    #region Pricing Helpers

    Task<decimal> GetPriceForCustomerAsync(int serviceId, string membershipType);

    #endregion
}
