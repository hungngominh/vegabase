// VegaBase.Core/Common/ConnectionStringHelper.cs
namespace VegaBase.Core.Common;

public static class ConnectionStringHelper
{
    public static bool IsPostgreSQL()
    {
        var val = Environment.GetEnvironmentVariable("DB_IS_POSTGRESQL") ?? "true";
        return bool.TryParse(val, out var result) && result;
    }

    public static string Build()
    {
        var host     = Environment.GetEnvironmentVariable("DB_HOST")     ?? "localhost";
        var port     = Environment.GetEnvironmentVariable("DB_PORT")     ?? "5432";
        var db       = Environment.GetEnvironmentVariable("DB_NAME")     ?? "AppDB";
        var user     = Environment.GetEnvironmentVariable("DB_USER")     ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

        if (IsPostgreSQL())
            return $"Host={host};Port={port};Database={db};Username={user};Password={password};Maximum Pool Size=200";
        else
            return $"Server={host},{port};Database={db};User Id={user};Password={password};TrustServerCertificate=True;Max Pool Size=200";
    }
}
