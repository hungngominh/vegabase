using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace VegaBase.Service.Infrastructure;

/// <summary>
/// Argon2id password hasher — OWASP 2023 minimums.
/// Stored format: Base64(salt[16] || hash[32]).
/// </summary>
public sealed class Argon2idHasher : IPasswordHasher
{
    private const int Parallelism = 4;
    private const int MemorySize  = 65536; // 64 MB
    private const int Iterations  = 3;
    private const int HashLength  = 32;    // 256-bit
    private const int SaltLength  = 16;    // 128-bit

    public string Hash(string plainPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);

        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(plainPassword));
        argon2.Salt                = salt;
        argon2.DegreeOfParallelism = Parallelism;
        argon2.MemorySize          = MemorySize;
        argon2.Iterations          = Iterations;

        var hash   = argon2.GetBytes(HashLength);
        var result = new byte[SaltLength + HashLength];
        Buffer.BlockCopy(salt, 0, result, 0,          SaltLength);
        Buffer.BlockCopy(hash, 0, result, SaltLength, HashLength);

        return Convert.ToBase64String(result);
    }

    public bool Verify(string plainPassword, string storedHash)
    {
        try
        {
            var storedBytes = Convert.FromBase64String(storedHash);
            if (storedBytes.Length != SaltLength + HashLength)
                return false;

            var salt       = storedBytes[..SaltLength];
            var storedHash_ = storedBytes[SaltLength..];

            using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(plainPassword));
            argon2.Salt                = salt;
            argon2.DegreeOfParallelism = Parallelism;
            argon2.MemorySize          = MemorySize;
            argon2.Iterations          = Iterations;

            var computed = argon2.GetBytes(HashLength);
            return CryptographicOperations.FixedTimeEquals(computed, storedHash_);
        }
        catch
        {
            return false;
        }
    }
}
