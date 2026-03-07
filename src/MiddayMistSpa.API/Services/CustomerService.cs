using Microsoft.EntityFrameworkCore;
using MiddayMistSpa.API.DTOs.Customer;
using MiddayMistSpa.API.DTOs.Employee;
using MiddayMistSpa.Core;
using MiddayMistSpa.Core.Entities.Customer;
using MiddayMistSpa.Infrastructure.Data;

namespace MiddayMistSpa.API.Services;

public class CustomerService : ICustomerService
{
    private readonly SpaDbContext _context;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(SpaDbContext context, ILogger<CustomerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Customer CRUD

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request)
    {
        // Check for duplicate phone number
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var existingByPhone = await _context.Customers
                .AnyAsync(c => c.PhoneNumber == request.PhoneNumber && c.IsActive);
            if (existingByPhone)
                throw new InvalidOperationException($"A customer with phone number '{request.PhoneNumber}' already exists");
        }

        // Check for duplicate email
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingByEmail = await _context.Customers
                .AnyAsync(c => c.Email == request.Email && c.IsActive);
            if (existingByEmail)
                throw new InvalidOperationException($"A customer with email '{request.Email}' already exists");
        }

        // Generate customer code with retry for race conditions
        string customerCode;
        var maxRetries = 3;
        for (var attempt = 0; ; attempt++)
        {
            var lastCustomer = await _context.Customers
                .OrderByDescending(c => c.CustomerId)
                .FirstOrDefaultAsync();
            var nextNumber = (lastCustomer?.CustomerId ?? 0) + 1 + attempt;
            customerCode = $"CUST-{nextNumber:D6}";

            var codeExists = await _context.Customers.AnyAsync(c => c.CustomerCode == customerCode);
            if (!codeExists) break;
            if (attempt >= maxRetries)
                throw new InvalidOperationException("Failed to generate unique customer code after multiple attempts");
        }

        var customer = new Customer
        {
            CustomerCode = customerCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            City = request.City,
            Province = request.Province,
            PostalCode = request.PostalCode,
            MembershipType = request.MembershipType,
            MembershipStartDate = request.MembershipType != "Regular" ? DateTime.UtcNow : null,
            PreferredTherapistId = request.PreferredTherapistId,
            PressurePreference = request.PressurePreference,
            TemperaturePreference = request.TemperaturePreference,
            MusicPreference = request.MusicPreference,
            Allergies = request.Allergies,
            MedicalNotes = request.MedicalNotes,
            SpecialRequests = request.SpecialRequests,
            MarketingConsent = request.MarketingConsent,
            ReferralSource = request.ReferralSource,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            EmergencyContactRelationship = request.EmergencyContactRelationship,
            PreferredCommunicationChannel = request.PreferredCommunicationChannel,
            SmsConsent = request.SmsConsent,
            LoyaltyPoints = 0,
            TotalVisits = 0,
            TotalSpent = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created customer {CustomerCode}: {FullName}", customer.CustomerCode, customer.FullName);

        return await GetCustomerResponseAsync(customer);
    }

    public async Task<CustomerResponse?> GetCustomerByIdAsync(int customerId)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse?> GetCustomerByCodeAsync(string customerCode)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.CustomerCode == customerCode);

        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<CustomerResponse?> GetCustomerByPhoneAsync(string phoneNumber)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.PhoneNumber == phoneNumber);

        return customer == null ? null : MapToResponse(customer);
    }

    public async Task<PagedResponse<CustomerListResponse>> SearchCustomersAsync(CustomerSearchRequest request)
    {
        var query = _context.Customers.AsNoTracking();

        // Search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term) ||
                c.CustomerCode.ToLower().Contains(term) ||
                c.PhoneNumber.Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        // Filters
        if (!string.IsNullOrWhiteSpace(request.MembershipType))
            query = query.Where(c => c.MembershipType == request.MembershipType);

        if (!string.IsNullOrWhiteSpace(request.Segment))
            query = query.Where(c => c.CustomerSegment == request.Segment);

        if (request.HasAllergies.HasValue)
            query = request.HasAllergies.Value
                ? query.Where(c => c.Allergies != null && c.Allergies != "")
                : query.Where(c => c.Allergies == null || c.Allergies == "");

        if (request.IsActive.HasValue)
            query = query.Where(c => c.IsActive == request.IsActive.Value);

        // Sort
        query = request.SortBy?.ToLower() switch
        {
            "name" => request.SortDescending
                ? query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName),
            "lastvisit" => request.SortDescending
                ? query.OrderByDescending(c => c.LastVisitDate)
                : query.OrderBy(c => c.LastVisitDate),
            "totalspent" => request.SortDescending
                ? query.OrderByDescending(c => c.TotalSpent)
                : query.OrderBy(c => c.TotalSpent),
            "points" => request.SortDescending
                ? query.OrderByDescending(c => c.LoyaltyPoints)
                : query.OrderBy(c => c.LoyaltyPoints),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerListResponse
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                MembershipType = c.MembershipType,
                LoyaltyPoints = c.LoyaltyPoints,
                LastVisitDate = c.LastVisitDate,
                TotalVisits = c.TotalVisits,
                TotalSpent = c.TotalSpent,
                Allergies = c.Allergies,
                CustomerSegment = c.CustomerSegment,
                HasAllergies = !string.IsNullOrEmpty(c.Allergies),
                IsActive = c.IsActive
            })
            .ToListAsync();

        return new PagedResponse<CustomerListResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<CustomerListResponse>> GetRecentCustomersAsync(int count = 10)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.LastVisitDate ?? c.CreatedAt)
            .Take(count)
            .Select(c => new CustomerListResponse
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                MembershipType = c.MembershipType,
                LoyaltyPoints = c.LoyaltyPoints,
                LastVisitDate = c.LastVisitDate,
                TotalVisits = c.TotalVisits,
                TotalSpent = c.TotalSpent,
                Allergies = c.Allergies,
                CustomerSegment = c.CustomerSegment,
                HasAllergies = !string.IsNullOrEmpty(c.Allergies),
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request)
    {
        var customer = await _context.Customers
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        customer.FirstName = request.FirstName;
        customer.LastName = request.LastName;
        customer.Email = request.Email;
        customer.PhoneNumber = request.PhoneNumber;
        customer.DateOfBirth = request.DateOfBirth;
        customer.Gender = request.Gender;
        customer.Address = request.Address;
        customer.City = request.City;
        customer.Province = request.Province;
        customer.PostalCode = request.PostalCode;
        customer.MembershipType = request.MembershipType;
        customer.MembershipStartDate = request.MembershipStartDate;
        customer.MembershipExpiryDate = request.MembershipExpiryDate;
        customer.PreferredTherapistId = request.PreferredTherapistId;
        customer.PressurePreference = request.PressurePreference;
        customer.TemperaturePreference = request.TemperaturePreference;
        customer.MusicPreference = request.MusicPreference;
        customer.Allergies = request.Allergies;
        customer.MedicalNotes = request.MedicalNotes;
        customer.SpecialRequests = request.SpecialRequests;
        customer.MarketingConsent = request.MarketingConsent;
        customer.ReferralSource = request.ReferralSource;
        customer.EmergencyContactName = request.EmergencyContactName;
        customer.EmergencyContactPhone = request.EmergencyContactPhone;
        customer.EmergencyContactRelationship = request.EmergencyContactRelationship;
        customer.PreferredCommunicationChannel = request.PreferredCommunicationChannel;
        customer.SmsConsent = request.SmsConsent;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated customer {CustomerCode}", customer.CustomerCode);

        return MapToResponse(customer);
    }

    public async Task<bool> DeactivateCustomerAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return false;

        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deactivated customer {CustomerCode}", customer.CustomerCode);
        return true;
    }

    public async Task<bool> ReactivateCustomerAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return false;

        customer.IsActive = true;
        customer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reactivated customer {CustomerCode}", customer.CustomerCode);
        return true;
    }

    #endregion

    #region Preferences

    public async Task<CustomerPreferencesResponse?> GetCustomerPreferencesAsync(int customerId)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);

        if (customer == null) return null;

        return new CustomerPreferencesResponse
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            PreferredTherapistId = customer.PreferredTherapistId,
            PreferredTherapistName = customer.PreferredTherapist?.FullName,
            PressurePreference = customer.PressurePreference,
            TemperaturePreference = customer.TemperaturePreference,
            MusicPreference = customer.MusicPreference,
            Allergies = customer.Allergies,
            MedicalNotes = customer.MedicalNotes,
            SpecialRequests = customer.SpecialRequests
        };
    }

    public async Task<CustomerPreferencesResponse> UpdateCustomerPreferencesAsync(int customerId, CustomerPreferencesResponse preferences)
    {
        var customer = await _context.Customers
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        customer.PreferredTherapistId = preferences.PreferredTherapistId;
        customer.PressurePreference = preferences.PressurePreference;
        customer.TemperaturePreference = preferences.TemperaturePreference;
        customer.MusicPreference = preferences.MusicPreference;
        customer.Allergies = preferences.Allergies;
        customer.MedicalNotes = preferences.MedicalNotes;
        customer.SpecialRequests = preferences.SpecialRequests;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload therapist for response
        if (customer.PreferredTherapistId.HasValue)
        {
            customer.PreferredTherapist = await _context.Employees.FindAsync(customer.PreferredTherapistId.Value);
        }

        return new CustomerPreferencesResponse
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            PreferredTherapistId = customer.PreferredTherapistId,
            PreferredTherapistName = customer.PreferredTherapist?.FullName,
            PressurePreference = customer.PressurePreference,
            TemperaturePreference = customer.TemperaturePreference,
            MusicPreference = customer.MusicPreference,
            Allergies = customer.Allergies,
            MedicalNotes = customer.MedicalNotes,
            SpecialRequests = customer.SpecialRequests
        };
    }

    #endregion

    #region Loyalty Program

    public async Task<LoyaltyTransactionResponse> AddLoyaltyPointsAsync(int customerId, AddLoyaltyPointsRequest request)
    {
        if (request.Points <= 0)
            throw new InvalidOperationException("Points must be a positive value");

        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        customer.LoyaltyPoints += request.Points;
        customer.UpdatedAt = DateTime.UtcNow;

        // Create audit record
        _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
        {
            CustomerId = customer.CustomerId,
            TransactionType = DomainConstants.LoyaltyTransactionTypes.Adjust,
            Points = request.Points,
            BalanceRemaining = request.Points,
            EarnedDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddMonths(DomainConstants.LoyaltyConfig.DefaultExpiryMonths),
            Description = $"Manual adjustment: {request.Reason}",
            CreatedAt = DateTime.UtcNow
        });

        // Re-evaluate membership tier
        var newTier = DomainConstants.MembershipTiers.GetTierForPoints(customer.LoyaltyPoints);
        if (customer.MembershipType != newTier)
        {
            _logger.LogInformation("Customer {CustomerCode} tier changed from {Old} to {New} after point addition",
                customer.CustomerCode, customer.MembershipType, newTier);
            customer.MembershipType = newTier;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added {Points} loyalty points to customer {CustomerCode}. New balance: {Balance}",
            request.Points, customer.CustomerCode, customer.LoyaltyPoints);

        return new LoyaltyTransactionResponse
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            PointsChange = request.Points,
            TransactionType = DomainConstants.LoyaltyTransactionTypes.Adjust,
            Reason = request.Reason,
            NewBalance = customer.LoyaltyPoints,
            TransactionDate = DateTime.UtcNow
        };
    }

    public async Task<LoyaltyTransactionResponse> RedeemLoyaltyPointsAsync(int customerId, RedeemLoyaltyPointsRequest request)
    {
        if (request.Points <= 0)
            throw new InvalidOperationException("Points must be a positive value");

        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        if (customer.LoyaltyPoints < request.Points)
            throw new InvalidOperationException($"Insufficient points. Available: {customer.LoyaltyPoints}, Requested: {request.Points}");

        customer.LoyaltyPoints -= request.Points;
        customer.UpdatedAt = DateTime.UtcNow;

        // Create audit record
        _context.LoyaltyPointTransactions.Add(new LoyaltyPointTransaction
        {
            CustomerId = customer.CustomerId,
            TransactionType = DomainConstants.LoyaltyTransactionTypes.Redeem,
            Points = -request.Points,
            BalanceRemaining = 0,
            EarnedDate = DateTime.UtcNow,
            Description = $"Redeemed: {request.Reason}",
            CreatedAt = DateTime.UtcNow
        });

        // Deduct from oldest non-expired earn batches (FIFO)
        var remainingToDeduct = request.Points;
        var earnBatches = await _context.LoyaltyPointTransactions
            .Where(t => t.CustomerId == customerId
                && t.TransactionType == DomainConstants.LoyaltyTransactionTypes.Earn
                && t.BalanceRemaining > 0
                && (!t.ExpiryDate.HasValue || t.ExpiryDate.Value > DateTime.UtcNow))
            .OrderBy(t => t.EarnedDate)
            .ToListAsync();

        foreach (var batch in earnBatches)
        {
            if (remainingToDeduct <= 0) break;
            var deductFromBatch = Math.Min(batch.BalanceRemaining, remainingToDeduct);
            batch.BalanceRemaining -= deductFromBatch;
            remainingToDeduct -= deductFromBatch;
        }

        // Re-evaluate membership tier
        var newTier = DomainConstants.MembershipTiers.GetTierForPoints(customer.LoyaltyPoints);
        if (customer.MembershipType != newTier)
        {
            _logger.LogInformation("Customer {CustomerCode} tier changed from {Old} to {New} after redemption",
                customer.CustomerCode, customer.MembershipType, newTier);
            customer.MembershipType = newTier;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Redeemed {Points} loyalty points from customer {CustomerCode}. New balance: {Balance}",
            request.Points, customer.CustomerCode, customer.LoyaltyPoints);

        return new LoyaltyTransactionResponse
        {
            CustomerId = customer.CustomerId,
            CustomerName = customer.FullName,
            PointsChange = -request.Points,
            TransactionType = DomainConstants.LoyaltyTransactionTypes.Redeem,
            Reason = request.Reason,
            NewBalance = customer.LoyaltyPoints,
            TransactionDate = DateTime.UtcNow
        };
    }

    public async Task<int> GetLoyaltyPointsBalanceAsync(int customerId)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        return customer.LoyaltyPoints;
    }

    public async Task<List<LoyaltyPointHistoryResponse>> GetLoyaltyTransactionHistoryAsync(int customerId, int count = 50)
    {
        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
        if (!customerExists)
            throw new InvalidOperationException($"Customer with ID {customerId} not found");

        return await _context.LoyaltyPointTransactions
            .AsNoTracking()
            .Include(t => t.Transaction)
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .Select(t => new LoyaltyPointHistoryResponse
            {
                LoyaltyPointTransactionId = t.LoyaltyPointTransactionId,
                TransactionType = t.TransactionType,
                Points = t.Points,
                BalanceRemaining = t.BalanceRemaining,
                EarnedDate = t.EarnedDate,
                ExpiryDate = t.ExpiryDate,
                IsExpired = t.ExpiryDate.HasValue && t.ExpiryDate.Value < DateTime.UtcNow,
                TransactionId = t.TransactionId,
                TransactionNumber = t.Transaction != null ? t.Transaction.TransactionNumber : null,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }
    #endregion

    #region Visit History

    public async Task<List<CustomerVisitHistoryResponse>> GetCustomerVisitHistoryAsync(int customerId, int count = 10)
    {
        var appointments = await _context.Appointments
            .AsNoTracking()
            .Include(a => a.Service)
            .Include(a => a.Therapist)
            .Where(a => a.CustomerId == customerId && a.Status == "Completed")
            .OrderByDescending(a => a.AppointmentDate)
            .Take(count)
            .Select(a => new CustomerVisitHistoryResponse
            {
                AppointmentId = a.AppointmentId,
                AppointmentDate = a.AppointmentDate,
                ServiceName = a.Service != null ? a.Service.ServiceName : "Unknown",
                TherapistName = a.Therapist != null ? a.Therapist.FullName : null,
                Amount = a.Service != null ? a.Service.RegularPrice : 0,
                Status = a.Status
            })
            .ToListAsync();

        return appointments;
    }

    public async Task UpdateCustomerVisitStatsAsync(int customerId, decimal transactionAmount)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) return;

        if (customer.FirstVisitDate == null)
            customer.FirstVisitDate = DateTime.UtcNow;

        customer.LastVisitDate = DateTime.UtcNow;
        customer.TotalVisits++;
        customer.TotalSpent += transactionAmount;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Segments

    public async Task<List<CustomerSegmentResponse>> GetAllSegmentsAsync()
    {
        return await _context.CustomerSegments
            .AsNoTracking()
            .OrderBy(s => s.SegmentName)
            .Select(s => new CustomerSegmentResponse
            {
                SegmentId = s.SegmentId,
                SegmentName = s.SegmentName,
                SegmentCode = s.SegmentCode,
                Description = s.Description,
                ClusterId = s.ClusterId,
                AverageRecency = s.AverageRecency,
                AverageFrequency = s.AverageFrequency,
                AverageMonetaryValue = s.AverageMonetaryValue,
                CustomerCount = s.CustomerCount,
                RecommendedAction = s.RecommendedAction,
                LastAnalysisDate = s.LastAnalysisDate
            })
            .ToListAsync();
    }

    public async Task<List<CustomerListResponse>> GetCustomersBySegmentAsync(string segmentName)
    {
        return await _context.Customers
            .AsNoTracking()
            .Where(c => c.CustomerSegment == segmentName && c.IsActive)
            .OrderBy(c => c.LastName)
            .ThenBy(c => c.FirstName)
            .Select(c => new CustomerListResponse
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                MembershipType = c.MembershipType,
                LoyaltyPoints = c.LoyaltyPoints,
                LastVisitDate = c.LastVisitDate,
                TotalVisits = c.TotalVisits,
                TotalSpent = c.TotalSpent,
                Allergies = c.Allergies,
                CustomerSegment = c.CustomerSegment,
                HasAllergies = !string.IsNullOrEmpty(c.Allergies),
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    public async Task AssignCustomerToSegmentAsync(int customerId, string segmentName)
    {
        var customer = await _context.Customers.FindAsync(customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        customer.CustomerSegment = segmentName;
        customer.SegmentAssignedDate = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned customer {CustomerCode} to segment {Segment}", customer.CustomerCode, segmentName);
    }

    #endregion

    #region Membership

    public async Task<CustomerResponse> UpgradeMembershipAsync(int customerId, string membershipType, DateTime? expiryDate)
    {
        var customer = await _context.Customers
            .Include(c => c.PreferredTherapist)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId)
            ?? throw new InvalidOperationException($"Customer with ID {customerId} not found");

        if (!DomainConstants.MembershipTiers.All.Contains(membershipType))
            throw new InvalidOperationException($"Invalid membership type: '{membershipType}'. Valid types: {string.Join(", ", DomainConstants.MembershipTiers.All)}");

        var oldMembership = customer.MembershipType;
        customer.MembershipType = membershipType;
        customer.MembershipStartDate = DateTime.UtcNow;
        customer.MembershipExpiryDate = expiryDate;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Upgraded customer {CustomerCode} membership from {Old} to {New}",
            customer.CustomerCode, oldMembership, membershipType);

        return MapToResponse(customer);
    }

    public async Task<List<CustomerListResponse>> GetExpiringMembershipsAsync(int daysAhead = 30)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(daysAhead);

        return await _context.Customers
            .AsNoTracking()
            .Where(c =>
                c.IsActive &&
                c.MembershipType != "Regular" &&
                c.MembershipExpiryDate != null &&
                c.MembershipExpiryDate <= cutoffDate &&
                c.MembershipExpiryDate > DateTime.UtcNow)
            .OrderBy(c => c.MembershipExpiryDate)
            .Select(c => new CustomerListResponse
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                FirstName = c.FirstName,
                LastName = c.LastName,
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                MembershipType = c.MembershipType,
                LoyaltyPoints = c.LoyaltyPoints,
                LastVisitDate = c.LastVisitDate,
                TotalVisits = c.TotalVisits,
                TotalSpent = c.TotalSpent,
                Allergies = c.Allergies,
                CustomerSegment = c.CustomerSegment,
                HasAllergies = !string.IsNullOrEmpty(c.Allergies),
                IsActive = c.IsActive
            })
            .ToListAsync();
    }

    #endregion

    #region Private Helpers

    private async Task<CustomerResponse> GetCustomerResponseAsync(Customer customer)
    {
        if (customer.PreferredTherapistId.HasValue && customer.PreferredTherapist == null)
        {
            customer.PreferredTherapist = await _context.Employees.FindAsync(customer.PreferredTherapistId.Value);
        }
        return MapToResponse(customer);
    }

    private static CustomerResponse MapToResponse(Customer customer) => new()
    {
        CustomerId = customer.CustomerId,
        CustomerCode = customer.CustomerCode,
        FullName = customer.FullName,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Email = customer.Email,
        PhoneNumber = customer.PhoneNumber,
        DateOfBirth = customer.DateOfBirth,
        Gender = customer.Gender,
        Address = customer.Address,
        City = customer.City,
        Province = customer.Province,
        PostalCode = customer.PostalCode,
        MembershipType = customer.MembershipType,
        MembershipStartDate = customer.MembershipStartDate,
        MembershipExpiryDate = customer.MembershipExpiryDate,
        LoyaltyPoints = customer.LoyaltyPoints,
        PreferredTherapistId = customer.PreferredTherapistId,
        PreferredTherapistName = customer.PreferredTherapist?.FullName,
        PressurePreference = customer.PressurePreference,
        TemperaturePreference = customer.TemperaturePreference,
        MusicPreference = customer.MusicPreference,
        Allergies = customer.Allergies,
        MedicalNotes = customer.MedicalNotes,
        SpecialRequests = customer.SpecialRequests,
        MarketingConsent = customer.MarketingConsent,
        ReferralSource = customer.ReferralSource,
        EmergencyContactName = customer.EmergencyContactName,
        EmergencyContactPhone = customer.EmergencyContactPhone,
        EmergencyContactRelationship = customer.EmergencyContactRelationship,
        PreferredCommunicationChannel = customer.PreferredCommunicationChannel,
        SmsConsent = customer.SmsConsent,
        FirstVisitDate = customer.FirstVisitDate,
        LastVisitDate = customer.LastVisitDate,
        TotalVisits = customer.TotalVisits,
        TotalSpent = customer.TotalSpent,
        CustomerSegment = customer.CustomerSegment,
        SegmentAssignedDate = customer.SegmentAssignedDate,
        IsActive = customer.IsActive,
        CreatedAt = customer.CreatedAt,
        UpdatedAt = customer.UpdatedAt
    };

    #endregion

    #region Stats

    public async Task<CustomerStatsResponse> GetCustomerStatsAsync()
    {
        var today = PhilippineTime.Today;
        var activeThreshold = today.AddDays(-30);
        var atRiskThreshold = today.AddDays(-90);

        var activeCustomers = _context.Customers.Where(c => c.IsActive);

        var membershipTypes = new[] { "Bronze", "Silver", "Gold", "Platinum" };

        return new CustomerStatsResponse
        {
            TotalCustomers = await activeCustomers.CountAsync(),
            LoyaltyMembers = await activeCustomers.CountAsync(c => membershipTypes.Contains(c.MembershipType)),
            ActiveCount = await activeCustomers.CountAsync(c => c.LastVisitDate >= activeThreshold),
            AtRiskCount = await activeCustomers.CountAsync(c => c.LastVisitDate != null && c.LastVisitDate < atRiskThreshold)
        };
    }

    #endregion
}
