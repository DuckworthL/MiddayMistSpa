using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Transaction;

namespace MiddayMistSpa.API.Services;

public interface ITransactionService
{
    // ============================================================================
    // Transaction CRUD
    // ============================================================================

    Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, int cashierId);
    Task<TransactionResponse> CreatePendingTransactionForAppointmentAsync(int appointmentId, int cashierId);
    Task<TransactionResponse> FinalizePendingTransactionAsync(int transactionId, FinalizePendingTransactionRequest request, int cashierId);
    Task<TransactionResponse?> GetPendingTransactionByAppointmentAsync(int appointmentId);
    Task<TransactionResponse?> GetTransactionByIdAsync(int transactionId);
    Task<TransactionResponse?> GetTransactionByNumberAsync(string transactionNumber);
    Task<PagedResponse<TransactionListResponse>> SearchTransactionsAsync(TransactionSearchRequest request);

    // ============================================================================
    // Payment Processing
    // ============================================================================

    Task<TransactionResponse> ProcessPaymentAsync(int transactionId, ProcessPaymentRequest request);
    Task<TransactionResponse> VoidTransactionAsync(int transactionId, VoidTransactionRequest request, int voidedById);

    // ============================================================================
    // Refunds
    // ============================================================================

    Task<RefundResponse> ProcessRefundAsync(int transactionId, CreateRefundRequest request, int approvedById, int processedById);
    Task<List<RefundResponse>> GetRefundsByTransactionAsync(int transactionId);

    // ============================================================================
    // Customer History
    // ============================================================================

    Task<PagedResponse<TransactionListResponse>> GetCustomerTransactionsAsync(int customerId, int page = 1, int pageSize = 20);

    // ============================================================================
    // Reports & Dashboard
    // ============================================================================

    Task<POSDashboardResponse> GetPOSDashboardAsync(DateTime date);
    Task<DailySalesReportResponse> GetDailySalesReportAsync(DateTime date);
    Task<TransactionSalesReportResponse> GetSalesReportAsync(DateTime startDate, DateTime endDate);
    Task<CashierShiftReportResponse> GetCashierShiftReportAsync(int cashierId, DateTime date);
    Task<TransactionStatsResponse> GetTransactionStatsAsync();

    // ============================================================================
    // Receipt
    // ============================================================================

    Task<ReceiptResponse> GenerateReceiptAsync(int transactionId);
}
