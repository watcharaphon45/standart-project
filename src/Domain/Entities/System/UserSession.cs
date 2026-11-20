namespace clean.Domain.Entities.System;

public sealed class UserSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public long UserId { get; set; }
    public string SecurityStamp { get; set; } = "";
    public string AuthorizationFingerprint { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public bool Revoked { get; set; }
}
