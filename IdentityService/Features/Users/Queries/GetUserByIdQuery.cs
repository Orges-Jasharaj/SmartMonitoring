using MediatR;

namespace IdentityService.Features.Users.Queries
{
    public class GetUserByIdQuery : IRequest<UserDto?>
    {
        public string Id { get; set; } = null!;
    }
}
