using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Users.Commands
{
    public class DeactivateUserCommand : IRequest<ResponseDto<bool>>
    {
        public string Id { get; set; } = null!;
    }
}
