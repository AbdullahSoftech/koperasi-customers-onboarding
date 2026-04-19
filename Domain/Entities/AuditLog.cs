using Domain.Common;

namespace Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IpAddress { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
}