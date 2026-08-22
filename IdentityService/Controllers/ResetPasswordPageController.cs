using IdentityService.Features.Authentication.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[Route("reset-password")]
public class ResetPasswordPageController(IMediator mediator) : Controller
{
    [HttpGet]
    public IActionResult Index([FromQuery] string? userId, [FromQuery] string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            ViewData["Title"] = "Invalid link";
            ViewData["Error"] = "This reset link is invalid or incomplete. Request a new link from the forgot password page.";
            return View("Invalid");
        }

        ViewData["Title"] = "Reset password";
        ViewData["UserId"] = userId;
        ViewData["Token"] = token;
        return View();
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Index(
        [FromForm] string userId,
        [FromForm] string token,
        [FromForm] string newPassword,
        [FromForm] string confirmPassword,
        CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Reset password";

        if (newPassword != confirmPassword)
        {
            ViewData["UserId"] = userId;
            ViewData["Token"] = token;
            ViewData["Error"] = "Passwords do not match.";
            return View();
        }

        var response = await mediator.Send(new ResetPasswordCommand
        {
            UserId = userId,
            Token = token,
            NewPassword = newPassword
        }, cancellationToken);

        if (!response.Success)
        {
            ViewData["UserId"] = userId;
            ViewData["Token"] = token;
            ViewData["Error"] = response.Message ?? "Password reset failed.";
            return View();
        }

        ViewData["Title"] = "Password reset successful";
        return View("Success");
    }
}
