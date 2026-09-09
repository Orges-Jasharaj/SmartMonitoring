using System.Security.Claims;
using NUnit.Framework;
using SmartMonitoring.Shared.Helpers;

namespace SmartMonitoring.Tests;

[TestFixture]
public class ClaimsPrincipalExtensionsTests
{
    [Test]
    public void CI004_GetUserId_ReturnsSubClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "user-123"),
        }));

        Assert.That(principal.GetUserId(), Is.EqualTo("user-123"));
    }

    [Test]
    public void CI005_GetUserId_ReturnsNullWhenMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.That(principal.GetUserId(), Is.Null);
    }
}
