using MediatR;
using NotificationService.Features.SendAlert;
using SmartMonitoring.Shared.Notifications;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpPost("alert")]
    public async Task<IActionResult> SendAlert([FromBody] AlertNotificationRequest request, CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new SendAlertNotificationCommand
        {
            Notification = request
        }, cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Ok(response);
    }
}
