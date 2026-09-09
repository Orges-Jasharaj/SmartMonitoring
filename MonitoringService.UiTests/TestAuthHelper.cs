using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using OpenQA.Selenium;

namespace MonitoringService.UiTests;

internal static class TestAuthHelper
{
    private const string DefaultJwtKey = "DockerDevJwtKey-MustBeAtLeast32CharactersLong!";
    private const string Issuer = "SmartMonitoring.Identity";
    private const string Audience = "SmartMonitoring.Clients";

    private static string ApiBaseUrl =>
        Environment.GetEnvironmentVariable("SM_API_URL") ?? "http://localhost:5173";

    public static AuthSession CreateSession(string userName, string password)
    {
        var apiSession = TryLoginViaApi(userName, password);
        return apiSession ?? CreateDevAdminSession(userName);
    }

    public static void ApplySession(IWebDriver driver, string webBaseUrl, AuthSession session)
    {
        driver.Navigate().GoToUrl(webBaseUrl);
        var js = (IJavaScriptExecutor)driver;
        js.ExecuteScript(
            "window.localStorage.setItem('sm_token', arguments[0]); window.localStorage.setItem('sm_token_expires', arguments[1]);",
            session.Token,
            session.ExpiresAt.ToString("o"));
    }

    private static AuthSession? TryLoginViaApi(string userName, string password)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var response = client.PostAsJsonAsync(
                $"{ApiBaseUrl}/identity/api/authentication/login",
                new { userNameOrEmail = userName, password }).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = response.Content.ReadFromJsonAsync<LoginResponse>().GetAwaiter().GetResult();
            if (payload?.Success != true || payload.Data?.Token is not { Length: > 0 } token)
            {
                return null;
            }

            return new AuthSession(token, payload.Data.ExpiresAt);
        }
        catch
        {
            return null;
        }
    }

    private static AuthSession CreateDevAdminSession(string userName)
    {
        var key = Environment.GetEnvironmentVariable("SM_JWT_KEY") ?? DefaultJwtKey;
        var expiresAt = DateTime.UtcNow.AddHours(1);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "ui-test-admin"),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.Role, "Admin"),
        };

        var jwt = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(jwt);
        return new AuthSession(token, expiresAt);
    }

    internal sealed record AuthSession(string Token, DateTime ExpiresAt);

    private sealed class LoginResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("data")]
        public LoginData? Data { get; set; }
    }

    private sealed class LoginData
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        [JsonPropertyName("expiresAt")]
        public DateTime ExpiresAt { get; set; }
    }
}
