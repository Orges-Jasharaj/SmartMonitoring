using MonitoringService.Constants;
using MonitoringService.Data;
using Microsoft.EntityFrameworkCore;

namespace MonitoringService.Services;

public interface ICompanyAccessService
{
    Task<IReadOnlyList<Guid>> GetAccessibleCompanyIdsAsync(CancellationToken cancellationToken = default);
    Task<bool> CanAccessCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> CanManageCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}

public class CompanyAccessService(
    MonitoringDbContext dbContext,
    ICurrentUserContext currentUser) : ICompanyAccessService
{
    public async Task<IReadOnlyList<Guid>> GetAccessibleCompanyIdsAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return [];
        }

        if (currentUser.IsSystemAdmin)
        {
            return await dbContext.Companies
                .Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
        }

        return await dbContext.CompanyUsers
            .Where(m => m.UserId == currentUser.UserId && m.Company.IsActive)
            .Select(m => m.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var accessible = await GetAccessibleCompanyIdsAsync(cancellationToken);
        return accessible.Contains(companyId);
    }

    public async Task<bool> CanManageCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(currentUser.UserId))
        {
            return false;
        }

        if (currentUser.IsSystemAdmin)
        {
            return await dbContext.Companies.AnyAsync(c => c.Id == companyId && c.IsActive, cancellationToken);
        }

        return await dbContext.CompanyUsers.AnyAsync(
            m => m.CompanyId == companyId
                 && m.UserId == currentUser.UserId
                 && m.Role == CompanyRoles.CompanyAdmin
                 && m.Company.IsActive,
            cancellationToken);
    }
}
