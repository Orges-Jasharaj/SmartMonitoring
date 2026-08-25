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
}

public class DeleteDeviceCommand : IRequest<ResponseDto<bool>>
{
    public Guid CompanyId { get; set; }
    public Guid DeviceId { get; set; }
}

public class DeviceDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = null!;
    public string ZoneName { get; set; } = null!;
    public decimal MinTempC { get; set; }
    public decimal MaxTempC { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastReadingAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DeviceCreatedDto : DeviceDto
{
    public string DeviceKey { get; set; } = null!;
}
