using System.ComponentModel.DataAnnotations;
using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.DTOs.Report;

// ============================================================================
// Dashboard DTOs
// ============================================================================

public class DashboardRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class DashboardResponse
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Key Performance Indicators
    public DashboardKpi Kpis { get; set; } = new();

    // Revenue Breakdown
    public RevenueBreakdown Revenue { get; set; } = new();

    // Appointments Summary
    public AppointmentsSummary Appointments { get; set; } = new();

    // Inventory Alerts
    public InventoryAlerts Inventory { get; set; } = new();

    // Top Performers
    public List<TopPerformer> TopTherapists { get; set; } = new();
    public List<TopService> TopServices { get; set; } = new();
    public List<TopCustomer> TopCustomers { get; set; } = new();
}

public class DashboardKpi
{
    public decimal TotalRevenue { get; set; }
    public decimal RevenueChange { get; set; } // Percentage change from previous period
    public int TotalAppointments { get; set; }
    public int AppointmentChange { get; set; }
    public int NewCustomers { get; set; }
    public int NewCustomerChange { get; set; }
    public decimal AverageTicket { get; set; }
    public decimal AverageTicketChange { get; set; }
    public decimal OccupancyRate { get; set; } // Percentage of available slots filled
    public decimal CustomerRetentionRate { get; set; }
}

public class RevenueBreakdown
{
    public decimal ServiceRevenue { get; set; }
    public decimal ProductRevenue { get; set; }
    public decimal PackageRevenue { get; set; }
    public decimal TipsReceived { get; set; }
    public decimal Discounts { get; set; }
    public decimal RefundsProcessed { get; set; }
    public decimal NetRevenue { get; set; }
    public List<DailyRevenue> DailyTrend { get; set; } = new();
}

public class DailyRevenue
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int AppointmentCount { get; set; }
}

public class AppointmentsSummary
{
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
    public int InProgress { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal NoShowRate { get; set; }
}

public class InventoryAlerts
{
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public int ExpiringItems { get; set; } // Items expiring within 30 days
    public List<LowStockItem> LowStockList { get; set; } = new();
}

public class LowStockItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class TopPerformer
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int AppointmentsCompleted { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal CommissionsEarned { get; set; }
    public decimal AverageRating { get; set; }
}

public class TopService
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopCustomer
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public decimal TotalSpent { get; set; }
    public int LoyaltyPoints { get; set; }
}

// ============================================================================
// Sales Report DTOs
// ============================================================================

public class SalesReportRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? GroupBy { get; set; } = "Day"; // Day, Week, Month
    public int? ServiceId { get; set; }
    public int? EmployeeId { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IncludeVoided { get; set; } = false;
}

public class SalesReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string GroupBy { get; set; } = "Day";

    // Summary
    public decimal GrossSales { get; set; }
    public decimal Discounts { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }

    // By Payment Method
    public List<PaymentMethodBreakdown> PaymentBreakdown { get; set; } = new();

    // By Category
    public List<CategorySales> CategoryBreakdown { get; set; } = new();

    // Time Series Data
    public List<PeriodSales> TimeSeries { get; set; } = new();
}

public class PaymentMethodBreakdown
{
    public string PaymentMethod { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class CategorySales
{
    public string Category { get; set; } = string.Empty;
    public int ItemsSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal Percentage { get; set; }
}

public class PeriodSales
{
    public DateTime PeriodStart { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal GrossSales { get; set; }
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }
}

// ============================================================================
// Service Performance Report DTOs
// ============================================================================

public class ServicePerformanceRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? CategoryId { get; set; }
    public string? SortBy { get; set; } = "Revenue"; // Revenue, BookingCount, Rating
}

public class ServicePerformanceResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalServicesAnalyzed { get; set; }
    public List<ServicePerformanceItem> Services { get; set; } = new();
}

public class ServicePerformanceItem
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRevenue { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public decimal AverageDuration { get; set; }
}

// ============================================================================
// Employee Performance Report DTOs
// ============================================================================

public class EmployeePerformanceRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? DepartmentId { get; set; }
    public string? JobTitle { get; set; }
    public string? SortBy { get; set; } = "Revenue"; // Revenue, Appointments, Rating
}

public class EmployeePerformanceResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEmployeesAnalyzed { get; set; }
    public List<EmployeePerformanceItem> Employees { get; set; } = new();
    public EmployeePerformanceSummary Summary { get; set; } = new();
}

