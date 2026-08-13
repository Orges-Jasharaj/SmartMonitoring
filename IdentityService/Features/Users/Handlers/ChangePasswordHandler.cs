using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using IdentityService.Data.Models;

namespace IdentityService.Features.Users.Handlers
{
    public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;

        public ChangePasswordHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");

            if (!user.isActive) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User is deactivated");

            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to change password", errors);
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Password changed");
        }
    }
}
