using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Dtos.Responses;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Authentication.Handlers
{
    public class RegisterHandler : IRequestHandler<RegisterCommand, ResponseDto<RegisterResponse>>
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<RegisterHandler> _logger;

        public RegisterHandler(UserManager<User> userManager, ILogger<RegisterHandler> logger)
        {
            _userManager = userManager;
            _logger = logger;
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
                _logger.LogAuditEvent(
                    "UserRegister",
                    "Failed",
                    actorUserName: request.UserName,
                    detail: string.Join(';', errors.Select(e => e.ErrorMessage)));
                return ResponseDto<RegisterResponse>.Failure("User creation failed", errors);
            }

            var response = new RegisterResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt
            };

            _logger.LogAuditEvent(
                "UserRegister",
                "Success",
                actorUserId: user.Id,
                actorUserName: user.UserName);

            return ResponseDto<RegisterResponse>.SuccessResponse(response, "User created successfully");
        }
    }
}
