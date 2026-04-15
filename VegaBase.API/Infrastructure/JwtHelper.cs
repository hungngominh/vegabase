// VegaBase.API/Infrastructure/JwtHelper.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VegaBase.Service.Infrastructure;

namespace VegaBase.API.Infrastructure;

public class JwtHelper : IJwtHelper
{
    private readonly IConfiguration _config;

    public JwtHelper(IConfiguration config)
    {
        _config = config;
        if (string.IsNullOrEmpty(config["JWT_SECRET"]))
            throw new InvalidOperationException("JWT_SECRET is not configured.");
    }

    public string GenerateToken(string username, IEnumerable<(string Code, Guid Id)> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
        };

        foreach (var (code, id) in roles)
        {
            claims.Add(new Claim("roleCode",      code));
            claims.Add(new Claim("roleId",        id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, code));
        }

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT_SECRET"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(
            double.Parse(_config["JWT_EXPIRY_HOURS"] ?? "24", System.Globalization.CultureInfo.InvariantCulture));

        var token = new JwtSecurityToken(
            issuer:             _config["JWT_ISSUER"],
            audience:           _config["JWT_AUDIENCE"],
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
