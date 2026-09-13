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
            MaxTempC = request.MaxTempC,
            MinHumidityPct = request.MinHumidityPct,
            MaxHumidityPct = request.MaxHumidityPct,
            MinCo2Ppm = request.MinCo2Ppm,
            MaxCo2Ppm = request.MaxCo2Ppm,
            MinLightLevelLux = request.MinLightLevelLux,
            MaxLightLevelLux = request.MaxLightLevelLux,
            MinNoiseLevelDb = request.MinNoiseLevelDb,
            MaxNoiseLevelDb = request.MaxNoiseLevelDb,
            MinBatteryPct = request.MinBatteryPct
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetByCompany(
        Guid companyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null)
    {
        var response = await mediator.Send(new GetDevicesByCompanyQuery
        {
            CompanyId = companyId,
            Page = page,
            PageSize = pageSize,
            Search = search
        });
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{deviceId:guid}/key")]
    public async Task<IActionResult> GetKey(Guid companyId, Guid deviceId)
    {
        var response = await mediator.Send(new GetDeviceKeyQuery
        {
            CompanyId = companyId,
            DeviceId = deviceId
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpDelete("{deviceId:guid}")]
    public async Task<IActionResult> Delete(Guid companyId, Guid deviceId)
    {
        var response = await mediator.Send(new DeleteDeviceCommand
        {
            CompanyId = companyId,
            DeviceId = deviceId
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPut("{deviceId:guid}")]
    public async Task<IActionResult> Update(Guid companyId, Guid deviceId, [FromBody] UpdateDeviceRequest request)
    {
        var response = await mediator.Send(new UpdateDeviceCommand
        {
            CompanyId = companyId,
            DeviceId = deviceId,
            Name = request.Name,
            ZoneName = request.ZoneName,
            MinTempC = request.MinTempC,
            MaxTempC = request.MaxTempC,
            MinHumidityPct = request.MinHumidityPct,
            MaxHumidityPct = request.MaxHumidityPct,
            MinCo2Ppm = request.MinCo2Ppm,
            MaxCo2Ppm = request.MaxCo2Ppm,
            MinLightLevelLux = request.MinLightLevelLux,
            MaxLightLevelLux = request.MaxLightLevelLux,
            MinNoiseLevelDb = request.MinNoiseLevelDb,
            MaxNoiseLevelDb = request.MaxNoiseLevelDb,
            MinBatteryPct = request.MinBatteryPct
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}

[ApiController]
[Route("api/devices")]
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

public class DeviceThresholdRequest
{
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public decimal MinHumidityPct { get; set; } = 40;
    public decimal MaxHumidityPct { get; set; } = 70;
    public decimal MinCo2Ppm { get; set; } = 400;
    public decimal MaxCo2Ppm { get; set; } = 1000;
    public decimal MinLightLevelLux { get; set; } = 0;
    public decimal MaxLightLevelLux { get; set; } = 500;
    public decimal MinNoiseLevelDb { get; set; } = 35;
    public decimal MaxNoiseLevelDb { get; set; } = 70;
    public decimal MinBatteryPct { get; set; } = 20;
}

public class CreateDeviceRequest : DeviceThresholdRequest
{
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
}

public class UpdateDeviceRequest : DeviceThresholdRequest
{
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
}
