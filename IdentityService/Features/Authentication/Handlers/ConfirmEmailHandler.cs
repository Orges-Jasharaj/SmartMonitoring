using IdentityService.Features.Authentication.Commands;
using IdentityService.Data.Models;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class ConfirmEmailHandler : IRequestHandler<ConfirmEmailCommand, ResponseDto<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;

    public ConfirmEmailHandler(UserManager<User> userManager, IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<ResponseDto<bool>> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            await _auditRecorder.RecordAsync(
                "EmailConfirm",
                "Failed",
                targetEntityType: "User",
                targetEntityId: request.UserId,
                detail: "UserNotFound",
                cancellationToken: cancellationToken);
            return ResponseDto<bool>.Failure("Invalid confirmation link");
        }

        if (user.EmailConfirmed)
        {
            return ResponseDto<bool>.SuccessResponse(true, "Email already confirmed");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "EmailConfirm",
                "Failed",
                actorUserId: user.Id,
                actorUserName: user.UserName,
                targetEntityType: "User",
                targetEntityId: user.Id,
                detail: string.Join(';', errors.Select(e => e.ErrorMessage)),
                cancellationToken: cancellationToken);
            return ResponseDto<bool>.Failure("Email confirmation failed", errors);
        }

        await _auditRecorder.RecordAsync(
            "EmailConfirm",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            cancellationToken: cancellationToken);

        return ResponseDto<bool>.SuccessResponse(true, "Email confirmed successfully");
    }
}
