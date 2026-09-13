using AuditLogging.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuditLogging.Services;

public interface IAuditRetentionService
{
    Task<int> DeleteExpiredAuditLogsAsync(CancellationToken cancellationToken = default);
}

public class AuditRetentionService(
    AuditDbContext dbContext,
    IOptions<AuditRetentionOptions> options,
    ILogger<AuditRetentionService> logger) : IAuditRetentionService
{
    public async Task<int> DeleteExpiredAuditLogsAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled || settings.RetentionDays <= 0)
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        var deleted = await dbContext.AuditLogs
            .Where(log => log.OccurredAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation(
                "Audit retention cleanup removed {DeletedCount} logs older than {RetentionDays} days (before {CutoffUtc:u})",
                deleted,
                settings.RetentionDays,
                cutoff);
        }

        return deleted;
    }
}
