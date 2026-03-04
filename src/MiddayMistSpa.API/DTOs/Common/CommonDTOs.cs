namespace MiddayMistSpa.API.DTOs.Common;

/// <summary>
/// Generic lookup item for dropdowns
/// </summary>
public class LookupItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public decimal? Price { get; set; }
    public int? Stock { get; set; }
}
