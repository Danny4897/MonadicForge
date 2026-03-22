using System.Text.Json;
using MonadicLeaf.Modules.Analyze.Domain.ValueObjects;

namespace MonadicLeaf.Modules.Analyze.Domain.Entities;

/// <summary>Persisted result of a single analysis run. Owned by the requesting tenant.</summary>
public sealed class AnalysisRecord
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = default!;
    public string UserId { get; private set; } = default!;
    public string? FileName { get; private set; }
    public int GreenScore { get; private set; }
    public int FindingCount { get; private set; }
    public string FindingsJson { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private AnalysisRecord() { } // EF Core

    public static AnalysisRecord Create(
        string tenantId,
        string userId,
        string? fileName,
        int greenScore,
        IReadOnlyList<EnrichedFinding> findings) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            FileName = fileName,
            GreenScore = greenScore,
            FindingCount = findings.Count,
            FindingsJson = JsonSerializer.Serialize(findings),
            CreatedAt = DateTime.UtcNow
        };

    public IReadOnlyList<EnrichedFinding> GetFindings() =>
        JsonSerializer.Deserialize<List<EnrichedFinding>>(FindingsJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? [];
}
