using MonitoringService.Features.Companies.Commands;
using MonitoringService.Features.Companies.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    //[Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCompanyCommand command)
    {
        var response = await mediator.Send(command);
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await mediator.Send(new GetCompaniesQuery());
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var response = await mediator.Send(new GetCompanyByIdQuery { Id = id });
        if (!response.Success) return NotFound(response);
        return Ok(response);
    }

    [HttpGet("{companyId:guid}/users")]
    public async Task<IActionResult> GetUsers(Guid companyId)
    {
        var response = await mediator.Send(new GetCompanyUsersQuery { CompanyId = companyId });
        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }

    [HttpPost("{companyId:guid}/users")]
    public async Task<IActionResult> AssignUser(Guid companyId, [FromBody] AssignCompanyUserRequest request)
    {
        var response = await mediator.Send(new AssignCompanyUserCommand
        {
            CompanyId = companyId,
            UserId = request.UserId,
            Role = request.Role
        });

        if (!response.Success) return BadRequest(response);
        return Ok(response);
    }
}

public class AssignCompanyUserRequest
{
    public string UserId { get; set; } = null!;
    public string Role { get; set; } = null!;
}
