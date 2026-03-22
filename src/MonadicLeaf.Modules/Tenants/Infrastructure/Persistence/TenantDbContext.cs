using Microsoft.EntityFrameworkCore;
using MonadicLeaf.Modules.Tenants.Domain.Entities;

namespace MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;

public sealed class TenantDbContext : DbContext
{
    public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();

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
    }
}
