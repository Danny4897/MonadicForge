using MonadicLeaf.Analyzer.Rules;

namespace MonadicLeaf.Analyzer.Core;

public static class DefaultRules
{
    public static IReadOnlyList<IAnalyzerRule> All { get; } =
    [
        new GC001_TryCatchInsideBind(),
        new GC002_MapOnFallibleOperation(),
        new GC003_ExpensiveBeforeValidation(),
        new GC004_RetryWithoutJitter(),
        new GC005_RetryWrapsValidation(),
        new GC006_LlmCallWithoutCache(),
        new GC007_LlmOutputWithoutValidation(),
        new GC008_OverGrantedCapability(),
        new GC009_MissingCircuitBreaker(),
        new GC010_SequenceOnLargeCollection(),
    ];
}
