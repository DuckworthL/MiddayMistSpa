namespace MiddayMistSpa.API.DTOs.Customer;

#region Customer DTOs

public record CreateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }

    // Membership
    public string MembershipType { get; init; } = "Regular";

    // Preferences
    public int? PreferredTherapistId { get; init; }
    public string? PressurePreference { get; init; }
    public string? TemperaturePreference { get; init; }
    public string? MusicPreference { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalNotes { get; init; }
    public string? SpecialRequests { get; init; }

    // Emergency Contact
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }

    // Communication
    public string PreferredCommunicationChannel { get; init; } = "Email";
    public bool SmsConsent { get; init; }

    // Marketing
    public bool MarketingConsent { get; init; }
    public string? ReferralSource { get; init; }
}

public record UpdateCustomerRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }

    // Membership
    public string MembershipType { get; init; } = "Regular";
    public DateTime? MembershipStartDate { get; init; }
    public DateTime? MembershipExpiryDate { get; init; }

    // Preferences
    public int? PreferredTherapistId { get; init; }
    public string? PressurePreference { get; init; }
    public string? TemperaturePreference { get; init; }
    public string? MusicPreference { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalNotes { get; init; }
    public string? SpecialRequests { get; init; }

    // Emergency Contact
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }

    // Communication
    public string PreferredCommunicationChannel { get; init; } = "Email";
    public bool SmsConsent { get; init; }

    // Marketing
    public bool MarketingConsent { get; init; }
    public string? ReferralSource { get; init; }
}

public record CustomerResponse
{
    public int CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Province { get; init; }
    public string? PostalCode { get; init; }

    // Membership
    public string MembershipType { get; init; } = string.Empty;
    public DateTime? MembershipStartDate { get; init; }
    public DateTime? MembershipExpiryDate { get; init; }
    public int LoyaltyPoints { get; init; }

    // Preferences
    public int? PreferredTherapistId { get; init; }
    public string? PreferredTherapistName { get; init; }
    public string? PressurePreference { get; init; }
    public string? TemperaturePreference { get; init; }
    public string? MusicPreference { get; init; }
    public string? Allergies { get; init; } // IMPORTANT: Red alert in UI
    public string? MedicalNotes { get; init; }
    public string? SpecialRequests { get; init; }

    // Emergency Contact
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }
    public string? EmergencyContactRelationship { get; init; }

    // Communication
    public string PreferredCommunicationChannel { get; init; } = "Email";
    public bool SmsConsent { get; init; }

    // Marketing
    public bool MarketingConsent { get; init; }
    public string? ReferralSource { get; init; }

    // Visit History
    public DateTime? FirstVisitDate { get; init; }
    public DateTime? LastVisitDate { get; init; }
    public int TotalVisits { get; init; }
    public decimal TotalSpent { get; init; }

    // Segmentation
    public string? CustomerSegment { get; init; }
    public DateTime? SegmentAssignedDate { get; init; }

    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CustomerListResponse
{
    public int CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string MembershipType { get; init; } = string.Empty;
    public int LoyaltyPoints { get; init; }
    public DateTime? LastVisitDate { get; init; }
    public int TotalVisits { get; init; }
    public decimal TotalSpent { get; init; }
    public string? Allergies { get; init; }
    public string? CustomerSegment { get; init; }
    public bool HasAllergies { get; init; }
    public bool IsActive { get; init; }
}

public record CustomerPreferencesResponse
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int? PreferredTherapistId { get; init; }
    public string? PreferredTherapistName { get; init; }
    public string? PressurePreference { get; init; }
    public string? TemperaturePreference { get; init; }
    public string? MusicPreference { get; init; }
    public string? Allergies { get; init; }
    public string? MedicalNotes { get; init; }
    public string? SpecialRequests { get; init; }
}

#endregion

#region Loyalty DTOs

public record AddLoyaltyPointsRequest
{
    public int Points { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record RedeemLoyaltyPointsRequest
{
    public int Points { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public record LoyaltyTransactionResponse
{
    public int CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public int PointsChange { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int NewBalance { get; init; }
    public DateTime TransactionDate { get; init; }
}

public record LoyaltyPointHistoryResponse
{
    public int LoyaltyPointTransactionId { get; init; }
    public string TransactionType { get; init; } = string.Empty;
    public int Points { get; init; }
    public int BalanceRemaining { get; init; }
    public DateTime EarnedDate { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public bool IsExpired { get; init; }
    public int? TransactionId { get; init; }
    public string? TransactionNumber { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion

#region Segment DTOs

public record CustomerSegmentResponse
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
}

public record CustomerVisitHistoryResponse
{
    public int AppointmentId { get; init; }
    public DateTime AppointmentDate { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string? TherapistName { get; init; }
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
}

#endregion

#region Search DTOs

public record CustomerSearchRequest
{
    public string? SearchTerm { get; init; }
    public string? MembershipType { get; init; }
    public string? Segment { get; init; }
    public bool? HasAllergies { get; init; }
    public bool? IsActive { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

#endregion

#region Export DTOs

public record SegmentExportRequest
{
    /// <summary>Export format: PDF or Excel</summary>
    public string Format { get; init; } = "PDF";
    /// <summary>Optional: filter to a specific segment name. Null = export all.</summary>
    public string? SegmentName { get; init; }
}

#endregion
