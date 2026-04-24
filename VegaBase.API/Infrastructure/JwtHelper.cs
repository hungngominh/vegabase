// VegaBase.API/Infrastructure/JwtHelper.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using VegaBase.Service.Infrastructure;

namespace VegaBase.API.Infrastructure;

/// <summary>
/// Generates HS256-signed JWTs from the following configuration keys:
/// <list type="bullet">
/// <item><c>JWT_SECRET</c> — required; at least 32 UTF-8 bytes (HMAC-SHA256 minimum).</item>
/// <item><c>JWT_EXPIRY_HOURS</c> — optional; positive double up to 8760 (1 year). Defaults to 24.</item>
/// <item><c>JWT_ISSUER</c>, <c>JWT_AUDIENCE</c> — optional. If null, no iss/aud claims are emitted;
/// the consuming API must configure <see cref="TokenValidationParameters"/> accordingly
/// (e.g. <c>ValidateIssuer=false</c>) or validation will fail.</item>
/// </list>
/// <para>
/// <b>Startup note (F1):</b> call <c>builder.Configuration.AddEnvironmentVariables()</c> in
/// <c>Program.cs</c> so the above env-var keys are visible to <see cref="IConfiguration"/>.
/// ASP.NET Core 6+ includes this by default via <c>WebApplication.CreateBuilder</c>; earlier
/// hosts may need to add it explicitly.
/// </para>
/// </summary>
public class JwtHelper : IJwtHelper
{
    private const int MinSecretBytes = 32;
    private const double MaxExpiryHours = 8760; // 1 year
    private const double DefaultExpiryHours = 24;

    private readonly IConfiguration _config;

    public JwtHelper(IConfiguration config)
    {
        _config = config;
        var secret = config["JWT_SECRET"];
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("JWT_SECRET is not configured.");
        if (Encoding.UTF8.GetByteCount(secret) < MinSecretBytes)
            throw new InvalidOperationException(
                $"JWT_SECRET must be at least {MinSecretBytes} UTF-8 bytes (HMAC-SHA256 minimum).");
    }

    public string GenerateToken(string username, IEnumerable<(string Code, Guid Id)> roles)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, username),
        };

        foreach (var (code, id) in roles)
        {
            if (string.IsNullOrEmpty(code) || id == Guid.Empty) continue;
            claims.Add(new Claim("roleCode",      code));
            claims.Add(new Claim("roleId",        id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, code));
        }

        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT_SECRET"]!));
        var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var hours   = double.TryParse(
                          _config["JWT_EXPIRY_HOURS"],
                          System.Globalization.NumberStyles.Float,
                          System.Globalization.CultureInfo.InvariantCulture,
                          out var h) && h > 0 && h <= MaxExpiryHours
                      ? h : DefaultExpiryHours;
        var expires = DateTime.UtcNow.AddHours(hours);

        var token = new JwtSecurityToken(
            issuer:             _config["JWT_ISSUER"],
            audience:           _config["JWT_AUDIENCE"],
            claims:             claims,
            expires:            expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
