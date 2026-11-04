namespace FamilyConnect.Application.Contracts;

public sealed record CreateFamilyMemberRequest(
    string FullName,
    string? TamilName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? PlaceOfBirth,
    DateOnly? DateOfDeath,
    string? ContactNumber,
    string? Email,
    Guid? LinkedUserId);

public sealed record UpdateFamilyMemberRequest(
    string FullName,
    string? TamilName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? PlaceOfBirth,
    DateOnly? DateOfDeath,
    string? ContactNumber,
    string? Email,
    Guid? LinkedUserId,
    string Status);

public sealed record FamilyMemberResponse(
    Guid FamilyMemberId,
    Guid FamilyGroupId,
    Guid? LinkedUserId,
    string FullName,
    string? TamilName,
    string? Gender,
    DateOnly? DateOfBirth,
    string? PlaceOfBirth,
    DateOnly? DateOfDeath,
    string? ContactNumber,
    string? Email,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
