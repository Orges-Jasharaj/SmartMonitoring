using AuditLogging.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Dtos.Responses;

namespace AuditLogging.Features.GetAudits;

public class GetAuditsHandler(AuditDbContext dbContext) : IRequestHandler<GetAuditsQuery, ResponseDto<PagedAuditLogsResult>>
{
    public async Task<ResponseDto<PagedAuditLogsResult>> Handle(GetAuditsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var query = dbContext.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
        {
            query = query.Where(x => x.ServiceName == request.ServiceName);
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            query = query.Where(x => x.EventType == request.EventType);
        }

        if (!string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            query = query.Where(x => x.ActorUserId == request.ActorUserId);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= request.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto
            {
                Id = x.Id,
                ServiceName = x.ServiceName,
                EventType = x.EventType,
                Outcome = x.Outcome,
                ActorUserId = x.ActorUserId,
                ActorUserName = x.ActorUserName,
                TargetEntityType = x.TargetEntityType,
                TargetEntityId = x.TargetEntityId,
                TargetUserName = x.TargetUserName,
                Detail = x.Detail,
                CorrelationId = x.CorrelationId,
                IpAddress = x.IpAddress,
                OccurredAtUtc = x.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);

        var result = new PagedAuditLogsResult
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return ResponseDto<PagedAuditLogsResult>.SuccessResponse(result);
    }
}
