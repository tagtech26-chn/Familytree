using FamilyConnect.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FamilyConnect.Infrastructure.Persistence;

public sealed class FamilyConnectDbContext(DbContextOptions<FamilyConnectDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<FamilyGroup> FamilyGroups => Set<FamilyGroup>();
    public DbSet<FamilyGroupMembership> FamilyGroupMemberships => Set<FamilyGroupMembership>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<AppUser>(e =>
        {
            e.ToTable("UserAccounts"); e.HasKey(x => x.UserId);
            e.Property(x => x.MobileNumber).HasMaxLength(20);
            e.Property(x => x.NormalizedMobileNumber).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(254);
            e.Property(x => x.NormalizedEmail).HasMaxLength(254);
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.PreferredLanguage).HasMaxLength(2).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.NormalizedMobileNumber).IsUnique().HasFilter("[NormalizedMobileNumber] IS NOT NULL");
            e.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("[NormalizedEmail] IS NOT NULL");
        });
        b.Entity<OtpChallenge>(e =>
        {
            e.ToTable("OtpChallenges"); e.HasKey(x => x.OtpChallengeId);
            e.Property(x => x.Destination).HasMaxLength(254).IsRequired();
            e.Property(x => x.Purpose).HasMaxLength(30).IsRequired();
            e.Property(x => x.CodeHash).HasMaxLength(48).IsRequired();
            e.HasIndex(x => new { x.UserId, x.Destination, x.Purpose, x.CreatedAtUtc });
        });
        b.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens"); e.HasKey(x => x.RefreshTokenId);
            e.Property(x => x.TokenHash).HasMaxLength(32).IsRequired();
            e.Property(x => x.ReplacedByTokenHash).HasMaxLength(32);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.UserId, x.ExpiresAtUtc });
        });
        b.Entity<FamilyGroup>(e =>
        {
            e.ToTable("FamilyGroups"); e.HasKey(x => x.FamilyGroupId);
            e.Property(x => x.GroupCode).HasMaxLength(20).IsRequired();
            e.Property(x => x.GroupName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.DefaultLanguage).HasMaxLength(2).IsRequired();
            e.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.GroupCode).IsUnique();
        });
        b.Entity<FamilyGroupMembership>(e =>
        {
            e.ToTable("FamilyGroupMemberships"); e.HasKey(x => x.MembershipId);
            e.Property(x => x.RoleCode).HasMaxLength(30).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).IsRequired();
            e.HasIndex(x => new { x.FamilyGroupId, x.UserId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Status });
        });
    }
}
