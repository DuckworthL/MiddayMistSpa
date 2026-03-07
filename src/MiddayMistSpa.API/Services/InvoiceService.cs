using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Accounting;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Accounting;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class InvoiceService : IInvoiceService
{
    private readonly SpaDbContext _context;
    private readonly IAccountingService _accountingService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(SpaDbContext context, IAccountingService accountingService, ILogger<InvoiceService> logger)
    {
        _context = context;
        _accountingService = accountingService;
        _logger = logger;
    }

    public async Task<PagedResponse<InvoiceResponse>> GetInvoicesAsync(int page = 1, int pageSize = 20, string? status = null, string? search = null)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(i =>
                i.InvoiceNumber.Contains(search) ||
                (i.Customer.FirstName + " " + i.Customer.LastName).Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => MapToResponse(i))
            .ToListAsync();

        // Auto-update overdue status
        var today = PhilippineTime.Today;
        foreach (var inv in items)
        {
            if (inv.DueDate < today && inv.Status is "Sent" or "Partial")
                inv.Status = "Overdue";
        }

        return new PagedResponse<InvoiceResponse>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<InvoiceResponse?> GetInvoiceByIdAsync(int invoiceId)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

        return invoice == null ? null : MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateInvoiceAsync(CreateInvoiceRequest request, int createdById)
    {
        var customer = await _context.Customers.FindAsync(request.CustomerId)
            ?? throw new InvalidOperationException("Customer not found");

        // Generate invoice number with retry for uniqueness
        var today = PhilippineTime.Today;
        string invoiceNumber;
        for (int attempt = 0; attempt < 5; attempt++)
        {
            var todayCount = await _context.Invoices.CountAsync(i => i.CreatedAt.Date == today);
            invoiceNumber = $"INV-{today:yyyy}-{(todayCount + 1 + attempt):D3}";
            if (!await _context.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber))
                goto numberResolved;
        }
        invoiceNumber = $"INV-{today:yyyy}-{PhilippineTime.Now:HHmmssfff}";
    numberResolved:

        var invoice = new Invoice
        {
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate == default ? today : request.InvoiceDate,
            DueDate = request.DueDate == default ? today.AddDays(15) : request.DueDate,
            CustomerId = request.CustomerId,
            Notes = request.Notes,
            Status = "Draft",
            CreatedBy = createdById,
            Lines = request.Lines.Select(l => new InvoiceLine
            {
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                Amount = l.Quantity * l.UnitPrice
            }).ToList()
        };

        invoice.Subtotal = invoice.Lines.Sum(l => l.Amount);
        invoice.TaxAmount = 0; // Tax can be added later if needed
        invoice.TotalAmount = invoice.Subtotal + invoice.TaxAmount;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return (await GetInvoiceByIdAsync(invoice.InvoiceId))!;
    }

    public async Task<InvoiceResponse> UpdateStatusAsync(int invoiceId, string newStatus)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found");

        invoice.Status = newStatus;
        await _context.SaveChangesAsync();

        return (await GetInvoiceByIdAsync(invoiceId))!;
    }

    public async Task<InvoiceResponse> RecordPaymentAsync(int invoiceId, decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be positive");

        var invoice = await _context.Invoices.FindAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found");

        invoice.AmountPaid += amount;
        if (invoice.AmountPaid >= invoice.TotalAmount)
        {
            invoice.AmountPaid = invoice.TotalAmount;
            invoice.Status = "Paid";
        }
        else
        {
            invoice.Status = "Partial";
        }

        await _context.SaveChangesAsync();

        // Auto-create income record for the payment
        try
        {
            await _accountingService.CreateInvoicePaymentIncomeAsync(invoiceId, amount, 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create auto-income for invoice {InvoiceId} payment", invoiceId);
        }

        return (await GetInvoiceByIdAsync(invoiceId))!;
    }

    private static InvoiceResponse MapToResponse(Invoice i) => new()
    {
        InvoiceId = i.InvoiceId,
        InvoiceNumber = i.InvoiceNumber,
        InvoiceDate = i.InvoiceDate,
        DueDate = i.DueDate,
        CustomerId = i.CustomerId,
        CustomerName = i.Customer != null ? $"{i.Customer.FirstName} {i.Customer.LastName}" : "Unknown",
        Subtotal = i.Subtotal,
        Tax = i.TaxAmount,
        Total = i.TotalAmount,
        AmountPaid = i.AmountPaid,
        Balance = i.TotalAmount - i.AmountPaid,
        Status = i.Status,
        Lines = i.Lines.Select(l => new InvoiceLineResponse
        {
            LineId = l.InvoiceLineId,
            Description = l.Description,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            Amount = l.Amount
        }).ToList()
    };
}
