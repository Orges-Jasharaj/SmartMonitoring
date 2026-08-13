using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Linq;

namespace IdentityService.Features.Roles.Handlers
{
    public class DeleteRoleHandler : IRequestHandler<DeleteRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public DeleteRoleHandler(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByNameAsync(request.RoleName);
            if (role == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role not found");

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to delete role", errors);
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role deleted");
        }
    }
}
