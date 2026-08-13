using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Roles.Commands
{
    public class UpdateRoleCommand : IRequest<ResponseDto<bool>>
    {
        public string RoleName { get; set; } = null!; // existing name
        public string NewRoleName { get; set; } = null!; // desired name
    }
}
