namespace MonadicLeaf.SharedKernel.Plan;

public static class PlanLimits
{
    public static readonly IReadOnlyDictionary<PlanTier, PlanConfig> Plans =
        new Dictionary<PlanTier, PlanConfig>
        {
            [PlanTier.Free] = new(
                AnalysesPerMonth: 50,
                GenerateEnabled: false,
                ReviewEnabled: false,
                ExplainEnabled: true,
                VsCodeEnabled: false,
                MaxCodeLength: 5_000),

            [PlanTier.Pro] = new(
                AnalysesPerMonth: int.MaxValue,
                GenerateEnabled: true,
                ReviewEnabled: false,
                ExplainEnabled: true,
                VsCodeEnabled: true,
                MaxCodeLength: 50_000),

            [PlanTier.Team] = new(
                AnalysesPerMonth: int.MaxValue,
                GenerateEnabled: true,
                ReviewEnabled: true,
                ExplainEnabled: true,
                VsCodeEnabled: true,
                MaxCodeLength: 100_000),

            [PlanTier.Enterprise] = new(
                AnalysesPerMonth: int.MaxValue,
                GenerateEnabled: true,
                ReviewEnabled: true,
                ExplainEnabled: true,
                VsCodeEnabled: true,
                MaxCodeLength: int.MaxValue),
        };
}
