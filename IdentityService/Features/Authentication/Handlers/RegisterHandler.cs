using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Audit;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, ResponseDto<RegisterResponse>>
{
    private readonly UserManager<User> _userManager;
    private readonly IAuditRecorder _auditRecorder;

    public RegisterHandler(UserManager<User> userManager, IAuditRecorder auditRecorder)
    {
        _userManager = userManager;
        _auditRecorder = auditRecorder;
    }

    public async Task<ResponseDto<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTime.UtcNow,
            isActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => new ApiError { ErrorCode = e.Code, ErrorMessage = e.Description }).ToList();
            await _auditRecorder.RecordAsync(
                "UserRegister",
                "Failed",
                actorUserName: request.UserName,
                targetEntityType: "User",
                detail: string.Join(';', errors.Select(e => e.ErrorMessage)),
                cancellationToken: cancellationToken);
            return ResponseDto<RegisterResponse>.Failure("User creation failed", errors);
        }

        var response = new RegisterResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            CreatedAt = user.CreatedAt
        };

        await _auditRecorder.RecordAsync(
            "UserRegister",
            "Success",
            actorUserId: user.Id,
            actorUserName: user.UserName,
            targetEntityType: "User",
            targetEntityId: user.Id,
            cancellationToken: cancellationToken);

        return ResponseDto<RegisterResponse>.SuccessResponse(response, "User created successfully");
    }
}
