namespace IdentityService.Services
{
    public class JwtResult
    {
        public string Token { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}
