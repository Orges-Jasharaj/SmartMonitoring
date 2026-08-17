using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;

namespace IdentityService.Features.Roles.Handlers;

public class CreateRoleHandler : IRequestHandler<CreateRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditRecorder _auditRecorder;

    public CreateRoleHandler(RoleManager<IdentityRole> roleManager, IAuditRecorder auditRecorder)
    {
        _roleManager = roleManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (await _roleManager.RoleExistsAsync(request.RoleName))
        {
            await _auditRecorder.RecordAsync(
                "CreateRole",
                "Failed",
                targetEntityType: "Role",
                detail: $"RoleExists:{request.RoleName}",
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role already exists");
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(request.RoleName));
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "CreateRole",
                "Failed",
                targetEntityType: "Role",
                detail: request.RoleName,
                cancellationToken: cancellationToken);
            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to create role", errors);
        }

        await _auditRecorder.RecordAsync(
            "CreateRole",
            "Success",
            targetEntityType: "Role",
            detail: request.RoleName,
            cancellationToken: cancellationToken);
        return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role created");
    }
}
