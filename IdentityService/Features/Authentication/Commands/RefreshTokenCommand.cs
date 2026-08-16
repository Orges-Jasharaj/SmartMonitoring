using MediatR;
using IdentityService.Services;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Commands;

public class RefreshTokenCommand : IRequest<ResponseDto<JwtResult>>
{
    public string RefreshToken { get; set; } = null!;
}
