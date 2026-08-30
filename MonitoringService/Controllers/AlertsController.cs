using MonitoringService.Features.Alerts.Commands;
using MonitoringService.Features.Alerts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringService.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/[controller]")]
[Authorize]
public class AlertsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByCompany(Guid companyId, [FromQuery] bool activeOnly = true)
    {
        var response = await mediator.Send(new GetAlertsQuery
        {
            CompanyId = companyId,
            ActiveOnly = activeOnly
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("{alertId:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid companyId, Guid alertId)
    {
        var response = await mediator.Send(new AcknowledgeAlertCommand
        {
            CompanyId = companyId,
            AlertId = alertId
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}
