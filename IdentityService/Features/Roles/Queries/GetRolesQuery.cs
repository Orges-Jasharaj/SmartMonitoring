using MediatR;
using System.Collections.Generic;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Roles.Queries
{
    public class GetRolesQuery : IRequest<ResponseDto<IEnumerable<string>>>
    {
    }
}
