using System.Security.Cryptography;
using System.Text;
using FamilyConnect.Application.Abstractions;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FamilyConnect.Infrastructure.Services;

public sealed class OtpService(FamilyConnectDbContext db, IOtpSender sender, ILogger<OtpService> logger)
{
    private const int LifetimeMinutes = 5;
    private const int MaxAttempts = 5;

    public async Task IssueAsync(AppUser user, string destination, string purpose, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var recent = await db.OtpChallenges.CountAsync(x =>
            x.UserId == user.UserId && x.CreatedAtUtc >= now.AddMinutes(-15), ct);
        if (recent >= 5) throw new InvalidOperationException("OTP_RATE_LIMITED");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = SHA256.HashData(salt.Concat(Encoding.UTF8.GetBytes(code)).ToArray());

        db.OtpChallenges.Add(new OtpChallenge {
            UserId = user.UserId, Destination = destination, Purpose = purpose,
            CodeHash = salt.Concat(hash).ToArray(),
            ExpiresAtUtc = now.AddMinutes(LifetimeMinutes), CreatedAtUtc = now
        });
        await db.SaveChangesAsync(ct);
        await sender.SendAsync(destination, code, ct);
        logger.LogInformation("OTP challenge issued for user {UserId}, destination {Destination}", user.UserId, Mask(destination));
    }

    public async Task<bool> VerifyAsync(AppUser user, string destination, string purpose, string code, CancellationToken ct)
    {
        var challenge = await db.OtpChallenges
            .Where(x => x.UserId == user.UserId && x.Destination == destination && x.Purpose == purpose && x.ConsumedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (challenge is null || challenge.ExpiresAtUtc <= DateTime.UtcNow || challenge.FailedAttempts >= MaxAttempts)
            return false;
        var stored = challenge.CodeHash;
        if (stored.Length != 48) return false;
        var expected = stored[16..];
        var actual = SHA256.HashData(stored[..16].Concat(Encoding.UTF8.GetBytes(code)).ToArray());
        if (!CryptographicOperations.FixedTimeEquals(expected, actual)) {
            challenge.FailedAttempts++;
            await db.SaveChangesAsync(ct);
            return false;
        }
        challenge.ConsumedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string Mask(string value) {
        if (value.Contains('@')) { var p=value.Split('@'); return (p[0].Length <= 2 ? "**" : p[0][..2]+"***")+"@"+p[1]; }
        return value.Length <= 4 ? "****" : value[..3]+"***"+value[^2..];
    }
}
