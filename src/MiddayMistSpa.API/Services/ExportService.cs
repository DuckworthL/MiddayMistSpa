using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MiddayMistSpa.API.DTOs.Customer;
using MiddayMistSpa.API.DTOs.Report;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Interfaces;

namespace MiddayMistSpa.API.Services;

public interface IExportService
{
    Task<ExportResponse> ExportSalesReportAsync(SalesReportResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null);
    Task<ExportResponse> ExportEmployeePerformanceAsync(EmployeePerformanceResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null);
    Task<ExportResponse> ExportCustomerAnalyticsAsync(CustomerAnalyticsResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null);
    Task<ExportResponse> ExportInventoryReportAsync(InventoryReportResponse data, string format, string? generatedBy = null);
    Task<ExportResponse> ExportPayrollReportAsync(PayrollSummaryReportResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null);
    Task<ExportResponse> ExportAppointmentAnalyticsAsync(AppointmentAnalyticsResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null);
    Task<ExportResponse> ExportFinancialSummaryAsync(FinancialSummaryResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null);
    Task<ExportResponse> ExportSegmentationAsync(List<CustomerSegmentResponse> segments, List<(string SegmentName, List<CustomerListResponse> Customers)> segmentCustomers, string format, ClusteringPerformanceMetrics? metrics = null, string? generatedBy = null);
    byte[] GenerateReceiptPdf(ReceiptData receipt);
}

public class ReceiptData
{
    public string TransactionNumber { get; set; } = "";
    public DateTime TransactionDate { get; set; }
    public string CustomerName { get; set; } = "Walk-in";
    public string CashierName { get; set; } = "";
    public string? AppointmentNumber { get; set; }
    public string? TherapistName { get; set; }
    public List<ReceiptLineItem> Services { get; set; } = new();
    public List<ReceiptLineItem> Products { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public string? ClientCurrency { get; set; }
    public decimal? TotalInClientCurrency { get; set; }
    public string? CurrencySymbol { get; set; }
}

public class ReceiptLineItem
{
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ExportService : IExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExportService> _logger;
    private readonly string _logoPath;

    // Company branding
    private const string CompanyName = "MiddayMist Spa";
    private const string CompanyTagline = "Wellness Management System";
    private const string CompanyAddress = "123 Wellness Avenue, Manila, Philippines";
    private const string CompanyPhone = "+63 2 1234 5678";
    private const string CompanyEmail = "info@middaymistspa.com";

    // Brand colors
    private static readonly string HeaderBgColor = "#2E7D6F";

    public ExportService(IUnitOfWork unitOfWork, ILogger<ExportService> logger, IWebHostEnvironment env)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;

        // QuestPDF Community License — wrapped in try/catch because the native
        // QuestPdfSkia library may not be available on shared hosting (MonsterASP.NET)
        try { QuestPDF.Settings.License = LicenseType.Community; }
        catch (Exception ex) { _logger.LogWarning(ex, "QuestPDF init failed — PDF exports will be unavailable"); }

        // Try to find logo
        _logoPath = Path.Combine(env.ContentRootPath, "..", "MiddayMistSpa.Web", "wwwroot", "images", "logo.png");
        if (!File.Exists(_logoPath))
        {
            _logoPath = "";
            _logger.LogWarning("Logo file not found at expected path");
        }
    }

    // ========================================================================
    // PDF Helpers - Branded Header/Footer
    // ========================================================================

    private void ComposeHeader(IContainer container, string reportTitle, DateTime? startDate = null, DateTime? endDate = null, string? generatedBy = null)
    {
        container.Column(outer =>
        {
            outer.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Row(headerRow =>
                    {
                        if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                        {
                            headerRow.ConstantItem(50).Image(_logoPath);
                            headerRow.ConstantItem(10).Text("");
                        }
                        headerRow.RelativeItem().Column(titleCol =>
                        {
                            titleCol.Item().Text(CompanyName)
                                .FontSize(18).Bold().FontColor(HeaderBgColor);
                            titleCol.Item().Text(CompanyTagline)
                                .FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingTop(3).Text(CompanyAddress).FontSize(7).FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Phone: {CompanyPhone} | Email: {CompanyEmail}").FontSize(7).FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(200).AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Text(reportTitle)
                        .FontSize(14).Bold().FontColor(HeaderBgColor);

                    if (startDate.HasValue && endDate.HasValue)
                    {
                        col.Item().AlignRight().Text($"{startDate.Value:MMMM dd, yyyy} — {endDate.Value:MMMM dd, yyyy}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    }

                    col.Item().AlignRight().Text($"Generated: {PhilippineTime.Now:MMM dd, yyyy hh:mm tt}")
                        .FontSize(8).FontColor(Colors.Grey.Medium);

                    if (!string.IsNullOrEmpty(generatedBy))
                    {
                        col.Item().AlignRight().Text($"By: {generatedBy}")
                            .FontSize(8).FontColor(Colors.Grey.Darken1).Italic();
                    }
                });
            });

            outer.Item().PaddingTop(5).LineHorizontal(1).LineColor(HeaderBgColor);
        });
    }

    private static void ComposeFooter(IContainer container, string reportTitle)
    {
        container.AlignCenter().Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(text =>
            {
                text.Span($"{CompanyName} — {reportTitle}").FontSize(8).FontColor(Colors.Grey.Medium);
            });
            row.RelativeItem().AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });
    }

    private static void SectionTitle(IContainer container, string title)
    {
        container.PaddingTop(8).PaddingBottom(3)
            .BorderLeft(3).BorderColor(HeaderBgColor)
            .PaddingLeft(6)
            .Text(title).FontSize(11).Bold().FontColor(HeaderBgColor);
    }

