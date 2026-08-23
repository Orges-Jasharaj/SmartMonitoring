using MonitoringService.Constants;
using MonitoringService.Data;
using MonitoringService.Data.Models;
using MonitoringService.Features.Companies.Commands;
using MonitoringService.Features.Companies.Queries;
using MonitoringService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Companies.Handlers;

public class CreateCompanyHandler(
    MonitoringDbContext dbContext,
    ICurrentUserContext currentUser,
    IAuditRecorder auditRecorder) : IRequestHandler<CreateCompanyCommand, ResponseDto<CompanyDto>>
{
    public async Task<ResponseDto<CompanyDto>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsSystemAdmin)
        {
            return ResponseDto<CompanyDto>.Failure("Only system administrators can create companies.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ResponseDto<CompanyDto>.Failure("Company name is required.");
        }

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Companies.Add(company);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditRecorder.RecordAsync(
            "CompanyCreated",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "Company",
            targetEntityId: company.Id.ToString(),
            detail: company.Name,
            cancellationToken: cancellationToken);

        return ResponseDto<CompanyDto>.SuccessResponse(Map(company), "Company created");
    }

    internal static CompanyDto Map(Company company) => new()
    {
        Id = company.Id,
        Name = company.Name,
        IsActive = company.IsActive,
        CreatedAtUtc = company.CreatedAtUtc
    };
}

public class AssignCompanyUserHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess,
    ICurrentUserContext currentUser,
    IAuditRecorder auditRecorder) : IRequestHandler<AssignCompanyUserCommand, ResponseDto<CompanyUserDto>>
{
    public async Task<ResponseDto<CompanyUserDto>> Handle(AssignCompanyUserCommand request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanManageCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<CompanyUserDto>.Failure("You do not have permission to manage users for this company.");
        }

        if (!CompanyRoles.All.Contains(request.Role))
        {
            return ResponseDto<CompanyUserDto>.Failure($"Invalid role. Allowed roles: {string.Join(", ", CompanyRoles.All)}");
        }

        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId && c.IsActive, cancellationToken);
        if (!companyExists)
        {
            return ResponseDto<CompanyUserDto>.Failure("Company not found");
        }

        var existing = await dbContext.CompanyUsers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == request.UserId, cancellationToken);

        if (existing != null)
        {
            existing.Role = request.Role;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditRecorder.RecordAsync(
                "CompanyUserAssigned",
                "Success",
                actorUserId: currentUser.UserId,
                targetEntityType: "CompanyUser",
                targetEntityId: existing.Id.ToString(),
                detail: $"UpdatedRole:{request.Role}",
                cancellationToken: cancellationToken);

            return ResponseDto<CompanyUserDto>.SuccessResponse(Map(existing), "Company user updated");
        }

        var member = new CompanyUser
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            UserId = request.UserId,
            Role = request.Role,
            AssignedAtUtc = DateTime.UtcNow
        };

        dbContext.CompanyUsers.Add(member);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditRecorder.RecordAsync(
            "CompanyUserAssigned",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "CompanyUser",
            targetEntityId: member.Id.ToString(),
            detail: request.Role,
            cancellationToken: cancellationToken);

        return ResponseDto<CompanyUserDto>.SuccessResponse(Map(member), "User assigned to company");
    }

    internal static CompanyUserDto Map(CompanyUser member) => new()
    {
        Id = member.Id,
        CompanyId = member.CompanyId,
        UserId = member.UserId,
        Role = member.Role,
        AssignedAtUtc = member.AssignedAtUtc
    };
}

public class GetCompaniesHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetCompaniesQuery, ResponseDto<IReadOnlyList<CompanyDto>>>
{
    public async Task<ResponseDto<IReadOnlyList<CompanyDto>>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companyIds = await companyAccess.GetAccessibleCompanyIdsAsync(cancellationToken);
        var companies = await dbContext.Companies
            .Where(c => companyIds.Contains(c.Id))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return ResponseDto<IReadOnlyList<CompanyDto>>.SuccessResponse(
            companies.Select(CreateCompanyHandler.Map).ToList());
    }
}

public class GetCompanyByIdHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetCompanyByIdQuery, ResponseDto<CompanyDto>>
{
    public async Task<ResponseDto<CompanyDto>> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.Id, cancellationToken))
        {
            return ResponseDto<CompanyDto>.Failure("Company not found or access denied.");
        }

        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (company == null)
        {
            return ResponseDto<CompanyDto>.Failure("Company not found.");
        }

        return ResponseDto<CompanyDto>.SuccessResponse(CreateCompanyHandler.Map(company));
    }
}

public class GetCompanyUsersHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetCompanyUsersQuery, ResponseDto<IReadOnlyList<CompanyUserDto>>>
{
    public async Task<ResponseDto<IReadOnlyList<CompanyUserDto>>> Handle(GetCompanyUsersQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<IReadOnlyList<CompanyUserDto>>.Failure("Company not found or access denied.");
        }

        var members = await dbContext.CompanyUsers
            .Where(m => m.CompanyId == request.CompanyId)
            .OrderBy(m => m.UserId)
            .ToListAsync(cancellationToken);

        return ResponseDto<IReadOnlyList<CompanyUserDto>>.SuccessResponse(
            members.Select(AssignCompanyUserHandler.Map).ToList());
    }
}
