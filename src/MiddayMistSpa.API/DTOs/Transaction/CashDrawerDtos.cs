using System.ComponentModel.DataAnnotations;

namespace MiddayMistSpa.API.DTOs.Transaction;

public class CashDrawerSessionResponse
{
    public int SessionId { get; set; }
    public int OpenedByUserId { get; set; }
    public string OpenedByName { get; set; } = string.Empty;
    public int? ClosedByUserId { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal StartingFloat { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal? ActualCash { get; set; }
    public decimal? Discrepancy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class OpenDrawerRequest
{
    [Required]
    [Range(0, 1000000)]
    public decimal StartingFloat { get; set; }
    public string? Notes { get; set; }
}

public class CloseDrawerRequest
{
    [Required]
    [Range(0, 10000000)]
    public decimal ActualCash { get; set; }
    public string? Notes { get; set; }
}
