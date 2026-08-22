using IdentityService.Data.Models;
using IdentityService.Features.Users.Commands;
using IdentityService.Services.Email;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Audit;

namespace IdentityService.Features.Users.Handlers;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    public ChangePasswordHandler(
        UserManager<User> userManager,
        IAuditRecorder auditRecorder,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions)
    {
        _userManager = userManager;
        _auditRecorder = auditRecorder;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(
        ChangePasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            await _auditRecorder.RecordAsync(
                "ChangePassword",
                "Failed",
                targetEntityType: "User",
                targetEntityId: request.UserId,
                detail: "UserNotFound",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
        }

        if (!user.isActive)
        {
            await _auditRecorder.RecordAsync(
                "ChangePassword",
                "Failed",
                targetEntityType: "User",
                targetEntityId: user.Id,
                targetUserName: user.UserName,
                detail: "UserDeactivated",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User is deactivated");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "ChangePassword",
                "Failed",
                targetEntityType: "User",
                targetEntityId: user.Id,
                targetUserName: user.UserName,
                detail: "ValidationFailed",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to change password", errors);
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);

        if (_emailOptions.Enabled && !string.IsNullOrWhiteSpace(user.Email))
        {
            var htmlBody = $"""
                <p>Hello {user.UserName},</p>
                <p>Your SmartMonitoring password was changed successfully.</p>
                <p>If you did not make this change, contact support immediately.</p>
                """;

            await _emailSender.SendEmailAsync(
                user.Email,
                "Your SmartMonitoring password was changed",
                htmlBody,
                cancellationToken);
        }

        await _auditRecorder.RecordAsync(
            "ChangePassword",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            cancellationToken: cancellationToken);
        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Password changed");
    }
}
