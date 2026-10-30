namespace FamilyConnect.Domain.Entities;

public sealed class RefreshToken
{
    public Guid RefreshTokenId { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public byte[] TokenHash { get; set; } = [];
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public byte[]? ReplacedByTokenHash { get; set; }
}
