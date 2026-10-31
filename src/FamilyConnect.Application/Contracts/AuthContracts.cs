namespace FamilyConnect.Application.Contracts;

public sealed record RegisterRequest(string? MobileNumber, string? Email, string Password, string PreferredLanguage = "en");
public sealed record RegisterResponse(Guid UserId, bool VerificationRequired, string Delivery);
public sealed record VerifyOtpRequest(string Destination, string Purpose, string Code);
public sealed record LoginRequest(string Login, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAtUtc, Guid UserId);
public sealed record CreateFamilyRequest(string Name, string? Description, string DefaultLanguage = "en", string TimeZoneId = "Asia/Kolkata");
public sealed record JoinFamilyRequest(string GroupCode);
public sealed record FamilyMembershipRequestResponse(Guid MembershipId, Guid UserId, string? MobileNumber, string? Email, DateTime RequestedAtUtc);
public sealed record FamilyResponse(Guid FamilyGroupId, string GroupCode, string Name, string? Description, string Role, string Status, DateTime CreatedAtUtc);
public sealed record ApiError(string Code, string Message);
