using System.Security.Claims;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/families")]
public sealed class FamiliesController(FamilyGroupService families) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new UnauthorizedAccessException("Invalid token subject.");

    [HttpPost]
    public async Task<IActionResult> Create(CreateFamilyRequest request, CancellationToken ct)
    {
        try {
            var created = await families.CreateAsync(CurrentUserId, request, ct);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
        catch (UnauthorizedAccessException) { return Forbid(); }
    }

    [HttpPost("join")]
    public async Task<IActionResult> Join(JoinFamilyRequest request, CancellationToken ct)
    {
        try {
            var joined = await families.JoinAsync(CurrentUserId, request, ct);
            return Ok(joined);
        }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
        catch (KeyNotFoundException) { return NotFound(new ApiError("FAMILY_NOT_FOUND", "The family code is invalid or the family is unavailable.")); }
        catch (InvalidOperationException ex) { return Conflict(new ApiError("ALREADY_A_MEMBER", ex.Message)); }
        catch (UnauthorizedAccessException ex) { return Forbid(); }
    }

    [HttpGet]
    public async Task<IActionResult> ListMine(CancellationToken ct) =>
        Ok(await families.ListMineAsync(CurrentUserId, ct));

    [HttpGet("{familyGroupId:guid}/access")]
    public async Task<IActionResult> CheckAccess(Guid familyGroupId, CancellationToken ct)
    {
        var allowed = await families.HasAccessAsync(CurrentUserId, familyGroupId, ct);
        return allowed ? Ok(new { familyGroupId, access = true }) : NotFound();
    }
}
