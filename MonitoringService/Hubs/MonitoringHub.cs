using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MonitoringService.Services;

namespace MonitoringService.Hubs;

[Authorize]
public class MonitoringHub(ICompanyAccessService companyAccess) : Hub
{
    public static string CompanyGroup(Guid companyId) => $"company-{companyId:D}";

    public override async Task OnConnectedAsync()
    {
        var companyIds = await companyAccess.GetAccessibleCompanyIdsAsync(Context.ConnectionAborted);
        foreach (var companyId in companyIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, CompanyGroup(companyId));
        }

        await base.OnConnectedAsync();
    }
}
