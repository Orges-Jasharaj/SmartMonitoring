using IdentityService.Features.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Roles.Handlers
{
    public class GetRolesHandler : IRequestHandler<GetRolesQuery, IEnumerable<string>>
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public GetRolesHandler(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public Task<IEnumerable<string>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = _roleManager.Roles.Select(r => r.Name ?? string.Empty).ToList();
            return Task.FromResult<IEnumerable<string>>(roles);
        }
    }
}
