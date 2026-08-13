using IdentityService.Data.Models;
using System.Collections.Generic;

namespace IdentityService.Services
{
    public interface IJwtService
    {
        JwtResult GenerateToken(User user, IEnumerable<string> roles);
    }
}
