using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Report;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class ReportingService : IReportingService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<ReportingService> _logger;
    private readonly IExportService _exportService;

    public ReportingService(SpaDbContext context, ILogger<ReportingService> logger, IExportService exportService)
    {
        _context = context;
        _logger = logger;
        _exportService = exportService;
    }

    // ============================================================================
    // Dashboard
    // ============================================================================

    public async Task<DashboardResponse> GetDashboardAsync(DashboardRequest request)
    {
        var endDate = (request.EndDate ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1);
        var startDate = request.StartDate?.Date ?? endDate.Date.AddDays(-30);

        // Calculate previous period for comparison
        var periodLength = (endDate - startDate).Days;
        var previousStart = startDate.AddDays(-periodLength - 1);
        var previousEnd = startDate.AddDays(-1);

        var response = new DashboardResponse
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Get KPIs
        response.Kpis = await GetDashboardKpisAsync(startDate, endDate, previousStart, previousEnd);

        // Get Revenue Breakdown
        response.Revenue = await GetRevenueBreakdownAsync(startDate, endDate);

        // Get Appointments Summary
        response.Appointments = await GetAppointmentsSummaryAsync(startDate, endDate);

        // Get Inventory Alerts
        response.Inventory = await GetInventoryAlertsAsync();

        // Get Top Performers
        response.TopTherapists = await GetTopTherapistsAsync(startDate, endDate, 5);
        response.TopServices = await GetTopServicesAsync(startDate, endDate, 5);
        response.TopCustomers = await GetTopCustomersAsync(startDate, endDate, 5);

        return response;
    }

    private async Task<DashboardKpi> GetDashboardKpisAsync(DateTime startDate, DateTime endDate, DateTime prevStart, DateTime prevEnd)
    {
        // Current period transactions
        var currentTransactions = await _context.Transactions
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.PaymentStatus != "Voided")
            .ToListAsync();

        // Previous period transactions
        var previousTransactions = await _context.Transactions
            .Where(t => t.TransactionDate >= prevStart && t.TransactionDate <= prevEnd && t.PaymentStatus != "Voided")
            .ToListAsync();

        var currentRevenue = currentTransactions.Sum(t => t.TotalAmount);
        var previousRevenue = previousTransactions.Sum(t => t.TotalAmount);

        // Current period appointments
        var currentAppointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
            .CountAsync();

        var previousAppointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= prevStart && a.AppointmentDate <= prevEnd)
            .CountAsync();

        // New customers
        var currentNewCustomers = await _context.Customers
            .Where(c => c.CreatedAt >= startDate && c.CreatedAt <= endDate)
            .CountAsync();

        var previousNewCustomers = await _context.Customers
            .Where(c => c.CreatedAt >= prevStart && c.CreatedAt <= prevEnd)
            .CountAsync();

        // Average ticket
        var currentAvgTicket = currentTransactions.Count > 0 ? currentRevenue / currentTransactions.Count : 0;
        var previousAvgTicket = previousTransactions.Count > 0 ? previousRevenue / previousTransactions.Count : 0;

        // Occupancy rate calculation (simplified)
        var completedAppointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate && a.Status == "Completed")
            .CountAsync();

        var occupancyRate = currentAppointments > 0 ? (decimal)completedAppointments / currentAppointments * 100 : 0;

        // Customer retention (customers who visited again)
        var returningCustomers = await _context.Appointments
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
            .Select(a => a.CustomerId)
            .Distinct()
            .CountAsync(cid => _context.Appointments
                .Any(a2 => a2.CustomerId == cid && a2.AppointmentDate < startDate));

        var totalActiveCustomers = await _context.Appointments
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
            .Select(a => a.CustomerId)
            .Distinct()
            .CountAsync();

        var retentionRate = totalActiveCustomers > 0 ? (decimal)returningCustomers / totalActiveCustomers * 100 : 0;

        return new DashboardKpi
        {
            TotalRevenue = currentRevenue,
            RevenueChange = previousRevenue > 0 ? (currentRevenue - previousRevenue) / previousRevenue * 100 : 0,
            TotalAppointments = currentAppointments,
            AppointmentChange = previousAppointments > 0 ? (currentAppointments - previousAppointments) * 100 / previousAppointments : 0,
            NewCustomers = currentNewCustomers,
            NewCustomerChange = previousNewCustomers > 0 ? (currentNewCustomers - previousNewCustomers) * 100 / previousNewCustomers : 0,
            AverageTicket = currentAvgTicket,
            AverageTicketChange = previousAvgTicket > 0 ? (currentAvgTicket - previousAvgTicket) / previousAvgTicket * 100 : 0,
            OccupancyRate = occupancyRate,
            CustomerRetentionRate = retentionRate
        };
    }

    private async Task<RevenueBreakdown> GetRevenueBreakdownAsync(DateTime startDate, DateTime endDate)
    {
        var transactions = await _context.Transactions
            .Include(t => t.ServiceItems)
            .Include(t => t.ProductItems)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.PaymentStatus != "Voided")
            .ToListAsync();

        var serviceRevenue = transactions.Sum(t => t.ServiceItems.Sum(si => si.TotalPrice));
        var productRevenue = transactions.Sum(t => t.ProductItems.Sum(pi => pi.TotalPrice));
        var discounts = transactions.Sum(t => t.DiscountAmount);
        var tips = transactions.Sum(t => t.TipAmount);

        var refunds = await _context.Refunds
            .Where(r => r.RefundDate >= startDate && r.RefundDate <= endDate)
            .SumAsync(r => r.RefundAmount);

        var dailyTrend = await _context.Transactions
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.PaymentStatus != "Voided")
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new DailyRevenue
            {
                Date = g.Key,
                Revenue = g.Sum(t => t.TotalAmount),
                AppointmentCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync();

        return new RevenueBreakdown
        {
            ServiceRevenue = serviceRevenue,
            ProductRevenue = productRevenue,
            PackageRevenue = 0, // Would need package entity
            TipsReceived = tips,
            Discounts = discounts,
            RefundsProcessed = refunds,
            NetRevenue = serviceRevenue + productRevenue + tips - discounts - refunds,
            DailyTrend = dailyTrend
        };
    }

    private async Task<AppointmentsSummary> GetAppointmentsSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var appointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = appointments.Sum(a => a.Count);
        var scheduled = appointments.FirstOrDefault(a => a.Status == "Scheduled")?.Count ?? 0;
        var completed = appointments.FirstOrDefault(a => a.Status == "Completed")?.Count ?? 0;
        var cancelled = appointments.FirstOrDefault(a => a.Status == "Cancelled")?.Count ?? 0;
        var noShow = appointments.FirstOrDefault(a => a.Status == "No Show")?.Count ?? 0;
        var inProgress = appointments.FirstOrDefault(a => a.Status == "In Progress")?.Count ?? 0;

        return new AppointmentsSummary
        {
            Scheduled = scheduled,
            Completed = completed,
            Cancelled = cancelled,
            NoShow = noShow,
            InProgress = inProgress,
            CompletionRate = total > 0 ? (decimal)completed / total * 100 : 0,
            CancellationRate = total > 0 ? (decimal)cancelled / total * 100 : 0,
            NoShowRate = total > 0 ? (decimal)noShow / total * 100 : 0
        };
    }

    private async Task<InventoryAlerts> GetInventoryAlertsAsync()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                CurrentStock = (int)p.CurrentStock,
                ReorderLevel = (int)p.ReorderLevel,
                CategoryName = p.Category.CategoryName,
                p.ExpiryDate
            })
            .ToListAsync();

        var lowStock = products.Where(p => p.CurrentStock <= p.ReorderLevel && p.CurrentStock > 0).ToList();
        var outOfStock = products.Where(p => p.CurrentStock == 0).ToList();
        var expiring = products.Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30)).ToList();

        return new InventoryAlerts
        {
            LowStockItems = lowStock.Count,
            OutOfStockItems = outOfStock.Count,
            ExpiringItems = expiring.Count,
            LowStockList = lowStock.Take(10).Select(p => new LowStockItem
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                CurrentStock = p.CurrentStock,
                ReorderLevel = p.ReorderLevel,
                Category = p.CategoryName ?? "Uncategorized"
            }).ToList()
        };
    }

    private async Task<List<TopPerformer>> GetTopTherapistsAsync(DateTime startDate, DateTime endDate, int count)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Therapist)
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate && a.Status == "Completed" && a.TherapistId.HasValue)
            .ToListAsync();

        return appointments
            .GroupBy(a => a.TherapistId!.Value)
            .Select(g => new TopPerformer
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Therapist != null ? $"{g.First().Therapist!.FirstName} {g.First().Therapist!.LastName}" : "Unknown",
                AppointmentsCompleted = g.Count(),
                RevenueGenerated = g.Sum(a => a.Service?.RegularPrice ?? 0),
                CommissionsEarned = g.Sum(a => (a.Service?.RegularPrice ?? 0) * (a.Service?.TherapistCommissionRate ?? 0)),
                AverageRating = 0 // Would need ratings entity
            })
            .OrderByDescending(t => t.RevenueGenerated)
            .Take(count)
            .ToList();
    }

    private async Task<List<TopService>> GetTopServicesAsync(DateTime startDate, DateTime endDate, int count)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Service).ThenInclude(s => s.Category)
            .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate && a.Status == "Completed")
            .ToListAsync();

        return appointments
            .GroupBy(a => a.ServiceId)
            .Select(g => new TopService
            {
                ServiceId = g.Key,
                ServiceName = g.First().Service?.ServiceName ?? "Unknown",
                Category = g.First().Service?.Category?.CategoryName ?? "Uncategorized",
                BookingCount = g.Count(),
                TotalRevenue = g.Sum(a => a.Service?.RegularPrice ?? 0)
            })
            .OrderByDescending(s => s.TotalRevenue)
            .Take(count)
            .ToList();
    }

    private async Task<List<TopCustomer>> GetTopCustomersAsync(DateTime startDate, DateTime endDate, int count)
    {
        var transactions = await _context.Transactions
            .Include(t => t.Customer)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate && t.PaymentStatus != "Voided")
            .ToListAsync();

        return transactions
            .GroupBy(t => t.CustomerId)
            .Select(g => new TopCustomer
            {
                CustomerId = g.Key,
                CustomerName = g.First().Customer != null ? $"{g.First().Customer.FirstName} {g.First().Customer.LastName}" : "Unknown",
                VisitCount = g.Count(),
                TotalSpent = g.Sum(t => t.TotalAmount),
                LoyaltyPoints = g.First().Customer?.LoyaltyPoints ?? 0
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(count)
            .ToList();
    }

    public async Task<QuickStatsResponse> GetQuickStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var lastWeekStart = weekStart.AddDays(-7);
        var lastMonthStart = monthStart.AddMonths(-1);

        return new QuickStatsResponse
        {
            Today = await GetTodayStatsAsync(today),
            ThisWeek = await GetWeekStatsAsync(weekStart, today, lastWeekStart),
            ThisMonth = await GetMonthStatsAsync(monthStart, today, lastMonthStart)
        };
    }

    private async Task<TodayStats> GetTodayStatsAsync(DateTime today)
    {
        var revenue = await _context.Transactions
            .Where(t => t.TransactionDate.Date == today && t.PaymentStatus != "Voided")
            .SumAsync(t => t.TotalAmount);

        var appointments = await _context.Appointments
            .Where(a => a.AppointmentDate.Date == today)
            .ToListAsync();

        var walkIns = await _context.Appointments
            .Where(a => a.AppointmentDate.Date == today && a.BookingSource == "Walk-In")
            .CountAsync();

        var newCustomers = await _context.Customers
            .Where(c => c.CreatedAt.Date == today)
            .CountAsync();

        return new TodayStats
        {
            Revenue = revenue,
            Appointments = appointments.Count,
            Completed = appointments.Count(a => a.Status == "Completed"),
            Remaining = appointments.Count(a => a.Status == "Scheduled" || a.Status == "Confirmed"),
            WalkIns = walkIns,
            NewCustomers = newCustomers
        };
    }

    private async Task<WeekStats> GetWeekStatsAsync(DateTime weekStart, DateTime today, DateTime lastWeekStart)
    {
        var currentRevenue = await _context.Transactions
            .Where(t => t.TransactionDate >= weekStart && t.TransactionDate <= today && t.PaymentStatus != "Voided")
            .SumAsync(t => t.TotalAmount);

        var lastRevenue = await _context.Transactions
            .Where(t => t.TransactionDate >= lastWeekStart && t.TransactionDate < weekStart && t.PaymentStatus != "Voided")
            .SumAsync(t => t.TotalAmount);

        var appointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= weekStart && a.AppointmentDate <= today)
            .ToListAsync();

        var completed = appointments.Count(a => a.Status == "Completed");

        return new WeekStats
        {
            Revenue = currentRevenue,
            RevenueVsLastWeek = lastRevenue > 0 ? (currentRevenue - lastRevenue) / lastRevenue * 100 : 0,
            Appointments = appointments.Count,
            CompletionRate = appointments.Count > 0 ? (decimal)completed / appointments.Count * 100 : 0
        };
    }

    private async Task<MonthStats> GetMonthStatsAsync(DateTime monthStart, DateTime today, DateTime lastMonthStart)
    {
        var currentRevenue = await _context.Transactions
            .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= today && t.PaymentStatus != "Voided")
            .SumAsync(t => t.TotalAmount);

        var lastRevenue = await _context.Transactions
            .Where(t => t.TransactionDate >= lastMonthStart && t.TransactionDate < monthStart && t.PaymentStatus != "Voided")
            .SumAsync(t => t.TotalAmount);

        var appointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= monthStart && a.AppointmentDate <= today)
            .CountAsync();

        var newCustomers = await _context.Customers
            .Where(c => c.CreatedAt >= monthStart && c.CreatedAt <= today)
            .CountAsync();

        var transactionCount = await _context.Transactions
            .Where(t => t.TransactionDate >= monthStart && t.TransactionDate <= today && t.PaymentStatus != "Voided")
            .CountAsync();

        return new MonthStats
        {
            Revenue = currentRevenue,
            RevenueVsLastMonth = lastRevenue > 0 ? (currentRevenue - lastRevenue) / lastRevenue * 100 : 0,
            Appointments = appointments,
            NewCustomers = newCustomers,
            AverageTicket = transactionCount > 0 ? currentRevenue / transactionCount : 0
        };
    }

    // ============================================================================
    // Sales Reports
    // ============================================================================

    public async Task<SalesReportResponse> GetSalesReportAsync(SalesReportRequest request)
    {
        var endOfDay = request.EndDate.Date.AddDays(1).AddTicks(-1);
        var query = _context.Transactions
            .Include(t => t.ServiceItems).ThenInclude(si => si.Service)
            .Include(t => t.ProductItems).ThenInclude(pi => pi.Product)
            .Where(t => t.TransactionDate >= request.StartDate.Date && t.TransactionDate <= endOfDay);

        if (!request.IncludeVoided)
            query = query.Where(t => t.PaymentStatus != "Voided");

        if (request.PaymentMethod != null)
            query = query.Where(t => t.PaymentMethod == request.PaymentMethod);

        var transactions = await query.ToListAsync();

        // Filter by ServiceId if specified
        if (request.ServiceId.HasValue)
        {
            transactions = transactions
                .Where(t => t.ServiceItems.Any(si => si.ServiceId == request.ServiceId.Value))
                .ToList();
        }

        var grossSales = transactions.Sum(t => t.TotalAmount);
        var discounts = transactions.Sum(t => t.DiscountAmount);
        var netSales = transactions.Sum(t => t.TotalAmount - t.DiscountAmount);

        var refunds = await _context.Refunds
            .Where(r => r.RefundDate >= request.StartDate && r.RefundDate <= request.EndDate)
            .SumAsync(r => r.RefundAmount);

        // Payment method breakdown
        var paymentBreakdown = transactions
            .GroupBy(t => t.PaymentMethod)
            .Select(g => new PaymentMethodBreakdown
            {
                PaymentMethod = g.Key,
                Count = g.Count(),
                Amount = g.Sum(t => t.TotalAmount),
                Percentage = netSales > 0 ? g.Sum(t => t.TotalAmount) / netSales * 100 : 0
            })
            .ToList();

        // Category breakdown
        var serviceItems = transactions.SelectMany(t => t.ServiceItems).ToList();
        var productItems = transactions.SelectMany(t => t.ProductItems).ToList();

        var categoryBreakdown = new List<CategorySales>
        {
            new()
            {
                Category = "Services",
                ItemsSold = serviceItems.Count,
                Revenue = serviceItems.Sum(si => si.TotalPrice),
                Percentage = netSales > 0 ? serviceItems.Sum(si => si.TotalPrice) / netSales * 100 : 0
            },
            new()
            {
                Category = "Products",
                ItemsSold = (int)productItems.Sum(pi => pi.Quantity),
                Revenue = productItems.Sum(pi => pi.TotalPrice),
                Percentage = netSales > 0 ? productItems.Sum(pi => pi.TotalPrice) / netSales * 100 : 0
            }
        };

        // Time series based on grouping
        var timeSeries = GetTimeSeriesData(transactions, request.GroupBy ?? "Day", request.StartDate, request.EndDate);

        return new SalesReportResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            GroupBy = request.GroupBy ?? "Day",
            GrossSales = grossSales,
            Discounts = discounts,
            Refunds = refunds,
            NetSales = netSales - refunds,
            TransactionCount = transactions.Count,
            AverageTransaction = transactions.Count > 0 ? netSales / transactions.Count : 0,
            PaymentBreakdown = paymentBreakdown,
            CategoryBreakdown = categoryBreakdown,
            TimeSeries = timeSeries
        };
    }

    private List<PeriodSales> GetTimeSeriesData(
        List<MiddayMistSpa.Core.Entities.Transaction.Transaction> transactions,
        string groupBy,
        DateTime startDate,
        DateTime endDate)
    {
        return groupBy switch
        {
            "Week" => transactions
                .GroupBy(t => new { Year = t.TransactionDate.Year, Week = GetWeekOfYear(t.TransactionDate) })
                .Select(g => new PeriodSales
                {
                    PeriodStart = GetFirstDayOfWeek(g.Key.Year, g.Key.Week),
                    PeriodLabel = $"Week {g.Key.Week}, {g.Key.Year}",
                    GrossSales = g.Sum(t => t.TotalAmount),
                    NetSales = g.Sum(t => t.TotalAmount - t.DiscountAmount),
                    TransactionCount = g.Count()
                })
                .OrderBy(p => p.PeriodStart)
                .ToList(),
            "Month" => transactions
                .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
                .Select(g => new PeriodSales
                {
                    PeriodStart = new DateTime(g.Key.Year, g.Key.Month, 1),
                    PeriodLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    GrossSales = g.Sum(t => t.TotalAmount),
                    NetSales = g.Sum(t => t.TotalAmount - t.DiscountAmount),
                    TransactionCount = g.Count()
                })
                .OrderBy(p => p.PeriodStart)
                .ToList(),
            _ => transactions
                .GroupBy(t => t.TransactionDate.Date)
                .Select(g => new PeriodSales
                {
                    PeriodStart = g.Key,
                    PeriodLabel = g.Key.ToString("MMM dd"),
                    GrossSales = g.Sum(t => t.TotalAmount),
                    NetSales = g.Sum(t => t.TotalAmount - t.DiscountAmount),
                    TransactionCount = g.Count()
                })
                .OrderBy(p => p.PeriodStart)
                .ToList()
        };
    }

    private static int GetWeekOfYear(DateTime date)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar
            .GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static DateTime GetFirstDayOfWeek(int year, int week)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        var firstMonday = jan1.AddDays(daysOffset);
        return firstMonday.AddDays((week - 1) * 7);
    }

    // ============================================================================
    // Service Performance
    // ============================================================================

    public async Task<ServicePerformanceResponse> GetServicePerformanceReportAsync(ServicePerformanceRequest request)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Service).ThenInclude(s => s.Category)
            .Where(a => a.AppointmentDate >= request.StartDate && a.AppointmentDate <= request.EndDate)
            .ToListAsync();

        if (request.CategoryId.HasValue)
        {
            appointments = appointments.Where(a => a.Service?.CategoryId == request.CategoryId.Value).ToList();
        }

        var services = appointments
            .GroupBy(a => a.ServiceId)
            .Select(g => new ServicePerformanceItem
            {
                ServiceId = g.Key,
                ServiceName = g.First().Service?.ServiceName ?? "Unknown",
                Category = g.First().Service?.Category?.CategoryName ?? "Uncategorized",
                BasePrice = g.First().Service?.RegularPrice ?? 0,
                BookingCount = g.Count(),
                TotalRevenue = g.Where(a => a.Status == "Completed").Sum(a => a.Service?.RegularPrice ?? 0),
                AverageRevenue = g.Count() > 0 ? g.Where(a => a.Status == "Completed").Sum(a => a.Service?.RegularPrice ?? 0) / g.Count() : 0,
                CompletedCount = g.Count(a => a.Status == "Completed"),
                CancelledCount = g.Count(a => a.Status == "Cancelled"),
                CompletionRate = g.Count() > 0 ? (decimal)g.Count(a => a.Status == "Completed") / g.Count() * 100 : 0,
                AverageRating = 0, // Would need ratings entity
                ReviewCount = 0,
                AverageDuration = g.First().Service?.DurationMinutes ?? 0
            })
            .ToList();

        // Sort based on request
        services = request.SortBy?.ToLower() switch
        {
            "bookingcount" => services.OrderByDescending(s => s.BookingCount).ToList(),
            "rating" => services.OrderByDescending(s => s.AverageRating).ToList(),
            _ => services.OrderByDescending(s => s.TotalRevenue).ToList()
        };

        return new ServicePerformanceResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalServicesAnalyzed = services.Count,
            Services = services
        };
    }

    // ============================================================================
    // Employee Performance
    // ============================================================================

    public async Task<EmployeePerformanceResponse> GetEmployeePerformanceReportAsync(EmployeePerformanceRequest request)
    {
        var endOfDay = request.EndDate.Date.AddDays(1).AddTicks(-1);
        var employees = await _context.Employees
            .Include(e => e.User)
            .Where(e => e.IsActive)
            .ToListAsync();

        if (!string.IsNullOrEmpty(request.JobTitle))
            employees = employees.Where(e => e.Position == request.JobTitle).ToList();

        var appointments = await _context.Appointments
            .Include(a => a.Service)
            .Where(a => a.AppointmentDate >= request.StartDate.Date && a.AppointmentDate <= endOfDay)
            .ToListAsync();

        // Load schedules to compute utilization
        var schedules = await _context.EmployeeSchedules
            .Where(s => !s.IsRestDay)
            .ToListAsync();

        var performanceItems = new List<EmployeePerformanceItem>();

        // Calculate total working days in period
        var periodDays = (int)(request.EndDate.Date - request.StartDate.Date).TotalDays + 1;

        foreach (var employee in employees)
        {
            var empAppointments = appointments.Where(a => a.TherapistId == employee.EmployeeId).ToList();
            var completed = empAppointments.Count(a => a.Status == "Completed");
            var cancelled = empAppointments.Count(a => a.Status == "Cancelled");
            var totalRevenue = empAppointments
                .Where(a => a.Status == "Completed")
                .Sum(a => a.Service?.RegularPrice ?? 0);

            var commissions = totalRevenue * (empAppointments.FirstOrDefault()?.Service?.TherapistCommissionRate ?? 0);
            var daysWorked = empAppointments
                .Where(a => a.Status == "Completed")
                .Select(a => a.AppointmentDate.Date)
                .Distinct()
                .Count();

            // Calculate utilization: booked service hours vs available schedule hours
            var empSchedules = schedules.Where(s => s.EmployeeId == employee.EmployeeId).ToList();
            decimal utilizationRate = 0;
            if (empSchedules.Any())
            {
                // Count scheduled work hours in the period
                decimal totalAvailableHours = 0;
                for (var day = request.StartDate.Date; day <= request.EndDate.Date; day = day.AddDays(1))
                {
                    var dayOfWeek = (int)day.DayOfWeek;
                    var schedule = empSchedules.FirstOrDefault(s => s.DayOfWeek == dayOfWeek);
                    if (schedule != null)
                    {
                        totalAvailableHours += (decimal)(schedule.ShiftEndTime - schedule.ShiftStartTime).TotalHours;
                        if (schedule.BreakStartTime.HasValue && schedule.BreakEndTime.HasValue)
                            totalAvailableHours -= (decimal)(schedule.BreakEndTime.Value - schedule.BreakStartTime.Value).TotalHours;
                    }
                }

                // Booked hours from completed appointments
                var bookedHours = empAppointments
                    .Where(a => a.Status == "Completed")
                    .Sum(a => (decimal)(a.EndTime - a.StartTime).TotalHours);

                utilizationRate = totalAvailableHours > 0 ? Math.Min(bookedHours / totalAvailableHours * 100, 100) : 0;
            }

            performanceItems.Add(new EmployeePerformanceItem
            {
                EmployeeId = employee.EmployeeId,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                JobTitle = employee.Position,
                Department = employee.Department,
                AppointmentsScheduled = empAppointments.Count,
                AppointmentsCompleted = completed,
                CancelledByTherapist = cancelled,
                CompletionRate = empAppointments.Count > 0 ? (decimal)completed / empAppointments.Count * 100 : 0,
                RevenueGenerated = totalRevenue,
                CommissionsEarned = commissions,
                AverageServiceValue = completed > 0 ? totalRevenue / completed : 0,
                DaysWorked = daysWorked,
                UtilizationRate = Math.Round(utilizationRate, 1),
                AverageRating = 0, // No review/rating system implemented yet
                ReviewCount = 0
            });
        }

        // Sort
        performanceItems = request.SortBy?.ToLower() switch
        {
            "appointments" => performanceItems.OrderByDescending(e => e.AppointmentsCompleted).ToList(),
            "rating" => performanceItems.OrderByDescending(e => e.AverageRating).ToList(),
            _ => performanceItems.OrderByDescending(e => e.RevenueGenerated).ToList()
        };

        return new EmployeePerformanceResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalEmployeesAnalyzed = performanceItems.Count,
            Employees = performanceItems,
            Summary = new EmployeePerformanceSummary
            {
                TotalRevenue = performanceItems.Sum(e => e.RevenueGenerated),
                TotalCommissions = performanceItems.Sum(e => e.CommissionsEarned),
                TotalAppointments = performanceItems.Sum(e => e.AppointmentsCompleted),
                AverageCompletionRate = performanceItems.Count > 0 ? performanceItems.Average(e => e.CompletionRate) : 0,
                AverageUtilization = performanceItems.Count > 0 ? performanceItems.Average(e => e.UtilizationRate) : 0,
                AverageRating = performanceItems.Count > 0 ? performanceItems.Average(e => e.AverageRating) : 0
            }
        };
    }

    // ============================================================================
    // Customer Analytics
    // ============================================================================

    public async Task<CustomerAnalyticsResponse> GetCustomerAnalyticsReportAsync(CustomerAnalyticsRequest request)
    {
        var customers = await _context.Customers.ToListAsync();
        var appointments = await _context.Appointments
            .Where(a => a.AppointmentDate >= request.StartDate && a.AppointmentDate <= request.EndDate)
            .ToListAsync();

        var allTimeAppointments = await _context.Appointments.ToListAsync();

        // Counts
        var activeCustomerIds = appointments.Select(a => a.CustomerId).Distinct().ToList();
        var newCustomers = customers.Count(c => c.CreatedAt >= request.StartDate && c.CreatedAt <= request.EndDate);
        var inactiveThreshold = DateTime.UtcNow.AddDays(-90);
        var churnThreshold = DateTime.UtcNow.AddDays(-180);

        var lastVisitDates = allTimeAppointments
            .GroupBy(a => a.CustomerId)
            .ToDictionary(g => g.Key, g => g.Max(a => a.AppointmentDate));

        var inactive = customers.Count(c =>
            lastVisitDates.ContainsKey(c.CustomerId) &&
            lastVisitDates[c.CustomerId] < inactiveThreshold &&
            lastVisitDates[c.CustomerId] >= churnThreshold);

        var churned = customers.Count(c =>
            lastVisitDates.ContainsKey(c.CustomerId) &&
            lastVisitDates[c.CustomerId] < churnThreshold);

        var returning = activeCustomerIds.Count(cid =>
            allTimeAppointments.Any(a => a.CustomerId == cid && a.AppointmentDate < request.StartDate));

        // Visit patterns
        var visitsByDayOfWeek = appointments
            .GroupBy(a => a.AppointmentDate.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var visitsByHour = appointments
            .GroupBy(a => a.StartTime.Hours)
            .ToDictionary(g => $"{g.Key}:00", g => g.Count());

        // Top customers
        var transactions = await _context.Transactions
            .Where(t => t.TransactionDate >= request.StartDate && t.TransactionDate <= request.EndDate && t.PaymentStatus != "Voided")
            .Include(t => t.Customer)
            .ToListAsync();

        var topCustomers = transactions
            .GroupBy(t => t.CustomerId)
            .Select(g => new TopCustomerDetail
            {
                CustomerId = g.Key,
                CustomerName = g.First().Customer != null ? $"{g.First().Customer.FirstName} {g.First().Customer.LastName}" : "Unknown",
                Email = g.First().Customer?.Email,
                MembershipTier = g.First().Customer?.MembershipType ?? "Regular",
                TotalVisits = g.Count(),
                TotalSpent = g.Sum(t => t.TotalAmount),
                AverageSpend = g.Sum(t => t.TotalAmount) / g.Count(),
                LastVisit = g.Max(t => t.TransactionDate),
                LoyaltyPoints = g.First().Customer?.LoyaltyPoints ?? 0,
                FavoriteService = null // Would need additional query
            })
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .ToList();

        return new CustomerAnalyticsResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Counts = new CustomerCounts
            {
                TotalCustomers = customers.Count,
                ActiveCustomers = activeCustomerIds.Count,
                NewCustomers = newCustomers,
                ReturningCustomers = returning,
                InactiveCustomers = inactive,
                ChurnedCustomers = churned
            },
            Retention = new RetentionAnalysis
            {
                OverallRetentionRate = customers.Count > 0 ? (decimal)(customers.Count - churned) / customers.Count * 100 : 0,
                FirstVisitReturnRate = 0, // Complex calculation
                AverageVisitFrequency = activeCustomerIds.Count > 0 ? (decimal)appointments.Count / activeCustomerIds.Count : 0,
                AverageCustomerLifetimeValue = customers.Count > 0 ? transactions.Sum(t => t.TotalAmount) / customers.Count : 0,
                MonthlyCohorts = new List<CohortRetention>()
            },
            Segments = new List<CustomerSegment>
            {
                new() { SegmentName = "New", CustomerCount = newCustomers, Percentage = customers.Count > 0 ? (decimal)newCustomers / customers.Count * 100 : 0 },
                new() { SegmentName = "Returning", CustomerCount = returning, Percentage = customers.Count > 0 ? (decimal)returning / customers.Count * 100 : 0 },
                new() { SegmentName = "Inactive", CustomerCount = inactive, Percentage = customers.Count > 0 ? (decimal)inactive / customers.Count * 100 : 0 }
            },
            Patterns = new VisitPatterns
            {
                VisitsByDayOfWeek = visitsByDayOfWeek,
                VisitsByHour = visitsByHour,
                PeakDay = visitsByDayOfWeek.OrderByDescending(v => v.Value).FirstOrDefault().Key ?? "",
                PeakHour = visitsByHour.OrderByDescending(v => v.Value).FirstOrDefault().Key ?? "",
                AverageSessionDuration = 60 // Default, would need tracking
            },
            TopCustomers = topCustomers
        };
    }

    // ============================================================================
    // Inventory Reports
    // ============================================================================

    public async Task<InventoryReportResponse> GetInventoryReportAsync(InventoryReportRequest request)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .ToListAsync();

        if (request.CategoryId.HasValue)
            products = products.Where(p => p.ProductCategoryId == request.CategoryId.Value).ToList();

        if (!request.IncludeZeroStock)
            products = products.Where(p => p.CurrentStock > 0).ToList();

        var adjustments = await _context.StockAdjustments
            .Where(sa => (!request.StartDate.HasValue || sa.CreatedAt >= request.StartDate) &&
                         (!request.EndDate.HasValue || sa.CreatedAt <= request.EndDate))
            .ToListAsync();

        var productSales = request.StartDate.HasValue && request.EndDate.HasValue
            ? await _context.TransactionProductItems
                .Include(tpi => tpi.Transaction)
                .Where(tpi => tpi.Transaction!.TransactionDate >= request.StartDate &&
                              tpi.Transaction.TransactionDate <= request.EndDate &&
                              tpi.Transaction.PaymentStatus != "Voided")
                .GroupBy(tpi => tpi.ProductId)
                .Select(g => new { ProductId = g.Key, Sold = (int)g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Sold)
            : new Dictionary<int, int>();

        var productStatuses = products.Select(p => new ProductStockStatus
        {
            ProductId = p.ProductId,
            Sku = p.ProductCode,
            ProductName = p.ProductName,
            Category = p.Category?.CategoryName ?? "Uncategorized",
            Supplier = p.Supplier,
            CurrentStock = (int)p.CurrentStock,
            ReorderLevel = (int)p.ReorderLevel,
            UnitCost = p.CostPrice,
            SellingPrice = p.SellingPrice ?? 0,
            StockValue = p.CurrentStock * p.CostPrice,
            StockStatus = p.CurrentStock == 0 ? "Out" :
                          p.CurrentStock <= p.ReorderLevel ? "Low" :
                          p.CurrentStock > p.ReorderLevel * 3 ? "Overstock" : "Ok",
            ExpiryDate = p.ExpiryDate,
            UnitsUsedPeriod = (int)adjustments.Where(a => a.ProductId == p.ProductId && a.AdjustmentType == "Service Usage").Sum(a => Math.Abs(a.QuantityChange)),
            UnitsSoldPeriod = productSales.TryGetValue(p.ProductId, out var sold) ? sold : 0
        }).ToList();

        var categoryMovements = adjustments
            .GroupBy(a => products.FirstOrDefault(p => p.ProductId == a.ProductId)?.Category?.CategoryName ?? "Unknown")
            .Select(g => new CategoryMovement
            {
                Category = g.Key,
                Received = (int)g.Where(a => a.AdjustmentType == "Received").Sum(a => a.QuantityChange),
                Sold = 0,
                Used = (int)g.Where(a => a.AdjustmentType == "Service Usage").Sum(a => Math.Abs(a.QuantityChange)),
                Wasted = (int)g.Where(a => a.AdjustmentType == "Damaged" || a.AdjustmentType == "Expired").Sum(a => Math.Abs(a.QuantityChange)),
                NetChange = (int)g.Sum(a => a.QuantityChange)
            })
            .ToList();

        var categoryValuations = products
            .GroupBy(p => p.Category?.CategoryName ?? "Uncategorized")
            .Select(g => new CategoryValuation
            {
                Category = g.Key,
                CostValue = g.Sum(p => p.CurrentStock * p.CostPrice),
                RetailValue = g.Sum(p => p.CurrentStock * (p.SellingPrice ?? 0)),
                PercentageOfTotal = 0
            })
            .ToList();

        var totalCostValue = categoryValuations.Sum(c => c.CostValue);
        foreach (var cat in categoryValuations)
        {
            cat.PercentageOfTotal = totalCostValue > 0 ? cat.CostValue / totalCostValue * 100 : 0;
        }

        return new InventoryReportResponse
        {
            Summary = new InventorySummary
            {
                TotalProducts = products.Count,
                TotalCategories = products.Select(p => p.ProductCategoryId).Distinct().Count(),
                InStockProducts = products.Count(p => p.CurrentStock > p.ReorderLevel),
                LowStockProducts = products.Count(p => p.CurrentStock > 0 && p.CurrentStock <= p.ReorderLevel),
                OutOfStockProducts = products.Count(p => p.CurrentStock == 0),
                ExpiredProducts = products.Count(p => p.IsExpired),
                ExpiringSoonProducts = products.Count(p => p.IsExpiringSoon && !p.IsExpired)
            },
            Products = productStatuses,
            Movement = new InventoryMovementSummary
            {
                TotalReceived = (int)adjustments.Where(a => a.AdjustmentType == "Received").Sum(a => a.QuantityChange),
                TotalSold = productSales.Values.Sum(),
                TotalUsed = (int)adjustments.Where(a => a.AdjustmentType == "Service Usage").Sum(a => Math.Abs(a.QuantityChange)),
                TotalWasted = (int)adjustments.Where(a => a.AdjustmentType == "Damaged" || a.AdjustmentType == "Expired").Sum(a => Math.Abs(a.QuantityChange)),
                TotalAdjusted = (int)adjustments.Where(a => a.AdjustmentType == "Audit").Sum(a => a.QuantityChange),
                ByCategory = categoryMovements
            },
            Valuation = new InventoryValuation
            {
                TotalCostValue = products.Sum(p => p.CurrentStock * p.CostPrice),
                TotalRetailValue = products.Sum(p => p.CurrentStock * (p.SellingPrice ?? 0)),
                PotentialProfit = products.Sum(p => p.CurrentStock * ((p.SellingPrice ?? 0) - p.CostPrice)),
                ProfitMargin = products.Sum(p => p.CurrentStock * (p.SellingPrice ?? 0)) > 0
                    ? products.Sum(p => p.CurrentStock * ((p.SellingPrice ?? 0) - p.CostPrice)) / products.Sum(p => p.CurrentStock * (p.SellingPrice ?? 0)) * 100
                    : 0,
                ByCategory = categoryValuations
            }
        };
    }

    // ============================================================================
    // Payroll Summary
    // ============================================================================

    public async Task<PayrollSummaryReportResponse> GetPayrollSummaryReportAsync(PayrollSummaryRequest request)
    {
        var payrollPeriods = await _context.PayrollPeriods
            .Where(pp => pp.StartDate >= request.StartDate && pp.EndDate <= request.EndDate)
            .ToListAsync();

        var payrollRecords = await _context.PayrollRecords
            .Include(pr => pr.Employee)
            .Include(pr => pr.PayrollPeriod)
            .Where(pr => pr.PayrollPeriod!.StartDate >= request.StartDate && pr.PayrollPeriod.EndDate <= request.EndDate)
            .ToListAsync();

        var totals = new PayrollTotals
        {
            GrossPay = payrollRecords.Sum(pr => pr.GrossPay),
            BasicSalary = payrollRecords.Sum(pr => pr.BasicSalary),
            Overtime = payrollRecords.Sum(pr => pr.OvertimePay),
            NightDifferential = payrollRecords.Sum(pr => pr.NightDifferentialPay),
            Commissions = payrollRecords.Sum(pr => pr.Commissions),
            Allowances = payrollRecords.Sum(pr => pr.RiceAllowance + pr.LaundryAllowance + pr.OtherAllowances),
            TotalDeductions = payrollRecords.Sum(pr => pr.TotalDeductions),
            NetPay = payrollRecords.Sum(pr => pr.NetPay),
            TotalEmployeesPaid = payrollRecords.Select(pr => pr.EmployeeId).Distinct().Count()
        };

        var byPeriod = payrollPeriods.Select(pp => new PeriodPayrollSummary
        {
            PayrollPeriodId = pp.PayrollPeriodId,
            PeriodName = pp.PeriodName,
            StartDate = pp.StartDate,
            EndDate = pp.EndDate,
            EmployeeCount = payrollRecords.Count(pr => pr.PayrollPeriodId == pp.PayrollPeriodId),
            GrossPay = payrollRecords.Where(pr => pr.PayrollPeriodId == pp.PayrollPeriodId).Sum(pr => pr.GrossPay),
            TotalDeductions = payrollRecords.Where(pr => pr.PayrollPeriodId == pp.PayrollPeriodId).Sum(pr => pr.TotalDeductions),
            NetPay = payrollRecords.Where(pr => pr.PayrollPeriodId == pp.PayrollPeriodId).Sum(pr => pr.NetPay)
        }).ToList();

        var contributions = new ContributionsSummary
        {
            TotalSssEmployee = payrollRecords.Sum(pr => pr.SSSContribution),
            TotalSssEmployer = payrollRecords.Sum(pr => pr.SSSContribution), // Employer share would need separate field
            TotalPhilHealthEmployee = payrollRecords.Sum(pr => pr.PhilHealthContribution),
            TotalPhilHealthEmployer = payrollRecords.Sum(pr => pr.PhilHealthContribution),
            TotalPagIbigEmployee = payrollRecords.Sum(pr => pr.PagIBIGContribution),
            TotalPagIbigEmployer = payrollRecords.Sum(pr => pr.PagIBIGContribution),
            TotalWithholdingTax = payrollRecords.Sum(pr => pr.WithholdingTax),
            GrandTotalEmployee = payrollRecords.Sum(pr => pr.SSSContribution + pr.PhilHealthContribution + pr.PagIBIGContribution),
            GrandTotalEmployer = payrollRecords.Sum(pr => pr.SSSContribution + pr.PhilHealthContribution + pr.PagIBIGContribution)
        };

        // Group by department
        var byDepartment = payrollRecords
            .GroupBy(pr => pr.Employee?.Department ?? "No Department")
            .Select(g => new DepartmentPayroll
            {
                DepartmentId = null,
                DepartmentName = g.Key,
                EmployeeCount = g.Select(pr => pr.EmployeeId).Distinct().Count(),
                GrossPay = g.Sum(pr => pr.GrossPay),
                NetPay = g.Sum(pr => pr.NetPay),
                AverageGross = g.Any() ? g.Average(pr => pr.GrossPay) : 0,
                AverageNet = g.Any() ? g.Average(pr => pr.NetPay) : 0
            })
            .ToList();

        return new PayrollSummaryReportResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PeriodsIncluded = payrollPeriods.Count,
            Totals = totals,
            ByPeriod = byPeriod,
            Contributions = contributions,
            ByDepartment = byDepartment
        };
    }

    // ============================================================================
    // Financial Summary
    // ============================================================================

    public async Task<FinancialSummaryResponse> GetFinancialSummaryReportAsync(FinancialSummaryRequest request)
    {
        var endOfDay = request.EndDate.Date.AddDays(1).AddTicks(-1);
        var transactions = await _context.Transactions
            .Include(t => t.ServiceItems)
            .Include(t => t.ProductItems)
            .Where(t => t.TransactionDate >= request.StartDate.Date && t.TransactionDate <= endOfDay && t.PaymentStatus != "Voided")
            .ToListAsync();

        var refunds = await _context.Refunds
            .Where(r => r.RefundDate >= request.StartDate.Date && r.RefundDate <= endOfDay)
            .SumAsync(r => r.RefundAmount);

        var payrollRecords = await _context.PayrollRecords
            .Include(pr => pr.PayrollPeriod)
            .Where(pr => pr.PayrollPeriod!.PaymentDate >= request.StartDate.Date && pr.PayrollPeriod.PaymentDate <= endOfDay)
            .ToListAsync();

        var serviceRevenue = transactions.Sum(t => t.ServiceItems.Sum(si => si.TotalPrice));
        var productRevenue = transactions.Sum(t => t.ProductItems.Sum(pi => pi.TotalPrice));
        var tips = transactions.Sum(t => t.TipAmount);
        var discounts = transactions.Sum(t => t.DiscountAmount);
        var grossRevenue = serviceRevenue + productRevenue + tips;
        var netRevenue = grossRevenue - discounts - refunds;

        var payrollCosts = payrollRecords.Sum(pr => pr.NetPay);
        var commissions = payrollRecords.Sum(pr => pr.Commissions);
        var contributions = payrollRecords.Sum(pr => pr.SSSContribution + pr.PhilHealthContribution + pr.PagIBIGContribution);

        // Inventory cost (products sold)
        var productsCost = transactions.Sum(t => t.ProductItems.Sum(pi =>
        {
            var product = _context.Products.FirstOrDefault(p => p.ProductId == pi.ProductId);
            return (product?.CostPrice ?? 0) * pi.Quantity;
        }));

        var totalExpenses = payrollCosts + productsCost + contributions;
        var grossProfit = netRevenue - productsCost;
        var operatingProfit = netRevenue - totalExpenses;

        // Cash flow by payment method
        var byPaymentMethod = transactions
            .GroupBy(t => t.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.TotalAmount));

        FinancialComparison? comparison = null;
        if (!string.IsNullOrEmpty(request.CompareWith))
        {
            var periodDays = (request.EndDate - request.StartDate).Days;
            DateTime compStart, compEnd;

            if (request.CompareWith == "PreviousYear")
            {
                compStart = request.StartDate.AddYears(-1);
                compEnd = request.EndDate.AddYears(-1);
            }
            else // PreviousPeriod
            {
                compEnd = request.StartDate.AddDays(-1);
                compStart = compEnd.AddDays(-periodDays);
            }

            var compTransactions = await _context.Transactions
                .Where(t => t.TransactionDate >= compStart && t.TransactionDate <= compEnd && t.PaymentStatus != "Voided")
                .ToListAsync();

            var compRevenue = compTransactions.Sum(t => t.TotalAmount);
            var compPayroll = await _context.PayrollRecords
                .Include(pr => pr.PayrollPeriod)
                .Where(pr => pr.PayrollPeriod!.PaymentDate >= compStart && pr.PayrollPeriod.PaymentDate <= compEnd)
                .SumAsync(pr => pr.NetPay);

            var compProfit = compRevenue - compPayroll;

            comparison = new FinancialComparison
            {
                ComparisonStartDate = compStart,
                ComparisonEndDate = compEnd,
                PreviousRevenue = compRevenue,
                RevenueChange = netRevenue - compRevenue,
                RevenueChangePercent = compRevenue > 0 ? (netRevenue - compRevenue) / compRevenue * 100 : 0,
                PreviousProfit = compProfit,
                ProfitChange = operatingProfit - compProfit,
                ProfitChangePercent = compProfit > 0 ? (operatingProfit - compProfit) / compProfit * 100 : 0
            };
        }

        return new FinancialSummaryResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ComparisonPeriod = request.CompareWith,
            Revenue = new FinancialRevenue
            {
                ServiceRevenue = serviceRevenue,
                ProductSales = productRevenue,
                PackageSales = 0,
                TipsReceived = tips,
                GrossRevenue = grossRevenue,
                Discounts = discounts,
                Refunds = refunds,
                NetRevenue = netRevenue
            },
            Expenses = new FinancialExpenses
            {
                PayrollCosts = payrollCosts,
                CommissionsPaid = commissions,
                InventoryCost = productsCost,
                GovernmentContributions = contributions,
                TotalExpenses = totalExpenses
            },
            Profit = new ProfitSummary
            {
                GrossProfit = grossProfit,
                GrossProfitMargin = netRevenue > 0 ? grossProfit / netRevenue * 100 : 0,
                OperatingProfit = operatingProfit,
                OperatingProfitMargin = netRevenue > 0 ? operatingProfit / netRevenue * 100 : 0
            },
            CashFlow = new CashFlowSummary
            {
                CashReceived = byPaymentMethod.GetValueOrDefault("Cash", 0),
                CardReceived = byPaymentMethod.GetValueOrDefault("Card", 0),
                EWalletReceived = byPaymentMethod.GetValueOrDefault("GCash", 0) + byPaymentMethod.GetValueOrDefault("Maya", 0),
                BankTransferReceived = byPaymentMethod.GetValueOrDefault("Bank Transfer", 0),
                TotalInflow = transactions.Sum(t => t.TotalAmount),
                TotalOutflow = payrollCosts,
                NetCashFlow = transactions.Sum(t => t.TotalAmount) - payrollCosts
            },
            Comparison = comparison
        };
    }

    // ============================================================================
    // Appointment Analytics
    // ============================================================================

    public async Task<AppointmentAnalyticsResponse> GetAppointmentAnalyticsReportAsync(AppointmentAnalyticsRequest request)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Therapist)
            .Include(a => a.Service).ThenInclude(s => s.Category)
            .Where(a => a.AppointmentDate >= request.StartDate && a.AppointmentDate <= request.EndDate)
            .ToListAsync();

        if (request.TherapistId.HasValue)
            appointments = appointments.Where(a => a.TherapistId == request.TherapistId.Value).ToList();

        if (request.ServiceCategoryId.HasValue)
            appointments = appointments.Where(a => a.Service?.CategoryId == request.ServiceCategoryId.Value).ToList();

        // Status breakdown
        var statusGroups = appointments.GroupBy(a => a.Status).ToDictionary(g => g.Key, g => g.Count());
        var total = appointments.Count;
        var completed = statusGroups.GetValueOrDefault("Completed", 0);
        var cancelled = statusGroups.GetValueOrDefault("Cancelled", 0);
        var noShow = statusGroups.GetValueOrDefault("No Show", 0);

        // Time analytics
        var byHour = appointments
            .GroupBy(a => a.StartTime.Hours)
            .ToDictionary(g => $"{g.Key}:00", g => g.Count());

        var byDayOfWeek = appointments
            .GroupBy(a => a.AppointmentDate.DayOfWeek.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        // Booking patterns
        var sameDayBookings = appointments.Count(a => a.CreatedAt.Date == a.AppointmentDate.Date);
        var weekInAdvance = appointments.Count(a => (a.AppointmentDate - a.CreatedAt).TotalDays >= 7);
        var monthInAdvance = appointments.Count(a => (a.AppointmentDate - a.CreatedAt).TotalDays >= 30);

        var bySource = appointments.GroupBy(a => a.BookingSource).ToDictionary(g => g.Key, g => g.Count());

        // Capacity utilization by therapist
        var therapistUtilization = appointments
            .Where(a => a.TherapistId.HasValue)
            .GroupBy(a => a.TherapistId!.Value)
            .Select(g => new TherapistUtilization
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Therapist != null ? $"{g.First().Therapist!.FirstName} {g.First().Therapist!.LastName}" : "Unknown",
                BookedSlots = g.Count(),
                AvailableSlots = 0, // Would need schedule calculation
                Utilization = 0
            })
            .ToList();

        // Daily utilization
        var dailyUtilization = appointments
            .GroupBy(a => a.AppointmentDate.Date)
            .Select(g => new DailyUtilization
            {
                Date = g.Key,
                BookedSlots = g.Count(),
                AvailableSlots = 0,
                Utilization = 0
            })
            .OrderBy(d => d.Date)
            .ToList();

        return new AppointmentAnalyticsResponse
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            StatusBreakdown = new AppointmentStatusBreakdown
            {
                Total = total,
                Scheduled = statusGroups.GetValueOrDefault("Scheduled", 0),
                Confirmed = statusGroups.GetValueOrDefault("Confirmed", 0),
                InProgress = statusGroups.GetValueOrDefault("In Progress", 0),
                Completed = completed,
                Cancelled = cancelled,
                NoShow = noShow,
                CompletionRate = total > 0 ? (decimal)completed / total * 100 : 0,
                CancellationRate = total > 0 ? (decimal)cancelled / total * 100 : 0,
                NoShowRate = total > 0 ? (decimal)noShow / total * 100 : 0
            },
            TimeAnalytics = new TimeSlotAnalytics
            {
                ByHour = byHour,
                ByDayOfWeek = byDayOfWeek,
                MostPopularHour = byHour.OrderByDescending(h => h.Value).FirstOrDefault().Key ?? "",
                MostPopularDay = byDayOfWeek.OrderByDescending(d => d.Value).FirstOrDefault().Key ?? "",
                AverageAppointmentDuration = appointments.Any() ? (decimal)appointments.Average(a => a.Service?.DurationMinutes ?? 60) : 0,
                AverageWaitTime = 0
            },
            Patterns = new BookingPatterns
            {
                AverageLeadTime = appointments.Any() ? (decimal)appointments.Average(a => (a.AppointmentDate - a.CreatedAt).TotalDays) : 0,
                SameDayBookings = sameDayBookings,
                WeekInAdvanceBookings = weekInAdvance,
                MonthInAdvanceBookings = monthInAdvance,
                OnlineBookings = bySource.GetValueOrDefault("Email", 0),
                WalkIns = bySource.GetValueOrDefault("Walk-In", 0),
                PhoneBookings = bySource.GetValueOrDefault("Direct", 0)
            },
            Capacity = new CapacityUtilization
            {
                TotalAvailableSlots = 0,
                BookedSlots = appointments.Count,
                OverallUtilization = 0,
                ByTherapist = therapistUtilization,
                ByDay = dailyUtilization
            }
        };
    }

    // ============================================================================
    // Export
    // ============================================================================

    public async Task<ExportResponse> ExportReportAsync(ExportRequest request)
    {
        _logger.LogInformation("Export requested for {ReportType} in {Format} format", request.ReportType, request.Format);

        try
        {
            return request.ReportType.ToLower() switch
            {
                "sales" => await ExportSalesAsync(request),
                "employee" or "employeeperformance" => await ExportEmployeeAsync(request),
                "customer" or "customeranalytics" => await ExportCustomerAsync(request),
                "inventory" => await ExportInventoryAsync(request),
                "payroll" => await ExportPayrollAsync(request),
                "appointment" or "appointmentanalytics" => await ExportAppointmentAsync(request),
                "financial" or "financialsummary" => await ExportFinancialAsync(request),
                _ => throw new ArgumentException($"Unknown report type: {request.ReportType}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export {ReportType} report", request.ReportType);
            throw;
        }
    }

    private string? FormatGeneratedBy(ExportRequest request)
    {
        if (string.IsNullOrEmpty(request.GeneratedByName)) return null;
        return $"{request.GeneratedByName} — Role: {request.GeneratedByRole ?? "Unknown"}";
    }

    private async Task<ExportResponse> ExportSalesAsync(ExportRequest request)
    {
        var data = await GetSalesReportAsync(new SalesReportRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportSalesReportAsync(data, request.StartDate, request.EndDate, request.Format, FormatGeneratedBy(request));
    }

    private async Task<ExportResponse> ExportEmployeeAsync(ExportRequest request)
    {
        var data = await GetEmployeePerformanceReportAsync(new EmployeePerformanceRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportEmployeePerformanceAsync(data, request.StartDate, request.EndDate, request.Format, FormatGeneratedBy(request));
    }

    private async Task<ExportResponse> ExportCustomerAsync(ExportRequest request)
    {
        var data = await GetCustomerAnalyticsReportAsync(new CustomerAnalyticsRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportCustomerAnalyticsAsync(data, request.StartDate, request.EndDate, request.Format, FormatGeneratedBy(request));
    }

    private async Task<ExportResponse> ExportInventoryAsync(ExportRequest request)
    {
        var data = await GetInventoryReportAsync(new InventoryReportRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportInventoryReportAsync(data, request.Format, FormatGeneratedBy(request));
    }

    private async Task<ExportResponse> ExportPayrollAsync(ExportRequest request)
    {
        var data = await GetPayrollSummaryReportAsync(new PayrollSummaryRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportPayrollReportAsync(data, request.StartDate, request.EndDate, request.Format, FormatGeneratedBy(request));
    }

    private async Task<ExportResponse> ExportAppointmentAsync(ExportRequest request)
    {
        var data = await GetAppointmentAnalyticsReportAsync(new AppointmentAnalyticsRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportAppointmentAnalyticsAsync(data, request.StartDate, request.EndDate, request.Format, FormatGeneratedBy(request));
    }

    private async Task<ExportResponse> ExportFinancialAsync(ExportRequest request)
    {
        var data = await GetFinancialSummaryReportAsync(new FinancialSummaryRequest
        {
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });
        return await _exportService.ExportFinancialSummaryAsync(data, request.StartDate, request.EndDate, request.Format, FormatGeneratedBy(request));
    }
}
