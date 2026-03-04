namespace MiddayMistSpa.Core.Entities.Customer;

/// <summary>
/// Customer entity with preferences, membership, and DBSCAN segmentation fields
/// </summary>
public class Customer
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? PostalCode { get; set; }

    // Membership
    public string MembershipType { get; set; } = "Regular"; // Regular, Silver, Gold, Platinum
    public DateTime? MembershipStartDate { get; set; }
    public DateTime? MembershipExpiryDate { get; set; }
    public int LoyaltyPoints { get; set; } = 0;

    // Preferences (displayed prominently in UI)
    public int? PreferredTherapistId { get; set; }
    public string? PressurePreference { get; set; } // Light, Medium, Firm
    public string? TemperaturePreference { get; set; } // Cool, Warm, Hot
    public string? MusicPreference { get; set; } // Classical, Spa Music, Nature Sounds, Silence
    public string? Allergies { get; set; } // IMPORTANT: Red alert in UI
    public string? MedicalNotes { get; set; }
    public string? SpecialRequests { get; set; }

    // Emergency Contact
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }

    // Communication Preferences
    public string PreferredCommunicationChannel { get; set; } = "Email"; // Email, SMS, Both, None
    public bool SmsConsent { get; set; } = false;

    // Marketing
    public bool MarketingConsent { get; set; } = false;
    public string? ReferralSource { get; set; } // Walk-in, Facebook, Google, Referral, etc.

    // Tracking (updated on each transaction)
    public DateTime? FirstVisitDate { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public int TotalVisits { get; set; } = 0;
    public decimal TotalSpent { get; set; } = 0;

    // Segmentation (DBSCAN results)
    public string? CustomerSegment { get; set; } // VIP Platinum, Loyal Regulars, At-Risk, etc.
    public DateTime? SegmentAssignedDate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed property
    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public virtual Employee.Employee? PreferredTherapist { get; set; }
    public virtual ICollection<Appointment.Appointment> Appointments { get; set; } = new List<Appointment.Appointment>();
    public virtual ICollection<Transaction.Transaction> Transactions { get; set; } = new List<Transaction.Transaction>();
    public virtual ICollection<LoyaltyPointTransaction> LoyaltyPointTransactions { get; set; } = new List<LoyaltyPointTransaction>();
}
