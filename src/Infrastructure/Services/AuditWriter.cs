using System.Text.Json;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Entities.Organization;
using clean.Domain.Entities.System;
using clean.Infrastructure.Persistence;

namespace clean.Infrastructure.Services;

public sealed class AuditWriter(SystemDbContext db, ICurrentActor actor, TimeProvider clock) : IAuditWriter
{
    public Task CompanyChangedAsync(string action, CompanySnapshot? before, Company company, CancellationToken cancellationToken) =>
        RecordOrganizationAsync("DBRT01", action, company.Id, company.Id, before, CompanySnapshot.Of(company), cancellationToken);

    public Task RecordOrganizationAsync(
        string programCode,
        string action,
        long? companyId,
        long entityId,
        object? before,
        object after,
        CancellationToken cancellationToken)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            OccurredAtUtc = clock.GetUtcNow(),
            ActorUserId = actor.UserId,
            CompanyId = companyId,
            EntityId = entityId,
            ProgramCode = programCode,
            Action = action,
            Result = "SUCCESS",
            TraceId = actor.TraceId,
            Before = before is null ? null : JsonSerializer.Serialize(before),
            After = JsonSerializer.Serialize(after)
        });
        return Task.CompletedTask;
    }
}
