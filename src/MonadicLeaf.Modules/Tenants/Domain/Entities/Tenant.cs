using MonadicLeaf.SharedKernel.Plan;

namespace MonadicLeaf.Modules.Tenants.Domain.Entities;

public sealed class Tenant
{
    public string Id { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public PlanTier Plan { get; private set; }
    public int AnalysesUsedThisMonth { get; private set; }
    public DateTime PlanResetDate { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Tenant() { } // EF Core constructor

    public static Tenant Create(string id, string name, PlanTier plan = PlanTier.Free) =>
        new()
        {
            Id = id,
            Name = name,
            Plan = plan,
            AnalysesUsedThisMonth = 0,
            PlanResetDate = DateTime.UtcNow.AddMonths(1),
            CreatedAt = DateTime.UtcNow
        };

    /// <summary>Increments usage counter, resetting it first if the billing cycle has rolled over.</summary>
    public void IncrementUsage()
    {
        if (DateTime.UtcNow > PlanResetDate)
        {
            AnalysesUsedThisMonth = 0;
            PlanResetDate = DateTime.UtcNow.AddMonths(1);
        }
        AnalysesUsedThisMonth++;
    }

    public void UpdatePlan(PlanTier plan)
    {
        Plan = plan;
        AnalysesUsedThisMonth = 0;
        PlanResetDate = DateTime.UtcNow.AddMonths(1);
    }

    public void SetStripeIds(string customerId, string? subscriptionId = null)
    {
        StripeCustomerId = customerId;
        StripeSubscriptionId = subscriptionId;
    }

    /// <summary>Returns true if the tenant has hit the monthly analysis limit for their plan.</summary>
    public bool HasExceededMonthlyLimit()
    {
        var config = PlanLimits.Plans[Plan];
        if (config.AnalysesPerMonth == int.MaxValue) return false;
        if (DateTime.UtcNow > PlanResetDate) return false; // Will reset on next IncrementUsage call
        return AnalysesUsedThisMonth >= config.AnalysesPerMonth;
    }
}
