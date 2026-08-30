using Microsoft.Extensions.Options;

namespace MonitoringService.Services;

public class ReadingRetentionCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReadingRetentionOptions> options,
    ILogger<ReadingRetentionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || settings.RetentionDays <= 0)
        {
            logger.LogInformation(
                "Reading retention cleanup is disabled (Enabled={Enabled}, RetentionDays={RetentionDays})",
                settings.Enabled,
                settings.RetentionDays);
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, settings.CleanupIntervalHours));
        logger.LogInformation(
            "Reading retention cleanup started. Retention={RetentionDays} days, interval={IntervalHours} hours",
            settings.RetentionDays,
            settings.CleanupIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Reading retention cleanup failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var retentionService = scope.ServiceProvider.GetRequiredService<IReadingRetentionService>();
        await retentionService.DeleteExpiredReadingsAsync(cancellationToken);
    }
}
