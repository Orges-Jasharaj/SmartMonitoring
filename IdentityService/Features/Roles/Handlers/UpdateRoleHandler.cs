using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Roles.Handlers
{
    public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<UpdateRoleHandler> _logger;

        public UpdateRoleHandler(RoleManager<IdentityRole> roleManager, ILogger<UpdateRoleHandler> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByNameAsync(request.RoleName);
            if (role == null)
            {
                _logger.LogAuditEvent("UpdateRole", "Failed", detail: $"RoleNotFound:{request.RoleName}");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role not found");
            }

            var existing = await _roleManager.FindByNameAsync(request.NewRoleName);
            if (existing != null)
            {
                _logger.LogAuditEvent("UpdateRole", "Failed", detail: $"TargetRoleExists:{request.NewRoleName}");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Target role name already exists");
            }

            role.Name = request.NewRoleName;
            var result = await _roleManager.UpdateAsync(role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                _logger.LogAuditEvent("UpdateRole", "Failed", detail: $"{request.RoleName}->{request.NewRoleName}");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to update role", errors);
            }

            _logger.LogAuditEvent("UpdateRole", "Success", detail: $"{request.RoleName}->{request.NewRoleName}");
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role updated");
        }
    }
}
