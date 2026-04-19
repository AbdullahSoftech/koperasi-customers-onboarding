using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public CustomerType CustomerType { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Pending;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties — all in same namespace, no extra using needed
    public virtual CustomerAuth? Auth { get; set; }
    public virtual ICollection<OtpRequest> OtpRequests { get; set; } = new List<OtpRequest>();
    public virtual ICollection<PrivacyConsent> PrivacyConsents { get; set; } = new List<PrivacyConsent>();
    public virtual ICollection<MigrationRecord> Migrations { get; set; } = new List<MigrationRecord>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<LoginSession> LoginSessions { get; set; } = new List<LoginSession>();
}