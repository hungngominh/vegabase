namespace VegaBase.Service.Infrastructure;

/// <summary>
/// Abstraction for password hashing and verification.
/// Default implementation uses Argon2id (OWASP 2023 minimums).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password. Returns a Base64-encoded salt+hash blob.</summary>
    string Hash(string plainPassword);

    /// <summary>Verifies a plain-text password against a stored hash produced by <see cref="Hash"/>.</summary>
    bool Verify(string plainPassword, string storedHash);
}
