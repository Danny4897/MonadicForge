using Microsoft.CodeAnalysis.CSharp;
using MonadicLeaf.Migrator.Core;
using MonadicLeaf.Migrator.Rules;

namespace MonadicLeaf.Migrator.Tests.Rules;

public sealed class M001Tests
{
    private readonly MigrationEngine _engine = new([new M001_ConvertTryCatchToBind()]);

    [Fact]
    public void Converts_TryCatch_Pattern_To_TryExecuteAsync()
    {
        const string source = """
            public async Task<Result<Order>> GetOrder(int id)
            {
                try
                {
                    return Result<Order>.Success(await _repo.FindAsync(id));
                }
                catch (Exception ex)
                {
                    return Result<Order>.Failure(Error.FromException(ex));
                }
            }
            """;

        var result = _engine.MigrateSource(source);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasChanges);
        Assert.Contains("Try.ExecuteAsync", result.Value.NewSource ?? string.Empty);
        Assert.Contains("M001", result.Value.AppliedRules);
    }

    [Fact]
    public void Is_Idempotent_When_Already_Using_TryExecuteAsync()
    {
        const string source = """
            public Task<Result<Order>> GetOrder(int id) =>
                Try.ExecuteAsync(() => _repo.FindAsync(id));
            """;

        var result = _engine.MigrateSource(source);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasChanges);
    }
}
