using MediatR;

namespace IdentityService.Features.Roles.Commands
{
    public class AssignRoleCommand : IRequest<bool>
    {
        public string UserId { get; set; } = null!;
        public string RoleName { get; set; } = null!;
    }
}
