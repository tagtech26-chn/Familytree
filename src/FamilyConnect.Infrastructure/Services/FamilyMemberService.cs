using FamilyConnect.Application.Contracts;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyConnect.Infrastructure.Services;

public sealed class FamilyMemberService(FamilyConnectDbContext db)
{
    private static FamilyMemberResponse ToResponse(FamilyMember x) => new(
        x.FamilyMemberId, x.FamilyGroupId, x.LinkedUserId, x.FullName, x.TamilName,
        x.Gender, x.DateOfBirth, x.PlaceOfBirth, x.DateOfDeath, x.ContactNumber,
        x.Email, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc);

    public async Task<IReadOnlyList<FamilyMemberResponse>> ListAsync(
        Guid userId, Guid familyGroupId, CancellationToken ct)
    {
        await EnsureAccessAsync(userId, familyGroupId, ct);
        return await db.FamilyMembers.AsNoTracking()
            .Where(x => x.FamilyGroupId == familyGroupId && x.Status != "Archived")
            .OrderBy(x => x.FullName)
            .Select(x => new FamilyMemberResponse(x.FamilyMemberId, x.FamilyGroupId, x.LinkedUserId,
                x.FullName, x.TamilName, x.Gender, x.DateOfBirth, x.PlaceOfBirth, x.DateOfDeath,
                x.ContactNumber, x.Email, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<FamilyMemberResponse> GetAsync(
        Guid userId, Guid familyGroupId, Guid familyMemberId, CancellationToken ct)
    {
        await EnsureAccessAsync(userId, familyGroupId, ct);
        var member = await db.FamilyMembers.AsNoTracking().SingleOrDefaultAsync(
            x => x.FamilyGroupId == familyGroupId && x.FamilyMemberId == familyMemberId
                 && x.Status != "Archived", ct);
        return member is null ? throw new KeyNotFoundException("FAMILY_MEMBER_NOT_FOUND") : ToResponse(member);
    }

    public async Task<FamilyMemberResponse> CreateAsync(
        Guid userId, Guid familyGroupId, CreateFamilyMemberRequest request, CancellationToken ct)
    {
        await EnsureEditorAsync(userId, familyGroupId, ct);
        Validate(request.FullName, request.Gender, request.DateOfBirth, request.DateOfDeath,
            request.TamilName, request.PlaceOfBirth, request.ContactNumber, request.Email);
        await ValidateLinkedUserAsync(familyGroupId, request.LinkedUserId, null, ct);

        var now = DateTime.UtcNow;
        var member = new FamilyMember {
            FamilyGroupId = familyGroupId, LinkedUserId = request.LinkedUserId,
            FullName = request.FullName.Trim(), TamilName = Clean(request.TamilName),
            Gender = Clean(request.Gender), DateOfBirth = request.DateOfBirth,
            PlaceOfBirth = Clean(request.PlaceOfBirth), DateOfDeath = request.DateOfDeath,
            ContactNumber = Clean(request.ContactNumber), Email = Clean(request.Email),
            Status = request.DateOfDeath.HasValue ? "Deceased" : "Active",
            CreatedAtUtc = now, UpdatedAtUtc = now
        };
        db.FamilyMembers.Add(member);
        await db.SaveChangesAsync(ct);
        return ToResponse(member);
    }

    public async Task<FamilyMemberResponse> UpdateAsync(
        Guid userId, Guid familyGroupId, Guid familyMemberId,
        UpdateFamilyMemberRequest request, CancellationToken ct)
    {
        await EnsureEditorAsync(userId, familyGroupId, ct);
        Validate(request.FullName, request.Gender, request.DateOfBirth, request.DateOfDeath,
            request.TamilName, request.PlaceOfBirth, request.ContactNumber, request.Email);
        if (request.Status is not ("Active" or "Deceased" or "Archived"))
            throw new ArgumentException("Status must be Active, Deceased or Archived.");
        if (request.DateOfDeath.HasValue && request.Status == "Active")
            throw new ArgumentException("A member with a date of death cannot have Active status.");
        var member = await db.FamilyMembers.SingleOrDefaultAsync(
            x => x.FamilyGroupId == familyGroupId && x.FamilyMemberId == familyMemberId, ct);
        if (member is null) throw new KeyNotFoundException("FAMILY_MEMBER_NOT_FOUND");
        await ValidateLinkedUserAsync(familyGroupId, request.LinkedUserId, familyMemberId, ct);
        member.FullName = request.FullName.Trim();
        member.TamilName = Clean(request.TamilName);
        member.Gender = Clean(request.Gender);
        member.DateOfBirth = request.DateOfBirth;
        member.PlaceOfBirth = Clean(request.PlaceOfBirth);
        member.DateOfDeath = request.DateOfDeath;
        member.ContactNumber = Clean(request.ContactNumber);
        member.Email = Clean(request.Email);
        member.LinkedUserId = request.LinkedUserId;
        member.Status = request.Status;
        member.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToResponse(member);
    }

    private async Task EnsureAccessAsync(Guid userId, Guid familyGroupId, CancellationToken ct)
    {
        var allowed = await (from m in db.FamilyGroupMemberships
                             join f in db.FamilyGroups on m.FamilyGroupId equals f.FamilyGroupId
                             where m.UserId == userId && m.FamilyGroupId == familyGroupId
                                   && m.Status == "Active" && f.Status == "Active"
                             select m.MembershipId).AnyAsync(ct);
        if (!allowed) throw new UnauthorizedAccessException("ACTIVE_FAMILY_MEMBERSHIP_REQUIRED");
    }

    private async Task EnsureEditorAsync(Guid userId, Guid familyGroupId, CancellationToken ct)
    {
        var allowed = await (from m in db.FamilyGroupMemberships
                             join f in db.FamilyGroups on m.FamilyGroupId equals f.FamilyGroupId
                             where m.UserId == userId && m.FamilyGroupId == familyGroupId
                                   && m.Status == "Active" && f.Status == "Active"
                                   && (m.RoleCode == "Owner" || m.RoleCode == "Admin" || m.RoleCode == "Editor")
                             select m.MembershipId).AnyAsync(ct);
        if (!allowed) throw new UnauthorizedAccessException("FAMILY_PROFILE_EDIT_PERMISSION_REQUIRED");
    }

    private async Task ValidateLinkedUserAsync(Guid familyGroupId, Guid? linkedUserId, Guid? excludeFamilyMemberId, CancellationToken ct)
    {
        if (!linkedUserId.HasValue) return;
        var isActiveMember = await db.FamilyGroupMemberships.AnyAsync(
            x => x.FamilyGroupId == familyGroupId && x.UserId == linkedUserId.Value && x.Status == "Active", ct);
        if (!isActiveMember)
            throw new ArgumentException("LinkedUserId must belong to an active member of this family group.");
        var alreadyLinked = await db.FamilyMembers.AnyAsync(
            x => x.FamilyGroupId == familyGroupId && x.LinkedUserId == linkedUserId.Value
                 && x.Status != "Archived" && (!excludeFamilyMemberId.HasValue || x.FamilyMemberId != excludeFamilyMemberId.Value), ct);
        if (alreadyLinked)
            throw new ArgumentException("This login account is already linked to a family-member profile.");
    }

    private static void Validate(string fullName, string? gender, DateOnly? dob, DateOnly? dod,
        string? tamilName, string? place, string? contact, string? email)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Trim().Length > 200)
            throw new ArgumentException("FullName is required and must not exceed 200 characters.");
        if (tamilName?.Length > 200 || place?.Length > 150 || contact?.Length > 30 || email?.Length > 254)
            throw new ArgumentException("One or more profile fields exceed their allowed length.");
        if (gender is not null && gender is not ("Male" or "Female" or "Other" or "PreferNotToSay"))
            throw new ArgumentException("Gender must be Male, Female, Other or PreferNotToSay.");
        if (dob.HasValue && dob.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("DateOfBirth cannot be in the future.");
        if (dob.HasValue && dod.HasValue && dod.Value < dob.Value)
            throw new ArgumentException("DateOfDeath cannot be earlier than DateOfBirth.");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
