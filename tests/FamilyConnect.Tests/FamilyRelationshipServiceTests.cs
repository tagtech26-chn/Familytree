using FamilyConnect.Application.Contracts;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using FamilyConnect.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FamilyConnect.Tests;

public sealed class FamilyRelationshipServiceTests
{
    private static async Task<(FamilyConnectDbContext Db, FamilyRelationshipService Service, Guid FamilyId, Guid OwnerId, Guid ParentId, Guid ChildId)> SeedAsync()
    {
        var db = new FamilyConnectDbContext(new DbContextOptionsBuilder<FamilyConnectDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var owner = new AppUser { Status = "Active", PasswordHash = "test" };
        var family = new FamilyGroup { GroupCode = "FAM-REL001", GroupName = "Relationship Test", CreatedByUserId = owner.UserId, Status = "Active" };
        var parent = new FamilyMember { FamilyGroupId = family.FamilyGroupId, FullName = "Parent" };
        var child = new FamilyMember { FamilyGroupId = family.FamilyGroupId, FullName = "Child" };
        db.Users.Add(owner);
        db.FamilyGroups.Add(family);
        db.FamilyMembers.AddRange(parent, child);
        db.FamilyGroupMemberships.Add(new FamilyGroupMembership
        { FamilyGroupId = family.FamilyGroupId, UserId = owner.UserId, RoleCode = "Owner", Status = "Active" });
        await db.SaveChangesAsync();
        return (db, new FamilyRelationshipService(db), family.FamilyGroupId, owner.UserId, parent.FamilyMemberId, child.FamilyMemberId);
    }

    [Fact]
    public async Task OwnerCanCreateAndReadParentChildRelationship()
    {
        var (db, service, familyId, ownerId, parentId, childId) = await SeedAsync();
        await using (db)
        {
            var created = await service.CreateAsync(ownerId, familyId,
                new CreateFamilyRelationshipRequest(parentId, childId, "ParentChild"), CancellationToken.None);
            Assert.Equal("ParentChild", created.RelationshipType);
            Assert.Equal("Parent", created.FromFullName);
            Assert.Equal("Child", created.ToFullName);
            Assert.Single(await service.ListAsync(ownerId, familyId, CancellationToken.None));
        }
    }

    [Fact]
    public async Task MemberCannotCreateRelationship()
    {
        var (db, service, familyId, ownerId, parentId, childId) = await SeedAsync();
        await using (db)
        {
            var member = new AppUser { Status = "Active", PasswordHash = "member" };
            db.Users.Add(member);
            db.FamilyGroupMemberships.Add(new FamilyGroupMembership
            { FamilyGroupId = familyId, UserId = member.UserId, RoleCode = "Member", Status = "Active" });
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.CreateAsync(member.UserId, familyId,
                new CreateFamilyRelationshipRequest(parentId, childId, "ParentChild"), CancellationToken.None));
        }
    }

    [Fact]
    public async Task RejectsSelfAndReverseParentChildRelationships()
    {
        var (db, service, familyId, ownerId, parentId, childId) = await SeedAsync();
        await using (db)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(ownerId, familyId,
                new CreateFamilyRelationshipRequest(parentId, parentId, "ParentChild"), CancellationToken.None));
            await service.CreateAsync(ownerId, familyId,
                new CreateFamilyRelationshipRequest(parentId, childId, "ParentChild"), CancellationToken.None);
            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(ownerId, familyId,
                new CreateFamilyRelationshipRequest(childId, parentId, "ParentChild"), CancellationToken.None));
        }
    }
}
