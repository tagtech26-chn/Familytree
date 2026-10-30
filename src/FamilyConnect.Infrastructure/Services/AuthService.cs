using System.Security.Cryptography;
using System.Text;
using FamilyConnect.Application.Abstractions;
using FamilyConnect.Application.Contracts;
using FamilyConnect.Domain.Entities;
using FamilyConnect.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FamilyConnect.Infrastructure.Services;

public sealed class AuthService(
    FamilyConnectDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    OtpService otpService,
    ITokenService tokenService)
{
    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var mobile = NormalizeMobile(request.MobileNumber);
        var email = NormalizeEmail(request.Email);
        if (mobile is null && email is null) throw new ArgumentException("Provide a mobile number or email.");
        if (request.PreferredLanguage is not ("en" or "ta")) throw new ArgumentException("PreferredLanguage must be en or ta.");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12)
            throw new ArgumentException("Password must contain at least 12 characters.");
        if (mobile != null && await db.Users.AnyAsync(x => x.NormalizedMobileNumber == mobile, ct))
            throw new InvalidOperationException("ACCOUNT_ALREADY_EXISTS");
        if (email != null && await db.Users.AnyAsync(x => x.NormalizedEmail == email, ct))
            throw new InvalidOperationException("ACCOUNT_ALREADY_EXISTS");

        var user = new AppUser {
            MobileNumber = request.MobileNumber?.Trim(), NormalizedMobileNumber = mobile,
            Email = request.Email?.Trim(), NormalizedEmail = email,
            PreferredLanguage = request.PreferredLanguage, Status = "Pending"
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        var destination = user.MobileNumber ?? user.Email!;
        await otpService.IssueAsync(user, destination, "Registration", ct);
        return new RegisterResponse(user.UserId, true, user.MobileNumber != null ? "sms" : "email");
    }

    public async Task<TokenResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct)
    {
        var mobile = NormalizeMobile(request.Destination);
        var email = NormalizeEmail(request.Destination);
        var user = await db.Users.FirstOrDefaultAsync(x =>
            (mobile != null && x.NormalizedMobileNumber == mobile) ||
            (email != null && x.NormalizedEmail == email), ct);
        if (user is null) throw new InvalidOperationException("INVALID_OR_EXPIRED_OTP");
        if (request.Purpose != "Registration") throw new ArgumentException("Unsupported OTP purpose.");
        var destination = user.MobileNumber ?? user.Email!;
        if (!await otpService.VerifyAsync(user, destination, request.Purpose, request.Code, ct))
            throw new InvalidOperationException("INVALID_OR_EXPIRED_OTP");
        if (user.MobileNumber != null) user.IsMobileVerified = true;
        else user.IsEmailVerified = true;
        user.Status = "Active";
        user.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var mobile = NormalizeMobile(request.Login);
        var email = NormalizeEmail(request.Login);
        var user = await db.Users.FirstOrDefaultAsync(x =>
            (mobile != null && x.NormalizedMobileNumber == mobile) ||
            (email != null && x.NormalizedEmail == email), ct);
        if (user is null || user.Status != "Active") throw new UnauthorizedAccessException("INVALID_CREDENTIALS");
        if ((mobile != null && user.NormalizedMobileNumber == mobile && !user.IsMobileVerified) ||
            (email != null && user.NormalizedEmail == email && !user.IsEmailVerified))
            throw new UnauthorizedAccessException("INVALID_CREDENTIALS");
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) throw new UnauthorizedAccessException("INVALID_CREDENTIALS");
        return await IssueTokensAsync(user, ct);
    }

    public async Task<TokenResponse> RefreshAsync(string raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw)) throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(x =>
            x.TokenHash.SequenceEqual(hash) && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, ct);
        if (stored is null) throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");
        var user = await db.Users.FirstOrDefaultAsync(x => x.UserId == stored.UserId && x.Status == "Active", ct);
        if (user is null) throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var tokens = tokenService.CreateTokens(user);
        var newHash = SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken));
        stored.RevokedAtUtc = DateTime.UtcNow;
        stored.ReplacedByTokenHash = newHash;
        db.RefreshTokens.Add(new RefreshToken { UserId = user.UserId, TokenHash = newHash, ExpiresAtUtc = DateTime.UtcNow.AddDays(30) });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return tokens;
    }

    public async Task RevokeRefreshAsync(string raw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var stored = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash.SequenceEqual(hash) && x.RevokedAtUtc == null, ct);
        if (stored is null) return;
        stored.RevokedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<TokenResponse> IssueTokensAsync(AppUser user, CancellationToken ct)
    {
        var tokens = tokenService.CreateTokens(user);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(tokens.RefreshToken));
        db.RefreshTokens.Add(new RefreshToken { UserId = user.UserId, TokenHash = hash, ExpiresAtUtc = DateTime.UtcNow.AddDays(30) });
        await db.SaveChangesAsync(ct);
        return tokens;
    }

    public static string? NormalizeMobile(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new string(value.Trim().Where(c => char.IsDigit(c) || c == '+').ToArray());

    public static string? NormalizeEmail(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
