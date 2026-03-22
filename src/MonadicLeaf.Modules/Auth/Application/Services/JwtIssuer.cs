using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MonadicLeaf.Modules.Auth.Domain.Entities;

namespace MonadicLeaf.Modules.Auth.Application.Services;

public sealed class JwtIssuer
{
    private readonly string _key;
    private const int ExpiryDays = 30;

    public JwtIssuer(IConfiguration config) =>
        _key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key required");

    public string Issue(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenantId", user.TenantId),
            new Claim("plan", user.Plan.ToString()),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(ExpiryDays),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
