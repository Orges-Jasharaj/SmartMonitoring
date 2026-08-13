using MediatR;
using IdentityService.Services;

namespace IdentityService.Features.Authentication.Commands
{
    public class LoginQuery : IRequest<JwtResult>
    {
        public string UserNameOrEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
