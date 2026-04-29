using Application.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();
    public DbSet<CustomerAuth> CustomerAuths => Set<CustomerAuth>();
    public DbSet<PrivacyConsent> PrivacyConsents => Set<PrivacyConsent>();
    public DbSet<MigrationRecord> MigrationRecords => Set<MigrationRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<LoginSession> LoginSessions => Set<LoginSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(100);
            e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(15);
            e.Property(x => x.Email).IsRequired().HasMaxLength(100);
            e.Property(x => x.NationalId).IsRequired().HasMaxLength(20);
            e.Property(x => x.CustomerType).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.HasIndex(x => x.PhoneNumber).IsUnique();
            e.HasIndex(x => x.NationalId).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        // OtpRequest
        modelBuilder.Entity<OtpRequest>(e =>
        {
            e.ToTable("OtpRequests");
            e.HasKey(x => x.Id);
            e.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(15);
            e.Property(x => x.EmailAddress).HasMaxLength(100);
            e.Property(x => x.OtpCode).IsRequired().HasMaxLength(6);
            e.Property(x => x.Purpose).HasConversion<string>();
            e.HasIndex(x => x.PhoneNumber);
            e.HasOne(x => x.Customer)
             .WithMany(c => c.OtpRequests)
             .HasForeignKey(x => x.CustomerId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // CustomerAuth
        modelBuilder.Entity<CustomerAuth>(e =>
        {
            e.ToTable("CustomerAuths");
            e.HasKey(x => x.Id);
            e.Property(x => x.PinHash).IsRequired().HasMaxLength(256);
            e.Property(x => x.BiometricToken).HasMaxLength(500);
            e.HasOne(x => x.Customer)
             .WithOne(c => c.Auth)
             .HasForeignKey<CustomerAuth>(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PrivacyConsent
        modelBuilder.Entity<PrivacyConsent>(e =>
        {
            e.ToTable("PrivacyConsents");
            e.HasKey(x => x.Id);
            e.Property(x => x.PolicyVersion).IsRequired().HasMaxLength(20);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasOne(x => x.Customer)
             .WithMany(c => c.PrivacyConsents)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // MigrationRecord
        modelBuilder.Entity<MigrationRecord>(e =>
        {
            e.ToTable("MigrationRecords");
            e.HasKey(x => x.Id);
            e.Property(x => x.OldSystemRef).HasMaxLength(100);
            e.Property(x => x.MigrationStatus).IsRequired().HasMaxLength(20);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(m => m.Customer)
             .WithOne(c => c.Migration)
             .HasForeignKey<MigrationRecord>(m => m.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.IpAddress).HasMaxLength(45);
            e.HasOne(x => x.Customer)
             .WithMany(c => c.AuditLogs)
             .HasForeignKey(x => x.CustomerId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // LoginSession
        modelBuilder.Entity<LoginSession>(e =>
        {
            e.ToTable("LoginSessions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Customer)
             .WithMany(c => c.LoginSessions)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}