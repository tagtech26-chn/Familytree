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
}
