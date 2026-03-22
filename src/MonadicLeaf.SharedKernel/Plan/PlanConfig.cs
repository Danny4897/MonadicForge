namespace MonadicLeaf.SharedKernel.Plan;

public sealed record PlanConfig(
    int AnalysesPerMonth,
    bool GenerateEnabled,
    bool ReviewEnabled,
    bool ExplainEnabled,
    bool VsCodeEnabled,
    int MaxCodeLength);
