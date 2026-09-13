using Microsoft.Extensions.Options;

namespace AuditLogging.Services;

public class AuditRetentionCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<AuditRetentionOptions> options,
    ILogger<AuditRetentionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || settings.RetentionDays <= 0)
        {
            logger.LogInformation(
                "Audit retention cleanup is disabled (Enabled={Enabled}, RetentionDays={RetentionDays})",
                settings.Enabled,
                settings.RetentionDays);
            return;
        }

        var interval = TimeSpan.FromHours(Math.Max(1, settings.CleanupIntervalHours));
        logger.LogInformation(
            "Audit retention cleanup started. Retention={RetentionDays} days, interval={IntervalHours} hours",
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
                logger.LogError(ex, "Audit retention cleanup failed");
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
        var retentionService = scope.ServiceProvider.GetRequiredService<IAuditRetentionService>();
        await retentionService.DeleteExpiredAuditLogsAsync(cancellationToken);
    }
}
