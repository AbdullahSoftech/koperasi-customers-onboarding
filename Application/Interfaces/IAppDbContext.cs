using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<OtpRequest> OtpRequests { get; }
    DbSet<CustomerAuth> CustomerAuths { get; }
    DbSet<PrivacyConsent> PrivacyConsents { get; }
    DbSet<MigrationRecord> MigrationRecords { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<LoginSession> LoginSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}