public class EmployeePerformanceItem
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string? Department { get; set; }
    public int AppointmentsScheduled { get; set; }
    public int AppointmentsCompleted { get; set; }
    public int CancelledByTherapist { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal CommissionsEarned { get; set; }
    public decimal AverageServiceValue { get; set; }
    public int DaysWorked { get; set; }
    public decimal UtilizationRate { get; set; } // Booked hours vs available hours
    public decimal AverageRating { get; set; }
    public int ReviewCount { get; set; }
}

public class EmployeePerformanceSummary
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommissions { get; set; }
    public int TotalAppointments { get; set; }
    public decimal AverageCompletionRate { get; set; }
    public decimal AverageUtilization { get; set; }
    public decimal AverageRating { get; set; }
}

// ============================================================================
// Customer Analytics Report DTOs
// ============================================================================

public class CustomerAnalyticsRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? CustomerSegment { get; set; } // New, Returning, VIP
    public bool IncludeChurnAnalysis { get; set; } = false;
}

public class CustomerAnalyticsResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Customer Counts
    public CustomerCounts Counts { get; set; } = new();

    // Retention Analysis
    public RetentionAnalysis Retention { get; set; } = new();

    // Customer Segments
    public List<CustomerSegment> Segments { get; set; } = new();

    // Visit Patterns
    public VisitPatterns Patterns { get; set; } = new();

    // Top Customers
    public List<TopCustomerDetail> TopCustomers { get; set; } = new();
}

public class CustomerCounts
{
    public int TotalCustomers { get; set; }
    public int ActiveCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int ReturningCustomers { get; set; }
    public int InactiveCustomers { get; set; } // No visit in 90 days
    public int ChurnedCustomers { get; set; } // No visit in 180 days
}

public class RetentionAnalysis
{
    public decimal OverallRetentionRate { get; set; }
    public decimal FirstVisitReturnRate { get; set; }
    public decimal AverageVisitFrequency { get; set; }
    public decimal AverageCustomerLifetimeValue { get; set; }
    public List<CohortRetention> MonthlyCohorts { get; set; } = new();
}

public class CohortRetention
{
    public string Cohort { get; set; } = string.Empty; // Month-Year of first visit
    public int CohortSize { get; set; }
    public decimal Month1Retention { get; set; }
    public decimal Month3Retention { get; set; }
    public decimal Month6Retention { get; set; }
    public decimal Month12Retention { get; set; }
}

public class CustomerSegment
{
    public string SegmentName { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal Percentage { get; set; }
    public decimal AverageSpend { get; set; }
    public decimal AverageVisits { get; set; }
}

public class VisitPatterns
{
    public Dictionary<string, int> VisitsByDayOfWeek { get; set; } = new();
    public Dictionary<string, int> VisitsByHour { get; set; } = new();
    public string PeakDay { get; set; } = string.Empty;
    public string PeakHour { get; set; } = string.Empty;
    public decimal AverageSessionDuration { get; set; }
}

public class TopCustomerDetail
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string MembershipTier { get; set; } = string.Empty;
    public int TotalVisits { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageSpend { get; set; }
    public DateTime LastVisit { get; set; }
    public int LoyaltyPoints { get; set; }
    public string? FavoriteService { get; set; }
}

// ============================================================================
// Inventory Report DTOs
// ============================================================================

public class InventoryReportRequest
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CategoryId { get; set; }
    public int? SupplierId { get; set; }
    public bool IncludeZeroStock { get; set; } = true;
}

public class InventoryReportResponse
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Summary
    public InventorySummary Summary { get; set; } = new();

    // Stock Status
    public List<ProductStockStatus> Products { get; set; } = new();

    // Movement Analysis
    public InventoryMovementSummary Movement { get; set; } = new();

    // Valuation
    public InventoryValuation Valuation { get; set; } = new();
}

public class InventorySummary
{
    public int TotalProducts { get; set; }
    public int TotalCategories { get; set; }
    public int InStockProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int ExpiredProducts { get; set; }
    public int ExpiringSoonProducts { get; set; }
}

public class ProductStockStatus
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Supplier { get; set; }
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public decimal UnitCost { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal StockValue { get; set; }
    public string StockStatus { get; set; } = string.Empty; // Ok, Low, Out, Overstock
    public DateTime? ExpiryDate { get; set; }
    public int UnitsUsedPeriod { get; set; }
    public int UnitsSoldPeriod { get; set; }
}

