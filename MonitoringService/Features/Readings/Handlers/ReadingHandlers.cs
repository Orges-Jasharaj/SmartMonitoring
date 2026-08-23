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
    IAuditRecorder auditRecorder) : IRequestHandler<IngestReadingCommand, ResponseDto<ReadingDto>>
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
        var reading = new TemperatureReading
        {
            Id = Guid.NewGuid(),
            DeviceId = device.Id,
            CompanyId = device.CompanyId,
            TemperatureC = request.TemperatureC,
            MeasuredAtUtc = measuredAt,
            ReceivedAtUtc = DateTime.UtcNow
        };

        device.LastReadingAtUtc = measuredAt;
        dbContext.TemperatureReadings.Add(reading);
        await alertEvaluator.EvaluateReadingAsync(device, request.TemperatureC, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditRecorder.RecordAsync(
            "ReadingIngested",
            "Success",
            targetEntityType: "Device",
            targetEntityId: device.Id.ToString(),
            detail: $"{request.TemperatureC}C",
            cancellationToken: cancellationToken);

        return ResponseDto<ReadingDto>.SuccessResponse(Map(reading), "Reading recorded");
    }

    internal static ReadingDto Map(TemperatureReading reading) => new()
    {
        Id = reading.Id,
        DeviceId = reading.DeviceId,
        CompanyId = reading.CompanyId,
        TemperatureC = reading.TemperatureC,
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
