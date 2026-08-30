using MonitoringService.Data;
using MonitoringService.Data.Models;
using MonitoringService.Features.Devices.Commands;
using MonitoringService.Features.Devices.Queries;
using MonitoringService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace MonitoringService.Features.Devices.Handlers;

public class CreateDeviceHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess,
    ICurrentUserContext currentUser,
    IAuditRecorder auditRecorder) : IRequestHandler<CreateDeviceCommand, ResponseDto<DeviceCreatedDto>>
{
    public async Task<ResponseDto<DeviceCreatedDto>> Handle(CreateDeviceCommand request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanManageCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<DeviceCreatedDto>.Failure("You do not have permission to manage devices for this company.");
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ZoneName))
        {
            return ResponseDto<DeviceCreatedDto>.Failure("Device name and zone name are required.");
        }

        if (request.MinTempC >= request.MaxTempC)
        {
            return ResponseDto<DeviceCreatedDto>.Failure("Minimum temperature must be less than maximum temperature.");
        }

        var deviceKey = DeviceKeyGenerator.Generate();
        var device = new Device
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            ZoneName = request.ZoneName.Trim(),
            MinTempC = request.MinTempC,
            MaxTempC = request.MaxTempC,
            DeviceKey = deviceKey,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Devices.Add(device);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditRecorder.RecordAsync(
            "DeviceRegistered",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "Device",
            targetEntityId: device.Id.ToString(),
            detail: device.Name,
            cancellationToken: cancellationToken);

        return ResponseDto<DeviceCreatedDto>.SuccessResponse(MapCreated(device), "Device created. Store the device key securely; it is shown only once.");
    }

    internal static DeviceDto Map(Device device) => new()
    {
        Id = device.Id,
        CompanyId = device.CompanyId,
        Name = device.Name,
        ZoneName = device.ZoneName,
        MinTempC = device.MinTempC,
        MaxTempC = device.MaxTempC,
        IsActive = device.IsActive,
        LastReadingAtUtc = device.LastReadingAtUtc,
        CreatedAtUtc = device.CreatedAtUtc
    };

    internal static DeviceCreatedDto MapCreated(Device device) => new()
    {
        Id = device.Id,
        CompanyId = device.CompanyId,
        Name = device.Name,
        ZoneName = device.ZoneName,
        MinTempC = device.MinTempC,
        MaxTempC = device.MaxTempC,
        IsActive = device.IsActive,
        LastReadingAtUtc = device.LastReadingAtUtc,
        CreatedAtUtc = device.CreatedAtUtc,
        DeviceKey = device.DeviceKey
    };
}

public class GetDevicesByCompanyHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetDevicesByCompanyQuery, ResponseDto<IReadOnlyList<DeviceDto>>>
{
    public async Task<ResponseDto<IReadOnlyList<DeviceDto>>> Handle(GetDevicesByCompanyQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<IReadOnlyList<DeviceDto>>.Failure("Company not found or access denied.");
        }

        var devices = await dbContext.Devices
            .Where(d => d.CompanyId == request.CompanyId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return ResponseDto<IReadOnlyList<DeviceDto>>.SuccessResponse(
            devices.Select(CreateDeviceHandler.Map).ToList());
    }
}

public class GetDeviceByIdHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetDeviceByIdQuery, ResponseDto<DeviceDto>>
{
    public async Task<ResponseDto<DeviceDto>> Handle(GetDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (device == null)
        {
            return ResponseDto<DeviceDto>.Failure("Device not found.");
        }

        if (!await companyAccess.CanAccessCompanyAsync(device.CompanyId, cancellationToken))
        {
            return ResponseDto<DeviceDto>.Failure("Device not found or access denied.");
        }

        return ResponseDto<DeviceDto>.SuccessResponse(CreateDeviceHandler.Map(device));
    }
}

public class DeleteDeviceHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess,
    ICurrentUserContext currentUser,
    IAuditRecorder auditRecorder) : IRequestHandler<DeleteDeviceCommand, ResponseDto<bool>>
{
    public async Task<ResponseDto<bool>> Handle(DeleteDeviceCommand request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanManageCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<bool>.Failure("You do not have permission to delete devices for this company.");
        }

        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.CompanyId == request.CompanyId, cancellationToken);

        if (device == null)
        {
            return ResponseDto<bool>.Failure("Device not found.");
        }

        var deviceName = device.Name;
        dbContext.Devices.Remove(device);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditRecorder.RecordAsync(
            "DeviceDeleted",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "Device",
            targetEntityId: request.DeviceId.ToString(),
            detail: deviceName,
            cancellationToken: cancellationToken);

        return ResponseDto<bool>.SuccessResponse(true, "Device deleted.");
    }
}

public class UpdateDeviceHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess,
    ICurrentUserContext currentUser,
    IAuditRecorder auditRecorder) : IRequestHandler<UpdateDeviceCommand, ResponseDto<DeviceDto>>
{
    public async Task<ResponseDto<DeviceDto>> Handle(UpdateDeviceCommand request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanManageCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<DeviceDto>.Failure("You do not have permission to update devices for this company.");
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ZoneName))
        {
            return ResponseDto<DeviceDto>.Failure("Device name and zone name are required.");
        }

        if (request.MinTempC >= request.MaxTempC)
        {
            return ResponseDto<DeviceDto>.Failure("Minimum temperature must be less than maximum temperature.");
        }

        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.CompanyId == request.CompanyId, cancellationToken);

        if (device == null)
        {
            return ResponseDto<DeviceDto>.Failure("Device not found.");
        }

        device.Name = request.Name.Trim();
        device.ZoneName = request.ZoneName.Trim();
        device.MinTempC = request.MinTempC;
        device.MaxTempC = request.MaxTempC;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditRecorder.RecordAsync(
            "DeviceUpdated",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "Device",
            targetEntityId: device.Id.ToString(),
            detail: device.Name,
            cancellationToken: cancellationToken);

        return ResponseDto<DeviceDto>.SuccessResponse(CreateDeviceHandler.Map(device), "Device updated.");
    }
}
