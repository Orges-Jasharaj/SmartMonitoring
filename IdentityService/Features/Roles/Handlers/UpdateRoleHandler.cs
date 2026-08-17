using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;

namespace IdentityService.Features.Roles.Handlers;

public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditRecorder _auditRecorder;

    public UpdateRoleHandler(RoleManager<IdentityRole> roleManager, IAuditRecorder auditRecorder)
    {
        _roleManager = roleManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _roleManager.FindByNameAsync(request.RoleName);
        if (role == null)
        {
            await _auditRecorder.RecordAsync(
                "UpdateRole",
                "Failed",
                targetEntityType: "Role",
                detail: $"RoleNotFound:{request.RoleName}",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role not found");
        }

        var existing = await _roleManager.FindByNameAsync(request.NewRoleName);
        if (existing != null)
        {
            await _auditRecorder.RecordAsync(
                "UpdateRole",
                "Failed",
                targetEntityType: "Role",
                detail: $"TargetRoleExists:{request.NewRoleName}",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Target role name already exists");
        }

        role.Name = request.NewRoleName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "UpdateRole",
                "Failed",
                targetEntityType: "Role",
                detail: $"{request.RoleName}->{request.NewRoleName}",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to update role", errors);
        }

        await _auditRecorder.RecordAsync(
            "UpdateRole",
            "Success",
            targetEntityType: "Role",
            detail: $"{request.RoleName}->{request.NewRoleName}",
            cancellationToken: cancellationToken);
        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role updated");
    }
}
