using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VegaBase.Service.Infrastructure.DbActions;
using VegaBase.Service.Permissions;

namespace VegaBase.Service.Infrastructure;

/// <summary>
/// DI registration helpers for the VegaBase service layer. (F14)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core VegaBase services:
    /// <list type="bullet">
    /// <item><see cref="IDbActionExecutor"/> (Scoped) — requires a registered <see cref="DbContext"/> subtype.</item>
    /// <item><see cref="IPermissionCache"/> (Singleton) — must be Singleton; see <see cref="IPermissionCache"/> docs.</item>
    /// <item><see cref="IPasswordHasher"/> (Singleton) — Argon2id implementation.</item>
    /// <item><see cref="IHttpContextAccessor"/> — required for <c>CallerUsername</c> / <c>CallerRole</c> in services.</item>
    /// </list>
    /// <para>
    /// Call <see cref="AddVegaBaseDbContext{TContext}(IServiceCollection)"/> separately to register your
    /// <see cref="DbContext"/> subtype with VegaBase's <see cref="ConnectionStringHelper.Build"/> connection string.
    /// </para>
    /// </summary>
    public static IServiceCollection AddVegaBaseCore(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IDbActionExecutor, DbActionExecutor>();
        services.AddSingleton<IPermissionCache, PermissionCache>();
        services.AddSingleton<IPasswordHasher, Argon2idHasher>();
        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TContext"/> as a <see cref="DbContext"/> using VegaBase's
    /// <see cref="ConnectionStringHelper.Build"/> connection string (reads <c>DB_*</c> env vars).
    /// Automatically selects Npgsql or SQL Server based on <c>DB_IS_POSTGRESQL</c>.
    /// </summary>
    public static IServiceCollection AddVegaBaseDbContext<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        var connectionString = ConnectionStringHelper.Build();

        if (ConnectionStringHelper.IsPostgreSQL())
            services.AddDbContext<TContext>(o => o.UseNpgsql(connectionString));
        else
            services.AddDbContext<TContext>(o => o.UseSqlServer(connectionString));

        // Forward the base DbContext type so DbActionExecutor (which depends on DbContext) can be resolved.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<TContext>());

        return services;
    }
}
