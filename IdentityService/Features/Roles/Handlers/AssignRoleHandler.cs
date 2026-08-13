using IdentityService.Data.Models;
using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace IdentityService.Features.Roles.Handlers
{
    public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AssignRoleHandler(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
            // Do not auto-create roles. Role must exist before assignment.
            if (!await _roleManager.RoleExistsAsync(request.RoleName))
            {
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role does not exist");
            }
            var result = await _userManager.AddToRoleAsync(user, request.RoleName);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to assign role", errors);
            }
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role assigned");
        }
    }
}
