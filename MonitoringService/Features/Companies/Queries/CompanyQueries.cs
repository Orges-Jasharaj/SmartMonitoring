using MediatR;
using MonitoringService.Features.Companies.Commands;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Companies.Queries;

public class GetCompaniesQuery : IRequest<ResponseDto<IReadOnlyList<CompanyDto>>>
{
}

public class GetCompanyByIdQuery : IRequest<ResponseDto<CompanyDto>>
{
    public Guid Id { get; set; }
}

public class GetCompanyUsersQuery : IRequest<ResponseDto<IReadOnlyList<CompanyUserDto>>>
{
    public Guid CompanyId { get; set; }
}
