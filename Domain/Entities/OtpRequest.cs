using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class OtpRequest : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public string OtpCode { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public bool IsVerified { get; set; } = false;
    public int AttemptCount { get; set; } = 0;
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
}