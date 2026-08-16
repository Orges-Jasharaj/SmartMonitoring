using IdentityService.Data.Models;
using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Users.Handlers
{
    public class ActivateUserHandler : IRequestHandler<ActivateUserCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ActivateUserHandler> _logger;

        public ActivateUserHandler(UserManager<User> userManager, ILogger<ActivateUserHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null)
            {
                _logger.LogAuditEvent("ActivateUser", "Failed", targetUserId: request.Id, detail: "UserNotFound");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
            }

            user.isActive = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                _logger.LogAuditEvent("ActivateUser", "Failed", targetUserId: user.Id, targetUserName: user.UserName, detail: "UpdateFailed");
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to activate user", errors);
            }

            _logger.LogAuditEvent("ActivateUser", "Success", targetUserId: user.Id, targetUserName: user.UserName);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "User activated");
        }
    }
}
