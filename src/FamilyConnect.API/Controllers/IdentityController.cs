using System.Security.Claims;
using FamilyConnect.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public sealed class IdentityController : ControllerBase
{
    [HttpGet]
    public IActionResult GetMe()
    {
        var subject = User.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId)) return Unauthorized();
        return Ok(new {
            userId,
            preferredLanguage = User.FindFirstValue("preferred_language") ?? "en"
        });
    }
}
