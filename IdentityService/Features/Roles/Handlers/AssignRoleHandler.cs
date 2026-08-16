using IdentityService.Data.Models;
using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Roles.Handlers
{
    public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AssignRoleHandler> _logger;

        public AssignRoleHandler(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AssignRoleHandler> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogAuditEvent("AssignRole", "Failed", targetUserId: request.UserId, detail: "UserNotFound");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
            }

            if (!await _roleManager.RoleExistsAsync(request.RoleName))
            {
                _logger.LogAuditEvent("AssignRole", "Failed", targetUserId: user.Id, targetUserName: user.UserName, detail: $"RoleNotFound:{request.RoleName}");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role does not exist");
            }

            var result = await _userManager.AddToRoleAsync(user, request.RoleName);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                _logger.LogAuditEvent("AssignRole", "Failed", targetUserId: user.Id, targetUserName: user.UserName, detail: request.RoleName);
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to assign role", errors);
            }

            _logger.LogAuditEvent("AssignRole", "Success", targetUserId: user.Id, targetUserName: user.UserName, detail: request.RoleName);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role assigned");
        }
    }
}
