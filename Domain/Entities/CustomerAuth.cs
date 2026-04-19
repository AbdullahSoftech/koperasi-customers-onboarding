using Domain.Common;

namespace Domain.Entities;

public class CustomerAuth : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string PinHash { get; set; } = string.Empty;
    public bool IsBiometricEnabled { get; set; } = false;
    public string? BiometricToken { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Customer? Customer { get; set; }
}