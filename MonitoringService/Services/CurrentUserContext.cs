using System.Security.Claims;
using MonitoringService.Constants;

namespace MonitoringService.Services;

public interface ICurrentUserContext
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsSystemAdmin { get; }
}

public class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirst("sub")?.Value
        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsSystemAdmin =>
        User?.IsInRole(SystemRoles.Admin) == true;
}
