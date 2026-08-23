using AuditLogging.Features.GetAudits;
using AuditLogging.Features.RecordAudit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMonitoring.Shared.Audit;

namespace AuditLogging.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController(IMediator mediator) : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> Record([FromBody] AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new RecordAuditCommand { AuditEvent = auditEvent }, cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? serviceName,
        [FromQuery] string? eventType,
        [FromQuery] string? actorUserId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await mediator.Send(new GetAuditsQuery
        {
            ServiceName = serviceName,
            EventType = eventType,
            ActorUserId = actorUserId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(response);
    }
}
