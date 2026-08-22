using IdentityService.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[Route("forgot-password")]
public class ForgotPasswordPageController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Forgot password";
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index([FromForm] string emailOrUserName, CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Forgot password";

        if (string.IsNullOrWhiteSpace(emailOrUserName))
        {
            ViewData["Error"] = "Please enter your email or username.";
            return View();
        }

        var response = await mediator.Send(new ForgotPasswordCommand
        {
            EmailOrUserName = emailOrUserName.Trim()
        }, cancellationToken);

        if (!response.Success)
        {
            ViewData["Error"] = response.Message ?? "Could not send reset email.";
            return View();
        }

        ViewData["Title"] = "Check your email";
        return View("Success");
    }
}
