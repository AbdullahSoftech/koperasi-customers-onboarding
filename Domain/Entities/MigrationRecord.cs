using Domain.Common;

namespace Domain.Entities;

public class MigrationRecord : BaseEntity
{
    public Guid CustomerId { get; set; }
    public string? OldSystemRef { get; set; }
    public string MigrationStatus { get; set; } = "PENDING";
    public DateTime? MigratedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Customer? Customer { get; set; }
}