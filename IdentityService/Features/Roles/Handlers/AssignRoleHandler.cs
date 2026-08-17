using IdentityService.Data.Models;
using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;

namespace IdentityService.Features.Roles.Handlers;

public class AssignRoleHandler : IRequestHandler<AssignRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditRecorder _auditRecorder;

    public AssignRoleHandler(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(
        AssignRoleCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            await _auditRecorder.RecordAsync(
                "AssignRole",
                "Failed",
                targetEntityType: "User",
                targetEntityId: request.UserId,
                detail: "UserNotFound",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("User not found");
        }

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
        {
            await _auditRecorder.RecordAsync(
                "AssignRole",
                "Failed",
                targetEntityType: "User",
                targetEntityId: user.Id,
                targetUserName: user.UserName,
                detail: $"RoleNotFound:{request.RoleName}",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role does not exist");
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "AssignRole",
                "Failed",
                targetEntityType: "User",
                targetEntityId: user.Id,
                targetUserName: user.UserName,
                detail: request.RoleName,
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to assign role", errors);
        }

        await _auditRecorder.RecordAsync(
            "AssignRole",
            "Success",
            targetEntityType: "User",
            targetEntityId: user.Id,
            targetUserName: user.UserName,
            detail: request.RoleName,
            cancellationToken: cancellationToken);
        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role assigned");
    }
}
