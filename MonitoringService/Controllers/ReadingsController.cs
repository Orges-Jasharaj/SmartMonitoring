using MonitoringService.Features.Readings.Commands;
using MonitoringService.Features.Readings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringService.Controllers;

[ApiController]
[Route("api/companies/{companyId:guid}/[controller]")]
[Authorize]
public class ReadingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByCompany(
        Guid companyId,
        [FromQuery] Guid? deviceId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int limit = 100)
    {
        var response = await mediator.Send(new GetReadingsQuery
        {
            CompanyId = companyId,
            DeviceId = deviceId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Limit = limit
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}

[ApiController]
[Route("api/ingest")]
public class IngestController(IMediator mediator) : ControllerBase
{
    public const string DeviceKeyHeader = "X-Device-Key";

    [HttpPost("readings")]
    [AllowAnonymous]
    public async Task<IActionResult> IngestReading([FromBody] IngestReadingRequest request)
    {
        if (!Request.Headers.TryGetValue(DeviceKeyHeader, out var deviceKey) || string.IsNullOrWhiteSpace(deviceKey))
        {
            return Unauthorized(SmartMonitoring.Shared.Dtos.Responses.ResponseDto<object>.Failure("Device key is required."));
        }

        var forcePersist = Request.Headers.ContainsKey("X-Force-Persist");

        var response = await mediator.Send(new IngestReadingCommand
        {
            DeviceKey = deviceKey.ToString(),
            TemperatureC = request.TemperatureC,
            HumidityPct = request.HumidityPct,
            Co2Ppm = request.Co2Ppm,
            LightLevelLux = request.LightLevelLux,
            NoiseLevelDb = request.NoiseLevelDb,
            BatteryLevelPct = request.BatteryLevelPct,
            MeasuredAtUtc = request.MeasuredAtUtc,
            ForcePersist = forcePersist
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}

public class IngestReadingRequest
{
    public decimal TemperatureC { get; set; }
    public decimal? HumidityPct { get; set; }
    public decimal? Co2Ppm { get; set; }
    public decimal? LightLevelLux { get; set; }
    public decimal? NoiseLevelDb { get; set; }
    public decimal? BatteryLevelPct { get; set; }
    public DateTime? MeasuredAtUtc { get; set; }
}
