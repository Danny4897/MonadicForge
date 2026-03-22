using MonadicLeaf.Modules.Analyze.Application.Commands;
using MonadicLeaf.Modules.Analyze.Contracts;
using MonadicLeaf.Modules.Analyze.Domain.Entities;
using MonadicLeaf.Modules.Analyze.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicSharp;

namespace MonadicLeaf.Modules.Analyze;

public sealed class AnalyzeService : IAnalyzeService
{
    private readonly AnalyzeCodeCommand _analyzeCmd;
    private readonly AnalysisRepository _repo;

    public AnalyzeService(AnalyzeCodeCommand analyzeCmd, AnalysisRepository repo)
    {
        _analyzeCmd = analyzeCmd;
        _repo = repo;
    }

    public Task<Result<AnalyzeResult>> AnalyzeAsync(AnalyzeRequest request, LeafContext context) =>
        _analyzeCmd.ExecuteAsync(request, context);

    public Task<Result<IReadOnlyList<AnalysisRecord>>> GetHistoryAsync(
        int page, int size, LeafContext context) =>
        _repo.GetByTenantAsync(context.TenantId, page, size, context);
}
