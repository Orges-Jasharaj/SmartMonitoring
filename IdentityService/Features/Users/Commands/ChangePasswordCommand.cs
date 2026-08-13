using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Users.Commands
{
    public class ChangePasswordCommand : IRequest<ResponseDto<bool>>
    {
        public string UserId { get; set; } = null!;
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
