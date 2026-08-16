using IdentityService.Data;
using IdentityService.Data.Models;
using IdentityService.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Features.Users.Handlers;

public class GetUsersHandler : IRequestHandler<GetUsersQuery, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>>
{
    private readonly UserManager<User> _userManager;
    private readonly IdentityAppDbContext _dbContext;

    public GetUsersHandler(UserManager<User> userManager, IdentityAppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>.SuccessResponse([]);
        }

        var userIds = users.Select(u => u.Id).ToList();

        var roleMappings = await (
            from userRole in _dbContext.UserRoles
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rolesByUserId = roleMappings
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RoleName ?? string.Empty).Where(name => name.Length > 0).ToList());

        var list = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.isActive,
            Roles = rolesByUserId.TryGetValue(u.Id, out var roles) ? roles : []
        }).ToList();

        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<IEnumerable<UserDto>>.SuccessResponse(list);
    }
}
