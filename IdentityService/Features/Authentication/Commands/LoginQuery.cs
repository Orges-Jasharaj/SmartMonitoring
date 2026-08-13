using MediatR;
using IdentityService.Services;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Commands
{
    public class LoginQuery : IRequest<ResponseDto<JwtResult>>
    {
        public string UserNameOrEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
