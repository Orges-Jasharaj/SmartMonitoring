using MediatR;

namespace IdentityService.Features.Roles.Commands
{
    public class CreateRoleCommand : IRequest<bool>
    {
        public string RoleName { get; set; } = null!;
    }
}
