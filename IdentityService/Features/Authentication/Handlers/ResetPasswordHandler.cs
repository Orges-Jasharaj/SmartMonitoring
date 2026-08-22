using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResponseDto<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;

    public ResetPasswordHandler(UserManager<User> userManager, IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<ResponseDto<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            await _auditRecorder.RecordAsync(
                "ResetPassword",
                "Failed",
                targetEntityType: "User",
                targetEntityId: request.UserId,
                detail: "UserNotFound",
                cancellationToken: cancellationToken);
            return ResponseDto<bool>.Failure("Invalid password reset request");
        }

        if (!user.isActive)
        {
            await _auditRecorder.RecordAsync(
                "ResetPassword",
                "Failed",
                targetEntityType: "User",
                targetEntityId: user.Id,
                targetUserName: user.UserName,
                detail: "UserDeactivated",
                cancellationToken: cancellationToken);
            return ResponseDto<bool>.Failure("Invalid password reset request");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "ResetPassword",
                "Failed",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetEntityType: "User",
                targetEntityId: user.Id,
                detail: string.Join(';', errors.Select(e => e.ErrorMessage)),
                cancellationToken: cancellationToken);
            return ResponseDto<bool>.Failure("Password reset failed", errors);
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        await _auditRecorder.RecordAsync(
            "ResetPassword",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            cancellationToken: cancellationToken);

        return ResponseDto<bool>.SuccessResponse(true, "Password reset successfully");
    }
}
