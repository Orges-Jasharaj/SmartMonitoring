using IdentityService.Data.Models;
using IdentityService.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Users.Handlers
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<UserDto?>>
    {
        private readonly UserManager<User> _userManager;

        public GetUserByIdHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<UserDto?>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var u = await _userManager.FindByIdAsync(request.Id);
            if (u == null) return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<UserDto?>.Failure("User not found");

            var roles = await _userManager.GetRolesAsync(u);

            var dto = new UserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.isActive,
                Roles = roles
            };

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<UserDto?>.SuccessResponse(dto);
        }
    }
}
