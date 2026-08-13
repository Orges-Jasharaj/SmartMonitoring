using IdentityService.Data.Models;
using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Roles.Handlers
{
    public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, bool>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AssignRoleHandler(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return false;
            if (!await _roleManager.RoleExistsAsync(request.RoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(request.RoleName));
            }
            var result = await _userManager.AddToRoleAsync(user, request.RoleName);
            return result.Succeeded;
        }
    }
}
