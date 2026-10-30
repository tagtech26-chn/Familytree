namespace FamilyConnect.Domain.Entities;

public sealed class AppUser
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string? MobileNumber { get; set; }
    public string? NormalizedMobileNumber { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string PasswordHash { get; set; } = "";
    public string PreferredLanguage { get; set; } = "en";
    public bool IsMobileVerified { get; set; }
    public bool IsEmailVerified { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
