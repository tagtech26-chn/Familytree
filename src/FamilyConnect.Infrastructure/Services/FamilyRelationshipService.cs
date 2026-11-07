using FamilyConnect.Application.Contracts;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyConnect.Infrastructure.Services;

public sealed class FamilyRelationshipService(FamilyConnectDbContext db)
{
    private static readonly string[] AllowedTypes = ["ParentChild", "Spouse"];

    public async Task<IReadOnlyList<FamilyRelationshipResponse>> ListAsync(
        Guid userId, Guid familyGroupId, CancellationToken ct)
    {
        await EnsureAccessAsync(userId, familyGroupId, ct);
        // Materialize the family-scoped result before sorting by DTO properties.
        // EF Core cannot translate OrderBy over the constructed response record.
        var relationships = await Query(familyGroupId).ToListAsync(ct);
        return relationships
            .OrderBy(x => x.FromFullName)
            .ThenBy(x => x.ToFullName)
            .ToList();
    }

    public async Task<FamilyRelationshipResponse> CreateAsync(
        Guid userId, Guid familyGroupId, CreateFamilyRelationshipRequest request, CancellationToken ct)
    {
        await EnsureEditorAsync(userId, familyGroupId, ct);
        if (request.FromFamilyMemberId == request.ToFamilyMemberId)
            throw new ArgumentException("A family member cannot have a relationship with themselves.");
        if (!AllowedTypes.Contains(request.RelationshipType))
            throw new ArgumentException("RelationshipType must be ParentChild or Spouse.");

        var ids = new[] { request.FromFamilyMemberId, request.ToFamilyMemberId };
        var profiles = await db.FamilyMembers
            .Where(x => x.FamilyGroupId == familyGroupId && ids.Contains(x.FamilyMemberId) && x.Status != "Archived")
            .Select(x => x.FamilyMemberId).ToListAsync(ct);
        if (profiles.Count != 2)
            throw new ArgumentException("Both profiles must exist and be available in this family.");

        var fromId = request.FromFamilyMemberId;
        var toId = request.ToFamilyMemberId;
        if (request.RelationshipType == "Spouse" && fromId.CompareTo(toId) > 0)
            (fromId, toId) = (toId, fromId);

        var exists = await db.FamilyRelationships.AnyAsync(x =>
            x.FamilyGroupId == familyGroupId && x.FromFamilyMemberId == fromId
            && x.ToFamilyMemberId == toId && x.RelationshipType == request.RelationshipType, ct);
        if (exists) throw new InvalidOperationException("RELATIONSHIP_ALREADY_EXISTS");

        if (request.RelationshipType == "ParentChild")
        {
            var reverseExists = await db.FamilyRelationships.AnyAsync(x =>
                x.FamilyGroupId == familyGroupId && x.FromFamilyMemberId == toId
                && x.ToFamilyMemberId == fromId && x.RelationshipType == "ParentChild", ct);
            if (reverseExists)
                throw new ArgumentException("This relationship conflicts with an existing parent-child direction.");
        }

        var relationship = new FamilyRelationship {
            FamilyGroupId = familyGroupId, FromFamilyMemberId = fromId, ToFamilyMemberId = toId,
            RelationshipType = request.RelationshipType, CreatedByUserId = userId, CreatedAtUtc = DateTime.UtcNow
        };
        db.FamilyRelationships.Add(relationship);
        await db.SaveChangesAsync(ct);
        return await (
            from r in db.FamilyRelationships.AsNoTracking()
            join fromMember in db.FamilyMembers
                on r.FromFamilyMemberId equals fromMember.FamilyMemberId
            join toMember in db.FamilyMembers
                on r.ToFamilyMemberId equals toMember.FamilyMemberId
            where r.FamilyGroupId == familyGroupId
                  && r.FamilyRelationshipId == relationship.FamilyRelationshipId
            select new FamilyRelationshipResponse(
                r.FamilyRelationshipId,
                r.FamilyGroupId,
                r.FromFamilyMemberId,
                fromMember.FullName,
                fromMember.TamilName,
                r.ToFamilyMemberId,
                toMember.FullName,
                toMember.TamilName,
                r.RelationshipType,
                r.CreatedAtUtc
            )
        ).SingleAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, Guid familyGroupId, Guid relationshipId, CancellationToken ct)
    {
        await EnsureEditorAsync(userId, familyGroupId, ct);
        var relationship = await db.FamilyRelationships.SingleOrDefaultAsync(
            x => x.FamilyGroupId == familyGroupId && x.FamilyRelationshipId == relationshipId, ct);
        if (relationship is null) throw new KeyNotFoundException("FAMILY_RELATIONSHIP_NOT_FOUND");
        db.FamilyRelationships.Remove(relationship);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<FamilyRelationshipResponse> Query(Guid familyGroupId) =>
        from r in db.FamilyRelationships.AsNoTracking()
        join fromMember in db.FamilyMembers on r.FromFamilyMemberId equals fromMember.FamilyMemberId
        join toMember in db.FamilyMembers on r.ToFamilyMemberId equals toMember.FamilyMemberId
        where r.FamilyGroupId == familyGroupId
        select new FamilyRelationshipResponse(r.FamilyRelationshipId, r.FamilyGroupId,
            r.FromFamilyMemberId, fromMember.FullName, fromMember.TamilName,
            r.ToFamilyMemberId, toMember.FullName, toMember.TamilName,
            r.RelationshipType, r.CreatedAtUtc);

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
        if (!allowed) throw new UnauthorizedAccessException("FAMILY_RELATIONSHIP_EDIT_PERMISSION_REQUIRED");
    }
}
