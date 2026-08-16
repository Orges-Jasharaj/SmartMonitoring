using IdentityService.Data.Models;
using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Users.Handlers
{
    public class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<DeactivateUserHandler> _logger;

        public DeactivateUserHandler(UserManager<User> userManager, ILogger<DeactivateUserHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null)
            {
                _logger.LogAuditEvent("DeactivateUser", "Failed", targetUserId: request.Id, detail: "UserNotFound");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
            }

            user.isActive = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                _logger.LogAuditEvent("DeactivateUser", "Failed", targetUserId: user.Id, targetUserName: user.UserName, detail: "UpdateFailed");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to deactivate user", errors);
            }

            _logger.LogAuditEvent("DeactivateUser", "Success", targetUserId: user.Id, targetUserName: user.UserName);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "User deactivated");
        }
    }
}
