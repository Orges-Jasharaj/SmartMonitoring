using MediatR;
using System.Collections.Generic;

namespace IdentityService.Features.Roles.Queries
{
    public class GetRolesQuery : IRequest<IEnumerable<string>>
    {
    }
}
