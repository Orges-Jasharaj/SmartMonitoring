using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services.Email;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ResponseDto<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    private const string GenericSuccessMessage =
        "If an account exists for that user, a password reset email has been sent.";

    public ForgotPasswordHandler(
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

    public async Task<ResponseDto<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_emailOptions.Enabled)
        {
            return ResponseDto<bool>.Failure("Password reset emails are disabled");
        }

        var user = await _userManager.FindByNameAsync(request.EmailOrUserName)
            ?? await _userManager.FindByEmailAsync(request.EmailOrUserName);

        if (user == null || !user.isActive || string.IsNullOrWhiteSpace(user.Email))
        {
            await _auditRecorder.RecordAsync(
                "ForgotPassword",
                "Requested",
                actorUserName: request.EmailOrUserName,
                targetEntityType: "User",
                detail: "NoActionTaken",
                cancellationToken: cancellationToken);
            return ResponseDto<bool>.SuccessResponse(true, GenericSuccessMessage);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var resetLink = $"{_emailOptions.ResetPasswordBaseUrl}?userId={user.Id}&token={encodedToken}";

        var htmlBody = $"""
            <p>Hello {user.UserName},</p>
            <p>We received a request to reset your SmartMonitoring password.</p>
            <p><a href="{resetLink}">Reset your password</a></p>
            <p>If you did not request this, you can ignore this email.</p>
            """;

        await _emailSender.SendEmailAsync(
            user.Email,
            "Reset your SmartMonitoring password",
            htmlBody,
            cancellationToken);

        await _auditRecorder.RecordAsync(
            "ForgotPassword",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            detail: "ResetEmailSent",
            cancellationToken: cancellationToken);

        return ResponseDto<bool>.SuccessResponse(true, GenericSuccessMessage);
    }
}
