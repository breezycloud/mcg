using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace Api.Context;

// Used only by `dotnet ef` CLI commands (migrations add/list/etc.) — never
// invoked at app runtime. Program.cs's normal startup pulls in RabbitMQ, the
// custom DatabaseLogger (which itself logs to Postgres and recurses into a
// stack overflow if the DB connection fails), and other services that make
// it an unreliable host for EF design-time tooling. This factory builds a
// minimal, isolated DbContext instead.
//
// Connection string resolution: ConnectionStrings__Local env var if set,
// otherwise falls back to local Docker Postgres with an empty password (same
// placeholder as appsettings.json — override via env var for a real password).
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Must match Program.cs's real UseNpgsql setup exactly — anything that
        // affects Npgsql's CLR<->PG type mapping can change the computed EF
        // model, which trips EF 9's PendingModelChangesWarning at runtime if
        // the design-time model (used to generate migrations) doesn't match.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var connString = Environment.GetEnvironmentVariable("ConnectionStrings__Local")
            ?? "Host=localhost;Port=5432;Username=postgres;Password=;Database=mcg_db";

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(dataSource, o => { o.SetPostgresVersion(16, 4); o.EnableRetryOnFailure(); });

        return new AppDbContext(optionsBuilder.Options);
    }
}
