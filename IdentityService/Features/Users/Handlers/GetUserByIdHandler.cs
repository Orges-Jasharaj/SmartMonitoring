using IdentityService.Data.Models;
using IdentityService.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Users.Handlers
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly UserManager<User> _userManager;

        public GetUserByIdHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var u = await _userManager.FindByIdAsync(request.Id);
            if (u == null) return null;

            return new UserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.isActive
            };
        }
    }
}
