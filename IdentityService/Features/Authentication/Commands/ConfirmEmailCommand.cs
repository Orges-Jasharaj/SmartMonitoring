using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Commands;

public class ConfirmEmailCommand : IRequest<ResponseDto<bool>>
{
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
}
