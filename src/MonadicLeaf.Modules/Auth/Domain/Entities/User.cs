using MonadicLeaf.SharedKernel.Plan;

namespace MonadicLeaf.Modules.Auth.Domain.Entities;

public sealed class User
{
    public string Id { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string TenantId { get; private set; } = default!;
    public PlanTier Plan { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private User() { } // EF Core

    public static User Create(
        string email, string passwordHash, string tenantId, PlanTier plan = PlanTier.Free) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = passwordHash,
            TenantId = tenantId,
            Plan = plan,
            CreatedAt = DateTime.UtcNow
        };
}
