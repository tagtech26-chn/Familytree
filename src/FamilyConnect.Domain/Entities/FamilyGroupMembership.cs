namespace FamilyConnect.Domain.Entities;

public sealed class FamilyGroupMembership
{
    public Guid MembershipId { get; set; } = Guid.NewGuid();
    public Guid FamilyGroupId { get; set; }
    public Guid UserId { get; set; }
    public string RoleCode { get; set; } = "Member";
    public string Status { get; set; } = "Active";
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
