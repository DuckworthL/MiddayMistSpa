using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.API.DTOs.Service;
using MiddayMistSpa.Core.Entities.Service;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class SpaServiceService : ISpaServiceService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<SpaServiceService> _logger;

    public SpaServiceService(SpaDbContext context, ILogger<SpaServiceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Categories

    public async Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request)
    {
        var category = new ServiceCategory
        {
            CategoryName = request.CategoryName,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ServiceCategories.Add(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created service category: {CategoryName}", category.CategoryName);

        return new CategoryResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ServiceCount = 0,
            IsActive = category.IsActive
        };
    }

    public async Task<CategoryResponse?> GetCategoryByIdAsync(int categoryId)
    {
        var category = await _context.ServiceCategories
            .AsNoTracking()
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null) return null;

        return new CategoryResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ServiceCount = category.Services.Count(s => s.IsActive),
            IsActive = category.IsActive
        };
    }

    public async Task<List<CategoryResponse>> GetAllCategoriesAsync(bool includeInactive = false)
    {
        var query = _context.ServiceCategories
            .AsNoTracking()
            .Include(c => c.Services)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.CategoryName)
            .Select(c => new CategoryResponse
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                Description = c.Description,
                DisplayOrder = c.DisplayOrder,
                ServiceCount = c.Services.Count(s => s.IsActive),
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(int categoryId, UpdateCategoryRequest request)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId)
            ?? throw new InvalidOperationException($"Category with ID {categoryId} not found");

        category.CategoryName = request.CategoryName;
        category.Description = request.Description;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated service category: {CategoryName}", category.CategoryName);

        return new CategoryResponse
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            DisplayOrder = category.DisplayOrder,
            ServiceCount = category.Services.Count(s => s.IsActive),
            IsActive = category.IsActive
        };
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.ServiceCategories
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category == null) return false;

        if (category.Services.Any())
            throw new InvalidOperationException("Cannot delete category with existing services. Deactivate or reassign services first.");

        _context.ServiceCategories.Remove(category);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted service category: {CategoryName}", category.CategoryName);
        return true;
    }

    #endregion

    #region Services

    public async Task<ServiceResponse> CreateServiceAsync(CreateServiceRequest request)
    {
        var category = await _context.ServiceCategories.FindAsync(request.CategoryId)
            ?? throw new InvalidOperationException($"Category with ID {request.CategoryId} not found");

        // Generate service code
        var lastService = await _context.Services
            .OrderByDescending(s => s.ServiceId)
            .FirstOrDefaultAsync();
        var nextNumber = (lastService?.ServiceId ?? 0) + 1;
        var serviceCode = $"SVC-{nextNumber:D4}";

        var service = new Core.Entities.Service.Service
        {
            ServiceCode = serviceCode,
            CategoryId = request.CategoryId,
            ServiceName = request.ServiceName,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            RegularPrice = request.RegularPrice,
            MemberPrice = request.MemberPrice,
            PromoPrice = request.PromoPrice,
            TherapistCommissionRate = request.TherapistCommissionRate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created service {ServiceCode}: {ServiceName}", service.ServiceCode, service.ServiceName);

        return MapToServiceResponse(service, category.CategoryName, new List<ProductRequirementResponse>());
    }

    public async Task<ServiceResponse?> GetServiceByIdAsync(int serviceId)
    {
        var service = await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.ProductRequirements)
                .ThenInclude(pr => pr.Product)
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId);

        if (service == null) return null;

        return MapToServiceResponse(service, service.Category.CategoryName,
            service.ProductRequirements.Select(MapToProductRequirementResponse).ToList());
    }

    public async Task<ServiceResponse?> GetServiceByCodeAsync(string serviceCode)
    {
        var service = await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.ProductRequirements)
                .ThenInclude(pr => pr.Product)
            .FirstOrDefaultAsync(s => s.ServiceCode == serviceCode);

        if (service == null) return null;

        return MapToServiceResponse(service, service.Category.CategoryName,
            service.ProductRequirements.Select(MapToProductRequirementResponse).ToList());
    }

    public async Task<PagedResponse<ServiceListResponse>> SearchServicesAsync(ServiceSearchRequest request)
    {
        var query = _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .AsQueryable();

        // Search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(s =>
                s.ServiceName.ToLower().Contains(term) ||
                s.ServiceCode.ToLower().Contains(term) ||
                (s.Description != null && s.Description.ToLower().Contains(term)));
        }

        // Filters
        if (request.CategoryId.HasValue)
            query = query.Where(s => s.CategoryId == request.CategoryId.Value);

        if (request.MinPrice.HasValue)
            query = query.Where(s => s.RegularPrice >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(s => s.RegularPrice <= request.MaxPrice.Value);

        if (request.MinDuration.HasValue)
            query = query.Where(s => s.DurationMinutes >= request.MinDuration.Value);

        if (request.MaxDuration.HasValue)
            query = query.Where(s => s.DurationMinutes <= request.MaxDuration.Value);

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        // Sort
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(s => s.ServiceName)
                : query.OrderBy(s => s.ServiceName),
            "price" => request.SortDescending
                ? query.OrderByDescending(s => s.RegularPrice)
                : query.OrderBy(s => s.RegularPrice),
            "duration" => request.SortDescending
                ? query.OrderByDescending(s => s.DurationMinutes)
                : query.OrderBy(s => s.DurationMinutes),
            "category" => request.SortDescending
                ? query.OrderByDescending(s => s.Category.CategoryName)
                : query.OrderBy(s => s.Category.CategoryName),
            _ => query.OrderBy(s => s.Category.DisplayOrder).ThenBy(s => s.ServiceName)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ServiceListResponse
            {
                ServiceId = s.ServiceId,
                ServiceCode = s.ServiceCode,
                ServiceName = s.ServiceName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.CategoryName,
                DurationMinutes = s.DurationMinutes,
                RegularPrice = s.RegularPrice,
                MemberPrice = s.MemberPrice,
                PromoPrice = s.PromoPrice,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return new PagedResponse<ServiceListResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<ServiceListResponse>> GetServicesByCategoryAsync(int categoryId)
    {
        return await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.CategoryId == categoryId && s.IsActive)
            .OrderBy(s => s.ServiceName)
            .Select(s => new ServiceListResponse
            {
                ServiceId = s.ServiceId,
                ServiceCode = s.ServiceCode,
                ServiceName = s.ServiceName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.CategoryName,
                DurationMinutes = s.DurationMinutes,
                RegularPrice = s.RegularPrice,
                MemberPrice = s.MemberPrice,
                PromoPrice = s.PromoPrice,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<ServiceListResponse>> GetActiveServicesAsync()
    {
        return await _context.Services
            .AsNoTracking()
            .Include(s => s.Category)
            .Where(s => s.IsActive && s.Category.IsActive)
            .OrderBy(s => s.Category.DisplayOrder)
            .ThenBy(s => s.ServiceName)
            .Select(s => new ServiceListResponse
            {
                ServiceId = s.ServiceId,
                ServiceCode = s.ServiceCode,
                ServiceName = s.ServiceName,
                CategoryId = s.CategoryId,
                CategoryName = s.Category.CategoryName,
                DurationMinutes = s.DurationMinutes,
                RegularPrice = s.RegularPrice,
                MemberPrice = s.MemberPrice,
                PromoPrice = s.PromoPrice,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<ServiceMenuResponse>> GetServiceMenuAsync()
    {
        var categories = await _context.ServiceCategories
            .AsNoTracking()
            .Include(c => c.Services.Where(s => s.IsActive))
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return categories.Select(c => new ServiceMenuResponse
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            CategoryDescription = c.Description,
            Services = c.Services
                .OrderBy(s => s.ServiceName)
                .Select(s => new ServiceListResponse
                {
                    ServiceId = s.ServiceId,
                    ServiceCode = s.ServiceCode,
                    ServiceName = s.ServiceName,
                    CategoryId = s.CategoryId,
                    CategoryName = c.CategoryName,
                    DurationMinutes = s.DurationMinutes,
                    RegularPrice = s.RegularPrice,
                    MemberPrice = s.MemberPrice,
                    PromoPrice = s.PromoPrice,
                    IsActive = s.IsActive
                })
                .ToList()
        }).ToList();
    }

    public async Task<ServiceResponse> UpdateServiceAsync(int serviceId, UpdateServiceRequest request)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.ProductRequirements)
                .ThenInclude(pr => pr.Product)
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId)
            ?? throw new InvalidOperationException($"Service with ID {serviceId} not found");

        var category = await _context.ServiceCategories.FindAsync(request.CategoryId)
            ?? throw new InvalidOperationException($"Category with ID {request.CategoryId} not found");

        service.CategoryId = request.CategoryId;
        service.ServiceName = request.ServiceName;
        service.Description = request.Description;
        service.DurationMinutes = request.DurationMinutes;
        service.RegularPrice = request.RegularPrice;
        service.MemberPrice = request.MemberPrice;
        service.PromoPrice = request.PromoPrice;
        service.TherapistCommissionRate = request.TherapistCommissionRate;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated service {ServiceCode}", service.ServiceCode);

        return MapToServiceResponse(service, category.CategoryName,
            service.ProductRequirements.Select(MapToProductRequirementResponse).ToList());
    }

    public async Task<ServiceResponse> UpdatePricingAsync(int serviceId, UpdatePricingRequest request)
    {
        var service = await _context.Services
            .Include(s => s.Category)
            .Include(s => s.ProductRequirements)
                .ThenInclude(pr => pr.Product)
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId)
            ?? throw new InvalidOperationException($"Service with ID {serviceId} not found");

        service.RegularPrice = request.RegularPrice;
        service.MemberPrice = request.MemberPrice;
        service.PromoPrice = request.PromoPrice;
        service.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated pricing for service {ServiceCode}", service.ServiceCode);

        return MapToServiceResponse(service, service.Category.CategoryName,
            service.ProductRequirements.Select(MapToProductRequirementResponse).ToList());
    }

    public async Task<int> ApplyBulkPriceAdjustmentAsync(BulkPriceAdjustmentRequest request)
    {
        var query = _context.Services.Where(s => s.IsActive);

        if (request.CategoryId.HasValue)
            query = query.Where(s => s.CategoryId == request.CategoryId.Value);

        var services = await query.ToListAsync();
        var multiplier = 1 + (request.PercentageChange / 100);

        foreach (var service in services)
        {
            if (request.AffectsRegularPrice)
                service.RegularPrice = Math.Round(service.RegularPrice * multiplier, 2);

            if (request.AffectsMemberPrice && service.MemberPrice.HasValue)
                service.MemberPrice = Math.Round(service.MemberPrice.Value * multiplier, 2);

            if (request.AffectsPromoPrice && service.PromoPrice.HasValue)
                service.PromoPrice = Math.Round(service.PromoPrice.Value * multiplier, 2);

            service.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Applied {Percentage}% price adjustment to {Count} services",
            request.PercentageChange, services.Count);

        return services.Count;
    }

    public async Task<bool> DeactivateServiceAsync(int serviceId)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return false;

        service.IsActive = false;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated service {ServiceCode}", service.ServiceCode);
        return true;
    }

    public async Task<bool> ReactivateServiceAsync(int serviceId)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return false;

        service.IsActive = true;
        service.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reactivated service {ServiceCode}", service.ServiceCode);
        return true;
    }

    #endregion

    #region Product Requirements

    public async Task<ProductRequirementResponse> AddProductRequirementAsync(int serviceId, AddProductRequirementRequest request)
    {
        var service = await _context.Services.FindAsync(serviceId)
            ?? throw new InvalidOperationException($"Service with ID {serviceId} not found");

        var product = await _context.Products.FindAsync(request.ProductId)
            ?? throw new InvalidOperationException($"Product with ID {request.ProductId} not found");

        var requirement = new ServiceProductRequirement
        {
            ServiceId = serviceId,
            ProductId = request.ProductId,
            QuantityRequired = request.QuantityRequired,
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceProductRequirements.Add(requirement);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added product requirement for service {ServiceCode}: {ProductName} x {Qty}",
            service.ServiceCode, product.ProductName, request.QuantityRequired);

        return new ProductRequirementResponse
        {
            RequirementId = requirement.RequirementId,
            ProductId = product.ProductId,
            ProductName = product.ProductName,
            ProductCode = product.ProductCode,
            QuantityRequired = requirement.QuantityRequired,
            Unit = product.UnitOfMeasure
        };
    }

    public async Task<List<ProductRequirementResponse>> GetServiceProductRequirementsAsync(int serviceId)
    {
        return await _context.ServiceProductRequirements
            .AsNoTracking()
            .Include(pr => pr.Product)
            .Where(pr => pr.ServiceId == serviceId)
            .Select(pr => new ProductRequirementResponse
            {
                RequirementId = pr.RequirementId,
                ProductId = pr.ProductId,
                ProductName = pr.Product.ProductName,
                ProductCode = pr.Product.ProductCode,
                QuantityRequired = pr.QuantityRequired,
                Unit = pr.Product.UnitOfMeasure
            })
            .ToListAsync();
    }

    public async Task<bool> RemoveProductRequirementAsync(int requirementId)
    {
        var requirement = await _context.ServiceProductRequirements.FindAsync(requirementId);
        if (requirement == null) return false;

        _context.ServiceProductRequirements.Remove(requirement);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ProductRequirementResponse> UpdateProductRequirementAsync(int requirementId, decimal quantityRequired)
    {
        var requirement = await _context.ServiceProductRequirements
            .Include(pr => pr.Product)
            .FirstOrDefaultAsync(pr => pr.RequirementId == requirementId)
            ?? throw new InvalidOperationException($"Product requirement with ID {requirementId} not found");

        requirement.QuantityRequired = quantityRequired;
        await _context.SaveChangesAsync();

        return new ProductRequirementResponse
        {
            RequirementId = requirement.RequirementId,
            ProductId = requirement.ProductId,
            ProductName = requirement.Product.ProductName,
            ProductCode = requirement.Product.ProductCode,
            QuantityRequired = requirement.QuantityRequired,
            Unit = requirement.Product.UnitOfMeasure
        };
    }

    #endregion

    #region Pricing Helpers

    public async Task<decimal> GetPriceForCustomerAsync(int serviceId, string membershipType)
    {
        var service = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ServiceId == serviceId)
            ?? throw new InvalidOperationException($"Service with ID {serviceId} not found");

        // Priority: Promo > Member > Regular
        if (service.PromoPrice.HasValue && service.PromoPrice.Value > 0)
            return service.PromoPrice.Value;

        if (membershipType != "Regular" && service.MemberPrice.HasValue && service.MemberPrice.Value > 0)
            return service.MemberPrice.Value;

        return service.RegularPrice;
    }

    #endregion

    #region Private Helpers

    private static ServiceResponse MapToServiceResponse(Core.Entities.Service.Service service, string categoryName, List<ProductRequirementResponse> requirements) => new()
    {
        ServiceId = service.ServiceId,
        ServiceCode = service.ServiceCode,
        ServiceName = service.ServiceName,
        Description = service.Description,
        CategoryId = service.CategoryId,
        CategoryName = categoryName,
        DurationMinutes = service.DurationMinutes,
        RegularPrice = service.RegularPrice,
        MemberPrice = service.MemberPrice,
        PromoPrice = service.PromoPrice,
        TherapistCommissionRate = service.TherapistCommissionRate,
        IsActive = service.IsActive,
        ProductRequirements = requirements,
        CreatedAt = service.CreatedAt,
        UpdatedAt = service.UpdatedAt
    };

    private static ProductRequirementResponse MapToProductRequirementResponse(ServiceProductRequirement requirement) => new()
    {
        RequirementId = requirement.RequirementId,
        ProductId = requirement.ProductId,
        ProductName = requirement.Product?.ProductName ?? "Unknown",
        ProductCode = requirement.Product?.ProductCode,
        QuantityRequired = requirement.QuantityRequired,
        Unit = requirement.Product?.UnitOfMeasure ?? "pcs"
    };

    #endregion
}
