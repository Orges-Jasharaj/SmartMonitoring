using IdentityService.Data;
using IdentityService.Data.Models;
using IdentityService.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Dtos;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Users.Handlers;

public class GetUsersHandler(UserManager<User> userManager, IdentityAppDbContext dbContext)
    : IRequestHandler<GetUsersQuery, ResponseDto<PagedResult<UserDto>>>
{
    public async Task<ResponseDto<PagedResult<UserDto>>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);

        var query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)) ||
                u.FirstName.Contains(term) ||
                u.LastName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return ResponseDto<PagedResult<UserDto>>.SuccessResponse(new PagedResult<UserDto>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        var userIds = users.Select(u => u.Id).ToList();

        var roleMappings = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, RoleName = role.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rolesByUserId = roleMappings
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.RoleName ?? string.Empty).Where(name => name.Length > 0).ToList());

        var items = users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.isActive,
            Roles = rolesByUserId.TryGetValue(u.Id, out var roles) ? roles : []
        }).ToList();

        return ResponseDto<PagedResult<UserDto>>.SuccessResponse(new PagedResult<UserDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }
}
