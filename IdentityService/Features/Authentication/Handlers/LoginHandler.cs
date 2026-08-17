using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class LoginHandler : IRequestHandler<LoginQuery, ResponseDto<JwtResult>>
{
    private readonly UserManager<User> _userManager;
    private readonly IJwtService _jwtService;
    private readonly IAuditRecorder _auditRecorder;

    public LoginHandler(
        UserManager<User> userManager,
        IJwtService jwtService,
        IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _jwtService = jwtService;
        _auditRecorder = auditRecorder;
    }

    public async Task<ResponseDto<JwtResult>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
            ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);

        if (user == null)
        {
            await _auditRecorder.RecordAsync(
                "UserLogin",
                "Failed",
                actorUserName: request.UserNameOrEmail,
                targetEntityType: "User",
                detail: "UserNotFound",
                cancellationToken: cancellationToken);
            return ResponseDto<JwtResult>.Failure(
                "Invalid credentials",
                [new ApiError { ErrorMessage = "Invalid credentials" }]);
        }

        if (!user.isActive)
        {
            await _auditRecorder.RecordAsync(
                "UserLogin",
                "Failed",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetEntityType: "User",
                targetEntityId: user.Id,
                detail: "UserDeactivated",
                cancellationToken: cancellationToken);
            return ResponseDto<JwtResult>.Failure(
                "Invalid credentials",
                [new ApiError { ErrorMessage = "Invalid credentials" }]);
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            await _auditRecorder.RecordAsync(
                "UserLogin",
                "Failed",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetEntityType: "User",
                targetEntityId: user.Id,
                detail: "InvalidPassword",
                cancellationToken: cancellationToken);
            return ResponseDto<JwtResult>.Failure(
                "Invalid credentials",
                [new ApiError { ErrorMessage = "Invalid credentials" }]);
        }

        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _jwtService.GenerateToken(user, roles);

        if (!string.IsNullOrEmpty(jwt.RefreshToken))
        {
            user.RefreshToken = jwt.RefreshToken;
            user.RefreshTokenExpiryTime = jwt.RefreshTokenExpiresAt;
            await _userManager.UpdateAsync(user);
        }

        await _auditRecorder.RecordAsync(
            "UserLogin",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            cancellationToken: cancellationToken);

        return ResponseDto<JwtResult>.SuccessResponse(jwt, "Login successful");
    }
}
