using MediatR;
using System.Collections.Generic;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Users.Queries
{
    public class GetUsersQuery : IRequest<ResponseDto<IEnumerable<UserDto>>>
    {
    }

    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public bool IsActive { get; set; }
        public IEnumerable<string> Roles { get; set; } = new List<string>();
    }
}
