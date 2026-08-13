using IdentityService.Data.Models;
using IdentityService.Features.Authentication.Commands;
using IdentityService.Services;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace IdentityService.Features.Authentication.Handlers
{
    public class LoginHandler : IRequestHandler<LoginQuery, JwtResult>
    {
        private readonly UserManager<User> _userManager;
        private readonly IJwtService _jwtService;

        public LoginHandler(UserManager<User> userManager, IJwtService jwtService)
        {
            _userManager = userManager;
            _jwtService = jwtService;
        }

        public async Task<JwtResult> Handle(LoginQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail) ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);
            if (user == null)
                throw new Exception("Invalid credentials");

            var valid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!valid)
                throw new Exception("Invalid credentials");

            var roles = await _userManager.GetRolesAsync(user);
            var jwt = _jwtService.GenerateToken(user, roles);
            return jwt;
        }
    }
}
