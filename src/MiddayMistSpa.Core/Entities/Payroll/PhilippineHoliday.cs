namespace MiddayMistSpa.Core.Entities.Payroll;

/// <summary>
/// Philippine holidays (Regular and Special Non-Working days)
/// </summary>
public class PhilippineHoliday
{
    public int HolidayId { get; set; }
    public string HolidayName { get; set; } = string.Empty;
    public DateTime HolidayDate { get; set; }
    public string HolidayType { get; set; } = string.Empty; // Regular, Special Non-Working
    public int Year { get; set; }
    public bool IsRecurring { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
