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
        if (raw is null) return true;
        if (raw.Equals("true",  StringComparison.OrdinalIgnoreCase) || raw.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("on",    StringComparison.OrdinalIgnoreCase) || raw == "1") return true;
        if (raw.Equals("false", StringComparison.OrdinalIgnoreCase) || raw.Equals("no",  StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("off",   StringComparison.OrdinalIgnoreCase) || raw == "0") return false;
        Console.Error.WriteLine($"[VegaBase] WARNING: DB_IS_POSTGRESQL '{raw}' is not recognized; defaulting to PostgreSQL.");
        return true;
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
            if (int.TryParse(port, out var pgPort) && pgPort is >= 1 and <= 65535)
                builder.Port = pgPort;
            else
                Console.Error.WriteLine($"[VegaBase] WARNING: DB_PORT '{port}' is not a valid port number (1-65535); using Npgsql default.");
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
