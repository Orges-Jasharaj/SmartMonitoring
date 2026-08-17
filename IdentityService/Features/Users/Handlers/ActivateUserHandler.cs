using IdentityService.Data.Models;
using IdentityService.Features.Users.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;

namespace IdentityService.Features.Users.Handlers;

public class ActivateUserHandler : IRequestHandler<ActivateUserCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;

    public ActivateUserHandler(UserManager<User> userManager, IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(
        ActivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id);
        if (user == null)
        {
            await _auditRecorder.RecordAsync(
                "ActivateUser",
                "Failed",
                targetEntityType: "User",
                targetEntityId: request.Id,
                detail: "UserNotFound",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
        }

        user.isActive = true;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "ActivateUser",
                "Failed",
                targetEntityType: "User",
                targetEntityId: user.Id,
                targetUserName: user.UserName,
                detail: "UpdateFailed",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to activate user", errors);
        }

        await _auditRecorder.RecordAsync(
            "ActivateUser",
            "Success",
            targetEntityType: "User",
            targetEntityId: user.Id,
            targetUserName: user.UserName,
            cancellationToken: cancellationToken);
        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "User activated");
    }
}
