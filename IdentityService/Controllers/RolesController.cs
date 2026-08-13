using IdentityService.Features.Roles.Commands;
using IdentityService.Features.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleCommand command)
        {
            var response = await _mediator.Send(command);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignRoleCommand command)
        {
            var response = await _mediator.Send(command);
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediator.Send(new GetRolesQuery());
            if (!response.Success)
            {
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] UpdateRoleCommand command)
        {
            var response = await _mediator.Send(command);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpDelete("{roleName}")]
        public async Task<IActionResult> Delete(string roleName)
        {
            var response = await _mediator.Send(new DeleteRoleCommand { RoleName = roleName });
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }
    }
}
