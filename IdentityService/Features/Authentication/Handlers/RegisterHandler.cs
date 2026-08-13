using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity;
using SmartMonitoring.Shared.Dtos.Responses;
using System.Linq;
using System.Collections.Generic;

namespace IdentityService.Features.Authentication.Handlers
{
    public class RegisterHandler : IRequestHandler<RegisterCommand, ResponseDto<RegisterResponse>>
    {
        private readonly UserManager<User> _userManager;

        public RegisterHandler(UserManager<User> userManager)
        {
            _userManager = userManager;
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
                return ResponseDto<RegisterResponse>.Failure("User creation failed", errors);
            }

            var response = new RegisterResponse
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                CreatedAt = user.CreatedAt
            };

            return ResponseDto<RegisterResponse>.SuccessResponse(response, "User created successfully");
        }
    }
}
