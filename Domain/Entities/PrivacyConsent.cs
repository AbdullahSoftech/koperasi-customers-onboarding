using Domain.Common;

namespace Domain.Entities;

public class PrivacyConsent : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string PolicyVersion { get; set; } = string.Empty;
    public bool IsAccepted { get; set; }
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
}