using MonitoringService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public interface IReadingRetentionService
{
    Task<int> DeleteExpiredReadingsAsync(CancellationToken cancellationToken = default);
}

public class ReadingRetentionService(
    MonitoringDbContext dbContext,
    IOptions<ReadingRetentionOptions> options,
    ILogger<ReadingRetentionService> logger) : IReadingRetentionService
{
    public async Task<int> DeleteExpiredReadingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled || settings.RetentionDays <= 0)
        {
            return 0;
        }

        var cutoff = DateTime.UtcNow.AddDays(-settings.RetentionDays);
        var deleted = await dbContext.TemperatureReadings
            .Where(r => r.MeasuredAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            logger.LogInformation(
                "Reading retention cleanup removed {DeletedCount} readings older than {RetentionDays} days (before {CutoffUtc:u})",
                deleted,
                settings.RetentionDays,
                cutoff);
        }

        return deleted;
    }
}
