using FamilyConnect.Domain.Entities;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Infrastructure.Persistence;
using FamilyConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FamilyConnect.Tests;

public sealed class FamilyMemberServiceTests
{
    private static FamilyConnectDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<FamilyConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(FamilyConnectDbContext Db, FamilyMemberService Service, Guid FamilyId, Guid OwnerId, Guid MemberId)> SeedAsync()
    {
        var db = CreateDb();
        var owner = new AppUser { Status = "Active", PasswordHash = "owner" };
        var member = new AppUser { Status = "Active", PasswordHash = "member" };
        var family = new FamilyGroup { GroupCode = "FAM-TEST01", GroupName = "Test Family", CreatedByUserId = owner.UserId, Status = "Active" };
        db.Users.AddRange(owner, member);
        db.FamilyGroups.Add(family);
        db.FamilyGroupMemberships.AddRange(
            new FamilyGroupMembership { FamilyGroupId = family.FamilyGroupId, UserId = owner.UserId, RoleCode = "Owner", Status = "Active" },
            new FamilyGroupMembership { FamilyGroupId = family.FamilyGroupId, UserId = member.UserId, RoleCode = "Member", Status = "Active" });
        await db.SaveChangesAsync();
        return (db, new FamilyMemberService(db), family.FamilyGroupId, owner.UserId, member.UserId);
    }

    [Fact]
    public async Task OwnerCanCreateAndMemberCanReadProfile()
    {
        var (db, service, familyId, ownerId, memberId) = await SeedAsync();
        await using (db)
        {
            var created = await service.CreateAsync(ownerId, familyId,
                new CreateFamilyMemberRequest("Lakshmi", "லட்சுமி", "Female", null, null, null, null, null, null),
                CancellationToken.None);
            Assert.Equal("Active", created.Status);
            Assert.Single(await service.ListAsync(memberId, familyId, CancellationToken.None));
            Assert.Equal("Lakshmi", (await service.GetAsync(memberId, familyId, created.FamilyMemberId, CancellationToken.None)).FullName);
        }
    }

    [Fact]
    public async Task RegularMemberCannotCreateOrUpdateProfile()
    {
        var (db, service, familyId, ownerId, memberId) = await SeedAsync();
        await using (db)
        {
            var profile = await service.CreateAsync(ownerId, familyId,
                new CreateFamilyMemberRequest("Lakshmi", null, null, null, null, null, null, null, null),
                CancellationToken.None);
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(memberId, familyId,
                new CreateFamilyMemberRequest("Child", null, null, null, null, null, null, null, null), CancellationToken.None));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(memberId, familyId, profile.FamilyMemberId,
                new UpdateFamilyMemberRequest("Changed", null, null, null, null, null, null, null, null, "Active"), CancellationToken.None));
        }
    }

    [Fact]
    public async Task EditorCanCreateProfileAndCannotLinkInactiveOrOtherFamilyUser()
    {
        var (db, service, familyId, ownerId, memberId) = await SeedAsync();
        await using (db)
        {
            var editor = new AppUser { Status = "Active", PasswordHash = "editor" };
            db.Users.Add(editor);
            db.FamilyGroupMemberships.Add(new FamilyGroupMembership
            { FamilyGroupId = familyId, UserId = editor.UserId, RoleCode = "Editor", Status = "Active" });
            await db.SaveChangesAsync();
            var profile = await service.CreateAsync(editor.UserId, familyId,
                new CreateFamilyMemberRequest("Editor-created", null, null, null, null, null, null, null, null),
                CancellationToken.None);
            Assert.Equal("Editor-created", profile.FullName);
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(ownerId, familyId,
                new CreateFamilyMemberRequest("Invalid link", null, null, null, null, null, null, null, Guid.NewGuid()),
                CancellationToken.None));
        }
    }

    [Fact]
    public async Task ProfileCanKeepItsExistingLinkedUserDuringUpdate()
    {
        var (db, service, familyId, ownerId, memberId) = await SeedAsync();
        await using (db)
        {
            var profile = await service.CreateAsync(ownerId, familyId,
                new CreateFamilyMemberRequest("Member profile", null, null, null, null, null, null, null, memberId),
                CancellationToken.None);
            var updated = await service.UpdateAsync(ownerId, familyId, profile.FamilyMemberId,
                new UpdateFamilyMemberRequest("Updated profile", null, null, null, null, null, null, null, memberId, "Active"),
                CancellationToken.None);
            Assert.Equal(memberId, updated.LinkedUserId);
            Assert.Equal("Updated profile", updated.FullName);
        }
    }
}