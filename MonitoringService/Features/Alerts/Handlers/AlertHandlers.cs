using MonitoringService.Data;
using MonitoringService.Features.Alerts.Queries;
using MonitoringService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
