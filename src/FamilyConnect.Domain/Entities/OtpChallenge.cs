namespace FamilyConnect.Domain.Entities;

public sealed class OtpChallenge
{
    public Guid OtpChallengeId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Destination { get; set; } = "";
    public string Purpose { get; set; } = "Registration";
    public byte[] CodeHash { get; set; } = [];
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
