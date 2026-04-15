namespace VegaBase.Service.Infrastructure;

/// <summary>
/// Abstraction for JWT token generation.
/// Defined in Service layer so UserService can depend on it without referencing API layer.
/// Implemented by VegaBase.API.Infrastructure.JwtHelper.
/// </summary>
public interface IJwtHelper
{
    string GenerateToken(string username, IEnumerable<(string Code, Guid Id)> roles);
}
