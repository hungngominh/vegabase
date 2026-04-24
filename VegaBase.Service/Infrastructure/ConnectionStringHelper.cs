// VegaBase.Service/Infrastructure/ConnectionStringHelper.cs
using Microsoft.Data.SqlClient;
using Npgsql;

namespace VegaBase.Service.Infrastructure;

public static class ConnectionStringHelper
{
    private const int DefaultMaxPoolSize = 200;

    public static bool IsPostgreSQL()
    {
        var raw = Environment.GetEnvironmentVariable("DB_IS_POSTGRESQL");
        return raw switch
        {
            null                                            => true,
            _ when raw.Equals("true",  StringComparison.OrdinalIgnoreCase) => true,
            _ when raw.Equals("yes",   StringComparison.OrdinalIgnoreCase) => true,
            _ when raw.Equals("on",    StringComparison.OrdinalIgnoreCase) => true,
            _ when raw == "1"                                              => true,
            _ when raw.Equals("false", StringComparison.OrdinalIgnoreCase) => false,
            _ when raw.Equals("no",    StringComparison.OrdinalIgnoreCase) => false,
            _ when raw.Equals("off",   StringComparison.OrdinalIgnoreCase) => false,
            _ when raw == "0"                                              => false,
            _                                                              => true,
        };
    }

    public static string Build()
    {
        var host     = Environment.GetEnvironmentVariable("DB_HOST")     ?? "localhost";
        var port     = Environment.GetEnvironmentVariable("DB_PORT")     ?? "5432";
        var db       = Environment.GetEnvironmentVariable("DB_NAME")     ?? "AppDB";
        var user     = Environment.GetEnvironmentVariable("DB_USER")     ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

        if (string.IsNullOrEmpty(password))
            Console.Error.WriteLine("[VegaBase] WARNING: DB_PASSWORD is not set.");

        var rawPoolSize = Environment.GetEnvironmentVariable("DB_MAX_POOL_SIZE");
        var maxPool = int.TryParse(rawPoolSize, out var parsed) && parsed > 0
            ? parsed : DefaultMaxPoolSize;
        if (!string.IsNullOrEmpty(rawPoolSize) && (!int.TryParse(rawPoolSize, out var parsedCheck) || parsedCheck <= 0))
            Console.Error.WriteLine($"[VegaBase] WARNING: DB_MAX_POOL_SIZE '{rawPoolSize}' is invalid; using default {DefaultMaxPoolSize}.");

        var trustCert = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERTIFICATE");
        var trustServerCert = trustCert switch
        {
            _ when string.Equals(trustCert, "true",  StringComparison.OrdinalIgnoreCase) => true,
            _ when string.Equals(trustCert, "yes",   StringComparison.OrdinalIgnoreCase) => true,
            _ when trustCert == "1" => true,
            _ => false  // default false for production safety
        };

        if (IsPostgreSQL())
        {
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host            = host,
                Database        = db,
                Username        = user,
                Password        = password,
                MaxPoolSize     = maxPool,
            };
            if (int.TryParse(port, out var pgPort)) builder.Port = pgPort;
            return builder.ConnectionString;
        }
        else
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource             = $"{host},{port}",
                InitialCatalog         = db,
                UserID                 = user,
                Password               = password,
                TrustServerCertificate = trustServerCert,
                MaxPoolSize            = maxPool,
            };
            return builder.ConnectionString;
        }
    }
}
