namespace MiddayMistSpa.Core.Entities.Customer;

/// <summary>
/// DBSCAN cluster definitions with RFM averages and recommended actions
/// </summary>
public class CustomerSegment
{
    public int SegmentId { get; set; }
    public string SegmentName { get; set; } = string.Empty;
    public string SegmentCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ClusterId { get; set; } // DBSCAN cluster ID (-1 for noise/outliers)
    public decimal? AverageRecency { get; set; } // Days since last visit
    public decimal? AverageFrequency { get; set; } // Visits per month
    public decimal? AverageMonetaryValue { get; set; } // Lifetime value
    public int CustomerCount { get; set; } = 0;
    public string? RecommendedAction { get; set; }
    public DateTime? LastAnalysisDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
