using MediatR;
using SmartMonitoring.Shared.Dtos;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Users.Queries;

public class GetUsersQuery : IRequest<ResponseDto<PagedResult<UserDto>>>
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = Pagination.DefaultPageSize;

    public string? Search { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public bool IsActive { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
}