public class InventoryMovementSummary
{
    public int TotalReceived { get; set; }
    public int TotalSold { get; set; }
    public int TotalUsed { get; set; }
    public int TotalWasted { get; set; }
    public int TotalAdjusted { get; set; }
    public List<CategoryMovement> ByCategory { get; set; } = new();
}

public class CategoryMovement
{
    public string Category { get; set; } = string.Empty;
    public int Received { get; set; }
    public int Sold { get; set; }
    public int Used { get; set; }
    public int Wasted { get; set; }
    public int NetChange { get; set; }
}

public class InventoryValuation
{
    public decimal TotalCostValue { get; set; }
    public decimal TotalRetailValue { get; set; }
    public decimal PotentialProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<CategoryValuation> ByCategory { get; set; } = new();
}

public class CategoryValuation
{
    public string Category { get; set; } = string.Empty;
    public decimal CostValue { get; set; }
    public decimal RetailValue { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

// ============================================================================
// Payroll Summary Report DTOs
// ============================================================================

public class PayrollSummaryRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? DepartmentId { get; set; }
}

public class PayrollSummaryReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int PeriodsIncluded { get; set; }

    // Totals
    public PayrollTotals Totals { get; set; } = new();

    // By Period
    public List<PeriodPayrollSummary> ByPeriod { get; set; } = new();

    // Government Contributions
    public ContributionsSummary Contributions { get; set; } = new();

    // By Department
    public List<DepartmentPayroll> ByDepartment { get; set; } = new();
}

public class PayrollTotals
{
    public decimal GrossPay { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Overtime { get; set; }
    public decimal NightDifferential { get; set; }
    public decimal Commissions { get; set; }
    public decimal Allowances { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
    public int TotalEmployeesPaid { get; set; }
}

public class PeriodPayrollSummary
{
    public int PayrollPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int EmployeeCount { get; set; }
    public decimal GrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetPay { get; set; }
}

public class ContributionsSummary
{
    public decimal TotalSssEmployee { get; set; }
    public decimal TotalSssEmployer { get; set; }
    public decimal TotalPhilHealthEmployee { get; set; }
    public decimal TotalPhilHealthEmployer { get; set; }
    public decimal TotalPagIbigEmployee { get; set; }
    public decimal TotalPagIbigEmployer { get; set; }
    public decimal TotalWithholdingTax { get; set; }
    public decimal GrandTotalEmployee { get; set; }
    public decimal GrandTotalEmployer { get; set; }
}

public class DepartmentPayroll
{
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal GrossPay { get; set; }
    public decimal NetPay { get; set; }
    public decimal AverageGross { get; set; }
    public decimal AverageNet { get; set; }
}

// ============================================================================
// Financial Summary Report DTOs
// ============================================================================

public class FinancialSummaryRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public string? CompareWith { get; set; } // PreviousPeriod, PreviousYear
}

public class FinancialSummaryResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? ComparisonPeriod { get; set; }

    // Revenue
    public FinancialRevenue Revenue { get; set; } = new();

    // Expenses (Payroll as proxy)
    public FinancialExpenses Expenses { get; set; } = new();

    // Profit Summary
    public ProfitSummary Profit { get; set; } = new();

    // Cash Flow
    public CashFlowSummary CashFlow { get; set; } = new();

    // Comparison (if requested)
    public FinancialComparison? Comparison { get; set; }
}

