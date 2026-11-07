using System.Security.Claims;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/families/{familyGroupId:guid}/relationships")]
public sealed class FamilyRelationshipsController(FamilyRelationshipService relationships) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new UnauthorizedAccessException("Invalid token subject.");

    [HttpGet]
    public async Task<IActionResult> List(Guid familyGroupId, CancellationToken ct)
    {
        try { return Ok(await relationships.ListAsync(CurrentUserId, familyGroupId, ct)); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid familyGroupId, CreateFamilyRelationshipRequest request, CancellationToken ct)
    {
        try { return StatusCode(StatusCodes.Status201Created,
            await relationships.CreateAsync(CurrentUserId, familyGroupId, request, ct)); }
        catch (UnauthorizedAccessException ex) when (ex.Message == "ACTIVE_FAMILY_MEMBERSHIP_REQUIRED") { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
        catch (InvalidOperationException ex) when (ex.Message == "RELATIONSHIP_ALREADY_EXISTS")
        {
            return Conflict(new ApiError(
                "RELATIONSHIP_ALREADY_EXISTS",
                "The relationship already exists."));
        }
    }

    [HttpDelete("{relationshipId:guid}")]
    public async Task<IActionResult> Delete(Guid familyGroupId, Guid relationshipId, CancellationToken ct)
    {
        try { await relationships.DeleteAsync(CurrentUserId, familyGroupId, relationshipId, ct); return NoContent(); }
        catch (UnauthorizedAccessException ex) when (ex.Message == "ACTIVE_FAMILY_MEMBERSHIP_REQUIRED") { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException) { return NotFound(new ApiError("FAMILY_RELATIONSHIP_NOT_FOUND", "The relationship was not found.")); }
    }
}
