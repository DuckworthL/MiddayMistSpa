using MiddayMistSpa.API.DTOs.Accounting;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.Services;

public interface IInvoiceService
{
    Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null);
    Task<InvoiceResponse?> GetInvoiceByIdAsync(int invoiceId);
    Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, int createdById);
    Task<InvoiceResponse> UpdateStatusAsync(int invoiceId, string newStatus);
    Task<InvoiceResponse> RecordPaymentAsync(int invoiceId, decimal amount);
}
