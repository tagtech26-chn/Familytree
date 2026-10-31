using System.Security.Cryptography;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FamilyConnect.Infrastructure.Services;

public sealed class FamilyGroupService(FamilyConnectDbContext db)
{
    public async Task<FamilyResponse> CreateAsync(Guid userId, CreateFamilyRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 150)
            throw new ArgumentException("Family name must contain between 2 and 150 characters.");
        if (request.DefaultLanguage is not ("en" or "ta"))
            throw new ArgumentException("DefaultLanguage must be en or ta.");
        if (!await db.Users.AnyAsync(x => x.UserId == userId && x.Status == "Active", ct))
            throw new UnauthorizedAccessException("ACTIVE_VERIFIED_ACCOUNT_REQUIRED");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var family = new FamilyGroup {
            GroupCode = await GenerateUniqueCodeAsync(ct), GroupName = name,
            Description = request.Description?.Trim(), CreatedByUserId = userId,
            DefaultLanguage = request.DefaultLanguage,
            TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId) ? "Asia/Kolkata" : request.TimeZoneId.Trim(),
            Status = "Active"
        };
        db.FamilyGroups.Add(family);
        await db.SaveChangesAsync(ct);
        db.FamilyGroupMemberships.Add(new FamilyGroupMembership {
            FamilyGroupId = family.FamilyGroupId, UserId = userId, RoleCode = "Owner", Status = "Active"
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new FamilyResponse(family.FamilyGroupId, family.GroupCode, family.GroupName,
            family.Description, "Owner", family.Status, family.CreatedAtUtc);
    }

    public async Task<FamilyResponse> JoinAsync(Guid userId, JoinFamilyRequest request, CancellationToken ct)
    {
        var groupCode = request.GroupCode?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(groupCode) || groupCode.Length > 20)
            throw new ArgumentException("A valid family group code is required.");

        if (!await db.Users.AnyAsync(x => x.UserId == userId && x.Status == "Active", ct))
            throw new UnauthorizedAccessException("ACTIVE_VERIFIED_ACCOUNT_REQUIRED");

        var family = await db.FamilyGroups.SingleOrDefaultAsync(
            x => x.GroupCode == groupCode && x.Status == "Active", ct);
        if (family is null)
            throw new KeyNotFoundException("FAMILY_NOT_FOUND");

        var membership = await db.FamilyGroupMemberships.SingleOrDefaultAsync(
            x => x.FamilyGroupId == family.FamilyGroupId && x.UserId == userId, ct);

        if (membership is not null)
        {
            if (membership.Status == "Active")
                throw new InvalidOperationException("ALREADY_A_MEMBER");
            if (membership.Status == "Suspended")
                throw new UnauthorizedAccessException("MEMBERSHIP_SUSPENDED");

            // A previous invitation or a user who left may rejoin the same family.
            membership.RoleCode = "Member";
            membership.Status = "Active";
            membership.JoinedAtUtc = DateTime.UtcNow;
        }
        else
        {
            membership = new FamilyGroupMembership
            {
                FamilyGroupId = family.FamilyGroupId,
                UserId = userId,
                RoleCode = "Member",
                Status = "Active"
            };
            db.FamilyGroupMemberships.Add(membership);
        }

        await db.SaveChangesAsync(ct);
        return new FamilyResponse(family.FamilyGroupId, family.GroupCode, family.GroupName,
            family.Description, membership.RoleCode, family.Status, family.CreatedAtUtc);
    }

    public async Task<IReadOnlyList<FamilyResponse>> ListMineAsync(Guid userId, CancellationToken ct)
    {
        return await (from membership in db.FamilyGroupMemberships
                      join family in db.FamilyGroups on membership.FamilyGroupId equals family.FamilyGroupId
                      where membership.UserId == userId && membership.Status == "Active" && family.Status == "Active"
                      orderby family.GroupName
                      select new FamilyResponse(family.FamilyGroupId, family.GroupCode, family.GroupName,
                          family.Description, membership.RoleCode, family.Status, family.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public Task<bool> HasAccessAsync(Guid userId, Guid familyGroupId, CancellationToken ct) =>
        db.FamilyGroupMemberships.AnyAsync(x => x.UserId == userId && x.FamilyGroupId == familyGroupId
            && x.Status == "Active", ct);

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (var i = 0; i < 10; i++) {
            var bytes = RandomNumberGenerator.GetBytes(6);
            var code = "FAM-" + new string(bytes.Select(b => alphabet[b % alphabet.Length]).ToArray());
            if (!await db.FamilyGroups.AnyAsync(x => x.GroupCode == code, ct)) return code;
        }
        throw new InvalidOperationException("Unable to generate a unique family code.");
    }
}
