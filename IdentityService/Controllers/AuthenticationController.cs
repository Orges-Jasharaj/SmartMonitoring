using IdentityService.Features.Authentication.Commands;
using MediatR;
using SmartMonitoring.Shared.Dtos.Responses;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthenticationController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var response = await _mediator.Send(command);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return CreatedAtAction(nameof(Register), new { id = response.Data?.Id }, response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginQuery query)
        {
            var result = await _mediator.Send(query);
            if (!result.Success)
            {
                return Unauthorized(result);
            }
            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
        {
            var result = await _mediator.Send(command);
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }
    }
}
