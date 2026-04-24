using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;

namespace VegaBase.API.Infrastructure;

/// <summary>
/// Configures a fixed-window rate-limiter policy for CPU-bound authentication endpoints
/// (e.g. Argon2id login) to prevent DoS via deliberate Argon2 computation. (A6)
/// <para>
/// Usage in Program.cs:
/// <code>
/// builder.Services.AddVegaBaseRateLimiting();
/// // ...
/// app.UseRateLimiter();
/// </code>
/// Apply to auth endpoints:
/// <code>
/// [EnableRateLimiting(VegaBaseRateLimiting.AuthPolicy)]
/// public async Task&lt;IActionResult&gt; Login(...)
/// </code>
/// </para>
/// </summary>
public static class VegaBaseRateLimiting
{
    /// <summary>Policy name for CPU-bound auth endpoints (login, password change).</summary>
    public const string AuthPolicy = "vegabase-auth";

    /// <summary>Default: 5 requests per 60-second window, partitioned per client IP.</summary>
    public static readonly RateLimitOptions DefaultAuthOptions = new()
    {
        PermitLimit    = 5,
        WindowSeconds  = 60,
        QueueLimit     = 0,
    };

    /// <summary>Registers the rate-limiter policy. Call before <c>app.UseRateLimiter()</c>.</summary>
    public static IServiceCollection AddVegaBaseRateLimiting(
        this IServiceCollection services,
        Action<RateLimitOptions>? configure = null)
    {
        var options = new RateLimitOptions();
        DefaultAuthOptions.CopyTo(options);
        configure?.Invoke(options);

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window      = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit  = options.QueueLimit,
                    }));

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }
}

/// <summary>Options for <see cref="VegaBaseRateLimiting.AddVegaBaseRateLimiting"/>.</summary>
public sealed class RateLimitOptions
{
    /// <summary>Maximum requests per window per client IP. Default: 5.</summary>
    public int PermitLimit { get; set; } = 5;

    /// <summary>Window length in seconds. Default: 60.</summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>Maximum queued requests after limit is hit. Default: 0 (no queue, immediate 429).</summary>
    public int QueueLimit { get; set; } = 0;

    internal void CopyTo(RateLimitOptions target)
    {
        target.PermitLimit   = PermitLimit;
        target.WindowSeconds = WindowSeconds;
        target.QueueLimit    = QueueLimit;
    }
}
