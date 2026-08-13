using IdentityService.Data.Models;
using IdentityService.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;


namespace IdentityService.Features.Users.Handlers
{
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>>
    {
        private readonly UserManager<User> _userManager;

        public GetUsersHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userManager.Users.ToListAsync(cancellationToken);

            var list = new List<UserDto>(users.Count);
            foreach (var u in users)
            {
                // Await roles sequentially to avoid concurrent DbContext operations
                var roles = await _userManager.GetRolesAsync(u);
                list.Add(new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    IsActive = u.isActive,
                    Roles = roles
                });
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>.SuccessResponse(list);
        }
    }
}
