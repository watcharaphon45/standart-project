using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Exceptions;
using clean.Domain.Security;
using clean.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Identity;

public sealed record CompanyGrant(long CompanyId, long RoleId, string ScopeType, long? ScopeId, string[] Permissions);
public sealed record AccessSnapshot(string[] GlobalPermissions, CompanyGrant[] Companies, string Fingerprint);

public sealed class AccessService(SystemDbContext db, ICurrentActor actor) : IAccessService
{
    private readonly Dictionary<long, AccessSnapshot> snapshots = [];

    public async Task<AccessSnapshot> SnapshotAsync(long userId, CancellationToken cancellationToken)
    {
        if (snapshots.TryGetValue(userId, out var cached)) return cached;
        var globals = await (from membership in db.UserRoles.AsNoTracking()
                             join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
                             where membership.UserId == userId && role.CompanyId == null && role.IsActive
                             orderby role.Id select role.Id).ToArrayAsync(cancellationToken);
        var assignments = await (from assignment in db.CompanyAssignments.AsNoTracking()
                                 join role in db.Roles.AsNoTracking() on assignment.RoleId equals role.Id
                                 where assignment.UserId == userId && assignment.IsActive && role.IsActive
                                     && role.CompanyId == assignment.CompanyId
                                 orderby assignment.CompanyId
                                 select new { assignment.CompanyId, assignment.RoleId, assignment.ScopeType, assignment.ScopeId })
                                 .ToArrayAsync(cancellationToken);
        var roleIds = globals.Concat(assignments.Select(x => x.RoleId)).Distinct().ToArray();
        var permissions = await db.RolePermissions.AsNoTracking().Where(x => roleIds.Contains(x.RoleId))
            .OrderBy(x => x.RoleId).ThenBy(x => x.Permission).ToArrayAsync(cancellationToken);
        var globalPermissions = permissions.Where(x => globals.Contains(x.RoleId)).Select(x => x.Permission).Distinct().Order().ToArray();
        var grants = assignments.Select(x => new CompanyGrant(x.CompanyId, x.RoleId, x.ScopeType, x.ScopeId,
            permissions.Where(p => p.RoleId == x.RoleId).Select(p => p.Permission).ToArray())).ToArray();
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { globals, grants, globalPermissions }))));
        var snapshot = new AccessSnapshot(globalPermissions, grants, fingerprint);
        snapshots.Add(userId, snapshot);
        return snapshot;
    }

    public async Task DemandGlobalAsync(string permission, CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(actor.UserId, cancellationToken);
        if (!snapshot.GlobalPermissions.Contains(permission)) Denied();
    }

    public async Task DemandSharedViewAsync(string permission, CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(actor.UserId, cancellationToken);
        if (snapshot.GlobalPermissions.Contains(permission)) return;
        if (snapshot.Companies.Any(x => ProgramRegistry.AllowsScope(x.ScopeType, permission)
            && x.Permissions.Contains(permission))) return;
        Denied();
    }

    public async Task DemandCompanyAsync(long companyId, string permission, CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(actor.UserId, cancellationToken);
        if (snapshot.GlobalPermissions.Contains(permission)) return;
        if (!ProgramRegistry.GlobalOnlyPermissions.Contains(permission)
            && snapshot.Companies.Any(x => x.CompanyId == companyId && x.ScopeType == "Company"
                && x.Permissions.Contains(permission))) return;
        Denied();
    }

    public async Task DemandBranchAsync(long companyId, long branchId, string permission, CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(actor.UserId, cancellationToken);
        if (snapshot.GlobalPermissions.Contains(permission)) return;
        if (!ProgramRegistry.GlobalOnlyPermissions.Contains(permission)
            && snapshot.Companies.Any(x => x.CompanyId == companyId
                && ProgramRegistry.AllowsScope(x.ScopeType, permission)
                && (x.ScopeType == "Company" || (x.ScopeType == "Branch" && x.ScopeId == branchId))
                && x.Permissions.Contains(permission))) return;
        Denied();
    }

    public async Task DemandDepartmentAsync(
        long companyId,
        long branchId,
        long departmentId,
        string permission,
        CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(actor.UserId, cancellationToken);
        if (snapshot.GlobalPermissions.Contains(permission)) return;
        if (!ProgramRegistry.GlobalOnlyPermissions.Contains(permission)
            && snapshot.Companies.Any(x => x.CompanyId == companyId
                && ProgramRegistry.AllowsScope(x.ScopeType, permission)
                && (x.ScopeType == "Company"
                    || (x.ScopeType == "Branch" && x.ScopeId == branchId)
                    || (x.ScopeType == "Department" && x.ScopeId == departmentId))
                && x.Permissions.Contains(permission))) return;
        Denied();
    }

    public Task<ScopeFilter> GetScopeFilterAsync(long companyId, string permission, CancellationToken cancellationToken) =>
        GetScopeFilterAnyAsync(companyId, [permission], cancellationToken);

    public async Task<ScopeFilter> GetScopeFilterAnyAsync(
        long companyId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        var snapshot = await SnapshotAsync(actor.UserId, cancellationToken);
        if (permissions.Any(p => snapshot.GlobalPermissions.Contains(p)))
            return new ScopeFilter(true, new HashSet<long>(), new HashSet<long>());

        var companyGrants = snapshot.Companies.Where(x => x.CompanyId == companyId).ToArray();
        if (companyGrants.Any(x => x.ScopeType == "Company"
            && x.Permissions.Any(p => permissions.Contains(p) && ProgramRegistry.AllowsScope("Company", p))))
        {
            return new ScopeFilter(true, new HashSet<long>(), new HashSet<long>());
        }

        var branchIds = companyGrants
            .Where(x => x.ScopeType == "Branch" && x.ScopeId is not null
                && x.Permissions.Any(p => permissions.Contains(p) && ProgramRegistry.AllowsScope("Branch", p)))
            .Select(x => x.ScopeId!.Value)
            .ToHashSet();

        var departmentIds = companyGrants
            .Where(x => x.ScopeType == "Department" && x.ScopeId is not null
                && x.Permissions.Any(p => permissions.Contains(p) && ProgramRegistry.AllowsScope("Department", p)))
            .Select(x => x.ScopeId!.Value)
            .ToHashSet();

        if (branchIds.Count == 0 && departmentIds.Count == 0) Denied();
        return new ScopeFilter(false, branchIds, departmentIds);
    }

    public async Task<bool> HasActiveAssignmentsAsync(long companyId, CancellationToken cancellationToken) =>
        await db.CompanyAssignments.AnyAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken)
        || await db.DocumentSequences.AnyAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken)
        || await db.ApprovalWorkflows.AnyAsync(x => x.CompanyId == companyId && x.IsActive, cancellationToken);

    public Task<bool> HasActiveBranchAssignmentsAsync(long companyId, long branchId, CancellationToken cancellationToken) =>
        db.CompanyAssignments.AnyAsync(
            x => x.CompanyId == companyId && x.IsActive && x.ScopeType == "Branch" && x.ScopeId == branchId,
            cancellationToken);

    public Task<bool> HasActiveDepartmentAssignmentsAsync(long companyId, long departmentId, CancellationToken cancellationToken) =>
        db.CompanyAssignments.AnyAsync(
            x => x.CompanyId == companyId && x.IsActive && x.ScopeType == "Department" && x.ScopeId == departmentId,
            cancellationToken);

    private static void Denied() => throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์ทำรายการนี้", 403);
}
