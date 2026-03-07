using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Customer;
using MiddayMistSpa.Core.Entities.Customer;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

/// <summary>
/// DBSCAN (Density-Based Spatial Clustering of Applications with Noise) implementation
/// for customer segmentation using RFM (Recency, Frequency, Monetary) metrics
/// </summary>
public class ClusteringService : IClusteringService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<ClusteringService> _logger;

    // Predefined segment definitions based on RFM characteristics
    private static readonly Dictionary<string, (string Name, string Code, string Description, string Action)> SegmentDefinitions = new()
    {
        { "VIP_PLATINUM", ("VIP Platinum", "VIP", "Top-tier customers with high frequency and spending", "Exclusive rewards and premium services") },
        { "LOYAL_REGULARS", ("Loyal Regulars", "LYL", "Consistent visitors with moderate to high value", "Loyalty bonuses and appreciation events") },
        { "PROMISING", ("Promising", "PRM", "Recent customers showing good potential", "Engagement campaigns and upselling") },
        { "NEW_CUSTOMERS", ("New Customers", "NEW", "First-time or very recent visitors", "Welcome offers and follow-up") },
        { "AT_RISK", ("At-Risk", "RSK", "Previously active customers who haven't visited recently", "Win-back campaigns and special offers") },
        { "HIBERNATING", ("Hibernating", "HIB", "Inactive customers with low recent engagement", "Reactivation campaigns") },
        { "LOST", ("Lost", "LST", "Customers with no recent activity", "Last chance offers") },
        { "NOISE", ("Outliers", "OUT", "Customers that don't fit standard patterns", "Manual review recommended") }
    };

    public ClusteringService(SpaDbContext context, ILogger<ClusteringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ClusteringResultResponse> RunDbscanAnalysisAsync(DbscanParametersRequest parameters)
    {
        _logger.LogInformation("Starting DBSCAN analysis with Epsilon={Epsilon}, MinSamples={MinSamples}",
            parameters.Epsilon, parameters.MinSamples);

        try
        {
            // 1. Get all active customers with their visit/transaction data
            var customers = await _context.Customers
                .Where(c => c.IsActive)
                .Select(c => new CustomerRfmData
                {
                    CustomerId = c.CustomerId,
                    CustomerName = $"{c.FirstName} {c.LastName}",
                    LastVisitDate = c.LastVisitDate,
                    TotalVisits = c.TotalVisits,
                    TotalSpent = c.TotalSpent,
                    FirstVisitDate = c.FirstVisitDate
                })
                .ToListAsync();

            if (customers.Count == 0)
            {
                return new ClusteringResultResponse
                {
                    Success = false,
                    Message = "No customers found to analyze",
                    AnalysisDate = DateTime.UtcNow
                };
            }

            // 2. Calculate RFM metrics for each customer
            var rfmData = CalculateRfmMetrics(customers, parameters);

            // 3. Run DBSCAN clustering (for density context)
            var clusterLabels = RunDbscan(rfmData, parameters);

            // 4. Assign segments per-customer based on individual RFM values
            //    (DBSCAN cluster info is retained but segment mapping uses each customer's own metrics)
            var segments = MapCustomersToIndividualSegments(rfmData, clusterLabels);

            // 5. Update database with results
            await UpdateDatabaseWithResults(rfmData, clusterLabels, segments);

            // 6. Compute performance metrics
            var performanceMetrics = CalculatePerformanceMetrics(rfmData, segments, parameters);

            // 7. Build response
            var result = new ClusteringResultResponse
            {
                Success = true,
                Message = $"Successfully analyzed {customers.Count} customers into {segments.Count} segments",
                TotalCustomersAnalyzed = customers.Count,
                ClustersFound = segments.Count,
                NoisePoints = clusterLabels.Count(l => l == -1),
                AnalysisDate = DateTime.UtcNow,
                Clusters = segments,
                ParametersUsed = parameters,
                PerformanceMetrics = performanceMetrics
            };

            _logger.LogInformation("DBSCAN analysis completed: {Clusters} clusters, {Noise} noise points",
                result.ClustersFound, result.NoisePoints);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during DBSCAN analysis");
            return new ClusteringResultResponse
            {
                Success = false,
                Message = $"Analysis failed: {ex.Message}",
                AnalysisDate = DateTime.UtcNow
            };
        }
    }

    public async Task<ClusteringStatusResponse> GetClusteringStatusAsync()
    {
        var segments = await _context.CustomerSegments
            .OrderByDescending(s => s.CustomerCount)
            .ToListAsync();

        var totalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
        var segmentedCustomers = await _context.Customers
            .CountAsync(c => c.IsActive && c.CustomerSegment != null);

        var lastAnalysis = segments.Any() ? segments.Max(s => s.LastAnalysisDate) : null;

        return new ClusteringStatusResponse
        {
            LastAnalysisDate = lastAnalysis,
            TotalCustomers = totalCustomers,
            SegmentedCustomers = segmentedCustomers,
            UnassignedCustomers = totalCustomers - segmentedCustomers,
            Segments = segments.Select(s => new SegmentStatusItem
            {
                SegmentId = s.SegmentId,
                SegmentName = s.SegmentName,
                CustomerCount = s.CustomerCount,
                Percentage = totalCustomers > 0 ? Math.Round((decimal)s.CustomerCount / totalCustomers * 100, 1) : 0
            }).ToList()
        };
    }

    public async Task<SegmentDetailResponse?> GetSegmentDetailsAsync(int segmentId)
    {
        var segment = await _context.CustomerSegments.FindAsync(segmentId);
        if (segment == null) return null;

        var customersInSegment = await _context.Customers
            .Where(c => c.CustomerSegment == segment.SegmentName && c.IsActive)
            .OrderByDescending(c => c.TotalSpent)
            .Take(10)
            .Select(c => new CustomerListResponse
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = $"{c.FirstName} {c.LastName}",
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                MembershipType = c.MembershipType,
                LoyaltyPoints = c.LoyaltyPoints,
                TotalVisits = c.TotalVisits,
                TotalSpent = c.TotalSpent,
                Allergies = c.Allergies,
                LastVisitDate = c.LastVisitDate,
                CustomerSegment = c.CustomerSegment,
                HasAllergies = !string.IsNullOrEmpty(c.Allergies),
                IsActive = c.IsActive
            })
            .ToListAsync();

        var rfmStats = await _context.Customers
            .Where(c => c.CustomerSegment == segment.SegmentName && c.IsActive)
            .GroupBy(c => 1)
            .Select(g => new
            {
                MinRecency = g.Min(c => c.LastVisitDate.HasValue
                    ? (decimal)(EF.Functions.DateDiffDay(c.LastVisitDate, DateTime.UtcNow) ?? 0)
                    : 999),
                MaxRecency = g.Max(c => c.LastVisitDate.HasValue
                    ? (decimal)(EF.Functions.DateDiffDay(c.LastVisitDate, DateTime.UtcNow) ?? 0)
                    : 999),
                MinFrequency = g.Min(c => (decimal)c.TotalVisits),
                MaxFrequency = g.Max(c => (decimal)c.TotalVisits),
                MinMonetary = g.Min(c => c.TotalSpent),
                MaxMonetary = g.Max(c => c.TotalSpent)
            })
            .FirstOrDefaultAsync();

        return new SegmentDetailResponse
        {
            SegmentId = segment.SegmentId,
            SegmentName = segment.SegmentName,
            SegmentCode = segment.SegmentCode,
            Description = segment.Description,
            ClusterId = segment.ClusterId,
            AverageRecency = segment.AverageRecency,
            AverageFrequency = segment.AverageFrequency,
            AverageMonetaryValue = segment.AverageMonetaryValue,
            CustomerCount = segment.CustomerCount,
            RecommendedAction = segment.RecommendedAction,
            LastAnalysisDate = segment.LastAnalysisDate,
            TopCustomers = customersInSegment,
            RfmDistribution = rfmStats != null ? new RfmDistribution
            {
                MinRecency = rfmStats.MinRecency,
                MaxRecency = rfmStats.MaxRecency,
                MinFrequency = rfmStats.MinFrequency,
                MaxFrequency = rfmStats.MaxFrequency,
                MinMonetary = rfmStats.MinMonetary,
                MaxMonetary = rfmStats.MaxMonetary
            } : new RfmDistribution()
        };
    }

    public async Task<CustomerRfmMetricsResponse?> GetCustomerRfmMetricsAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return null;

        var daysSinceLastVisit = customer.LastVisitDate.HasValue
            ? (int)(DateTime.UtcNow - customer.LastVisitDate.Value).TotalDays
            : 999;

        // Calculate RFM scores (1-5 scale)
        var recencyScore = CalculateRecencyScore(daysSinceLastVisit);
        var frequencyScore = CalculateFrequencyScore(customer.TotalVisits);
        var monetaryScore = CalculateMonetaryScore(customer.TotalSpent);

        return new CustomerRfmMetricsResponse
        {
            CustomerId = customer.CustomerId,
            CustomerName = $"{customer.FirstName} {customer.LastName}",
            CurrentSegment = customer.CustomerSegment,
            DaysSinceLastVisit = daysSinceLastVisit,
            TotalVisits = customer.TotalVisits,
            TotalSpent = customer.TotalSpent,
            RecencyScore = recencyScore,
            FrequencyScore = frequencyScore,
            MonetaryScore = monetaryScore,
            RfmSegment = $"{recencyScore}-{frequencyScore}-{monetaryScore}",
            FirstVisitDate = customer.FirstVisitDate,
            LastVisitDate = customer.LastVisitDate
        };
    }

    public async Task RecalculateSegmentStatsAsync()
    {
        var segments = await _context.CustomerSegments.ToListAsync();

        foreach (var segment in segments)
        {
            var stats = await _context.Customers
                .Where(c => c.CustomerSegment == segment.SegmentName && c.IsActive)
                .GroupBy(c => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    AvgRecency = g.Average(c => c.LastVisitDate.HasValue
                        ? EF.Functions.DateDiffDay(c.LastVisitDate, DateTime.UtcNow)
                        : 999),
                    AvgFrequency = g.Average(c => c.TotalVisits),
                    AvgMonetary = g.Average(c => c.TotalSpent)
                })
                .FirstOrDefaultAsync();

            if (stats != null)
            {
                segment.CustomerCount = stats.Count;
                segment.AverageRecency = (decimal?)stats.AvgRecency;
                segment.AverageFrequency = (decimal?)stats.AvgFrequency;
                segment.AverageMonetaryValue = stats.AvgMonetary;
                segment.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Calculates clustering quality metrics: Silhouette Score, Davies-Bouldin Index, 
    /// Intra/Inter-cluster distances, and an overall quality score.
    /// </summary>
    private ClusteringPerformanceMetrics CalculatePerformanceMetrics(
        List<CustomerRfmData> data, List<ClusterSummary> segments, DbscanParametersRequest parameters)
    {
        var segmentGroups = data
            .Where(d => d.AssignedSegmentKey != "NOISE")
            .GroupBy(d => d.AssignedSegmentKey)
            .Where(g => g.Count() > 1)
            .ToList();

        int totalAssigned = data.Count(d => d.AssignedSegmentKey != "NOISE");
        double coveragePct = data.Count > 0 ? (double)totalAssigned / data.Count * 100.0 : 0;

        if (segmentGroups.Count < 2)
        {
            return new ClusteringPerformanceMetrics
            {
                SilhouetteScore = 0,
                AvgIntraClusterDistance = 0,
                AvgInterClusterDistance = 0,
                DaviesBouldinIndex = 0,
                CoveragePercent = coveragePct,
                QualityRating = segmentGroups.Count == 1 ? "Fair" : "N/A",
                OverallScore = segmentGroups.Count == 1 ? 50 : 0
            };
        }

        // Compute centroids per segment
        var centroids = segmentGroups.ToDictionary(
            g => g.Key,
            g => (
                R: g.Average(c => c.NormalizedRecency),
                F: g.Average(c => c.NormalizedFrequency),
                M: g.Average(c => c.NormalizedMonetary)
            ));

        // --- Silhouette Score ---
        double silhouetteSum = 0;
        int silhouetteCount = 0;

        foreach (var point in data.Where(d => d.AssignedSegmentKey != "NOISE"))
        {
            var ownGroup = segmentGroups.FirstOrDefault(g => g.Key == point.AssignedSegmentKey);
            if (ownGroup == null || ownGroup.Count() <= 1) continue;

            // a(i) = avg distance to other points in same cluster
            double a = ownGroup
                .Where(p => p.CustomerId != point.CustomerId)
                .Average(p => NormDistance(point, p, parameters));

            // b(i) = min avg distance to points in any other cluster
            double b = double.MaxValue;
            foreach (var otherGroup in segmentGroups.Where(g => g.Key != point.AssignedSegmentKey))
            {
                double avgDist = otherGroup.Average(p => NormDistance(point, p, parameters));
                if (avgDist < b) b = avgDist;
            }

            double s = Math.Max(a, b) > 0 ? (b - a) / Math.Max(a, b) : 0;
            silhouetteSum += s;
            silhouetteCount++;
        }

        double silhouetteScore = silhouetteCount > 0 ? silhouetteSum / silhouetteCount : 0;

        // --- Avg intra-cluster distance ---
        double totalIntra = 0;
        int intraCount = 0;
        foreach (var group in segmentGroups)
        {
            var members = group.ToList();
            var centroid = centroids[group.Key];
            foreach (var m in members)
            {
                totalIntra += CentroidDistance(m, centroid, parameters);
                intraCount++;
            }
        }
        double avgIntra = intraCount > 0 ? totalIntra / intraCount : 0;

        // --- Avg inter-cluster distance (between centroids) ---
        double totalInter = 0;
        int interCount = 0;
        var centroidKeys = centroids.Keys.ToList();
        for (int i = 0; i < centroidKeys.Count; i++)
        {
            for (int j = i + 1; j < centroidKeys.Count; j++)
            {
                var c1 = centroids[centroidKeys[i]];
                var c2 = centroids[centroidKeys[j]];
                double d = Math.Sqrt(
                    Math.Pow((c1.R - c2.R) * parameters.RecencyWeight, 2) +
                    Math.Pow((c1.F - c2.F) * parameters.FrequencyWeight, 2) +
                    Math.Pow((c1.M - c2.M) * parameters.MonetaryWeight, 2));
                totalInter += d;
                interCount++;
            }
        }
        double avgInter = interCount > 0 ? totalInter / interCount : 0;

        // --- Davies-Bouldin Index ---
        double dbiSum = 0;
        foreach (var gi in segmentGroups)
        {
            var ci = centroids[gi.Key];
            double si = gi.Average(p => CentroidDistance(p, ci, parameters)); // scatter of cluster i

            double maxRatio = 0;
            foreach (var gj in segmentGroups.Where(g => g.Key != gi.Key))
            {
                var cj = centroids[gj.Key];
                double sj = gj.Average(p => CentroidDistance(p, cj, parameters)); // scatter of cluster j
                double dij = Math.Sqrt(
                    Math.Pow((ci.R - cj.R) * parameters.RecencyWeight, 2) +
                    Math.Pow((ci.F - cj.F) * parameters.FrequencyWeight, 2) +
                    Math.Pow((ci.M - cj.M) * parameters.MonetaryWeight, 2));
                double ratio = dij > 0 ? (si + sj) / dij : 0;
                if (ratio > maxRatio) maxRatio = ratio;
            }
            dbiSum += maxRatio;
        }
        double daviesBouldin = segmentGroups.Count > 0 ? dbiSum / segmentGroups.Count : 0;

        // --- Overall Score (0-100) ---
        // Silhouette contributes 40% (map -1..1 to 0..100)
        double silhouetteComponent = Math.Clamp((silhouetteScore + 1) / 2.0 * 100, 0, 100) * 0.40;
        // DBI contributes 30% (inverse, lower=better, map 0..3 to 100..0)
        double dbiComponent = Math.Clamp((1 - daviesBouldin / 3.0) * 100, 0, 100) * 0.30;
        // Coverage contributes 20%
        double coverageComponent = coveragePct * 0.20;
        // Number of segments contributes 10% (ideal ~5-8 segments)
        double segCountScore = segmentGroups.Count >= 3 && segmentGroups.Count <= 10 ? 100 :
                               segmentGroups.Count >= 2 ? 60 : 20;
        double segComponent = segCountScore * 0.10;

        double overallScore = Math.Round(silhouetteComponent + dbiComponent + coverageComponent + segComponent, 1);

        string qualityRating = overallScore switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            _ => "Poor"
        };

        return new ClusteringPerformanceMetrics
        {
            SilhouetteScore = Math.Round(silhouetteScore, 4),
            AvgIntraClusterDistance = Math.Round(avgIntra, 4),
            AvgInterClusterDistance = Math.Round(avgInter, 4),
            DaviesBouldinIndex = Math.Round(daviesBouldin, 4),
            CoveragePercent = Math.Round(coveragePct, 1),
            QualityRating = qualityRating,
            OverallScore = overallScore
        };
    }

    private double NormDistance(CustomerRfmData a, CustomerRfmData b, DbscanParametersRequest p)
    {
        return Math.Sqrt(
            Math.Pow((a.NormalizedRecency - b.NormalizedRecency) * p.RecencyWeight, 2) +
            Math.Pow((a.NormalizedFrequency - b.NormalizedFrequency) * p.FrequencyWeight, 2) +
            Math.Pow((a.NormalizedMonetary - b.NormalizedMonetary) * p.MonetaryWeight, 2));
    }

    private double CentroidDistance(CustomerRfmData point, (double R, double F, double M) centroid, DbscanParametersRequest p)
    {
        return Math.Sqrt(
            Math.Pow((point.NormalizedRecency - centroid.R) * p.RecencyWeight, 2) +
            Math.Pow((point.NormalizedFrequency - centroid.F) * p.FrequencyWeight, 2) +
            Math.Pow((point.NormalizedMonetary - centroid.M) * p.MonetaryWeight, 2));
    }

    #region Private DBSCAN Implementation

    private class CustomerRfmData
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime? LastVisitDate { get; set; }
        public int TotalVisits { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? FirstVisitDate { get; set; }

        // Calculated RFM values
        public double Recency { get; set; }  // Days since last visit
        public double Frequency { get; set; }  // Visits per month
        public double Monetary { get; set; }  // Total spent

        // Normalized values (0-1)
        public double NormalizedRecency { get; set; }
        public double NormalizedFrequency { get; set; }
        public double NormalizedMonetary { get; set; }

        // Cluster assignment
        public int ClusterLabel { get; set; } = -1;
        public string AssignedSegmentKey { get; set; } = "NOISE";
    }

    private List<CustomerRfmData> CalculateRfmMetrics(List<CustomerRfmData> customers, DbscanParametersRequest parameters)
    {
        var now = DateTime.UtcNow;

        // Calculate raw RFM values
        foreach (var customer in customers)
        {
            customer.Recency = customer.LastVisitDate.HasValue
                ? (now - customer.LastVisitDate.Value).TotalDays
                : 365; // Default to 1 year for never visited

            // Calculate frequency as visits per month since first visit
            if (customer.FirstVisitDate.HasValue && customer.TotalVisits > 0)
            {
                var monthsSinceFirst = Math.Max(1, (now - customer.FirstVisitDate.Value).TotalDays / 30.0);
                customer.Frequency = customer.TotalVisits / monthsSinceFirst;
            }
            else
            {
                customer.Frequency = customer.TotalVisits > 0 ? 1 : 0;
            }

            customer.Monetary = (double)customer.TotalSpent;
        }

        if (parameters.NormalizeData && customers.Count > 0)
        {
            // Normalize to 0-1 range
            var maxRecency = customers.Max(c => c.Recency);
            var maxFrequency = customers.Max(c => c.Frequency);
            var maxMonetary = customers.Max(c => c.Monetary);

            foreach (var customer in customers)
            {
                // Invert recency (lower is better)
                customer.NormalizedRecency = maxRecency > 0 ? 1 - (customer.Recency / maxRecency) : 1;
                customer.NormalizedFrequency = maxFrequency > 0 ? customer.Frequency / maxFrequency : 0;
                customer.NormalizedMonetary = maxMonetary > 0 ? customer.Monetary / maxMonetary : 0;
            }
        }

        return customers;
    }

    private int[] RunDbscan(List<CustomerRfmData> data, DbscanParametersRequest parameters)
    {
        var n = data.Count;
        var labels = new int[n];
        Array.Fill(labels, -2); // -2 = unvisited, -1 = noise, >= 0 = cluster

        var currentCluster = 0;

        for (int i = 0; i < n; i++)
        {
            if (labels[i] != -2) continue; // Already visited

            var neighbors = GetNeighbors(data, i, parameters);

            if (neighbors.Count < parameters.MinSamples)
            {
                labels[i] = -1; // Mark as noise
            }
            else
            {
                // Expand cluster
                labels[i] = currentCluster;
                ExpandCluster(data, labels, i, neighbors, currentCluster, parameters);
                currentCluster++;
            }
        }

        // Copy labels back to data
        for (int i = 0; i < n; i++)
        {
            data[i].ClusterLabel = labels[i];
        }

        return labels;
    }

    private List<int> GetNeighbors(List<CustomerRfmData> data, int pointIndex, DbscanParametersRequest parameters)
    {
        var neighbors = new List<int>();
        var point = data[pointIndex];

        for (int i = 0; i < data.Count; i++)
        {
            if (i == pointIndex) continue;

            var distance = CalculateDistance(point, data[i], parameters);
            if (distance <= parameters.Epsilon)
            {
                neighbors.Add(i);
            }
        }

        return neighbors;
    }

    private double CalculateDistance(CustomerRfmData a, CustomerRfmData b, DbscanParametersRequest parameters)
    {
        // Weighted Euclidean distance in normalized RFM space
        var recencyDiff = (a.NormalizedRecency - b.NormalizedRecency) * parameters.RecencyWeight;
        var frequencyDiff = (a.NormalizedFrequency - b.NormalizedFrequency) * parameters.FrequencyWeight;
        var monetaryDiff = (a.NormalizedMonetary - b.NormalizedMonetary) * parameters.MonetaryWeight;

        return Math.Sqrt(recencyDiff * recencyDiff + frequencyDiff * frequencyDiff + monetaryDiff * monetaryDiff);
    }

    private void ExpandCluster(List<CustomerRfmData> data, int[] labels, int pointIndex,
        List<int> neighbors, int clusterId, DbscanParametersRequest parameters)
    {
        var seeds = new Queue<int>(neighbors);

        while (seeds.Count > 0)
        {
            var current = seeds.Dequeue();

            if (labels[current] == -1)
            {
                labels[current] = clusterId; // Change noise to border point
            }

            if (labels[current] != -2) continue; // Already processed

            labels[current] = clusterId;

            var currentNeighbors = GetNeighbors(data, current, parameters);
            if (currentNeighbors.Count >= parameters.MinSamples)
            {
                foreach (var neighbor in currentNeighbors)
                {
                    if (labels[neighbor] == -2 || labels[neighbor] == -1)
                    {
                        seeds.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Assigns each customer to a business segment based on their INDIVIDUAL RFM metrics,
    /// then aggregates into segment summaries. This ensures proper distribution even if
    /// DBSCAN produces few density clusters.
    /// </summary>
    private List<ClusterSummary> MapCustomersToIndividualSegments(List<CustomerRfmData> data, int[] labels)
    {
        // Assign each customer their own segment based on individual RFM values
        var customerSegments = new Dictionary<int, string>(); // customerId -> segmentKey
        foreach (var customer in data)
        {
            var segmentKey = DetermineSegmentKeyForCustomer(
                customer.Recency, customer.Frequency, customer.Monetary, customer.ClusterLabel);
            customerSegments[customer.CustomerId] = segmentKey;
            // Store on the data object for later DB update
            customer.AssignedSegmentKey = segmentKey;
        }

        // Group by segment and build summaries
        var segmentGroups = data.GroupBy(d => d.AssignedSegmentKey).OrderBy(g => GetSegmentSortOrder(g.Key));
        var summaries = new List<ClusterSummary>();
        int syntheticClusterId = 0;

        foreach (var group in segmentGroups)
        {
            var segmentDef = SegmentDefinitions[group.Key];
            var customers = group.ToList();

            summaries.Add(new ClusterSummary
            {
                ClusterId = syntheticClusterId,
                SegmentName = segmentDef.Name,
                SegmentCode = segmentDef.Code,
                CustomerCount = customers.Count,
                AverageRecency = (decimal)customers.Average(c => c.Recency),
                AverageFrequency = (decimal)customers.Average(c => c.Frequency),
                AverageMonetaryValue = (decimal)customers.Average(c => c.Monetary),
                RecommendedAction = segmentDef.Action
            });

            // Update each customer's cluster label to match the synthetic segment ID
            foreach (var c in customers)
                c.ClusterLabel = syntheticClusterId;

            syntheticClusterId++;
        }

        return summaries;
    }

    private static int GetSegmentSortOrder(string key) => key switch
    {
        "VIP_PLATINUM" => 0,
        "LOYAL_REGULARS" => 1,
        "PROMISING" => 2,
        "NEW_CUSTOMERS" => 3,
        "AT_RISK" => 4,
        "HIBERNATING" => 5,
        "LOST" => 6,
        "NOISE" => 7,
        _ => 99
    };

    /// <summary>
    /// Determines the business segment for a single customer based on their individual RFM values.
    /// </summary>
    private string DetermineSegmentKeyForCustomer(double recency, double frequency, double monetary, int clusterId)
    {
        // Recency thresholds (days since last visit)
        bool isRecent = recency < 30;
        bool isMediumRecent = recency >= 30 && recency < 90;
        bool isOld = recency >= 90;

        // Frequency thresholds (visits per month)
        bool isHighFreq = frequency > 2;
        bool isMediumFreq = frequency >= 0.5 && frequency <= 2;
        bool isLowFreq = frequency < 0.5;

        // Monetary thresholds (total spend in PHP)
        bool isHighValue = monetary > 10000;
        bool isMediumValue = monetary >= 3000 && monetary <= 10000;
        bool isLowValue = monetary < 3000;

        // VIP Platinum: Recent + High Frequency + High Value
        if (isRecent && isHighFreq && isHighValue) return "VIP_PLATINUM";

        // Loyal Regulars: Recent/Medium Recent + Medium-High frequency + High value
        if ((isRecent || isMediumRecent) && (isHighFreq || isMediumFreq) && isHighValue)
            return "LOYAL_REGULARS";

        // New Customers: Recent + Low Frequency (must be checked BEFORE Promising)
        if (isRecent && isLowFreq) return "NEW_CUSTOMERS";

        // Promising: Recent + Medium Frequency + Medium Value (growing customers)
        if (isRecent && isMediumFreq && isMediumValue) return "PROMISING";

        // Promising fallback: Recent + Medium Value + any freq
        if (isRecent && isMediumValue) return "PROMISING";

        // At-Risk: Medium Recency + Previously Active
        if (isMediumRecent && (isHighFreq || isMediumFreq)) return "AT_RISK";

        // At-Risk fallback: Medium Recency + Low Frequency but some value
        if (isMediumRecent && isLowFreq && !isLowValue) return "AT_RISK";

        // Hibernating: Old + Previously Active (had decent frequency/value)
        if (isOld && (isHighFreq || isMediumFreq)) return "HIBERNATING";
        if (isOld && isHighValue) return "HIBERNATING";

        // Lost: Old + Low Frequency + Low Value
        if (isOld && isLowFreq && isLowValue) return "LOST";

        // Lost fallback: Old + Low Frequency
        if (isOld && isLowFreq) return "LOST";

        // Medium Recency + Low everything → At-Risk
        if (isMediumRecent) return "AT_RISK";

        return "PROMISING"; // Default fallback
    }

    private async Task UpdateDatabaseWithResults(List<CustomerRfmData> data, int[] labels, List<ClusterSummary> segments)
    {
        var now = DateTime.UtcNow;

        // Update or create segment records
        foreach (var summary in segments)
        {
            var existingSegment = await _context.CustomerSegments
                .FirstOrDefaultAsync(s => s.ClusterId == summary.ClusterId);

            if (existingSegment != null)
            {
                existingSegment.SegmentName = summary.SegmentName;
                existingSegment.SegmentCode = summary.SegmentCode;
                existingSegment.CustomerCount = summary.CustomerCount;
                existingSegment.AverageRecency = summary.AverageRecency;
                existingSegment.AverageFrequency = summary.AverageFrequency;
                existingSegment.AverageMonetaryValue = summary.AverageMonetaryValue;
                existingSegment.RecommendedAction = summary.RecommendedAction;
                existingSegment.LastAnalysisDate = now;
                existingSegment.UpdatedAt = now;
            }
            else
            {
                var newSegment = new CustomerSegment
                {
                    SegmentName = summary.SegmentName,
                    SegmentCode = summary.SegmentCode,
                    Description = SegmentDefinitions.Values
                        .FirstOrDefault(d => d.Name == summary.SegmentName).Description,
                    ClusterId = summary.ClusterId,
                    CustomerCount = summary.CustomerCount,
                    AverageRecency = summary.AverageRecency,
                    AverageFrequency = summary.AverageFrequency,
                    AverageMonetaryValue = summary.AverageMonetaryValue,
                    RecommendedAction = summary.RecommendedAction,
                    LastAnalysisDate = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.CustomerSegments.Add(newSegment);
            }
        }

        // Update customer segment assignments using individual segment mapping
        var customerIds = data.Select(d => d.CustomerId).ToList();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.CustomerId))
            .ToListAsync();

        foreach (var customer in customers)
        {
            var rfmData = data.FirstOrDefault(d => d.CustomerId == customer.CustomerId);
            if (rfmData != null)
            {
                // Use the per-customer assigned segment (from AssignedSegmentKey)
                var segment = segments.FirstOrDefault(s => s.ClusterId == rfmData.ClusterLabel);
                if (segment != null)
                {
                    customer.CustomerSegment = segment.SegmentName;
                    customer.SegmentAssignedDate = now;
                    customer.UpdatedAt = now;
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    private static decimal CalculateRecencyScore(int daysSinceLastVisit)
    {
        return daysSinceLastVisit switch
        {
            <= 7 => 5,
            <= 14 => 4,
            <= 30 => 3,
            <= 60 => 2,
            _ => 1
        };
    }

    private static decimal CalculateFrequencyScore(int totalVisits)
    {
        return totalVisits switch
        {
            >= 20 => 5,
            >= 10 => 4,
            >= 5 => 3,
            >= 2 => 2,
            _ => 1
        };
    }

    private static decimal CalculateMonetaryScore(decimal totalSpent)
    {
        return totalSpent switch
        {
            >= 50000 => 5,
            >= 20000 => 4,
            >= 10000 => 3,
            >= 5000 => 2,
            _ => 1
        };
    }

    #endregion
}
