using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;

namespace IdentityService.Features.Authentication.Commands
{
    public class RegisterCommand : IRequest<ResponseDto<RegisterResponse>>
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
    }

    public class RegisterResponse
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
