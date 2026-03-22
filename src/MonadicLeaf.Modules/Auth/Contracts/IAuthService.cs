using MonadicSharp;

namespace MonadicLeaf.Modules.Auth.Contracts;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(string email, string password);
    Task<Result<AuthResponse>> LoginAsync(string email, string password);
}

public sealed record AuthResponse(
    string Token,
    string Email,
    string TenantId,
    string Plan,
    int AnalysesUsedThisMonth,
    int AnalysesPerMonth);
