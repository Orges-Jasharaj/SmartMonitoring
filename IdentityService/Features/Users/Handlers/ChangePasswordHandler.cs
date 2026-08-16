using IdentityService.Data.Models;
using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Users.Handlers
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ChangePasswordHandler> _logger;

        public ChangePasswordHandler(UserManager<User> userManager, ILogger<ChangePasswordHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                _logger.LogAuditEvent("ChangePassword", "Failed", targetUserId: request.UserId, detail: "UserNotFound");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
            }

            if (!user.isActive)
            {
                _logger.LogAuditEvent("ChangePassword", "Failed", targetUserId: user.Id, targetUserName: user.UserName, detail: "UserDeactivated");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User is deactivated");
            }

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                _logger.LogAuditEvent("ChangePassword", "Failed", targetUserId: user.Id, targetUserName: user.UserName, detail: "ValidationFailed");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to change password", errors);
            }

            _logger.LogAuditEvent("ChangePassword", "Success", targetUserId: user.Id, targetUserName: user.UserName);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Password changed");
        }
    }
}
