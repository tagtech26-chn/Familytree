using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using FamilyConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace FamilyConnect.Tests;

public sealed class FamilyGroupServiceTests
{
    private static FamilyConnectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<FamilyConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new FamilyConnectDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_CreatesFamilyAndOwnerMembership()
    {
        await using var db = CreateDb();
        var user = new AppUser { Status = "Active", PasswordHash = "test-hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);

        var family = await service.CreateAsync(user.UserId,
            new("My Family", "Test group", "en", "Asia/Kolkata"), CancellationToken.None);

        Assert.Equal("Owner", family.Role);
        Assert.StartsWith("FAM-", family.GroupCode);
        Assert.Single(db.FamilyGroups);
        var membership = await db.FamilyGroupMemberships.SingleAsync();
        Assert.Equal(user.UserId, membership.UserId);
        Assert.Equal(family.FamilyGroupId, membership.FamilyGroupId);
        Assert.Equal("Owner", membership.RoleCode);
    }

    [Fact]
    public async Task CreateAsync_RejectsPendingAccount()
    {
        await using var db = CreateDb();
        var user = new AppUser { Status = "Pending", PasswordHash = "test-hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CreateAsync(user.UserId, new("Test Family", null), CancellationToken.None));
    }

    [Fact]
    public async Task UserCanCreateMultipleFamilyGroups()
    {
        await using var db = CreateDb();
        var user = new AppUser { Status = "Active", PasswordHash = "test-hash" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);

        await service.CreateAsync(user.UserId, new("Family One", null), CancellationToken.None);
        await service.CreateAsync(user.UserId, new("Family Two", null), CancellationToken.None);

        Assert.Equal(2, await db.FamilyGroups.CountAsync());
        Assert.Equal(2, await db.FamilyGroupMemberships.CountAsync(x => x.UserId == user.UserId));
    }

    [Fact]
    public async Task JoinAsync_CreatesPendingRequestWithoutGrantingAccess()
    {
        await using var db = CreateDb();
        var owner = new AppUser { Status = "Active", PasswordHash = "owner-hash" };
        var member = new AppUser { Status = "Active", PasswordHash = "member-hash" };
        db.Users.AddRange(owner, member);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);
        var created = await service.CreateAsync(owner.UserId, new("Shared Family", null), CancellationToken.None);

        var joined = await service.JoinAsync(member.UserId,
            new(created.GroupCode.ToLowerInvariant()), CancellationToken.None);

        Assert.Equal("Member", joined.Role);
        Assert.Equal("Pending", joined.MembershipStatus);
        Assert.False(await service.HasAccessAsync(member.UserId, created.FamilyGroupId, CancellationToken.None));
        Assert.Equal("Owner", (await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == owner.UserId)).RoleCode);
        Assert.Equal("Pending", (await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == member.UserId)).Status);
    }

    [Fact]
    public async Task OwnerCanApprovePendingJoinRequest()
    {
        await using var db = CreateDb();
        var owner = new AppUser { Status = "Active", PasswordHash = "owner-hash" };
        var member = new AppUser { Status = "Active", PasswordHash = "member-hash" };
        db.Users.AddRange(owner, member);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);
        var family = await service.CreateAsync(owner.UserId, new("Shared Family", null), CancellationToken.None);
        await service.JoinAsync(member.UserId, new(family.GroupCode), CancellationToken.None);
        var pending = await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == member.UserId);

        await service.ApproveJoinRequestAsync(owner.UserId, family.FamilyGroupId, pending.MembershipId, CancellationToken.None);

        Assert.True(await service.HasAccessAsync(member.UserId, family.FamilyGroupId, CancellationToken.None));
        Assert.Equal("Active", pending.Status);
        Assert.Equal("Owner", (await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == owner.UserId)).RoleCode);
    }

    [Fact]
    public async Task MemberCannotApproveJoinRequest()
    {
        await using var db = CreateDb();
        var owner = new AppUser { Status = "Active", PasswordHash = "owner-hash" };
        var member = new AppUser { Status = "Active", PasswordHash = "member-hash" };
        var applicant = new AppUser { Status = "Active", PasswordHash = "applicant-hash" };
        db.Users.AddRange(owner, member, applicant);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);
        var family = await service.CreateAsync(owner.UserId, new("Shared Family", null), CancellationToken.None);
        await service.JoinAsync(member.UserId, new(family.GroupCode), CancellationToken.None);
        await service.ApproveJoinRequestAsync(owner.UserId, family.FamilyGroupId,
            (await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == member.UserId)).MembershipId, CancellationToken.None);
        await service.JoinAsync(applicant.UserId, new(family.GroupCode), CancellationToken.None);
        var pending = await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == applicant.UserId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApproveJoinRequestAsync(member.UserId, family.FamilyGroupId, pending.MembershipId, CancellationToken.None));
        Assert.Equal("Pending", pending.Status);
    }

    [Fact]
    public async Task OwnerCanRejectPendingJoinRequest()
    {
        await using var db = CreateDb();
        var owner = new AppUser { Status = "Active", PasswordHash = "owner-hash" };
        var applicant = new AppUser { Status = "Active", PasswordHash = "applicant-hash" };
        db.Users.AddRange(owner, applicant);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);
        var family = await service.CreateAsync(owner.UserId, new("Shared Family", null), CancellationToken.None);
        await service.JoinAsync(applicant.UserId, new(family.GroupCode), CancellationToken.None);
        var pending = await db.FamilyGroupMemberships.SingleAsync(x => x.UserId == applicant.UserId);

        await service.RejectJoinRequestAsync(owner.UserId, family.FamilyGroupId, pending.MembershipId, CancellationToken.None);

        Assert.Equal("Left", pending.Status);
        Assert.False(await service.HasAccessAsync(applicant.UserId, family.FamilyGroupId, CancellationToken.None));
    }

    [Fact]
    public async Task JoinAsync_RejectsExistingActiveMember()
    {
        await using var db = CreateDb();
        var owner = new AppUser { Status = "Active", PasswordHash = "owner-hash" };
        db.Users.Add(owner);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);
        var created = await service.CreateAsync(owner.UserId,
            new("Shared Family", null), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.JoinAsync(owner.UserId, new(created.GroupCode), CancellationToken.None));
    }

    [Fact]
    public async Task JoinAsync_RejectsUnknownGroupCode()
    {
        await using var db = CreateDb();
        var member = new AppUser { Status = "Active", PasswordHash = "member-hash" };
        db.Users.Add(member);
        await db.SaveChangesAsync();
        var service = new FamilyGroupService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.JoinAsync(member.UserId, new("FAM-UNKNOWN"), CancellationToken.None));
    }

}
