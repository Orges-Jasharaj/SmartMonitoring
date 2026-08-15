using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Handlers
{
    public class LoginHandler : IRequestHandler<LoginQuery, ResponseDto<JwtResult>>
    {
        private readonly UserManager<User> _userManager;
        private readonly IJwtService _jwtService;

        public LoginHandler(UserManager<User> userManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        public async Task<ResponseDto<JwtResult>> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail) ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);
            if (user == null)
            {
                return ResponseDto<JwtResult>.Failure("Invalid credentials", new List<ApiError> { new ApiError { ErrorMessage = "User not found" } });
            }

            if (!user.isActive)
            {
                return ResponseDto<JwtResult>.Failure("User is deactivated", new List<ApiError> { new ApiError { ErrorMessage = "User account is deactivated" } });
            }

            var valid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!valid)
            {
                return ResponseDto<JwtResult>.Failure("Invalid credentials", new List<ApiError> { new ApiError { ErrorMessage = "Invalid password" } });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var jwt = _jwtService.GenerateToken(user, roles);

            // store refresh token and expiry on user
            if (!string.IsNullOrEmpty(jwt.RefreshToken))
            {
                user.RefreshToken = jwt.RefreshToken;
                user.RefreshTokenExpiryTime = jwt.RefreshTokenExpiresAt;
                await _userManager.UpdateAsync(user);
            }

            return ResponseDto<JwtResult>.SuccessResponse(jwt, "Login successful");
        }
    }
}
