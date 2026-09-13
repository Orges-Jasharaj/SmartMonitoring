using MonitoringService.Data;
using MonitoringService.Data.Models;
using MonitoringService.Features.Devices;
using MonitoringService.Features.Devices.Commands;
using MonitoringService.Features.Devices.Queries;
using MonitoringService.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos;
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

        var rangeError = DeviceRangeValidator.ValidateAllRanges(
            request.MinTempC,
            request.MaxTempC,
            request.MinHumidityPct,
            request.MaxHumidityPct,
            request.MinCo2Ppm,
            request.MaxCo2Ppm,
            request.MinLightLevelLux,
            request.MaxLightLevelLux,
            request.MinNoiseLevelDb,
            request.MaxNoiseLevelDb);
        if (rangeError != null)
        {
            return ResponseDto<DeviceCreatedDto>.Failure(rangeError);
        }

        var deviceKey = DeviceKeyGenerator.Generate();
        var device = new Device
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Name = request.Name.Trim(),
            ZoneName = request.ZoneName.Trim(),
            DeviceKey = deviceKey,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        ApplyRanges(device, request);

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
        MinHumidityPct = device.MinHumidityPct,
        MaxHumidityPct = device.MaxHumidityPct,
        MinCo2Ppm = device.MinCo2Ppm,
        MaxCo2Ppm = device.MaxCo2Ppm,
        MinLightLevelLux = device.MinLightLevelLux,
        MaxLightLevelLux = device.MaxLightLevelLux,
        MinNoiseLevelDb = device.MinNoiseLevelDb,
        MaxNoiseLevelDb = device.MaxNoiseLevelDb,
        MinBatteryPct = device.MinBatteryPct,
        IsActive = device.IsActive,
        LastReadingAtUtc = device.LastReadingAtUtc,
        CreatedAtUtc = device.CreatedAtUtc
    };

    internal static DeviceCreatedDto MapCreated(Device device)
    {
        var dto = Map(device);
        return new DeviceCreatedDto
        {
            Id = dto.Id,
            CompanyId = dto.CompanyId,
            Name = dto.Name,
            ZoneName = dto.ZoneName,
            MinTempC = dto.MinTempC,
            MaxTempC = dto.MaxTempC,
            MinHumidityPct = dto.MinHumidityPct,
            MaxHumidityPct = dto.MaxHumidityPct,
            MinCo2Ppm = dto.MinCo2Ppm,
            MaxCo2Ppm = dto.MaxCo2Ppm,
            MinLightLevelLux = dto.MinLightLevelLux,
            MaxLightLevelLux = dto.MaxLightLevelLux,
            MinNoiseLevelDb = dto.MinNoiseLevelDb,
            MaxNoiseLevelDb = dto.MaxNoiseLevelDb,
            MinBatteryPct = dto.MinBatteryPct,
            IsActive = dto.IsActive,
            LastReadingAtUtc = dto.LastReadingAtUtc,
            CreatedAtUtc = dto.CreatedAtUtc,
            DeviceKey = device.DeviceKey
        };
    }

    private static void ApplyRanges(Device device, CreateDeviceCommand request)
    {
        device.MinTempC = request.MinTempC;
        device.MaxTempC = request.MaxTempC;
        device.MinHumidityPct = request.MinHumidityPct;
        device.MaxHumidityPct = request.MaxHumidityPct;
        device.MinCo2Ppm = request.MinCo2Ppm;
        device.MaxCo2Ppm = request.MaxCo2Ppm;
        device.MinLightLevelLux = request.MinLightLevelLux;
        device.MaxLightLevelLux = request.MaxLightLevelLux;
        device.MinNoiseLevelDb = request.MinNoiseLevelDb;
        device.MaxNoiseLevelDb = request.MaxNoiseLevelDb;
        device.MinBatteryPct = request.MinBatteryPct;
    }
}

public class GetDevicesByCompanyHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess) : IRequestHandler<GetDevicesByCompanyQuery, ResponseDto<PagedResult<DeviceDto>>>
{
    public async Task<ResponseDto<PagedResult<DeviceDto>>> Handle(GetDevicesByCompanyQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanAccessCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<PagedResult<DeviceDto>>.Failure("Company not found or access denied.");
        }

        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);

        var query = dbContext.Devices.Where(d => d.CompanyId == request.CompanyId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(d => d.Name.Contains(term) || d.ZoneName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var devices = await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return ResponseDto<PagedResult<DeviceDto>>.SuccessResponse(new PagedResult<DeviceDto>
        {
            Items = devices.Select(CreateDeviceHandler.Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }
}

public class GetDeviceKeyHandler(
    MonitoringDbContext dbContext,
    ICompanyAccessService companyAccess,
    ICurrentUserContext currentUser,
    IAuditRecorder auditRecorder) : IRequestHandler<GetDeviceKeyQuery, ResponseDto<DeviceKeyDto>>
{
    public async Task<ResponseDto<DeviceKeyDto>> Handle(GetDeviceKeyQuery request, CancellationToken cancellationToken)
    {
        if (!await companyAccess.CanManageCompanyAsync(request.CompanyId, cancellationToken))
        {
            return ResponseDto<DeviceKeyDto>.Failure("You do not have permission to view device keys for this company.");
        }

        var device = await dbContext.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId && d.CompanyId == request.CompanyId, cancellationToken);

        if (device == null)
        {
            return ResponseDto<DeviceKeyDto>.Failure("Device not found.");
        }

        await auditRecorder.RecordAsync(
            "DeviceKeyViewed",
            "Success",
            actorUserId: currentUser.UserId,
            targetEntityType: "Device",
            targetEntityId: device.Id.ToString(),
            detail: device.Name,
            cancellationToken: cancellationToken);

        return ResponseDto<DeviceKeyDto>.SuccessResponse(new DeviceKeyDto
        {
            DeviceKey = device.DeviceKey
        });
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

        var rangeError = DeviceRangeValidator.ValidateAllRanges(
            request.MinTempC,
            request.MaxTempC,
            request.MinHumidityPct,
            request.MaxHumidityPct,
            request.MinCo2Ppm,
            request.MaxCo2Ppm,
            request.MinLightLevelLux,
            request.MaxLightLevelLux,
            request.MinNoiseLevelDb,
            request.MaxNoiseLevelDb);
        if (rangeError != null)
        {
            return ResponseDto<DeviceDto>.Failure(rangeError);
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
        device.MinHumidityPct = request.MinHumidityPct;
        device.MaxHumidityPct = request.MaxHumidityPct;
        device.MinCo2Ppm = request.MinCo2Ppm;
        device.MaxCo2Ppm = request.MaxCo2Ppm;
        device.MinLightLevelLux = request.MinLightLevelLux;
        device.MaxLightLevelLux = request.MaxLightLevelLux;
        device.MinNoiseLevelDb = request.MinNoiseLevelDb;
        device.MaxNoiseLevelDb = request.MaxNoiseLevelDb;
        device.MinBatteryPct = request.MinBatteryPct;

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
