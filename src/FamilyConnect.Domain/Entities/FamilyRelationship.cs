namespace FamilyConnect.Domain.Entities;

public sealed class FamilyRelationship
{
    public Guid FamilyRelationshipId { get; set; } = Guid.NewGuid();
    public Guid FamilyGroupId { get; set; }
    public Guid FromFamilyMemberId { get; set; }
    public Guid ToFamilyMemberId { get; set; }
    // ParentChild is directed from parent to child. Spouse is stored with the lower GUID first.
    public string RelationshipType { get; set; } = "ParentChild";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }
}
