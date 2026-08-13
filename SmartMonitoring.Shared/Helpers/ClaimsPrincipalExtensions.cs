using System.Security.Claims;

namespace SmartMonitoring.Shared.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static string? GetUserId(this ClaimsPrincipal user)
        {
            if (user == null) return null;

            // try 'sub' claim (JWT) then NameIdentifier
            var sub = user.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(sub)) return sub;

            var nameId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(nameId)) return nameId;

            return null;
        }
    }
}
