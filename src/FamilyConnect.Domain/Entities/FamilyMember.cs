namespace FamilyConnect.Domain.Entities;

public sealed class FamilyMember
{
    public Guid FamilyMemberId { get; set; } = Guid.NewGuid();
    public Guid FamilyGroupId { get; set; }
    public Guid? LinkedUserId { get; set; }
    public string FullName { get; set; } = "";
    public string? TamilName { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? PlaceOfBirth { get; set; }
    public DateOnly? DateOfDeath { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
