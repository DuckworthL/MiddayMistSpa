using MiddayMistSpa.API.DTOs.Employee;

namespace MiddayMistSpa.API.DTOs.Service;

#region Category DTOs

public record CreateCategoryRequest
{
    public string CategoryName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
}

public record UpdateCategoryRequest
{
    public string CategoryName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public bool IsActive { get; init; }
}

public record CategoryResponse
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public int ServiceCount { get; init; }
    public bool IsActive { get; init; }
}

#endregion

#region Service DTOs

public record CreateServiceRequest
{
    public int CategoryId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal RegularPrice { get; init; }
    public decimal? MemberPrice { get; init; }
    public decimal? PromoPrice { get; init; }
    public decimal TherapistCommissionRate { get; init; }
}

public record UpdateServiceRequest
{
    public int CategoryId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal RegularPrice { get; init; }
    public decimal? MemberPrice { get; init; }
    public decimal? PromoPrice { get; init; }
    public decimal TherapistCommissionRate { get; init; }
    public bool IsActive { get; init; }
}

public record ServiceResponse
{
    public int ServiceId { get; init; }
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public decimal RegularPrice { get; init; }
    public decimal? MemberPrice { get; init; }
    public decimal? PromoPrice { get; init; }
    public decimal TherapistCommissionRate { get; init; }
    public bool IsActive { get; init; }
    public List<ProductRequirementResponse> ProductRequirements { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ServiceListResponse
{
    public int ServiceId { get; init; }
    public string ServiceCode { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
    public int? CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int DurationMinutes { get; init; }
    public decimal RegularPrice { get; init; }
    public decimal? MemberPrice { get; init; }
    public decimal? PromoPrice { get; init; }
    public bool IsActive { get; init; }
}

public record ServiceMenuResponse
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? CategoryDescription { get; init; }
    public List<ServiceListResponse> Services { get; init; } = new();
}

#endregion

#region Product Requirement DTOs

public record AddProductRequirementRequest
{
    public int ProductId { get; init; }
    public decimal QuantityRequired { get; init; }
}

public record ProductRequirementResponse
{
    public int RequirementId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductCode { get; init; }
    public decimal QuantityRequired { get; init; }
    public string Unit { get; init; } = string.Empty;
}

#endregion

#region Pricing DTOs

public record UpdatePricingRequest
{
    public decimal RegularPrice { get; init; }
    public decimal? MemberPrice { get; init; }
    public decimal? PromoPrice { get; init; }
}

public record BulkPriceAdjustmentRequest
{
    public int? CategoryId { get; init; } // If null, apply to all
    public decimal PercentageChange { get; init; } // e.g., 10 for 10% increase, -5 for 5% decrease
    public bool AffectsRegularPrice { get; init; } = true;
    public bool AffectsMemberPrice { get; init; } = true;
    public bool AffectsPromoPrice { get; init; }
}

#endregion

#region Search DTOs

public record ServiceSearchRequest
{
    public string? SearchTerm { get; init; }
    public int? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinDuration { get; init; }
    public int? MaxDuration { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

#endregion
