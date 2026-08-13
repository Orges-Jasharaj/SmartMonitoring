using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace IdentityService.Features.Roles.Handlers
{
    public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public UpdateRoleHandler(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByNameAsync(request.RoleName);
            if (role == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role not found");

            // check if new name already exists
            var existing = await _roleManager.FindByNameAsync(request.NewRoleName);
            if (existing != null)
            {
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Target role name already exists");
            }

            role.Name = request.NewRoleName;
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to update role", errors);
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role updated");
        }
    }
}
