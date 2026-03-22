using MonadicLeaf.Modules.Auth.Application.Commands;
using MonadicLeaf.Modules.Auth.Contracts;
using MonadicSharp;

namespace MonadicLeaf.Modules.Auth;

public sealed class AuthService : IAuthService
{
    private readonly RegisterCommand _register;
    private readonly LoginCommand _login;

    public AuthService(RegisterCommand register, LoginCommand login)
    {
        _register = register;
        _login = login;
    }

    public Task<Result<AuthResponse>> RegisterAsync(string email, string password) =>
        _register.ExecuteAsync(email, password);

    public Task<Result<AuthResponse>> LoginAsync(string email, string password) =>
        _login.ExecuteAsync(email, password);
}
