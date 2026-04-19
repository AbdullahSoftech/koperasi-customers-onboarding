using Domain.Common;

namespace Domain.Entities;

public class LoginSession : BaseEntity
{
    public Guid CustomerId { get; set; }
    public bool IsPhoneOtpVerified { get; set; } = false;
    public bool IsEmailOtpVerified { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    public DateTime ExpiresAt { get; set; }

    // Navigation
    public virtual Customer? Customer { get; set; }
}