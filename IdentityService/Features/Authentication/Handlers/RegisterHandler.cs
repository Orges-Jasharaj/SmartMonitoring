using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services.Email;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, ResponseDto<RegisterResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;

    public RegisterHandler(
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

    public async Task<ResponseDto<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTime.UtcNow,
            isActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "UserRegister",
                "Failed",
                actorUserName: request.UserName,
                targetEntityType: "User",
                detail: string.Join(';', errors.Select(e => e.ErrorMessage)),
                cancellationToken: cancellationToken);
            return ResponseDto<RegisterResponse>.Failure("User creation failed", errors);
        }

        if (!_emailOptions.Enabled)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }
        else
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationLink = $"{_emailOptions.ConfirmationBaseUrl}?userId={user.Id}&token={encodedToken}";

            var htmlBody = $"""
                <p>Hello {user.UserName},</p>
                <p>Thanks for registering with SmartMonitoring. Please confirm your email by clicking the link below:</p>
                <p><a href="{confirmationLink}">Confirm your email</a></p>
                <p>If you did not create this account, you can ignore this email.</p>
                """;

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Confirm your SmartMonitoring account",
                htmlBody,
                cancellationToken);
        }

        var response = new RegisterResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt,
            EmailConfirmationRequired = _emailOptions.Enabled
        };

        await _auditRecorder.RecordAsync(
            "UserRegister",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            detail: _emailOptions.Enabled ? "ConfirmationEmailSent" : "EmailConfirmationSkipped",
            cancellationToken: cancellationToken);

        var message = _emailOptions.Enabled
            ? "User created successfully. Please check your email to confirm your account."
            : "User created successfully";

        return ResponseDto<RegisterResponse>.SuccessResponse(response, message);
    }
}
