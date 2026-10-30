namespace FamilyConnect.Domain.Entities;

public sealed class FamilyGroup
{
    public Guid FamilyGroupId { get; set; } = Guid.NewGuid();
    public string GroupCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string DefaultLanguage { get; set; } = "en";
    public string TimeZoneId { get; set; } = "Asia/Kolkata";
    public string Status { get; set; } = "Active";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
