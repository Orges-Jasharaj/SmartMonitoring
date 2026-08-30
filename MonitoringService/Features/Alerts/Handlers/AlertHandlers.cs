using MonitoringService.Data;
using MonitoringService.Features.Alerts.Commands;
using MonitoringService.Features.Alerts.Queries;
using MonitoringService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Alerts.Handlers;

public class GetAlertsHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetAlertsQuery, ResponseDto<IReadOnlyList<AlertDto>>>
{
    public async Task<ResponseDto<IReadOnlyList<AlertDto>>> Handle(GetAlertsQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<IReadOnlyList<AlertDto>>.Failure("Company not found or access denied.");
        }

        var query = dbContext.Alerts.Where(a => a.CompanyId == request.CompanyId);
        if (request.ActiveOnly)
        {
            query = query.Where(a => a.IsActive);
        }

        var alerts = await query
            .OrderByDescending(a => a.TriggeredAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        return ResponseDto<IReadOnlyList<AlertDto>>.SuccessResponse(alerts.Select(a => new AlertDto
        {
            Id = a.Id,
            DeviceId = a.DeviceId,
            CompanyId = a.CompanyId,
            AlertType = a.AlertType,
            Message = a.Message,
            TemperatureC = a.TemperatureC,
            TriggeredAtUtc = a.TriggeredAtUtc,
            ResolvedAtUtc = a.ResolvedAtUtc,
            IsActive = a.IsActive
        }).ToList());
    }
}

public class AcknowledgeAlertHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess,
    ICurrentUserContext currentUser,
    IRealtimeNotifier realtimeNotifier,
    IAuditRecorder auditRecorder) : IRequestHandler<AcknowledgeAlertCommand, ResponseDto<AlertDto>>
{
    public async Task<ResponseDto<AlertDto>> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<AlertDto>.Failure("Company not found or access denied.");
        }

        var alert = await dbContext.Alerts
            .FirstOrDefaultAsync(
                a => a.Id == request.AlertId && a.CompanyId == request.CompanyId,
                cancellationToken);

        if (alert == null)
        {
            return ResponseDto<AlertDto>.Failure("Alert not found.");
        }

        if (!alert.IsActive)
        {
            return ResponseDto<AlertDto>.Failure("Alert is already resolved.");
        }

        alert.IsActive = false;
        alert.ResolvedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await realtimeNotifier.NotifyAlertsAsync(request.CompanyId, [alert], cancellationToken);

        await auditRecorder.RecordAsync(
            "AlertAcknowledged",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "Alert",
            targetEntityId: alert.Id.ToString(),
            detail: alert.AlertType,
            cancellationToken: cancellationToken);

        return ResponseDto<AlertDto>.SuccessResponse(Map(alert), "Alert acknowledged.");
    }

    private static AlertDto Map(Data.Models.Alert alert) => new()
    {
        Id = alert.Id,
        DeviceId = alert.DeviceId,
        CompanyId = alert.CompanyId,
        AlertType = alert.AlertType,
        Message = alert.Message,
        TemperatureC = alert.TemperatureC,
        TriggeredAtUtc = alert.TriggeredAtUtc,
        ResolvedAtUtc = alert.ResolvedAtUtc,
        IsActive = alert.IsActive
    };
}
