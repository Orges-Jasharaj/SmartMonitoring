using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SmartMonitoring.Shared.Data;

public static class DatabaseMigrationExtensions
{
    private const int MaxAttempts = 15;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public static void MigrateWithRetry<TContext>(this TContext context, ILogger logger)
        where TContext : DbContext
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                context.Database.Migrate();
                logger.LogInformation("Database migrations applied.");
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Database migration attempt {Attempt}/{MaxAttempts} failed; retrying in {DelaySeconds}s.",
                    attempt,
                    MaxAttempts,
                    RetryDelay.TotalSeconds);
                Thread.Sleep(RetryDelay);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is SqlException sql)
            {
                // Transient connectivity / SQL-not-ready errors, plus 1801 when CREATE DATABASE races.
                if (sql.Number is -2 or 4060 or 18456 or 233 or 997 or 10061 or 1801)
                {
                    return true;
                }
            }

            if (current is IOException or TimeoutException)
            {
                return true;
            }
        }

        return false;
    }
}
