using System.Net.Http.Json;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Services;

public interface IIdentityUserEmailResolver
{
    Task<IReadOnlyList<string>> ResolveEmailsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}

public class IdentityUserEmailResolver(
    HttpClient httpClient,
    ILogger<IdentityUserEmailResolver> logger) : IIdentityUserEmailResolver
{
    private sealed class UserEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public async Task<IReadOnlyList<string>> ResolveEmailsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var emails = new List<string>();

        foreach (var userId in userIds.Distinct())
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<ResponseDto<UserEmailDto>>($"api/users/{userId}", cancellationToken);
                if (response is not { Success: true, Data.IsActive: true } || string.IsNullOrWhiteSpace(response.Data.Email))
                {
                    continue;
                }

                emails.Add(response.Data.Email.Trim());
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve email for user {UserId}", userId);
            }
        }

        return emails.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
