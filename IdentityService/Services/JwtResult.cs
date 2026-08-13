namespace IdentityService.Services
{
    public class JwtResult
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
