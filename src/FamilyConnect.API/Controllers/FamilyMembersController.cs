using System.Security.Claims;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FamilyConnect.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/families/{familyGroupId:guid}/members")]
public sealed class FamilyMembersController(FamilyMemberService members) : ControllerBase
{
    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id : throw new UnauthorizedAccessException("Invalid token subject.");

    [HttpGet]
    public async Task<IActionResult> List(Guid familyGroupId, CancellationToken ct)
    {
        try { return Ok(await members.ListAsync(CurrentUserId, familyGroupId, ct)); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }

    [HttpGet("{familyMemberId:guid}")]
    public async Task<IActionResult> Get(Guid familyGroupId, Guid familyMemberId, CancellationToken ct)
    {
        try { return Ok(await members.GetAsync(CurrentUserId, familyGroupId, familyMemberId, ct)); }
        catch (UnauthorizedAccessException) { return NotFound(); }
        catch (KeyNotFoundException) { return NotFound(new ApiError("FAMILY_MEMBER_NOT_FOUND", "The family member was not found.")); }
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid familyGroupId, CreateFamilyMemberRequest request, CancellationToken ct)
    {
        try
        {
            var created = await members.CreateAsync(CurrentUserId, familyGroupId, request, ct);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "ACTIVE_FAMILY_MEMBERSHIP_REQUIRED") { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
    }

    [HttpPut("{familyMemberId:guid}")]
    public async Task<IActionResult> Update(Guid familyGroupId, Guid familyMemberId, UpdateFamilyMemberRequest request, CancellationToken ct)
    {
        try { return Ok(await members.UpdateAsync(CurrentUserId, familyGroupId, familyMemberId, request, ct)); }
        catch (UnauthorizedAccessException ex) when (ex.Message == "ACTIVE_FAMILY_MEMBERSHIP_REQUIRED") { return NotFound(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new ApiError("VALIDATION_ERROR", ex.Message)); }
        catch (KeyNotFoundException) { return NotFound(new ApiError("FAMILY_MEMBER_NOT_FOUND", "The family member was not found.")); }
    }
}