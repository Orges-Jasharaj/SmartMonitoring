using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Users.Commands
{
    public class ActivateUserCommand : IRequest<ResponseDto<bool>>
    {
        public string Id { get; set; } = null!;
    }
}
