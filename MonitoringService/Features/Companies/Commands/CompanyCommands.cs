using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Companies.Commands;

public class CreateCompanyCommand : IRequest<ResponseDto<CompanyDto>>
{
    public string Name { get; set; } = null!;
}

public class AssignCompanyUserCommand : IRequest<ResponseDto<CompanyUserDto>>
{
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = null!;
    public string Role { get; set; } = null!;
}

public class CompanyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class CompanyUserDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = null!;
    public string Role { get; set; } = null!;
    public DateTime AssignedAtUtc { get; set; }
}
