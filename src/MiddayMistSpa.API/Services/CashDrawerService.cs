using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Transaction;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Transaction;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class CashDrawerService : ICashDrawerService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<CashDrawerService> _logger;

    public CashDrawerService(SpaDbContext context, ILogger<CashDrawerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CashDrawerSessionResponse?> GetActiveSessionAsync()
    {
        var session = await _context.CashDrawerSessions
            .Include(s => s.OpenedByUser)
            .FirstOrDefaultAsync(s => s.Status == "Open");

        if (session == null) return null;

        // Recalculate cash in/out from transactions since drawer opened
        await RecalculateCashTotals(session);

        return MapToResponse(session);
    }

    public async Task<CashDrawerSessionResponse> OpenDrawerAsync(OpenDrawerRequest request, int userId)
    {
        // Check for existing open session
        var existingOpen = await _context.CashDrawerSessions.AnyAsync(s => s.Status == "Open");
        if (existingOpen)
            throw new InvalidOperationException("A cash drawer session is already open. Close it before opening a new one.");

        var session = new CashDrawerSession
        {
            OpenedByUserId = userId,
            OpenedAt = PhilippineTime.Now,
            StartingFloat = request.StartingFloat,
            TotalCashIn = 0,
            TotalCashOut = 0,
            ExpectedCash = request.StartingFloat,
            Status = "Open",
            Notes = request.Notes
        };

        _context.CashDrawerSessions.Add(session);
        await _context.SaveChangesAsync();

        // Reload with navigation
        await _context.Entry(session).Reference(s => s.OpenedByUser).LoadAsync();

        _logger.LogInformation("Cash drawer opened by user {UserId} with float ₱{Float:N2}", userId, request.StartingFloat);

        return MapToResponse(session);
    }

    public async Task<CashDrawerSessionResponse> CloseDrawerAsync(CloseDrawerRequest request, int userId)
    {
        var session = await _context.CashDrawerSessions
            .Include(s => s.OpenedByUser)
            .FirstOrDefaultAsync(s => s.Status == "Open");

        if (session == null)
            throw new InvalidOperationException("No open cash drawer session found.");

        // Recalculate totals from actual transactions
        await RecalculateCashTotals(session);

        session.ClosedByUserId = userId;
        session.ClosedAt = PhilippineTime.Now;
        session.ActualCash = request.ActualCash;
        session.Discrepancy = request.ActualCash - session.ExpectedCash;
        session.Status = "Closed";
        if (!string.IsNullOrEmpty(request.Notes))
            session.Notes = (session.Notes != null ? session.Notes + " | " : "") + request.Notes;

        await _context.SaveChangesAsync();

        // Load closed-by user
        await _context.Entry(session).Reference(s => s.ClosedByUser).LoadAsync();

        _logger.LogInformation("Cash drawer closed by user {UserId}. Expected: ₱{Expected:N2}, Actual: ₱{Actual:N2}, Discrepancy: ₱{Disc:N2}",
            userId, session.ExpectedCash, request.ActualCash, session.Discrepancy);

        return MapToResponse(session);
    }

    public async Task<List<CashDrawerSessionResponse>> GetSessionHistoryAsync(DateTime? startDate, DateTime? endDate)
    {
        var query = _context.CashDrawerSessions
            .Include(s => s.OpenedByUser)
            .Include(s => s.ClosedByUser)
            .AsQueryable();

        if (startDate.HasValue) query = query.Where(s => s.OpenedAt >= startDate.Value);
        if (endDate.HasValue) query = query.Where(s => s.OpenedAt <= endDate.Value.AddDays(1));

        var sessions = await query.OrderByDescending(s => s.OpenedAt).Take(50).ToListAsync();
        return sessions.Select(MapToResponse).ToList();
    }

    private async Task RecalculateCashTotals(CashDrawerSession session)
    {
        // Sum cash transactions since drawer opened
        var cashTransactions = await _context.Transactions
            .Where(t => t.PaymentMethod == "Cash" &&
                        t.PaymentStatus == "Paid" &&
                        t.TransactionDate >= session.OpenedAt)
            .ToListAsync();

        session.TotalCashIn = cashTransactions.Sum(t => t.AmountTendered ?? t.TotalAmount);

        // Sum cash refunds since drawer opened
        var cashRefunds = await _context.Refunds
            .Where(r => r.RefundMethod == "Cash" &&
                        r.RefundDate >= session.OpenedAt)
            .ToListAsync();

        session.TotalCashOut = cashRefunds.Sum(r => r.RefundAmount);

        session.ExpectedCash = session.StartingFloat + session.TotalCashIn - session.TotalCashOut;
    }

    private static CashDrawerSessionResponse MapToResponse(CashDrawerSession session) => new()
    {
        SessionId = session.SessionId,
        OpenedByUserId = session.OpenedByUserId,
        OpenedByName = session.OpenedByUser != null ? $"{session.OpenedByUser.FirstName} {session.OpenedByUser.LastName}" : "Unknown",
        ClosedByUserId = session.ClosedByUserId,
        ClosedByName = session.ClosedByUser != null ? $"{session.ClosedByUser.FirstName} {session.ClosedByUser.LastName}" : null,
        OpenedAt = session.OpenedAt,
        ClosedAt = session.ClosedAt,
        StartingFloat = session.StartingFloat,
        TotalCashIn = session.TotalCashIn,
        TotalCashOut = session.TotalCashOut,
        ExpectedCash = session.ExpectedCash,
        ActualCash = session.ActualCash,
        Discrepancy = session.Discrepancy,
        Status = session.Status,
        Notes = session.Notes
    };
}
