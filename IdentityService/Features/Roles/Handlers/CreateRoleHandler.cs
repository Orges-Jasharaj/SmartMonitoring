using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Roles.Handlers
{
    public class CreateRoleHandler : IRequestHandler<CreateRoleCommand, bool>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateRoleHandler(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<bool> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            if (await _roleManager.RoleExistsAsync(request.RoleName)) return false;
            var result = await _roleManager.CreateAsync(new IdentityRole(request.RoleName));
            return result.Succeeded;
        }
    }
}
