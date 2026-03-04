using MiddayMistSpa.API.DTOs.Customer;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// DBSCAN Customer Segmentation Service for RFM-based clustering
/// </summary>
public interface IClusteringService
{
    /// <summary>
    /// Run DBSCAN clustering analysis on all customers using RFM metrics
    /// </summary>
    Task<ClusteringResultResponse> RunDbscanAnalysisAsync(DbscanParametersRequest parameters);

    /// <summary>
    /// Get the current clustering status/summary
    /// </summary>
    Task<ClusteringStatusResponse> GetClusteringStatusAsync();

    /// <summary>
    /// Get detailed metrics for a specific segment
    /// </summary>
    Task<SegmentDetailResponse?> GetSegmentDetailsAsync(int segmentId);

    /// <summary>
    /// Get RFM metrics for a specific customer
    /// </summary>
    Task<CustomerRfmMetricsResponse?> GetCustomerRfmMetricsAsync(int customerId);

    /// <summary>
    /// Recalculate and update segment statistics
    /// </summary>
    Task RecalculateSegmentStatsAsync();
}

#region Clustering DTOs

public record DbscanParametersRequest
{
    /// <summary>
    /// Epsilon - Maximum distance between two samples for one to be considered in neighborhood
    /// </summary>
    public double Epsilon { get; init; } = 0.5;

    /// <summary>
    /// Minimum samples - Number of samples in a neighborhood for a point to be a core point
    /// </summary>
    public int MinSamples { get; init; } = 3;

    /// <summary>
    /// Whether to use normalized RFM values
    /// </summary>
    public bool NormalizeData { get; init; } = true;

    /// <summary>
    /// Weight for Recency in clustering (days since last visit - lower is better)
    /// </summary>
    public double RecencyWeight { get; init; } = 1.0;

    /// <summary>
    /// Weight for Frequency in clustering (visits per month - higher is better)
    /// </summary>
    public double FrequencyWeight { get; init; } = 1.0;

    /// <summary>
    /// Weight for Monetary in clustering (total spent - higher is better)
    /// </summary>
    public double MonetaryWeight { get; init; } = 1.0;
}

public record ClusteringResultResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int TotalCustomersAnalyzed { get; init; }
    public int ClustersFound { get; init; }
    public int NoisePoints { get; init; }
    public DateTime AnalysisDate { get; init; }
    public List<ClusterSummary> Clusters { get; init; } = new();
    public DbscanParametersRequest ParametersUsed { get; init; } = new();
    public ClusteringPerformanceMetrics? PerformanceMetrics { get; init; }
}

public record ClusteringPerformanceMetrics
{
    /// <summary>Silhouette Score: -1 (bad) to 1 (excellent). Measures how well each point fits its cluster vs neighboring clusters.</summary>
    public double SilhouetteScore { get; init; }
    /// <summary>Average distance between points within the same cluster (lower = tighter clusters).</summary>
    public double AvgIntraClusterDistance { get; init; }
    /// <summary>Average distance between cluster centroids (higher = better separation).</summary>
    public double AvgInterClusterDistance { get; init; }
    /// <summary>Davies-Bouldin Index: lower is better. Measures ratio of within-cluster scatter to between-cluster separation.</summary>
    public double DaviesBouldinIndex { get; init; }
    /// <summary>Percentage of customers assigned to a segment (not noise).</summary>
    public double CoveragePercent { get; init; }
    /// <summary>Overall quality rating: Excellent, Good, Fair, Poor.</summary>
    public string QualityRating { get; init; } = string.Empty;
    /// <summary>Overall score 0-100 combining all metrics.</summary>
    public double OverallScore { get; init; }
}

public record ClusterSummary
{
    public int ClusterId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public string SegmentCode { get; init; } = string.Empty;
    public int CustomerCount { get; init; }
    public decimal AverageRecency { get; init; }
    public decimal AverageFrequency { get; init; }
    public decimal AverageMonetaryValue { get; init; }
    public string RecommendedAction { get; init; } = string.Empty;
}

public record ClusteringStatusResponse
{
    public DateTime? LastAnalysisDate { get; init; }
    public int TotalCustomers { get; init; }
    public int SegmentedCustomers { get; init; }
    public int UnassignedCustomers { get; init; }
    public List<SegmentStatusItem> Segments { get; init; } = new();
}

public record SegmentStatusItem
{
    public int SegmentId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public int CustomerCount { get; init; }
    public decimal Percentage { get; init; }
}

public record SegmentDetailResponse
{
    public int SegmentId { get; init; }
    public string SegmentName { get; init; } = string.Empty;
    public string SegmentCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int ClusterId { get; init; }
    public decimal? AverageRecency { get; init; }
    public decimal? AverageFrequency { get; init; }
    public decimal? AverageMonetaryValue { get; init; }
    public int CustomerCount { get; init; }
    public string? RecommendedAction { get; init; }
    public DateTime? LastAnalysisDate { get; init; }
    public List<CustomerListResponse> TopCustomers { get; init; } = new();
    public RfmDistribution RfmDistribution { get; init; } = new();
}

public record RfmDistribution
{
    public decimal MinRecency { get; init; }
    public decimal MaxRecency { get; init; }
    public decimal MinFrequency { get; init; }
    public decimal MaxFrequency { get; init; }
    public decimal MinMonetary { get; init; }
    public decimal MaxMonetary { get; init; }
}

public record CustomerRfmMetricsResponse
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CurrentSegment { get; init; }

    // Raw RFM values
    public int DaysSinceLastVisit { get; init; }
    public int TotalVisits { get; init; }
    public decimal TotalSpent { get; init; }

    // Calculated metrics
    public decimal RecencyScore { get; init; }  // 1-5 score
    public decimal FrequencyScore { get; init; }  // 1-5 score  
    public decimal MonetaryScore { get; init; }  // 1-5 score
    public string RfmSegment { get; init; } = string.Empty;  // e.g., "5-5-5" for best customers

    public DateTime? FirstVisitDate { get; init; }
    public DateTime? LastVisitDate { get; init; }
}

#endregion
