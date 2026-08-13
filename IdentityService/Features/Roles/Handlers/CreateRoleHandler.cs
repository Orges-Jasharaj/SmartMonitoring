using IdentityService.Features.Roles.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Roles.Handlers
{
    public class CreateRoleHandler : IRequestHandler<CreateRoleCommand, SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateRoleHandler(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            if (await _roleManager.RoleExistsAsync(request.RoleName))
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Role already exists");

            var result = await _roleManager.CreateAsync(new IdentityRole(request.RoleName));
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new SmartMonitoring.Shared.Dtos.Responses.ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
                return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Failed to create role", errors);
            }

            return SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.SuccessResponse(true, "Role created");
        }
    }
}
