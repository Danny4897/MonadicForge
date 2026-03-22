using MonadicLeaf.Modules.Analyze.Domain.Entities;
using MonadicLeaf.Modules.Analyze.Domain.ValueObjects;
using MonadicLeaf.SharedKernel.Context;
using MonadicSharp;

namespace MonadicLeaf.Modules.Analyze.Contracts;

/// <summary>Only public surface of the Analyze module.</summary>
public interface IAnalyzeService
{
    Task<Result<AnalyzeResult>> AnalyzeAsync(AnalyzeRequest request, LeafContext context);
    Task<Result<IReadOnlyList<AnalysisRecord>>> GetHistoryAsync(int page, int size, LeafContext context);
}

public sealed record AnalyzeRequest(string Code, string? FileName = null);

public sealed record AnalyzeResult(
    Guid RecordId,
    int GreenScore,
    IReadOnlyList<EnrichedFinding> Findings,
    string? FileName);
