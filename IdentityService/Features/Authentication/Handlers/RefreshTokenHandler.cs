using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Dtos.Responses;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Authentication.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ResponseDto<JwtResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<RefreshTokenHandler> _logger;

    public RefreshTokenHandler(
        UserManager<User> userManager,
        IJwtService jwtService,
        ILogger<RefreshTokenHandler> logger)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<ResponseDto<JwtResult>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                u => u.RefreshToken == request.RefreshToken,
                cancellationToken);

        if (user is null
            || user.RefreshTokenExpiryTime is null
            || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            _logger.LogAuditEvent("RefreshToken", "Failed", detail: "InvalidOrExpiredToken");
            return ResponseDto<JwtResult>.Failure(
                "Invalid refresh token",
                [new ApiError { ErrorMessage = "Invalid refresh token" }]);
        }

        if (!user.isActive)
        {
            _logger.LogAuditEvent(
                "RefreshToken",
                "Failed",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                detail: "UserDeactivated");
            return ResponseDto<JwtResult>.Failure(
                "Invalid refresh token",
                [new ApiError { ErrorMessage = "Invalid refresh token" }]);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _jwtService.GenerateToken(user, roles);

        user.RefreshToken = jwt.RefreshToken;
        user.RefreshTokenExpiryTime = jwt.RefreshTokenExpiresAt;
        await _userManager.UpdateAsync(user);

        _logger.LogAuditEvent(
            "RefreshToken",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName);

        return ResponseDto<JwtResult>.SuccessResponse(jwt, "Token refreshed");
    }
}
