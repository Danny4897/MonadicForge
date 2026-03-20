using MonadicForge.Migrator.Core;
using MonadicForge.Migrator.Rules;

namespace MonadicForge.Migrator.Tests.Rules;

public sealed class M004Tests
{
    private readonly MigrationEngine _engine = new([new M004_WrapNullReturnWithOption()]);

    [Fact]
    public void Converts_Null_Return_To_Option_None()
    {
        const string source = """
            class Repo
            {
                public Option<User> FindByEmail(string email)
                {
                    var user = _db.Users.FirstOrDefault(u => u.Email == email);
                    if (user is null)
                        return null;
                    return Option<User>.Some(user);
                }
            }
            """;

        var migrated = _engine.MigrateSource(source);

        Assert.True(migrated.IsSuccess);
        Assert.True(migrated.Value.HasChanges);
        Assert.Contains("Option<User>.None", migrated.Value.NewSource ?? string.Empty);
        Assert.DoesNotContain("return null", migrated.Value.NewSource ?? string.Empty);
    }

    [Fact]
    public void Does_Not_Modify_Methods_Without_Nullable_Return()
    {
        const string source = """
            class Service
            {
                public Result<int> Compute(int x) => Result<int>.Success(x * 2);
            }
            """;

        var migrated = _engine.MigrateSource(source);

        Assert.True(migrated.IsSuccess);
        Assert.False(migrated.Value.HasChanges);
    }
}
