using MediatR;
using MonitoringService.Features.Devices.Commands;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Devices.Queries;

public class GetDevicesByCompanyQuery : IRequest<ResponseDto<IReadOnlyList<DeviceDto>>>
{
    public Guid CompanyId { get; set; }
}

public class GetDeviceByIdQuery : IRequest<ResponseDto<DeviceDto>>
{
    public Guid Id { get; set; }
}

public class GetDeviceKeyQuery : IRequest<ResponseDto<DeviceKeyDto>>
{
    public Guid CompanyId { get; set; }
    public Guid DeviceId { get; set; }
}
