using MonitoringService.Data;
using MonitoringService.Data.Models;
using MonitoringService.Features.Readings.Commands;
using MonitoringService.Features.Readings.Queries;
using MonitoringService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Readings.Handlers;

public class IngestReadingHandler(
    MonitoringDbContext dbContext,
    IAlertEvaluator alertEvaluator,
    IDeviceOfflineEvaluator deviceOfflineEvaluator,
    IAlertNotificationDispatcher alertNotificationDispatcher,
    IRealtimeNotifier realtimeNotifier,
    IAuditRecorder auditRecorder,
    IReadingPersistencePolicy readingPersistencePolicy) : IRequestHandler<IngestReadingCommand, ResponseDto<ReadingDto>>
{
    public async Task<ResponseDto<ReadingDto>> Handle(IngestReadingCommand request, CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.DeviceKey == request.DeviceKey && d.IsActive, cancellationToken);

        if (device == null)
        {
            await auditRecorder.RecordAsync(
                "ReadingIngested",
                "Failed",
                targetEntityType: "Device",
                detail: "InvalidDeviceKey",
                cancellationToken: cancellationToken);
            return ResponseDto<ReadingDto>.Failure("Invalid device key.");
        }

        var measuredAt = request.MeasuredAtUtc ?? DateTime.UtcNow;
        var receivedAt = DateTime.UtcNow;

        var lastPersistedReading = await dbContext.TemperatureReadings
            .Where(r => r.DeviceId == device.Id)
            .OrderByDescending(r => r.MeasuredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        device.LastReadingAtUtc = measuredAt;

        var metricAlerts = await alertEvaluator.EvaluateReadingAsync(device, request, cancellationToken);
        var offlineAlerts = await deviceOfflineEvaluator.EvaluateDeviceReadingAsync(device, cancellationToken);
        var alertsToNotify = metricAlerts.Concat(offlineAlerts).ToList();

        var shouldPersist = readingPersistencePolicy.ShouldPersist(
            device,
            request,
            lastPersistedReading,
            measuredAt,
            request.ForcePersist);

        TemperatureReading? reading = null;
        if (shouldPersist)
        {
            reading = new TemperatureReading
            {
                Id = Guid.NewGuid(),
                DeviceId = device.Id,
                CompanyId = device.CompanyId,
                TemperatureC = request.TemperatureC,
                HumidityPct = request.HumidityPct,
                Co2Ppm = request.Co2Ppm,
                LightLevelLux = request.LightLevelLux,
                NoiseLevelDb = request.NoiseLevelDb,
                BatteryLevelPct = request.BatteryLevelPct,
                MeasuredAtUtc = measuredAt,
                ReceivedAtUtc = receivedAt
            };

            dbContext.TemperatureReadings.Add(reading);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await alertNotificationDispatcher.DispatchAsync(device, alertsToNotify, cancellationToken);

        var readingDto = reading != null
            ? Map(reading)
            : MapEphemeral(device, request, measuredAt, receivedAt);

        await realtimeNotifier.NotifyReadingAsync(readingDto, cancellationToken);
        await realtimeNotifier.NotifyAlertsAsync(device.CompanyId, alertsToNotify, cancellationToken);

        var message = shouldPersist ? "Reading recorded" : "Reading accepted";
        return ResponseDto<ReadingDto>.SuccessResponse(readingDto, message);
    }

    internal static ReadingDto MapEphemeral(
        Device device,
        IngestReadingCommand request,
        DateTime measuredAtUtc,
        DateTime receivedAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        DeviceId = device.Id,
        CompanyId = device.CompanyId,
        TemperatureC = request.TemperatureC,
        HumidityPct = request.HumidityPct,
        Co2Ppm = request.Co2Ppm,
        LightLevelLux = request.LightLevelLux,
        NoiseLevelDb = request.NoiseLevelDb,
        BatteryLevelPct = request.BatteryLevelPct,
        MeasuredAtUtc = measuredAtUtc,
        ReceivedAtUtc = receivedAtUtc
    };

    internal static ReadingDto Map(TemperatureReading reading) => new()
    {
        Id = reading.Id,
        DeviceId = reading.DeviceId,
        CompanyId = reading.CompanyId,
        TemperatureC = reading.TemperatureC,
        HumidityPct = reading.HumidityPct,
        Co2Ppm = reading.Co2Ppm,
        LightLevelLux = reading.LightLevelLux,
        NoiseLevelDb = reading.NoiseLevelDb,
        BatteryLevelPct = reading.BatteryLevelPct,
        MeasuredAtUtc = reading.MeasuredAtUtc,
        ReceivedAtUtc = reading.ReceivedAtUtc
    };
}

public class GetReadingsHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetReadingsQuery, ResponseDto<IReadOnlyList<ReadingDto>>>
{
    public async Task<ResponseDto<IReadOnlyList<ReadingDto>>> Handle(GetReadingsQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<IReadOnlyList<ReadingDto>>.Failure("Company not found or access denied.");
        }

        var query = dbContext.TemperatureReadings
            .Where(r => r.CompanyId == request.CompanyId);

        if (request.DeviceId.HasValue)
        {
            query = query.Where(r => r.DeviceId == request.DeviceId.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(r => r.MeasuredAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(r => r.MeasuredAtUtc <= request.ToUtc.Value);
        }

        var limit = Math.Clamp(request.Limit, 1, 1000);
        var readings = await query
            .OrderByDescending(r => r.MeasuredAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return ResponseDto<IReadOnlyList<ReadingDto>>.SuccessResponse(
            readings.Select(IngestReadingHandler.Map).ToList());
    }
}
