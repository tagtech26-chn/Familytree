namespace FamilyConnect.Application.Contracts;

public sealed record CreateFamilyRelationshipRequest(
    Guid FromFamilyMemberId,
    Guid ToFamilyMemberId,
    string RelationshipType);

public sealed record FamilyRelationshipResponse(
    Guid FamilyRelationshipId,
    Guid FamilyGroupId,
    Guid FromFamilyMemberId,
    string FromFullName,
    string? FromTamilName,
    Guid ToFamilyMemberId,
    string ToFullName,
    string? ToTamilName,
    string RelationshipType,
    DateTime CreatedAtUtc);

