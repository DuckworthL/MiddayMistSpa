using MiddayMistSpa.API.DTOs.Customer;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.Services;

public interface ICustomerService
{
    #region Customer CRUD

    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> GetCustomerByIdAsync(int customerId);
    Task<CustomerResponse?> GetCustomerByCodeAsync(string customerCode);
    Task<CustomerResponse?> GetCustomerByPhoneAsync(string phoneNumber);
    Task<PagedResponse<CustomerListResponse>> SearchCustomersAsync(CustomerSearchRequest request);
    Task<List<CustomerListResponse>> GetRecentCustomersAsync(int count = 10);
    Task<CustomerResponse> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request);
    Task<bool> DeactivateCustomerAsync(int customerId);
    Task<bool> ReactivateCustomerAsync(int customerId);

    #endregion

    #region Preferences

    Task<CustomerPreferencesResponse?> GetCustomerPreferencesAsync(int customerId);
    Task<CustomerPreferencesResponse> UpdateCustomerPreferencesAsync(int customerId, CustomerPreferencesResponse preferences);

    #endregion

    #region Loyalty Program

    Task<LoyaltyTransactionResponse> AddLoyaltyPointsAsync(int customerId, AddLoyaltyPointsRequest request);
    Task<LoyaltyTransactionResponse> RedeemLoyaltyPointsAsync(int customerId, RedeemLoyaltyPointsRequest request);
    Task<int> GetLoyaltyPointsBalanceAsync(int customerId);
    Task<List<LoyaltyPointHistoryResponse>> GetLoyaltyTransactionHistoryAsync(int customerId, int count = 50);

    #endregion

    #region Visit History

    Task<List<CustomerVisitHistoryResponse>> GetCustomerVisitHistoryAsync(int customerId, int count = 10);
    Task UpdateCustomerVisitStatsAsync(int customerId, decimal transactionAmount);

    #endregion

    #region Segments

    Task<List<CustomerSegmentResponse>> GetAllSegmentsAsync();
    Task<List<CustomerListResponse>> GetCustomersBySegmentAsync(string segmentName);
    Task AssignCustomerToSegmentAsync(int customerId, string segmentName);

    #endregion

    #region Membership

    Task<CustomerResponse> UpgradeMembershipAsync(int customerId, string membershipType, DateTime? expiryDate);
    Task<List<CustomerListResponse>> GetExpiringMembershipsAsync(int daysAhead = 30);

    #endregion

    #region Stats

    Task<CustomerStatsResponse> GetCustomerStatsAsync();

    #endregion
}
