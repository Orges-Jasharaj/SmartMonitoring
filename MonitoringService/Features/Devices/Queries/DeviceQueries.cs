using MediatR;
using MonitoringService.Features.Devices.Commands;
using SmartMonitoring.Shared.Dtos;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Devices.Queries;

public class GetDevicesByCompanyQuery : IRequest<ResponseDto<PagedResult<DeviceDto>>>
{
    public Guid CompanyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = Pagination.DefaultPageSize;
    public string? Search { get; set; }
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
