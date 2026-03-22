using Microsoft.EntityFrameworkCore;
using MonadicLeaf.Modules.Analyze.Domain.Entities;
using MonadicLeaf.Modules.Tenants.Infrastructure.Persistence;
using MonadicLeaf.SharedKernel.Context;
using MonadicSharp;

namespace MonadicLeaf.Modules.Analyze.Infrastructure.Persistence;

public sealed class AnalysisRepository
{
    private readonly TenantDbContext _db;

    public AnalysisRepository(TenantDbContext db) => _db = db;

    // Green-code: Try.ExecuteAsync at every I/O boundary
    public Task<Result<AnalysisRecord>> SaveAsync(AnalysisRecord record, LeafContext context) =>
        Try.ExecuteAsync(async () =>
        {
            _db.AnalysisRecords.Add(record);
            await _db.SaveChangesAsync(context.CancellationToken);
            return record;
        });

    public Task<Result<IReadOnlyList<AnalysisRecord>>> GetByTenantAsync(
        string tenantId, int page, int size, LeafContext context) =>
        Try.ExecuteAsync(async () =>
        {
            var records = await _db.AnalysisRecords
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync(context.CancellationToken);
            return (IReadOnlyList<AnalysisRecord>)records;
        });
}
