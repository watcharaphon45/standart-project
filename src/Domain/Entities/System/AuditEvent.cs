namespace clean.Domain.Entities.System;

public sealed class AuditEvent
{
    public long Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public long? ActorUserId { get; set; }
    public long? CompanyId { get; set; }
    public long? EntityId { get; set; }
    public string ProgramCode { get; set; } = "";
    public string Action { get; set; } = "";
    public string Result { get; set; } = "";
    public string TraceId { get; set; } = "";
    public string? Before { get; set; }
    public string? After { get; set; }
}
