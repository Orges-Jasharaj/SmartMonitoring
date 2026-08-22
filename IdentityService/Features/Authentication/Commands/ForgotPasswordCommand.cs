using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Commands;

public class ForgotPasswordCommand : IRequest<ResponseDto<bool>>
{
    public string EmailOrUserName { get; set; } = null!;
}
