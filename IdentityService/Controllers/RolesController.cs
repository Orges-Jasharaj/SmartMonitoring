using IdentityService.Features.Roles.Commands;
using IdentityService.Features.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
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
            var created = await _mediator.Send(command);
            if (!created) return BadRequest();
            return Ok();
        }

        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignRoleCommand command)
        {
            var assigned = await _mediator.Send(command);
            if (!assigned) return BadRequest();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _mediator.Send(new GetRolesQuery());
            return Ok(roles);
        }
    }
}
