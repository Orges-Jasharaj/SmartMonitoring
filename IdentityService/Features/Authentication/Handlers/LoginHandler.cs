using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Dtos.Responses;
using SmartMonitoring.Shared.Observability;

namespace IdentityService.Features.Authentication.Handlers
{
    public class LoginHandler : IRequestHandler<LoginQuery, ResponseDto<JwtResult>>
    {
        private readonly UserManager<User> _userManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(
            UserManager<User> userManager,
            IJwtService jwtService,
            ILogger<LoginHandler> logger)
        {
            _userManager = userManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<ResponseDto<JwtResult>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
                ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);

            if (user == null)
            {
                _logger.LogAuditEvent(
                    "UserLogin",
                    "Failed",
                    actorUserName: request.UserNameOrEmail,
                    detail: "UserNotFound");
                return ResponseDto<JwtResult>.Failure(
                    "Invalid credentials",
                    [new ApiError { ErrorMessage = "Invalid credentials" }]);
            }

            if (!user.isActive)
            {
                _logger.LogAuditEvent(
                    "UserLogin",
                    "Failed",
                    actorUserId: user.Id,
                    actorUserName: user.UserName,
                    detail: "UserDeactivated");
                return ResponseDto<JwtResult>.Failure(
                    "Invalid credentials",
                    [new ApiError { ErrorMessage = "Invalid credentials" }]);
            }

            var valid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!valid)
            {
                _logger.LogAuditEvent(
                    "UserLogin",
                    "Failed",
                    actorUserId: user.Id,
                    actorUserName: user.UserName,
                    detail: "InvalidPassword");
                return ResponseDto<JwtResult>.Failure(
                    "Invalid credentials",
                    [new ApiError { ErrorMessage = "Invalid credentials" }]);
            }

            var roles = await _userManager.GetRolesAsync(user);
            var jwt = _jwtService.GenerateToken(user, roles);

            if (!string.IsNullOrEmpty(jwt.RefreshToken))
            {
                user.RefreshToken = jwt.RefreshToken;
                user.RefreshTokenExpiryTime = jwt.RefreshTokenExpiresAt;
                await _userManager.UpdateAsync(user);
            }

            _logger.LogAuditEvent(
                "UserLogin",
                "Success",
                actorUserId: user.Id,
                actorUserName: user.UserName);

            return ResponseDto<JwtResult>.SuccessResponse(jwt, "Login successful");
        }
    }
}
