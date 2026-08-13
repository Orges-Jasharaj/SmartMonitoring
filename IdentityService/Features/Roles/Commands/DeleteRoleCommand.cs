using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Roles.Commands
{
    public class DeleteRoleCommand : IRequest<ResponseDto<bool>>
    {
        public string RoleName { get; set; } = null!;
    }
}
