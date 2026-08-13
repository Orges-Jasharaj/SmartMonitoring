using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using IdentityService.Data.Models;

namespace IdentityService.Features.Users.Handlers
{
    public class ActivateUserHandler : IRequestHandler<ActivateUserCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly UserManager<User> _userManager;

        public ActivateUserHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(ActivateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.Id);
            if (user == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");

            user.isActive = true;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to activate user", errors);
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "User activated");
        }
    }
}
