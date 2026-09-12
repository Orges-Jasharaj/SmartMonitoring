using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Devices.Commands;

public class CreateDeviceCommand : IRequest<ResponseDto<DeviceCreatedDto>>
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public decimal MinHumidityPct { get; set; }
    public decimal MaxHumidityPct { get; set; }
    public decimal MinCo2Ppm { get; set; }
    public decimal MaxCo2Ppm { get; set; }
    public decimal MinLightLevelLux { get; set; }
    public decimal MaxLightLevelLux { get; set; }
    public decimal MinNoiseLevelDb { get; set; }
    public decimal MaxNoiseLevelDb { get; set; }
    public decimal MinBatteryPct { get; set; }
}

public class DeleteDeviceCommand : IRequest<ResponseDto<bool>>
{
    public Guid CompanyId { get; set; }
    public Guid DeviceId { get; set; }
}

public class UpdateDeviceCommand : IRequest<ResponseDto<DeviceDto>>
{
    public Guid CompanyId { get; set; }
    public Guid DeviceId { get; set; }
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public decimal MinHumidityPct { get; set; }
    public decimal MaxHumidityPct { get; set; }
    public decimal MinCo2Ppm { get; set; }
    public decimal MaxCo2Ppm { get; set; }
    public decimal MinLightLevelLux { get; set; }
    public decimal MaxLightLevelLux { get; set; }
    public decimal MinNoiseLevelDb { get; set; }
    public decimal MaxNoiseLevelDb { get; set; }
    public decimal MinBatteryPct { get; set; }
}

public class DeviceDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public decimal MinHumidityPct { get; set; }
    public decimal MaxHumidityPct { get; set; }
    public decimal MinCo2Ppm { get; set; }
    public decimal MaxCo2Ppm { get; set; }
    public decimal MinLightLevelLux { get; set; }
    public decimal MaxLightLevelLux { get; set; }
    public decimal MinNoiseLevelDb { get; set; }
    public decimal MaxNoiseLevelDb { get; set; }
    public decimal MinBatteryPct { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastReadingAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DeviceCreatedDto : DeviceDto
{
    public string DeviceKey { get; set; } = null!;
}

public class DeviceKeyDto
{
    public string DeviceKey { get; set; } = null!;
}