public class FinancialRevenue
{
    public decimal ServiceRevenue { get; set; }
    public decimal ProductSales { get; set; }
    public decimal PackageSales { get; set; }
    public decimal TipsReceived { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal Discounts { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetRevenue { get; set; }
}

public class FinancialExpenses
{
    public decimal PayrollCosts { get; set; }
    public decimal CommissionsPaid { get; set; }
    public decimal InventoryCost { get; set; }
    public decimal GovernmentContributions { get; set; }
    public decimal TotalExpenses { get; set; }
}

public class ProfitSummary
{
    public decimal GrossProfit { get; set; }
    public decimal GrossProfitMargin { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal OperatingProfitMargin { get; set; }
}

public class CashFlowSummary
{
    public decimal CashReceived { get; set; }
    public decimal CardReceived { get; set; }
    public decimal EWalletReceived { get; set; }
    public decimal BankTransferReceived { get; set; }
    public decimal TotalInflow { get; set; }
    public decimal TotalOutflow { get; set; }
    public decimal NetCashFlow { get; set; }
}

public class FinancialComparison
{
    public DateTime ComparisonStartDate { get; set; }
    public DateTime ComparisonEndDate { get; set; }
    public decimal PreviousRevenue { get; set; }
    public decimal RevenueChange { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal PreviousProfit { get; set; }
    public decimal ProfitChange { get; set; }
    public decimal ProfitChangePercent { get; set; }
}

// ============================================================================
// Appointment Analytics DTOs
// ============================================================================

public class AppointmentAnalyticsRequest
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? TherapistId { get; set; }
    public int? ServiceCategoryId { get; set; }
}

public class AppointmentAnalyticsResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Status Distribution
    public AppointmentStatusBreakdown StatusBreakdown { get; set; } = new();

    // Time Analytics
    public TimeSlotAnalytics TimeAnalytics { get; set; } = new();

    // Booking Patterns
    public BookingPatterns Patterns { get; set; } = new();

    // Capacity Utilization
    public CapacityUtilization Capacity { get; set; } = new();
}

public class AppointmentStatusBreakdown
{
    public int Total { get; set; }
    public int Scheduled { get; set; }
    public int Confirmed { get; set; }
    public int InProgress { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int NoShow { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal NoShowRate { get; set; }
}

public class TimeSlotAnalytics
{
    public Dictionary<string, int> ByHour { get; set; } = new();
    public Dictionary<string, int> ByDayOfWeek { get; set; } = new();
    public string MostPopularHour { get; set; } = string.Empty;
    public string MostPopularDay { get; set; } = string.Empty;
    public decimal AverageAppointmentDuration { get; set; }
    public decimal AverageWaitTime { get; set; }
}

public class BookingPatterns
{
    public decimal AverageLeadTime { get; set; } // Days between booking and appointment
    public int SameDayBookings { get; set; }
    public int WeekInAdvanceBookings { get; set; }
    public int MonthInAdvanceBookings { get; set; }
    public int OnlineBookings { get; set; }
    public int WalkIns { get; set; }
    public int PhoneBookings { get; set; }
}

public class CapacityUtilization
{
    public int TotalAvailableSlots { get; set; }
    public int BookedSlots { get; set; }
    public decimal OverallUtilization { get; set; }
    public List<TherapistUtilization> ByTherapist { get; set; } = new();
    public List<DailyUtilization> ByDay { get; set; } = new();
}

public class TherapistUtilization
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public int AvailableSlots { get; set; }
    public int BookedSlots { get; set; }
    public decimal Utilization { get; set; }
}

public class DailyUtilization
{
    public DateTime Date { get; set; }
    public int AvailableSlots { get; set; }
    public int BookedSlots { get; set; }
    public decimal Utilization { get; set; }
}

// ============================================================================
// Report Export DTOs
// ============================================================================

public class ExportRequest
{
    [Required]
    public string ReportType { get; set; } = string.Empty; // Sales, Employee, Customer, Inventory, Payroll, Financial, Appointment

    [Required]
    public string Format { get; set; } = "PDF"; // PDF, Excel, CSV

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    // Additional filters based on report type
    public Dictionary<string, string>? Filters { get; set; }

    // Populated server-side from JWT claims
    public string? GeneratedByName { get; set; }
    public string? GeneratedByRole { get; set; }
}

public class ExportResponse
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

// ============================================================================
// Quick Stats DTOs (for widgets)
// ============================================================================

public class QuickStatsResponse
{
    public DateTime AsOf { get; set; } = DateTime.UtcNow;
    public TodayStats Today { get; set; } = new();
    public WeekStats ThisWeek { get; set; } = new();
    public MonthStats ThisMonth { get; set; } = new();
}

public class TodayStats
{
    public decimal Revenue { get; set; }
    public int Appointments { get; set; }
    public int Completed { get; set; }
    public int Remaining { get; set; }
    public int WalkIns { get; set; }
    public int NewCustomers { get; set; }
}

public class WeekStats
{
    public decimal Revenue { get; set; }
    public decimal RevenueVsLastWeek { get; set; }
    public int Appointments { get; set; }
    public decimal CompletionRate { get; set; }
}

public class MonthStats
{
    public decimal Revenue { get; set; }
    public decimal RevenueVsLastMonth { get; set; }
    public int Appointments { get; set; }
    public int NewCustomers { get; set; }
    public decimal AverageTicket { get; set; }
}
