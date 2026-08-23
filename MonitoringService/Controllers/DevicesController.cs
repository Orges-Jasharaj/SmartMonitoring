using MonitoringService.Features.Devices.Commands;
using MonitoringService.Features.Devices.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringService.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/[controller]")]
[Authorize]
public class DevicesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Guid companyId, [FromBody] CreateDeviceRequest request)
    {
        var response = await mediator.Send(new CreateDeviceCommand
        {
            CompanyId = companyId,
            Name = request.Name,
            ZoneName = request.ZoneName,
            MinTempC = request.MinTempC,
            MaxTempC = request.MaxTempC
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetByCompany(Guid companyId)
    {
        var response = await mediator.Send(new GetDevicesByCompanyQuery { CompanyId = companyId });
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeviceDetailsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await mediator.Send(new GetDeviceByIdQuery { Id = id });
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }
}

public class CreateDeviceRequest
{
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
}
