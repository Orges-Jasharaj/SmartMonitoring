using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Roles.Handlers
{
    public class DeleteRoleHandler : IRequestHandler<DeleteRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DeleteRoleHandler> _logger;

        public DeleteRoleHandler(RoleManager<IdentityRole> roleManager, ILogger<DeleteRoleHandler> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleManager.FindByNameAsync(request.RoleName);
            if (role == null)
            {
                _logger.LogAuditEvent("DeleteRole", "Failed", detail: $"RoleNotFound:{request.RoleName}");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role not found");
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                _logger.LogAuditEvent("DeleteRole", "Failed", detail: request.RoleName);
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to delete role", errors);
            }

            _logger.LogAuditEvent("DeleteRole", "Success", detail: request.RoleName);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role deleted");
        }
    }
}
