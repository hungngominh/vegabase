using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using VegaBase.API.Middleware;
using VegaBase.Service.Infrastructure;

namespace VegaBase.API.Infrastructure;

/// <summary>
/// DI registration helpers for the VegaBase API layer. (F13)
/// </summary>
public static class ApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers all VegaBase API services:
    /// <list type="bullet">
    /// <item>All services from <see cref="ServiceCollectionExtensions.AddVegaBaseCore"/>.</item>
    /// <item><see cref="IJwtHelper"/> (Singleton) — reads <c>JWT_SECRET</c> from <see cref="IConfiguration"/>.</item>
    /// </list>
    /// <para>
    /// Call <see cref="AddVegaBaseJwtAuthentication"/> to also configure ASP.NET Core JWT bearer
    /// authentication with the same settings.
    /// </para>
    /// </summary>
    public static IServiceCollection AddVegaBase(this IServiceCollection services)
    {
        services.AddVegaBaseCore();
        services.AddSingleton<IJwtHelper, JwtHelper>();
        return services;
    }

    /// <summary>
    /// Configures ASP.NET Core JWT bearer authentication using VegaBase's standard
    /// <c>JWT_SECRET</c> / <c>JWT_ISSUER</c> / <c>JWT_AUDIENCE</c> configuration keys.
    /// Call after <see cref="AddVegaBase"/> and before <c>app.UseAuthentication()</c>.
    /// </summary>
    public static IServiceCollection AddVegaBaseJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secret   = configuration["JWT_SECRET"]   ?? string.Empty;
        var issuer   = configuration["JWT_ISSUER"];
        var audience = configuration["JWT_AUDIENCE"];

        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException("JWT_SECRET is required for JWT authentication.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = key,
                    ValidateIssuer           = issuer   != null,
                    ValidIssuer              = issuer,
                    ValidateAudience         = audience != null,
                    ValidAudience            = audience,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.Zero,
                };
            });

        return services;
    }

    /// <summary>
    /// Registers <see cref="ExceptionHandlingMiddleware"/> in the pipeline.
    /// Call before <c>app.UseRouting()</c> / <c>app.MapControllers()</c>.
    /// </summary>
    public static IApplicationBuilder UseVegaBase(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    /// <summary>
    /// Registers <see cref="FlatQueryModelBinderProvider"/> so <c>[FromQuery] TParam</c>
    /// parameters derived from <see cref="VegaBase.Service.Models.BaseParamModel"/> are bound
    /// via flat query-string reflection rather than default complex-object binding.
    /// Call inside <c>builder.Services.AddControllers(o => ...)</c>.
    /// </summary>
    public static void AddVegaBaseModelBinder(this Microsoft.AspNetCore.Mvc.MvcOptions options)
    {
        options.ModelBinderProviders.Insert(0, new FlatQueryModelBinderProvider());
    }
}
