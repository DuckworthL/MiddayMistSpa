using MiddayMistSpa.API.DTOs.Transaction;

namespace MiddayMistSpa.API.Services;

public interface ICashDrawerService
{
    Task<CashDrawerSessionResponse?> GetActiveSessionAsync();
    Task<CashDrawerSessionResponse> OpenDrawerAsync(OpenDrawerRequest request, int userId);
    Task<CashDrawerSessionResponse> CloseDrawerAsync(CloseDrawerRequest request, int userId);
    Task<List<CashDrawerSessionResponse>> GetSessionHistoryAsync(DateTime? startDate, DateTime? endDate);
}