    private static void KpiCard(IContainer container, string label, string value, string? change = null)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(col =>
        {
            col.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
            col.Item().Text(value).FontSize(13).Bold();
            if (!string.IsNullOrEmpty(change))
            {
                col.Item().Text(change).FontSize(7).FontColor(
                    change.StartsWith("+") ? Colors.Green.Darken2 : Colors.Red.Darken2);
            }
        });
    }

    // Chart color palette
    private static readonly string[] ChartPalette = new[]
    {
        "#2E7D6F", "#3498DB", "#E74C3C", "#F39C12", "#9B59B6",
        "#1ABC9C", "#E67E22", "#2980B9", "#27AE60", "#8E44AD"
    };

    private static void HorizontalBarChart(IContainer container, string chartTitle,
        List<(string Label, decimal Value)> items, string valuePrefix = "₱", bool showDecimals = true)
    {
        if (!items.Any()) return;
        // Limit to 6 bars max for compactness
        var displayItems = items.Take(6).ToList();
        var maxVal = displayItems.Max(x => Math.Abs(x.Value));
        if (maxVal == 0) maxVal = 1;

        container.PaddingTop(6).PaddingBottom(4).Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(4).Text(chartTitle).FontSize(9).Bold().FontColor(HeaderBgColor);

            for (int i = 0; i < displayItems.Count; i++)
            {
                var item = displayItems[i];
                var pct = (float)(Math.Abs(item.Value) / maxVal);
                var color = ChartPalette[i % ChartPalette.Length];
                var valText = showDecimals ? $"{valuePrefix}{item.Value:N2}" : $"{valuePrefix}{item.Value:N0}";
                var truncLabel = item.Label.Length > 14 ? item.Label[..14] + "…" : item.Label;

                col.Item().PaddingVertical(1).Row(row =>
                {
                    row.ConstantItem(80).AlignRight().PaddingRight(4)
                        .AlignMiddle().Text(truncLabel).FontSize(7);
                    row.RelativeItem().Column(barCol =>
                    {
                        barCol.Item().Height(11)
                            .Background(Colors.Grey.Lighten3)
                            .Padding(0).Row(bgRow =>
                            {
                                var fillWidth = Math.Max(pct, 0.02f);
                                bgRow.RelativeItem((int)(fillWidth * 100))
                                    .Background(color).Height(11);
                                if (fillWidth < 1f)
                                    bgRow.RelativeItem((int)((1f - fillWidth) * 100))
                                        .Height(11);
                            });
                    });
                    row.ConstantItem(70).AlignLeft().PaddingLeft(4)
                        .AlignMiddle().Text(valText).FontSize(7);
                });
            }
        });
    }

    // ========================================================================
    // Sales Report Export
    // ========================================================================

    public Task<ExportResponse> ExportSalesReportAsync(SalesReportResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null)
    {
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportSalesCsv(data, startDate, endDate));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportSalesExcel(data, startDate, endDate));
        return Task.FromResult(ExportSalesPdf(data, startDate, endDate, generatedBy));
    }

    private ExportResponse ExportSalesPdf(SalesReportResponse data, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Footer().Element(c => ComposeFooter(c, "Sales Report"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Sales Report", startDate, endDate, generatedBy));
                    col.Item().PaddingTop(8);
                    // KPI Summary
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Gross Sales", $"₱{data.GrossSales:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Net Sales", $"₱{data.NetSales:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Transactions", data.TransactionCount.ToString("N0")));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Avg Transaction", $"₱{data.AverageTransaction:N2}"));
                    });

                    // Discounts & Refunds
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Discounts", $"₱{data.Discounts:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Refunds", $"₱{data.Refunds:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Text("");
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Text("");
                    });

                    // Payment Method Breakdown
                    if (data.PaymentBreakdown?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Payment Method Breakdown"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Payment Method").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Count").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Amount").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("% Share").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            foreach (var pm in data.PaymentBreakdown)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(pm.PaymentMethod).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(pm.Count.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{pm.Amount:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{pm.Percentage:N1}%").FontSize(9);
                            }
                        });

                        // Chart after table
                        col.Item().Element(c => HorizontalBarChart(c, "Revenue by Payment Method",
                            data.PaymentBreakdown.Select(p => (p.PaymentMethod, p.Amount)).ToList()));
                    }

                    // Category Sales Breakdown
                    if (data.CategoryBreakdown?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Sales by Category"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Category").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Items Sold").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Revenue").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("% Share").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            foreach (var cat in data.CategoryBreakdown)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(cat.Category).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(cat.ItemsSold.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{cat.Revenue:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{cat.Percentage:N1}%").FontSize(9);
                            }
                        });

                        // Chart after table
                        col.Item().Element(c => HorizontalBarChart(c, "Revenue by Category",
                            data.CategoryBreakdown.Select(cat => (cat.Category, cat.Revenue)).ToList()));
                    }

                    // Daily/Period Trend
                    if (data.TimeSeries?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Sales Trend"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Period").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Gross Sales").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Net Sales").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Transactions").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            foreach (var ts in data.TimeSeries)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(ts.PeriodLabel).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{ts.GrossSales:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{ts.NetSales:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(ts.TransactionCount.ToString()).FontSize(9);
                            }
                        });
                    }
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Sales_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportSalesExcel(SalesReportResponse data, DateTime startDate, DateTime endDate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Sales Report");

        // Header
        WriteExcelHeader(ws, "Sales Report", startDate, endDate);

        // Summary
        var row = 5;
        ws.Cell(row, 1).Value = "Gross Sales"; ws.Cell(row, 2).Value = data.GrossSales; ws.Cell(row, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 1, 1).Value = "Discounts"; ws.Cell(row + 1, 2).Value = data.Discounts; ws.Cell(row + 1, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 2, 1).Value = "Refunds"; ws.Cell(row + 2, 2).Value = data.Refunds; ws.Cell(row + 2, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 3, 1).Value = "Net Sales"; ws.Cell(row + 3, 2).Value = data.NetSales; ws.Cell(row + 3, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 3, 1).Style.Font.Bold = true; ws.Cell(row + 3, 2).Style.Font.Bold = true;
        ws.Cell(row + 4, 1).Value = "Transaction Count"; ws.Cell(row + 4, 2).Value = data.TransactionCount;
        ws.Cell(row + 5, 1).Value = "Average Transaction"; ws.Cell(row + 5, 2).Value = data.AverageTransaction; ws.Cell(row + 5, 2).Style.NumberFormat.Format = "₱#,##0.00";

        // Payment Breakdown Sheet
        if (data.PaymentBreakdown?.Any() == true)
        {
            var pmWs = wb.Worksheets.Add("Payment Methods");
            WriteExcelHeader(pmWs, "Payment Method Breakdown", startDate, endDate);
            var pmRow = 5;
            pmWs.Cell(pmRow, 1).Value = "Payment Method"; pmWs.Cell(pmRow, 2).Value = "Count"; pmWs.Cell(pmRow, 3).Value = "Amount"; pmWs.Cell(pmRow, 4).Value = "% Share";
            StyleExcelHeaderRow(pmWs, pmRow, 4);
            foreach (var pm in data.PaymentBreakdown)
            {
                pmRow++;
                pmWs.Cell(pmRow, 1).Value = pm.PaymentMethod;
                pmWs.Cell(pmRow, 2).Value = pm.Count;
                pmWs.Cell(pmRow, 3).Value = pm.Amount; pmWs.Cell(pmRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                pmWs.Cell(pmRow, 4).Value = pm.Percentage / 100; pmWs.Cell(pmRow, 4).Style.NumberFormat.Format = "0.0%";
            }
            pmWs.Columns().AdjustToContents();
        }

        // Time Series Sheet
        if (data.TimeSeries?.Any() == true)
        {
            var tsWs = wb.Worksheets.Add("Trend");
            WriteExcelHeader(tsWs, "Sales Trend", startDate, endDate);
            var tsRow = 5;
            tsWs.Cell(tsRow, 1).Value = "Period"; tsWs.Cell(tsRow, 2).Value = "Gross Sales"; tsWs.Cell(tsRow, 3).Value = "Net Sales"; tsWs.Cell(tsRow, 4).Value = "Transactions";
            StyleExcelHeaderRow(tsWs, tsRow, 4);
            foreach (var ts in data.TimeSeries)
            {
                tsRow++;
                tsWs.Cell(tsRow, 1).Value = ts.PeriodLabel;
                tsWs.Cell(tsRow, 2).Value = ts.GrossSales; tsWs.Cell(tsRow, 2).Style.NumberFormat.Format = "₱#,##0.00";
                tsWs.Cell(tsRow, 3).Value = ts.NetSales; tsWs.Cell(tsRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                tsWs.Cell(tsRow, 4).Value = ts.TransactionCount;
            }
            tsWs.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Sales_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    private ExportResponse ExportSalesCsv(SalesReportResponse data, DateTime startDate, DateTime endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Sales Report — {CompanyName}");
        sb.AppendLine($"Period: {startDate:MMM dd yyyy} to {endDate:MMM dd yyyy}");
        sb.AppendLine($"Generated: {PhilippineTime.Now:MMM dd yyyy hh:mm tt}");
        sb.AppendLine();
        sb.AppendLine($"Gross Sales,{data.GrossSales:N2}");
        sb.AppendLine($"Discounts,{data.Discounts:N2}");
        sb.AppendLine($"Refunds,{data.Refunds:N2}");
        sb.AppendLine($"Net Sales,{data.NetSales:N2}");
        sb.AppendLine($"Transaction Count,{data.TransactionCount}");
        sb.AppendLine($"Average Transaction,{data.AverageTransaction:N2}");

        if (data.PaymentBreakdown?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine("Payment Method,Count,Amount,% Share");
            foreach (var pm in data.PaymentBreakdown)
                sb.AppendLine($"{pm.PaymentMethod},{pm.Count},{pm.Amount:N2},{pm.Percentage:N1}%");
        }

        if (data.TimeSeries?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine("Period,Gross Sales,Net Sales,Transactions");
            foreach (var ts in data.TimeSeries)
                sb.AppendLine($"{ts.PeriodLabel},{ts.GrossSales:N2},{ts.NetSales:N2},{ts.TransactionCount}");
        }

        return new ExportResponse
        {
            FileName = $"Sales_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv",
            ContentType = "text/csv",
            FileContent = System.Text.Encoding.UTF8.GetBytes(sb.ToString())
        };
    }

    // ========================================================================
    // Employee Performance Export
    // ========================================================================

    public Task<ExportResponse> ExportEmployeePerformanceAsync(EmployeePerformanceResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null)
    {
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportEmployeeCsv(data, startDate, endDate));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportEmployeeExcel(data, startDate, endDate));
        return Task.FromResult(ExportEmployeePdf(data, startDate, endDate, generatedBy));
    }

    private ExportResponse ExportEmployeePdf(EmployeePerformanceResponse data, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Footer().Element(c => ComposeFooter(c, "Employee Performance Report"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Employee Performance Report", startDate, endDate, generatedBy));
                    col.Item().PaddingTop(8);
                    // Summary
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Total Employees", data.TotalEmployeesAnalyzed.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Total Revenue", $"₱{data.Summary.TotalRevenue:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Total Commissions", $"₱{data.Summary.TotalCommissions:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Total Appointments", data.Summary.TotalAppointments.ToString()));
                    });

                    // Employee Details
                    if (data.Employees?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Employee Performance Details"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);  // Employee
                                cols.RelativeColumn(2);  // Position
                                cols.RelativeColumn(1.5f); // Scheduled
                                cols.RelativeColumn(1.5f); // Worked
                                cols.RelativeColumn(1.5f); // Absences
                                cols.RelativeColumn(1.5f); // Attendance
                                cols.RelativeColumn(1.5f); // Services
                                cols.RelativeColumn(2);  // Revenue
                                cols.RelativeColumn(2);  // Total Comm
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(4).Text("Employee").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).Text("Position").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignCenter().Text("Scheduled").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignCenter().Text("Worked").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignCenter().Text("Absences").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignCenter().Text("Attendance").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignCenter().Text("Services").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignRight().Text("Revenue").FontSize(8).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(4).AlignRight().Text("Total Comm").FontSize(8).FontColor(Colors.White).Bold();
                            });

                            foreach (var emp in data.Employees)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(emp.EmployeeName).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(emp.JobTitle).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{emp.ScheduledDays}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{emp.DaysWorked}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{emp.Absences}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text($"{emp.AttendanceRate:N0}%").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(emp.AppointmentsCompleted.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{emp.RevenueGenerated:N2}").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{emp.CommissionsEarned:N2}").FontSize(8);
                            }
                        });

                        // Chart after table
                        col.Item().Element(c => HorizontalBarChart(c, "Revenue by Employee",
                            data.Employees.OrderByDescending(e => e.RevenueGenerated).Take(6)
                                .Select(e => (e.EmployeeName, e.RevenueGenerated)).ToList()));
                    }
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Employee_Performance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportEmployeeExcel(EmployeePerformanceResponse data, DateTime startDate, DateTime endDate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Employee Performance");
        WriteExcelHeader(ws, "Employee Performance Report", startDate, endDate);

        var row = 5;
        ws.Cell(row, 1).Value = "Employee";
        ws.Cell(row, 2).Value = "Position";
        ws.Cell(row, 3).Value = "Scheduled Days";
        ws.Cell(row, 4).Value = "Days Worked";
        ws.Cell(row, 5).Value = "Absences";
        ws.Cell(row, 6).Value = "Attendance %";
        ws.Cell(row, 7).Value = "Services Completed";
        ws.Cell(row, 8).Value = "Revenue";
        ws.Cell(row, 9).Value = "Service Comm";
        ws.Cell(row, 10).Value = "Product Comm";
        ws.Cell(row, 11).Value = "Total Comm";
        StyleExcelHeaderRow(ws, row, 11);

        if (data.Employees != null)
        {
            foreach (var emp in data.Employees)
            {
                row++;
                ws.Cell(row, 1).Value = emp.EmployeeName;
                ws.Cell(row, 2).Value = emp.JobTitle;
                ws.Cell(row, 3).Value = emp.ScheduledDays;
                ws.Cell(row, 4).Value = emp.DaysWorked;
                ws.Cell(row, 5).Value = emp.Absences;
                ws.Cell(row, 6).Value = emp.AttendanceRate / 100; ws.Cell(row, 6).Style.NumberFormat.Format = "0.0%";
                ws.Cell(row, 7).Value = emp.AppointmentsCompleted;
                ws.Cell(row, 8).Value = emp.RevenueGenerated; ws.Cell(row, 8).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(row, 9).Value = emp.ServiceCommissions; ws.Cell(row, 9).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(row, 10).Value = emp.ProductCommissions; ws.Cell(row, 10).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(row, 11).Value = emp.CommissionsEarned; ws.Cell(row, 11).Style.NumberFormat.Format = "₱#,##0.00";
            }
        }

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Employee_Performance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    private ExportResponse ExportEmployeeCsv(EmployeePerformanceResponse data, DateTime startDate, DateTime endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Employee Performance Report — {CompanyName}");
        sb.AppendLine($"Period: {startDate:MMM dd yyyy} to {endDate:MMM dd yyyy}");
        sb.AppendLine();
        sb.AppendLine("Employee,Position,Scheduled Days,Days Worked,Absences,Attendance %,Services Completed,Revenue,Service Comm,Product Comm,Total Comm");
        if (data.Employees != null)
            foreach (var emp in data.Employees)
                sb.AppendLine($"\"{emp.EmployeeName}\",\"{emp.JobTitle}\",{emp.ScheduledDays},{emp.DaysWorked},{emp.Absences},{emp.AttendanceRate:N1}%,{emp.AppointmentsCompleted},{emp.RevenueGenerated:N2},{emp.ServiceCommissions:N2},{emp.ProductCommissions:N2},{emp.CommissionsEarned:N2}");

        return CsvToResponse(sb, $"Employee_Performance_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    // ========================================================================
    // Customer Analytics Export
    // ========================================================================

    public Task<ExportResponse> ExportCustomerAnalyticsAsync(CustomerAnalyticsResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null)
    {
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportCustomerCsv(data, startDate, endDate));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportCustomerExcel(data, startDate, endDate));
        return Task.FromResult(ExportCustomerPdf(data, startDate, endDate, generatedBy));
    }

    private ExportResponse ExportCustomerPdf(CustomerAnalyticsResponse data, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Footer().Element(c => ComposeFooter(c, "Customer Analytics Report"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Customer Analytics Report", startDate, endDate, generatedBy));
                    col.Item().PaddingTop(8);
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Total Customers", data.Counts.TotalCustomers.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "New Customers", data.Counts.NewCustomers.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Returning", data.Counts.ReturningCustomers.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Retention Rate", $"{data.Retention.OverallRetentionRate:N1}%"));
                    });

                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Avg Lifetime Value", $"₱{data.Retention.AverageCustomerLifetimeValue:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Active Customers", data.Counts.ActiveCustomers.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Text("");
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Text("");
                    });

                    // Customer Segments
                    if (data.Segments?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Customer Segments"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Segment").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Customers").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("% of Total").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Avg Spend").FontSize(9).FontColor(Colors.White).Bold();
                            });
                            foreach (var seg in data.Segments)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(seg.SegmentName).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(seg.CustomerCount.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"{seg.Percentage:N1}%").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{seg.AverageSpend:N2}").FontSize(9);
                            }
                        });

                        col.Item().Element(c => HorizontalBarChart(c, "Customers by Segment",
                            data.Segments.Select(s => (s.SegmentName, (decimal)s.CustomerCount)).ToList(),
                            valuePrefix: "", showDecimals: false));
                    }

                    // Top Customers
                    if (data.TopCustomers?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Top Customers"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Customer").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Visits").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Total Spent").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Loyalty Pts").FontSize(9).FontColor(Colors.White).Bold();
                            });
                            foreach (var c in data.TopCustomers)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(c.CustomerName).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(c.TotalVisits.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{c.TotalSpent:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(c.LoyaltyPoints.ToString()).FontSize(9);
                            }
                        });

                        col.Item().Element(c => HorizontalBarChart(c, "Top Customers by Spend",
                            data.TopCustomers.Take(6).Select(tc => (tc.CustomerName, tc.TotalSpent)).ToList()));
                    }
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Customer_Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportCustomerExcel(CustomerAnalyticsResponse data, DateTime startDate, DateTime endDate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Customer Analytics");
        WriteExcelHeader(ws, "Customer Analytics Report", startDate, endDate);

        var row = 5;
        ws.Cell(row, 1).Value = "Total Customers"; ws.Cell(row, 2).Value = data.Counts.TotalCustomers;
        ws.Cell(row + 1, 1).Value = "New Customers"; ws.Cell(row + 1, 2).Value = data.Counts.NewCustomers;
        ws.Cell(row + 2, 1).Value = "Returning Customers"; ws.Cell(row + 2, 2).Value = data.Counts.ReturningCustomers;
        ws.Cell(row + 3, 1).Value = "Retention Rate"; ws.Cell(row + 3, 2).Value = data.Retention.OverallRetentionRate / 100; ws.Cell(row + 3, 2).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row + 4, 1).Value = "Active Customers"; ws.Cell(row + 4, 2).Value = data.Counts.ActiveCustomers;
        ws.Cell(row + 5, 1).Value = "Avg Lifetime Value"; ws.Cell(row + 5, 2).Value = data.Retention.AverageCustomerLifetimeValue; ws.Cell(row + 5, 2).Style.NumberFormat.Format = "₱#,##0.00";

        if (data.TopCustomers?.Any() == true)
        {
            var tcWs = wb.Worksheets.Add("Top Customers");
            WriteExcelHeader(tcWs, "Top Customers", startDate, endDate);
            var tcRow = 5;
            tcWs.Cell(tcRow, 1).Value = "Customer"; tcWs.Cell(tcRow, 2).Value = "Visits"; tcWs.Cell(tcRow, 3).Value = "Total Spent"; tcWs.Cell(tcRow, 4).Value = "Loyalty Points";
            StyleExcelHeaderRow(tcWs, tcRow, 4);
            foreach (var c in data.TopCustomers)
            {
                tcRow++;
                tcWs.Cell(tcRow, 1).Value = c.CustomerName;
                tcWs.Cell(tcRow, 2).Value = c.TotalVisits;
                tcWs.Cell(tcRow, 3).Value = c.TotalSpent; tcWs.Cell(tcRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                tcWs.Cell(tcRow, 4).Value = c.LoyaltyPoints;
            }
            tcWs.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Customer_Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    private ExportResponse ExportCustomerCsv(CustomerAnalyticsResponse data, DateTime startDate, DateTime endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Customer Analytics Report — {CompanyName}");
        sb.AppendLine($"Period: {startDate:MMM dd yyyy} to {endDate:MMM dd yyyy}");
        sb.AppendLine();
        sb.AppendLine($"Total Customers,{data.Counts.TotalCustomers}");
        sb.AppendLine($"New Customers,{data.Counts.NewCustomers}");
        sb.AppendLine($"Returning Customers,{data.Counts.ReturningCustomers}");
        sb.AppendLine($"Retention Rate,{data.Retention.OverallRetentionRate:N1}%");
        sb.AppendLine($"Active Customers,{data.Counts.ActiveCustomers}");
        sb.AppendLine($"Avg Lifetime Value,{data.Retention.AverageCustomerLifetimeValue:N2}");

        if (data.TopCustomers?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine("Customer,Visits,Total Spent,Loyalty Points");
            foreach (var c in data.TopCustomers)
                sb.AppendLine($"\"{c.CustomerName}\",{c.TotalVisits},{c.TotalSpent:N2},{c.LoyaltyPoints}");
        }

        return CsvToResponse(sb, $"Customer_Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    // ========================================================================
    // Inventory Report Export
    // ========================================================================

    public Task<ExportResponse> ExportInventoryReportAsync(InventoryReportResponse data, string format, string? generatedBy = null)
    {
        var now = PhilippineTime.Now;
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportInventoryCsv(data));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportInventoryExcel(data));
        return Task.FromResult(ExportInventoryPdf(data, generatedBy));
    }

    private ExportResponse ExportInventoryPdf(InventoryReportResponse data, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Footer().Element(c => ComposeFooter(c, "Inventory Report"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Inventory Report", generatedBy: generatedBy));
                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Total Products", data.Summary.TotalProducts.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Total Value", $"₱{data.Valuation.TotalCostValue:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Low Stock", data.Summary.LowStockProducts.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Out of Stock", data.Summary.OutOfStockProducts.ToString()));
                    });

                    if (data.Products?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Product Inventory"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Product").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Category").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Stock").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Reorder").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Unit Price").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Value").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            foreach (var p in data.Products)
                            {
                                var bgColor = p.CurrentStock <= 0 ? Colors.Red.Lighten4
                                    : p.CurrentStock <= p.ReorderLevel ? Colors.Orange.Lighten4
                                    : Colors.White;

                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.ProductName).FontSize(9);
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(p.Category).FontSize(9);
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(p.CurrentStock.ToString()).FontSize(9);
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(p.ReorderLevel.ToString()).FontSize(9);
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{p.SellingPrice:N2}").FontSize(9);
                                table.Cell().Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{p.StockValue:N2}").FontSize(9);
                            }
                        });

                        col.Item().Element(c => HorizontalBarChart(c, "Top Products by Stock Value",
                            data.Products.OrderByDescending(p => p.StockValue).Take(6)
                                .Select(p => (p.ProductName, p.StockValue)).ToList()));
                    }
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Inventory_Report_{PhilippineTime.Now:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportInventoryExcel(InventoryReportResponse data)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Inventory");
        WriteExcelHeader(ws, "Inventory Report");

        var row = 5;
        ws.Cell(row, 1).Value = "Product"; ws.Cell(row, 2).Value = "Category"; ws.Cell(row, 3).Value = "Stock";
        ws.Cell(row, 4).Value = "Reorder Lvl"; ws.Cell(row, 5).Value = "Unit Price"; ws.Cell(row, 6).Value = "Total Value";
        StyleExcelHeaderRow(ws, row, 6);

        if (data.Products != null)
        {
            foreach (var p in data.Products)
            {
                row++;
                ws.Cell(row, 1).Value = p.ProductName;
                ws.Cell(row, 2).Value = p.Category;
                ws.Cell(row, 3).Value = p.CurrentStock;
                ws.Cell(row, 4).Value = p.ReorderLevel;
                ws.Cell(row, 5).Value = p.SellingPrice; ws.Cell(row, 5).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(row, 6).Value = p.StockValue; ws.Cell(row, 6).Style.NumberFormat.Format = "₱#,##0.00";
                if (p.CurrentStock <= 0)
                    ws.Row(row).Style.Font.FontColor = XLColor.Red;
                else if (p.CurrentStock <= p.ReorderLevel)
                    ws.Row(row).Style.Font.FontColor = XLColor.OrangePeel;
            }
        }

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Inventory_Report_{PhilippineTime.Now:yyyyMMdd}");
    }

    private ExportResponse ExportInventoryCsv(InventoryReportResponse data)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Inventory Report — {CompanyName}");
        sb.AppendLine($"Generated: {PhilippineTime.Now:MMM dd yyyy hh:mm tt}");
        sb.AppendLine();
        sb.AppendLine("Product,Category,Stock,Reorder Level,Unit Price,Total Value,Status");
        if (data.Products != null)
            foreach (var p in data.Products)
            {
                var status = p.CurrentStock <= 0 ? "Out of Stock" : p.CurrentStock <= p.ReorderLevel ? "Low Stock" : "In Stock";
                sb.AppendLine($"\"{p.ProductName}\",\"{p.Category}\",{p.CurrentStock},{p.ReorderLevel},{p.SellingPrice:N2},{p.StockValue:N2},{status}");
            }

        return CsvToResponse(sb, $"Inventory_Report_{PhilippineTime.Now:yyyyMMdd}");
    }

    // ========================================================================
    // Payroll Report Export
    // ========================================================================

    public Task<ExportResponse> ExportPayrollReportAsync(PayrollSummaryReportResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null)
    {
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportPayrollCsv(data, startDate, endDate));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportPayrollExcel(data, startDate, endDate));
        return Task.FromResult(ExportPayrollPdf(data, startDate, endDate, generatedBy));
    }

    private ExportResponse ExportPayrollPdf(PayrollSummaryReportResponse data, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Footer().Element(c => ComposeFooter(c, "Payroll Summary Report"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Payroll Summary Report", startDate, endDate, generatedBy));
                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Total Employees", data.Totals.TotalEmployeesPaid.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Gross Pay", $"₱{data.Totals.GrossPay:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Total Deductions", $"₱{data.Totals.TotalDeductions:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Net Pay", $"₱{data.Totals.NetPay:N2}"));
                    });

                    if (data.ByDepartment?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "By Department"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Department").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Employees").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Gross Pay").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Net Pay").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Avg Net").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            foreach (var dept in data.ByDepartment)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(dept.DepartmentName).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(dept.EmployeeCount.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.GrossPay:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.NetPay:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{dept.AverageNet:N2}").FontSize(9);
                            }

                            // Totals row
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("TOTAL").FontSize(9).Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(data.Totals.TotalEmployeesPaid.ToString()).FontSize(9).Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₱{data.Totals.GrossPay:N2}").FontSize(9).Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text($"₱{data.Totals.NetPay:N2}").FontSize(9).Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("").FontSize(9);
                        });

                        col.Item().Element(c => HorizontalBarChart(c, "Net Pay by Department",
                            data.ByDepartment.OrderByDescending(d => d.NetPay).Take(6)
                                .Select(d => (d.DepartmentName, d.NetPay)).ToList()));
                    }

                    if (data.ByPeriod?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "By Payroll Period"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Period").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Employees").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Gross Pay").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Deductions").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Net Pay").FontSize(9).FontColor(Colors.White).Bold();
                            });

                            foreach (var period in data.ByPeriod)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(period.PeriodName).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(period.EmployeeCount.ToString()).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{period.GrossPay:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{period.TotalDeductions:N2}").FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{period.NetPay:N2}").FontSize(9);
                            }
                        });
                    }
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Payroll_Summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportPayrollExcel(PayrollSummaryReportResponse data, DateTime startDate, DateTime endDate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Payroll Summary");
        WriteExcelHeader(ws, "Payroll Summary Report", startDate, endDate);

        var row = 5;
        ws.Cell(row, 1).Value = "Department"; ws.Cell(row, 2).Value = "Employees"; ws.Cell(row, 3).Value = "Gross Pay";
        ws.Cell(row, 4).Value = "Net Pay"; ws.Cell(row, 5).Value = "Avg Net Pay";
        StyleExcelHeaderRow(ws, row, 5);

        if (data.ByDepartment != null)
        {
            foreach (var dept in data.ByDepartment)
            {
                row++;
                ws.Cell(row, 1).Value = dept.DepartmentName;
                ws.Cell(row, 2).Value = dept.EmployeeCount;
                ws.Cell(row, 3).Value = dept.GrossPay; ws.Cell(row, 3).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(row, 4).Value = dept.NetPay; ws.Cell(row, 4).Style.NumberFormat.Format = "₱#,##0.00";
                ws.Cell(row, 5).Value = dept.AverageNet; ws.Cell(row, 5).Style.NumberFormat.Format = "₱#,##0.00";
            }
            row++;
            ws.Cell(row, 1).Value = "TOTAL"; ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = data.Totals.TotalEmployeesPaid; ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 3).Value = data.Totals.GrossPay; ws.Cell(row, 3).Style.NumberFormat.Format = "₱#,##0.00"; ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = data.Totals.NetPay; ws.Cell(row, 4).Style.NumberFormat.Format = "₱#,##0.00"; ws.Cell(row, 4).Style.Font.Bold = true;
        }

        if (data.ByPeriod?.Any() == true)
        {
            var pws = wb.Worksheets.Add("By Period");
            WriteExcelHeader(pws, "Payroll By Period", startDate, endDate);
            var pRow = 5;
            pws.Cell(pRow, 1).Value = "Period"; pws.Cell(pRow, 2).Value = "Employees"; pws.Cell(pRow, 3).Value = "Gross Pay";
            pws.Cell(pRow, 4).Value = "Deductions"; pws.Cell(pRow, 5).Value = "Net Pay";
            StyleExcelHeaderRow(pws, pRow, 5);
            foreach (var period in data.ByPeriod)
            {
                pRow++;
                pws.Cell(pRow, 1).Value = period.PeriodName;
                pws.Cell(pRow, 2).Value = period.EmployeeCount;
                pws.Cell(pRow, 3).Value = period.GrossPay; pws.Cell(pRow, 3).Style.NumberFormat.Format = "₱#,##0.00";
                pws.Cell(pRow, 4).Value = period.TotalDeductions; pws.Cell(pRow, 4).Style.NumberFormat.Format = "₱#,##0.00";
                pws.Cell(pRow, 5).Value = period.NetPay; pws.Cell(pRow, 5).Style.NumberFormat.Format = "₱#,##0.00";
            }
            pws.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Payroll_Summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    private ExportResponse ExportPayrollCsv(PayrollSummaryReportResponse data, DateTime startDate, DateTime endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Payroll Summary Report — {CompanyName}");
        sb.AppendLine($"Period: {startDate:MMM dd yyyy} to {endDate:MMM dd yyyy}");
        sb.AppendLine();
        sb.AppendLine("Department,Employees,Gross Pay,Net Pay,Avg Net");
        if (data.ByDepartment != null)
            foreach (var dept in data.ByDepartment)
                sb.AppendLine($"\"{dept.DepartmentName}\",{dept.EmployeeCount},{dept.GrossPay:N2},{dept.NetPay:N2},{dept.AverageNet:N2}");
        sb.AppendLine($"TOTAL,{data.Totals.TotalEmployeesPaid},{data.Totals.GrossPay:N2},{data.Totals.NetPay:N2},");

        if (data.ByPeriod?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine("Period,Employees,Gross Pay,Deductions,Net Pay");
            foreach (var period in data.ByPeriod)
                sb.AppendLine($"\"{period.PeriodName}\",{period.EmployeeCount},{period.GrossPay:N2},{period.TotalDeductions:N2},{period.NetPay:N2}");
        }

        return CsvToResponse(sb, $"Payroll_Summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    // ========================================================================
    // Appointment Analytics Export
    // ========================================================================

    public Task<ExportResponse> ExportAppointmentAnalyticsAsync(AppointmentAnalyticsResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null)
    {
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportAppointmentCsv(data, startDate, endDate));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportAppointmentExcel(data, startDate, endDate));
        return Task.FromResult(ExportAppointmentPdf(data, startDate, endDate, generatedBy));
    }

    private ExportResponse ExportAppointmentPdf(AppointmentAnalyticsResponse data, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Footer().Element(c => ComposeFooter(c, "Appointment Analytics"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Appointment Analytics", startDate, endDate, generatedBy));
                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Total Appointments", data.StatusBreakdown.Total.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Completed", data.StatusBreakdown.Completed.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Cancelled", data.StatusBreakdown.Cancelled.ToString()));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "No-Show", data.StatusBreakdown.NoShow.ToString()));
                    });

                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Completion Rate", $"{data.StatusBreakdown.CompletionRate:N1}%"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Avg Duration", $"{data.TimeAnalytics.AverageAppointmentDuration:N0} min"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Peak Hour", data.TimeAnalytics.MostPopularHour));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Peak Day", data.TimeAnalytics.MostPopularDay));
                    });

                    // Hourly Distribution
                    if (data.TimeAnalytics?.ByHour?.Any() == true)
                    {
                        col.Item().Element(c => SectionTitle(c, "Bookings by Hour"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Hour").FontSize(9).FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Bookings").FontSize(9).FontColor(Colors.White).Bold();
                            });
                            foreach (var h in data.TimeAnalytics.ByHour.OrderByDescending(x => x.Value).Take(12))
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(h.Key).FontSize(9);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text(h.Value.ToString()).FontSize(9);
                            }
                        });

                        col.Item().Element(c => HorizontalBarChart(c, "Bookings by Hour",
                            data.TimeAnalytics.ByHour.OrderByDescending(x => x.Value).Take(6)
                                .Select(h => (h.Key, (decimal)h.Value)).ToList(), valuePrefix: "", showDecimals: false));
                    }
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Appointment_Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportAppointmentExcel(AppointmentAnalyticsResponse data, DateTime startDate, DateTime endDate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Appointment Analytics");
        WriteExcelHeader(ws, "Appointment Analytics", startDate, endDate);

        var row = 5;
        ws.Cell(row, 1).Value = "Total Appointments"; ws.Cell(row, 2).Value = data.StatusBreakdown.Total;
        ws.Cell(row + 1, 1).Value = "Completed"; ws.Cell(row + 1, 2).Value = data.StatusBreakdown.Completed;
        ws.Cell(row + 2, 1).Value = "Cancelled"; ws.Cell(row + 2, 2).Value = data.StatusBreakdown.Cancelled;
        ws.Cell(row + 3, 1).Value = "No-Show"; ws.Cell(row + 3, 2).Value = data.StatusBreakdown.NoShow;
        ws.Cell(row + 4, 1).Value = "Completion Rate"; ws.Cell(row + 4, 2).Value = data.StatusBreakdown.CompletionRate / 100; ws.Cell(row + 4, 2).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row + 5, 1).Value = "Avg Duration (min)"; ws.Cell(row + 5, 2).Value = data.TimeAnalytics.AverageAppointmentDuration;

        if (data.TimeAnalytics?.ByHour?.Any() == true)
        {
            var phWs = wb.Worksheets.Add("Bookings by Hour");
            WriteExcelHeader(phWs, "Bookings by Hour", startDate, endDate);
            var phRow = 5;
            phWs.Cell(phRow, 1).Value = "Hour"; phWs.Cell(phRow, 2).Value = "Bookings";
            StyleExcelHeaderRow(phWs, phRow, 2);
            foreach (var h in data.TimeAnalytics.ByHour.OrderByDescending(x => x.Value))
            {
                phRow++;
                phWs.Cell(phRow, 1).Value = h.Key;
                phWs.Cell(phRow, 2).Value = h.Value;
            }
            phWs.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Appointment_Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    private ExportResponse ExportAppointmentCsv(AppointmentAnalyticsResponse data, DateTime startDate, DateTime endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Appointment Analytics — {CompanyName}");
        sb.AppendLine($"Period: {startDate:MMM dd yyyy} to {endDate:MMM dd yyyy}");
        sb.AppendLine();
        sb.AppendLine($"Total Appointments,{data.StatusBreakdown.Total}");
        sb.AppendLine($"Completed,{data.StatusBreakdown.Completed}");
        sb.AppendLine($"Cancelled,{data.StatusBreakdown.Cancelled}");
        sb.AppendLine($"No-Show,{data.StatusBreakdown.NoShow}");
        sb.AppendLine($"Completion Rate,{data.StatusBreakdown.CompletionRate:N1}%");
        sb.AppendLine($"Avg Duration (min),{data.TimeAnalytics.AverageAppointmentDuration:N0}");

        if (data.TimeAnalytics?.ByHour?.Any() == true)
        {
            sb.AppendLine();
            sb.AppendLine("Hour,Bookings");
            foreach (var h in data.TimeAnalytics.ByHour.OrderByDescending(x => x.Value))
                sb.AppendLine($"{h.Key},{h.Value}");
        }

        return CsvToResponse(sb, $"Appointment_Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    // ========================================================================
    // Financial Summary Export
    // ========================================================================

    public Task<ExportResponse> ExportFinancialSummaryAsync(FinancialSummaryResponse data, DateTime startDate, DateTime endDate, string format, string? generatedBy = null)
    {
        if (format.Equals("CSV", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportFinancialCsv(data, startDate, endDate));
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportFinancialExcel(data, startDate, endDate));
        return Task.FromResult(ExportFinancialPdf(data, startDate, endDate, generatedBy));
    }

    private ExportResponse ExportFinancialPdf(FinancialSummaryResponse data, DateTime startDate, DateTime endDate, string? generatedBy = null)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Footer().Element(c => ComposeFooter(c, "Financial Summary"));

                page.Content().Column(col =>
                {
                    col.Item().Element(c => ComposeHeader(c, "Financial Summary", startDate, endDate, generatedBy));
                    col.Item().PaddingTop(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Net Revenue", $"₱{data.Revenue.NetRevenue:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Total Expenses", $"₱{data.Expenses.TotalExpenses:N2}"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Operating Profit", $"₱{data.Profit.OperatingProfit:N2}",
                            data.Profit.OperatingProfitMargin >= 0 ? $"+{data.Profit.OperatingProfitMargin:N1}% margin" : $"{data.Profit.OperatingProfitMargin:N1}% margin"));
                        row.ConstantItem(8).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Gross Margin", $"{data.Profit.GrossProfitMargin:N1}%"));
                    });

                    // Revenue Breakdown Table
                    col.Item().Element(c => SectionTitle(c, "Revenue Breakdown"));
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(4);
                            cols.RelativeColumn(3);
                        });

                        void AddRow(string label, decimal value, bool bold = false)
                        {
                            var cellLabel = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(label).FontSize(9);
                            var cellValue = table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight().Text($"₱{value:N2}").FontSize(9);
                            if (bold) { cellLabel.Bold(); cellValue.Bold(); }
                        }

                        AddRow("Service Revenue", data.Revenue.ServiceRevenue);
                        AddRow("Product Sales", data.Revenue.ProductSales);
                        AddRow("Package Sales", data.Revenue.PackageSales);
                        AddRow("Tips Received", data.Revenue.TipsReceived);
                        AddRow("Discounts", data.Revenue.Discounts);
                        AddRow("Refunds", data.Revenue.Refunds);
                        AddRow("Net Revenue", data.Revenue.NetRevenue, true);
                        AddRow("Total Expenses", data.Expenses.TotalExpenses);
                        AddRow("Operating Profit", data.Profit.OperatingProfit, true);
                    });

                    // Chart: Revenue Breakdown after table
                    var revenueItems = new List<(string Label, decimal Value)>
                    {
                        ("Service Revenue", data.Revenue.ServiceRevenue),
                        ("Product Sales", data.Revenue.ProductSales),
                        ("Package Sales", data.Revenue.PackageSales),
                        ("Tips Received", data.Revenue.TipsReceived),
                        ("Total Expenses", data.Expenses.TotalExpenses)
                    };
                    revenueItems = revenueItems.Where(x => x.Value > 0).ToList();
                    if (revenueItems.Any())
                        col.Item().Element(c => HorizontalBarChart(c, "Revenue & Expense Breakdown", revenueItems));
                });
            });
        });

        return new ExportResponse
        {
            FileName = $"Financial_Summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf",
            ContentType = "application/pdf",
            FileContent = pdf.GeneratePdf()
        };
    }

    private ExportResponse ExportFinancialExcel(FinancialSummaryResponse data, DateTime startDate, DateTime endDate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Financial Summary");
        WriteExcelHeader(ws, "Financial Summary", startDate, endDate);

        var row = 5;
        ws.Cell(row, 1).Value = "Service Revenue"; ws.Cell(row, 2).Value = data.Revenue.ServiceRevenue; ws.Cell(row, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 1, 1).Value = "Product Sales"; ws.Cell(row + 1, 2).Value = data.Revenue.ProductSales; ws.Cell(row + 1, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 2, 1).Value = "Net Revenue"; ws.Cell(row + 2, 2).Value = data.Revenue.NetRevenue; ws.Cell(row + 2, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 2, 1).Style.Font.Bold = true; ws.Cell(row + 2, 2).Style.Font.Bold = true;
        ws.Cell(row + 3, 1).Value = "Total Expenses"; ws.Cell(row + 3, 2).Value = data.Expenses.TotalExpenses; ws.Cell(row + 3, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 4, 1).Value = "Operating Profit"; ws.Cell(row + 4, 2).Value = data.Profit.OperatingProfit; ws.Cell(row + 4, 2).Style.NumberFormat.Format = "₱#,##0.00";
        ws.Cell(row + 4, 1).Style.Font.Bold = true; ws.Cell(row + 4, 2).Style.Font.Bold = true;
        ws.Cell(row + 5, 1).Value = "Gross Profit Margin"; ws.Cell(row + 5, 2).Value = data.Profit.GrossProfitMargin / 100; ws.Cell(row + 5, 2).Style.NumberFormat.Format = "0.0%";
        ws.Cell(row + 6, 1).Value = "Operating Profit Margin"; ws.Cell(row + 6, 2).Value = data.Profit.OperatingProfitMargin / 100; ws.Cell(row + 6, 2).Style.NumberFormat.Format = "0.0%";

        ws.Columns().AdjustToContents();
        return WorkbookToResponse(wb, $"Financial_Summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    private ExportResponse ExportFinancialCsv(FinancialSummaryResponse data, DateTime startDate, DateTime endDate)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Financial Summary — {CompanyName}");
        sb.AppendLine($"Period: {startDate:MMM dd yyyy} to {endDate:MMM dd yyyy}");
        sb.AppendLine();
        sb.AppendLine($"Service Revenue,{data.Revenue.ServiceRevenue:N2}");
        sb.AppendLine($"Product Sales,{data.Revenue.ProductSales:N2}");
        sb.AppendLine($"Net Revenue,{data.Revenue.NetRevenue:N2}");
        sb.AppendLine($"Total Expenses,{data.Expenses.TotalExpenses:N2}");
        sb.AppendLine($"Operating Profit,{data.Profit.OperatingProfit:N2}");
        sb.AppendLine($"Gross Profit Margin,{data.Profit.GrossProfitMargin:N1}%");
        sb.AppendLine($"Operating Profit Margin,{data.Profit.OperatingProfitMargin:N1}%");

        return CsvToResponse(sb, $"Financial_Summary_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}");
    }

    // ========================================================================
    // Receipt PDF generation
    // ========================================================================

    public byte[] GenerateReceiptPdf(ReceiptData receipt)
    {
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                // Receipt size - narrow like a thermal receipt
                page.Size(new PageSize(226, 600)); // ~80mm width
                page.Margin(10);
                page.DefaultTextStyle(x => x.FontSize(8));

                page.Content().Column(col =>
                {
                    // Logo + Company Header
                    col.Item().AlignCenter().Column(header =>
                    {
                        if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                        {
                            header.Item().AlignCenter().Width(60).Image(_logoPath);
                            header.Item().PaddingTop(3);
                        }
                        header.Item().AlignCenter().Text(CompanyName).FontSize(12).Bold();
                        header.Item().AlignCenter().Text(CompanyAddress).FontSize(7);
                        header.Item().AlignCenter().Text($"Tel: {CompanyPhone}").FontSize(7);
                        header.Item().AlignCenter().Text(CompanyEmail).FontSize(7);
                    });

                    // Divider
                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    // Receipt Info
                    col.Item().Row(r => { r.RelativeItem().Text("Receipt #:").FontSize(8); r.RelativeItem().AlignRight().Text(receipt.TransactionNumber).FontSize(8).Bold(); });
                    col.Item().Row(r => { r.RelativeItem().Text("Date:").FontSize(8); r.RelativeItem().AlignRight().Text(receipt.TransactionDate.ToString("MMM dd, yyyy hh:mm tt")).FontSize(7); });
                    col.Item().Row(r => { r.RelativeItem().Text("Customer:").FontSize(8); r.RelativeItem().AlignRight().Text(receipt.CustomerName).FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("Cashier:").FontSize(8); r.RelativeItem().AlignRight().Text(receipt.CashierName).FontSize(8); });

                    if (!string.IsNullOrEmpty(receipt.AppointmentNumber))
                    {
                        col.Item().Row(r => { r.RelativeItem().Text("Appointment:").FontSize(8); r.RelativeItem().AlignRight().Text(receipt.AppointmentNumber).FontSize(8); });
                    }
                    if (!string.IsNullOrEmpty(receipt.TherapistName))
                    {
                        col.Item().Row(r => { r.RelativeItem().Text("Therapist:").FontSize(8); r.RelativeItem().AlignRight().Text(receipt.TherapistName).FontSize(8); });
                    }

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    // Services
                    if (receipt.Services.Any())
                    {
                        col.Item().Text("SERVICES").FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
                        foreach (var item in receipt.Services)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{item.Name} x{item.Quantity}").FontSize(8);
                                r.ConstantItem(60).AlignRight().Text($"₱{item.TotalPrice:N2}").FontSize(8);
                            });
                        }
                    }

                    // Products
                    if (receipt.Products.Any())
                    {
                        col.Item().PaddingTop(3).Text("PRODUCTS").FontSize(7).Bold().FontColor(Colors.Grey.Darken1);
                        foreach (var item in receipt.Products)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text($"{item.Name} x{item.Quantity}").FontSize(8);
                                r.ConstantItem(60).AlignRight().Text($"₱{item.TotalPrice:N2}").FontSize(8);
                            });
                        }
                    }

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    // Totals
                    col.Item().Row(r => { r.RelativeItem().Text("Subtotal:").FontSize(8); r.ConstantItem(60).AlignRight().Text($"₱{receipt.Subtotal:N2}").FontSize(8); });
                    if (receipt.DiscountAmount > 0)
                    {
                        col.Item().Row(r => { r.RelativeItem().Text("Discount:").FontSize(8); r.ConstantItem(60).AlignRight().Text($"-₱{receipt.DiscountAmount:N2}").FontSize(8).FontColor(Colors.Red.Darken1); });
                    }
                    col.Item().Row(r => { r.RelativeItem().Text("Tax (12%):").FontSize(8); r.ConstantItem(60).AlignRight().Text($"₱{receipt.TaxAmount:N2}").FontSize(8); });

                    col.Item().PaddingVertical(3).LineHorizontal(1).LineColor(Colors.Black);
                    col.Item().Row(r => { r.RelativeItem().Text("TOTAL:").FontSize(10).Bold(); r.ConstantItem(70).AlignRight().Text($"₱{receipt.TotalAmount:N2}").FontSize(10).Bold(); });

                    if (receipt.ClientCurrency != null && receipt.ClientCurrency != "PHP" && receipt.TotalInClientCurrency.HasValue)
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"In {receipt.ClientCurrency}:").FontSize(8);
                            r.ConstantItem(70).AlignRight().Text($"{receipt.CurrencySymbol}{receipt.TotalInClientCurrency:N2}").FontSize(8);
                        });
                    }

                    col.Item().PaddingTop(3).Row(r => { r.RelativeItem().Text("Payment:").FontSize(8); r.ConstantItem(70).AlignRight().Text(receipt.PaymentMethod).FontSize(8); });

                    col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    // Footer
                    col.Item().AlignCenter().Text("Thank you for visiting").FontSize(8);
                    col.Item().AlignCenter().Text($"{CompanyName}!").FontSize(8).Bold();
                    col.Item().AlignCenter().Text("We hope to see you again soon.").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return pdf.GeneratePdf();
    }

    // ========================================================================
    // Excel/CSV Helpers
    // ========================================================================

    private static void WriteExcelHeader(IXLWorksheet ws, string title, DateTime? startDate = null, DateTime? endDate = null)
    {
        ws.Cell(1, 1).Value = CompanyName;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(HeaderBgColor);

        ws.Cell(2, 1).Value = title;
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 12;

        if (startDate.HasValue && endDate.HasValue)
            ws.Cell(3, 1).Value = $"Period: {startDate.Value:MMMM dd, yyyy} — {endDate.Value:MMMM dd, yyyy}";
        else
            ws.Cell(3, 1).Value = $"Generated: {PhilippineTime.Now:MMMM dd, yyyy hh:mm tt}";

        ws.Cell(3, 1).Style.Font.FontColor = XLColor.Gray;
    }

    private static void StyleExcelHeaderRow(IXLWorksheet ws, int row, int colCount)
    {
        for (int c = 1; c <= colCount; c++)
        {
            ws.Cell(row, c).Style.Font.Bold = true;
            ws.Cell(row, c).Style.Font.FontColor = XLColor.White;
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderBgColor);
        }
    }

    private static ExportResponse WorkbookToResponse(XLWorkbook wb, string baseName)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportResponse
        {
            FileName = $"{baseName}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileContent = ms.ToArray()
        };
    }

    private static ExportResponse CsvToResponse(System.Text.StringBuilder sb, string baseName)
    {
        return new ExportResponse
        {
            FileName = $"{baseName}.csv",
            ContentType = "text/csv",
            FileContent = System.Text.Encoding.UTF8.GetBytes(sb.ToString())
        };
    }

    // ========================================================================
    // Customer Segmentation Export
    // ========================================================================

    public Task<ExportResponse> ExportSegmentationAsync(
        List<CustomerSegmentResponse> segments,
        List<(string SegmentName, List<CustomerListResponse> Customers)> segmentCustomers,
        string format,
        ClusteringPerformanceMetrics? metrics = null,
        string? generatedBy = null)
    {
        if (format.Equals("Excel", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(ExportSegmentationExcel(segments, segmentCustomers, metrics));
        return Task.FromResult(ExportSegmentationPdf(segments, segmentCustomers, metrics, generatedBy));
    }

    private ExportResponse ExportSegmentationPdf(
        List<CustomerSegmentResponse> segments,
        List<(string SegmentName, List<CustomerListResponse> Customers)> segmentCustomers,
        ClusteringPerformanceMetrics? metrics,
        string? generatedBy)
    {
        var now = PhilippineTime.Now;
        var pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(9));
                page.Footer().Element(c => ComposeFooter(c, "Customer Segmentation Report"));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Element(c => ComposeHeader(c, "Customer Segmentation Report", now.AddDays(-365), now, generatedBy));
                    col.Item().PaddingTop(8);

                    // KPI row
                    int totalCustomers = segments.Sum(s => s.CustomerCount);
                    int totalSegments = segments.Count;
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => KpiCard(c, "Total Customers", totalCustomers.ToString()));
                        row.ConstantItem(6).Text("");
                        row.RelativeItem().Element(c => KpiCard(c, "Segments Found", totalSegments.ToString()));
                        row.ConstantItem(6).Text("");
                        if (metrics != null)
                        {
                            row.RelativeItem().Element(c => KpiCard(c, "Coverage", $"{metrics.CoveragePercent:N1}%"));
                            row.ConstantItem(6).Text("");
                            row.RelativeItem().Element(c => KpiCard(c, "Model Score", $"{metrics.OverallScore:N0}/100"));
                        }
                        else
                        {
                            row.RelativeItem().Text("");
                            row.ConstantItem(6).Text("");
                            row.RelativeItem().Text("");
                        }
                    });

                    // Performance metrics
                    if (metrics != null)
                    {
                        col.Item().PaddingTop(10).Element(c => SectionTitle(c, "DBSCAN Model Performance"));
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(4);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Metric").FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignCenter().Text("Value").FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Interpretation").FontColor(Colors.White).Bold();
                            });
                            void MetricRow(string name, string value, string interpretation)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(value).Bold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(interpretation).FontColor(Colors.Grey.Medium);
                            }
                            MetricRow("Silhouette Score", $"{metrics.SilhouetteScore:N3}", metrics.SilhouetteScore >= 0.5 ? "Strong structure" : metrics.SilhouetteScore >= 0.25 ? "Reasonable structure" : "Weak structure");
                            MetricRow("Davies-Bouldin Index", $"{metrics.DaviesBouldinIndex:N3}", metrics.DaviesBouldinIndex <= 0.5 ? "Excellent separation" : metrics.DaviesBouldinIndex <= 1.0 ? "Good separation" : "Moderate separation");
                            MetricRow("Avg Intra-Cluster Dist", $"{metrics.AvgIntraClusterDistance:N4}", "Lower = tighter clusters");
                            MetricRow("Avg Inter-Cluster Dist", $"{metrics.AvgInterClusterDistance:N4}", "Higher = better separation");
                            MetricRow("Customer Coverage", $"{metrics.CoveragePercent:N1}%", "Customers assigned to segments");
                            MetricRow("Quality Rating", metrics.QualityRating, $"Overall Score: {metrics.OverallScore:N0}/100");
                        });
                    }

                    // Segment summary table
                    col.Item().PaddingTop(10).Element(c => SectionTitle(c, "Segment Overview"));
                    col.Item().PaddingTop(4).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.ConstantColumn(35);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Background(HeaderBgColor).Padding(5).Text("Segment").FontColor(Colors.White).Bold();
                            header.Cell().Background(HeaderBgColor).Padding(5).AlignCenter().Text("Code").FontColor(Colors.White).Bold();
                            header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Count").FontColor(Colors.White).Bold();
                            header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Avg Recency").FontColor(Colors.White).Bold();
                            header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Avg Freq").FontColor(Colors.White).Bold();
                            header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Avg Spend").FontColor(Colors.White).Bold();
                        });
                        bool odd = true;
                        foreach (var seg in segments)
                        {
                            var bg = odd ? Colors.White : Colors.Grey.Lighten4;
                            odd = !odd;
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(seg.SegmentName).Bold();
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignCenter().Text(seg.SegmentCode).FontColor(Colors.Grey.Medium);
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(seg.CustomerCount.ToString()).Bold();
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(seg.AverageRecency.HasValue ? $"{seg.AverageRecency:N0}d" : "—");
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(seg.AverageFrequency.HasValue ? $"{seg.AverageFrequency:N1}" : "—");
                            table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(seg.AverageMonetaryValue.HasValue ? $"₱{seg.AverageMonetaryValue:N0}" : "—");
                        }
                    });

                    // Bar chart by segment count
                    if (segments.Any())
                        col.Item().Element(c => HorizontalBarChart(c, "Customers per Segment",
                            segments.Select(s => (s.SegmentName, (decimal)s.CustomerCount)).ToList(),
                            valuePrefix: "", showDecimals: false));

                    // Per-segment customer tables
                    foreach (var (segName, customers) in segmentCustomers)
                    {
                        if (!customers.Any()) continue;
                        col.Item().PaddingTop(10).Element(c => SectionTitle(c, $"{segName} — Customer List ({customers.Count})"));
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Customer Name").FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).Text("Phone").FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Total Spent").FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Visits").FontColor(Colors.White).Bold();
                                header.Cell().Background(HeaderBgColor).Padding(5).AlignRight().Text("Last Visit").FontColor(Colors.White).Bold();
                            });
                            bool alt = true;
                            foreach (var c in customers)
                            {
                                var bg = alt ? Colors.White : Colors.Grey.Lighten4;
                                alt = !alt;
                                table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.FullName);
                                table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.PhoneNumber).FontColor(Colors.Grey.Medium);
                                table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text($"₱{c.TotalSpent:N2}");
                                table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(c.TotalVisits.ToString());
                                table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(c.LastVisitDate.HasValue ? c.LastVisitDate.Value.ToString("MMM dd, yyyy") : "Never");
                            }
                        });
                    }
                });
            });
        });

        var bytes = pdf.GeneratePdf();
        return new ExportResponse
        {
            FileName = $"Customer_Segmentation_{now:yyyyMMdd_HHmm}.pdf",
            ContentType = "application/pdf",
            FileContent = bytes
        };
    }

    private ExportResponse ExportSegmentationExcel(
        List<CustomerSegmentResponse> segments,
        List<(string SegmentName, List<CustomerListResponse> Customers)> segmentCustomers,
        ClusteringPerformanceMetrics? metrics)
    {
        var now = PhilippineTime.Now;
        using var wb = new XLWorkbook();

        // ── Summary Sheet ──────────────────────────────────────────────────
        var ws = wb.Worksheets.Add("Segment Summary");
        ws.ShowGridLines = false;

        int row = 1;
        ws.Cell(row, 1).Value = "MiddayMist Spa — Customer Segmentation Report";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#2E7D6F");
        ws.Range(row, 1, row, 7).Merge();
        row++;

        ws.Cell(row, 1).Value = $"Generated: {now:MMMM dd, yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#6B7280");
        ws.Range(row, 1, row, 7).Merge();
        row += 2;

        if (metrics != null)
        {
            ws.Cell(row, 1).Value = "Model Performance";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            row++;
            string[] metricsHdr = { "Silhouette Score", "Davies-Bouldin Index", "Coverage %", "Quality Rating", "Overall Score" };
            for (int i = 0; i < metricsHdr.Length; i++)
            {
                ws.Cell(row, i + 1).Value = metricsHdr[i];
                ws.Cell(row, i + 1).Style.Font.Bold = true;
                ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D6F");
                ws.Cell(row, i + 1).Style.Font.FontColor = XLColor.White;
            }
            row++;
            ws.Cell(row, 1).Value = metrics.SilhouetteScore;
            ws.Cell(row, 2).Value = metrics.DaviesBouldinIndex;
            ws.Cell(row, 3).Value = metrics.CoveragePercent;
            ws.Cell(row, 4).Value = metrics.QualityRating;
            ws.Cell(row, 5).Value = metrics.OverallScore;
            ws.Cell(row, 1).Style.NumberFormat.Format = "0.000";
            ws.Cell(row, 2).Style.NumberFormat.Format = "0.000";
            ws.Cell(row, 3).Style.NumberFormat.Format = "0.0\"%\"";
            ws.Cell(row, 5).Style.NumberFormat.Format = "0";
            row += 2;
        }

        ws.Cell(row, 1).Value = "Segments";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 11;
        row++;

        string[] hdr = { "Segment Name", "Code", "Customers", "Avg Recency (days)", "Avg Frequency", "Avg Spend (₱)", "Recommended Action" };
        for (int i = 0; i < hdr.Length; i++)
        {
            ws.Cell(row, i + 1).Value = hdr[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D6F");
            ws.Cell(row, i + 1).Style.Font.FontColor = XLColor.White;
        }
        row++;

        bool alt = true;
        foreach (var seg in segments)
        {
            var bg = alt ? XLColor.White : XLColor.FromHtml("#F0FDF4");
            alt = !alt;
            ws.Cell(row, 1).Value = seg.SegmentName;
            ws.Cell(row, 2).Value = seg.SegmentCode;
            ws.Cell(row, 3).Value = seg.CustomerCount;
            ws.Cell(row, 4).Value = seg.AverageRecency.HasValue ? (double)seg.AverageRecency.Value : (double?)null;
            ws.Cell(row, 5).Value = seg.AverageFrequency.HasValue ? (double)seg.AverageFrequency.Value : (double?)null;
            ws.Cell(row, 6).Value = seg.AverageMonetaryValue.HasValue ? (double)seg.AverageMonetaryValue.Value : (double?)null;
            ws.Cell(row, 7).Value = seg.RecommendedAction ?? "";
            for (int c = 1; c <= 7; c++)
                ws.Cell(row, c).Style.Fill.BackgroundColor = bg;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(7).Width = 40;

        // ── Per-Segment Sheets ──────────────────────────────────────────────
        foreach (var (segName, customers) in segmentCustomers)
        {
            if (!customers.Any()) continue;
            var sheetName = segName.Length > 31 ? segName.Substring(0, 31) : segName;
            var sheet = wb.Worksheets.Add(sheetName);
            sheet.ShowGridLines = false;

            int r = 1;
            sheet.Cell(r, 1).Value = $"{segName} — Customers";
            sheet.Cell(r, 1).Style.Font.Bold = true;
            sheet.Cell(r, 1).Style.Font.FontSize = 12;
            sheet.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#2E7D6F");
            sheet.Range(r, 1, r, 6).Merge();
            r += 2;

            string[] cols = { "Name", "Phone", "Email", "Total Spent (₱)", "Visits", "Last Visit" };
            for (int i = 0; i < cols.Length; i++)
            {
                sheet.Cell(r, i + 1).Value = cols[i];
                sheet.Cell(r, i + 1).Style.Font.Bold = true;
                sheet.Cell(r, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D6F");
                sheet.Cell(r, i + 1).Style.Font.FontColor = XLColor.White;
            }
            r++;

            bool a = true;
            foreach (var c in customers)
            {
                var bg = a ? XLColor.White : XLColor.FromHtml("#F0FDF4");
                a = !a;
                sheet.Cell(r, 1).Value = c.FullName;
                sheet.Cell(r, 2).Value = c.PhoneNumber;
                sheet.Cell(r, 3).Value = c.Email ?? "";
                sheet.Cell(r, 4).Value = (double)c.TotalSpent;
                sheet.Cell(r, 5).Value = c.TotalVisits;
                sheet.Cell(r, 6).Value = c.LastVisitDate.HasValue ? c.LastVisitDate.Value.ToString("MMM dd, yyyy") : "Never";
                for (int ci = 1; ci <= 6; ci++)
                    sheet.Cell(r, ci).Style.Fill.BackgroundColor = bg;
                sheet.Cell(r, 4).Style.NumberFormat.Format = "#,##0.00";
                r++;
            }
            sheet.Columns().AdjustToContents();
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return new ExportResponse
        {
            FileName = $"Customer_Segmentation_{now:yyyyMMdd_HHmm}.xlsx",
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            FileContent = ms.ToArray()
        };
    }
}
