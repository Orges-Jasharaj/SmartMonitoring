using IdentityService.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using SmartMonitoring.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediator.Send(new GetUsersQuery());
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var response = await _mediator.Send(new GetUserByIdQuery { Id = id });
            if (!response.Success)
            {
                return NotFound(response);
            }
            return Ok(response);
        }
        [HttpPost("deactivate/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Deactivate(string id)
        {
            var response = await _mediator.Send(new IdentityService.Features.Users.Commands.DeactivateUserCommand { Id = id });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("activate/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Activate(string id)
        {
            var response = await _mediator.Send(new IdentityService.Features.Users.Commands.ActivateUserCommand { Id = id });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPost("me/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] IdentityService.Features.Users.Requests.ChangePasswordRequest request)
        {
            var userId = User.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(SmartMonitoring.Shared.Dtos.Responses.ResponseDto<bool>.Failure("Unable to determine user from token"));
            }

            var command = new IdentityService.Features.Users.Commands.ChangePasswordCommand
            {
                UserId = userId,
                OldPassword = request.OldPassword,
                NewPassword = request.NewPassword
            };

            var response = await _mediator.Send(command);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}
