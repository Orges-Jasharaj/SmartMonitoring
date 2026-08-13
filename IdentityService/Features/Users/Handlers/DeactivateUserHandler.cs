using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using IdentityService.Data.Models;

namespace IdentityService.Features.Users.Handlers
{
    public class DeactivateUserHandler : IRequestHandler<DeactivateUserCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;

        public DeactivateUserHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");

            user.isActive = false;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to deactivate user", errors);
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "User deactivated");
        }
    }
}
