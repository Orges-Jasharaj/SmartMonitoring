using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;

namespace IdentityService.Features.Roles.Handlers;

public class DeleteRoleHandler : IRequestHandler<DeleteRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditRecorder _auditRecorder;

    public DeleteRoleHandler(RoleManager<IdentityRole> roleManager, IAuditRecorder auditRecorder)
    {
        _roleManager = roleManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role == null)
        {
            await _auditRecorder.RecordAsync(
                "DeleteRole",
                "Failed",
                targetEntityType: "Role",
                detail: $"RoleNotFound:{request.RoleName}",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role not found");
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "DeleteRole",
                "Failed",
                targetEntityType: "Role",
                detail: request.RoleName,
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to delete role", errors);
        }

        await _auditRecorder.RecordAsync(
            "DeleteRole",
            "Success",
            targetEntityType: "Role",
            detail: request.RoleName,
            cancellationToken: cancellationToken);
        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role deleted");
    }
}
