using System.Text.Json;
using MiddayMistSpa.Web.Models;
using ApiSvc = MiddayMistSpa.API.Services;
using ApiDtos = MiddayMistSpa.API.DTOs.Customer;

namespace MiddayMistSpa.Web.Services;

/// <summary>
/// Production implementation that calls clustering/customer services directly,
/// bypassing the HTTP loopback that doesn't work on MonsterASP.NET hosting.
/// </summary>
public class DirectCustomerSegmentationService : ICustomerSegmentationService
{
    private readonly ApiSvc.IClusteringService _clusteringService;
    private readonly ApiSvc.ICustomerService _customerService;
    private readonly IServiceProvider _serviceProvider;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DirectCustomerSegmentationService(
        ApiSvc.IClusteringService clusteringService,
        ApiSvc.ICustomerService customerService,
        IServiceProvider serviceProvider)
    {
        _clusteringService = clusteringService;
        _customerService = customerService;
        _serviceProvider = serviceProvider;
    }

    public async Task<ClusteringResultResponse?> RunDbscanAnalysisAsync(DbscanParametersRequest parameters)
    {
        try
        {
            var apiParams = ConvertType<ApiSvc.DbscanParametersRequest>(parameters);
            var apiResult = await _clusteringService.RunDbscanAnalysisAsync(apiParams!);
            return ConvertType<ClusteringResultResponse>(apiResult);
        }
        catch (Exception ex)
        {
            return new ClusteringResultResponse { Success = false, Message = $"Analysis failed: {ex.Message}" };
        }
    }

    public async Task<ClusteringStatusResponse?> GetClusteringStatusAsync()
    {
        var apiResult = await _clusteringService.GetClusteringStatusAsync();
        return ConvertType<ClusteringStatusResponse>(apiResult);
    }

    public async Task<List<CustomerSegmentResponse>> GetAllSegmentsAsync()
    {
        var apiResult = await _customerService.GetAllSegmentsAsync();
        return ConvertType<List<CustomerSegmentResponse>>(apiResult) ?? new();
    }

    public async Task<SegmentDetailResponse?> GetSegmentDetailsAsync(int segmentId)
    {
        var apiResult = await _clusteringService.GetSegmentDetailsAsync(segmentId);
        return ConvertType<SegmentDetailResponse>(apiResult);
    }

    public async Task<List<CustomerListItem>> GetCustomersBySegmentAsync(string segmentName)
    {
        var apiResult = await _customerService.GetCustomersBySegmentAsync(segmentName);
        return ConvertType<List<CustomerListItem>>(apiResult) ?? new();
    }

    public async Task<CustomerRfmMetricsResponse?> GetCustomerRfmMetricsAsync(int customerId)
    {
        var apiResult = await _clusteringService.GetCustomerRfmMetricsAsync(customerId);
        return ConvertType<CustomerRfmMetricsResponse>(apiResult);
    }

    public async Task RecalculateSegmentStatsAsync()
    {
        await _clusteringService.RecalculateSegmentStatsAsync();
    }

    public async Task<(byte[]? FileContent, string? FileName, string? ContentType, string? ErrorMessage)> ExportSegmentationAsync(string format, string? segmentName = null)
    {
        try
        {
            var exportService = _serviceProvider.GetRequiredService<ApiSvc.IExportService>();

            var segments = await _customerService.GetAllSegmentsAsync();
            if (!string.IsNullOrWhiteSpace(segmentName))
                segments = segments.Where(s => s.SegmentName.Equals(segmentName, StringComparison.OrdinalIgnoreCase)).ToList();

            var segmentCustomers = new List<(string, List<ApiDtos.CustomerListResponse>)>();
            foreach (var seg in segments)
            {
                var customers = await _customerService.GetCustomersBySegmentAsync(seg.SegmentName);
                segmentCustomers.Add((seg.SegmentName, customers));
            }

            var result = await exportService.ExportSegmentationAsync(segments, segmentCustomers, format, generatedBy: "System");
            return (result.FileContent, result.FileName, result.ContentType, null);
        }
        catch (Exception ex)
        {
            return (null, null, null, ex.Message);
        }
    }

    private static T? ConvertType<T>(object? source)
    {
        if (source == null) return default;
        var json = JsonSerializer.Serialize(source, _jsonOptions);
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }
}
