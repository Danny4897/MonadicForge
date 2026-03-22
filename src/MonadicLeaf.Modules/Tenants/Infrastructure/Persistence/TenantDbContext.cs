using Microsoft.EntityFrameworkCore;
using MonadicLeaf.Modules.Analyze.Domain.Entities;
using MonadicLeaf.Modules.Auth.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Domain.Entities;

namespace MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;

/// <summary>
/// Single DbContext for the whole application — one database, one migration history.
/// Both Tenants and Analyze modules write here; the API configures the connection string.
/// </summary>
public sealed class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AnalysisRecord> AnalysisRecords => Set<AnalysisRecord>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasMaxLength(128);
            entity.Property(t => t.Name).HasMaxLength(256).IsRequired();
            entity.Property(t => t.Plan).HasConversion<string>().HasMaxLength(32);
            entity.Property(t => t.StripeCustomerId).HasMaxLength(256);
            entity.Property(t => t.StripeSubscriptionId).HasMaxLength(256);
            entity.HasIndex(t => t.StripeCustomerId);
        });

        modelBuilder.Entity<AnalysisRecord>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(r => r.UserId).HasMaxLength(256).IsRequired();
            entity.Property(r => r.FileName).HasMaxLength(512);
            entity.HasIndex(r => new { r.TenantId, r.CreatedAt });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(u => u.Plan).HasConversion<string>().HasMaxLength(32);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.TenantId);
        });
    }
}
