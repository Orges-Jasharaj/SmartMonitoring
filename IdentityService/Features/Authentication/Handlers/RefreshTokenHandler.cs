using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, ResponseDto<JwtResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IAuditRecorder _auditRecorder;

    public RefreshTokenHandler(
        UserManager<User> userManager,
        IJwtService jwtService,
        IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _auditRecorder = auditRecorder;
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
            await _auditRecorder.RecordAsync(
                "RefreshToken",
                "Failed",
                detail: "InvalidOrExpiredToken",
                cancellationToken: cancellationToken);
            return ResponseDto<JwtResult>.Failure(
                "Invalid refresh token",
                [new ApiError { ErrorMessage = "Invalid refresh token" }]);
        }

        if (!user.isActive)
        {
            await _auditRecorder.RecordAsync(
                "RefreshToken",
                "Failed",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetEntityType: "User",
                targetEntityId: user.Id,
                detail: "UserDeactivated",
                cancellationToken: cancellationToken);
            return ResponseDto<JwtResult>.Failure(
                "Invalid refresh token",
                [new ApiError { ErrorMessage = "Invalid refresh token" }]);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _jwtService.GenerateToken(user, roles);

        user.RefreshToken = jwt.RefreshToken;
        user.RefreshTokenExpiryTime = jwt.RefreshTokenExpiresAt;
        await _userManager.UpdateAsync(user);

        await _auditRecorder.RecordAsync(
            "RefreshToken",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            cancellationToken: cancellationToken);

        return ResponseDto<JwtResult>.SuccessResponse(jwt, "Token refreshed");
    }
}
