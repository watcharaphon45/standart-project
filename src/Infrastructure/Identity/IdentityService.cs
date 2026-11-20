using System.Data;
using System.Globalization;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Domain.Entities.System;
using clean.Domain.Exceptions;
using clean.Domain.Security;
using clean.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Identity;

public sealed class IdentityService(SystemDbContext db, UserManager<ApplicationUser> users,
    AccessService access, IOrganizationLookup organizations, TimeProvider clock, ICurrentActor actor) : IIdentityService
{
    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();
        var normalized = users.NormalizeName(identifier);
        var id = await db.Users.AsNoTracking()
            .Where(x => identifier.Contains('@') ? x.NormalizedEmail == normalized : x.NormalizedUserName == normalized)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(cancellationToken);
        if (id is null)
        {
            db.AuditEvents.Add(new AuditEvent
            {
                ProgramCode = "AUTH", Action = "LOGIN", Result = "FAILURE",
                TraceId = actor.TraceId, OccurredAtUtc = clock.GetUtcNow()
            });
            await db.SaveChangesAsync(cancellationToken);
            throw LoginFailed();
        }
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var user = await LockedUserAsync(id.Value, cancellationToken);
        var now = clock.GetUtcNow();
        var available = user.IsActive && !await users.IsLockedOutAsync(user)
            && (!user.MustChangePassword || user.TemporaryPasswordExpiresAtUtc > now);
        var valid = available && await users.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            if (available)
            {
                var failure = await users.AccessFailedAsync(user);
                if (!failure.Succeeded) throw new RuleException("LOGIN_RETRY", "กรุณาลองเข้าสู่ระบบอีกครั้ง", 409);
            }
            AddSecurityEvent(user.Id, "LOGIN", "FAILURE");
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw LoginFailed();
        }
        Ensure(await users.ResetAccessFailedCountAsync(user));
        var snapshot = await access.SnapshotAsync(user.Id, cancellationToken);
        var session = new UserSession
        {
            UserId = user.Id, SecurityStamp = user.SecurityStamp ?? "",
            AuthorizationFingerprint = snapshot.Fingerprint, CreatedAtUtc = now, LastSeenAtUtc = now
        };
        db.Sessions.Add(session);
        AddSecurityEvent(user.Id, "LOGIN", "SUCCESS");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(session.Id, user.Id.ToString(CultureInfo.InvariantCulture), user.MustChangePassword);
    }

    public async Task<SessionUser?> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null || session.Revoked) return null;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        var now = clock.GetUtcNow();
        if (user is null || !user.IsActive || session.SecurityStamp != user.SecurityStamp
            || now - session.CreatedAtUtc >= TimeSpan.FromHours(8)
            || now - session.LastSeenAtUtc >= TimeSpan.FromMinutes(30)
            || (user.MustChangePassword && !(user.TemporaryPasswordExpiresAtUtc > now))) return null;
        var snapshot = await access.SnapshotAsync(user.Id, cancellationToken);
        if (session.AuthorizationFingerprint != snapshot.Fingerprint) return null;
        var updated = await db.Sessions.Where(x => x.Id == sessionId && !x.Revoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.LastSeenAtUtc, now), cancellationToken);
        return updated == 1 ? new(user.Id, user.UserName ?? "", user.MustChangePassword) : null;
    }

    public async Task<CurrentUserDto> CurrentAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId, cancellationToken);
        if (user.MustChangePassword) return new(userId.ToString(CultureInfo.InvariantCulture), user.UserName ?? "",
            true, [], [], [], []);
        var snapshot = await access.SnapshotAsync(userId, cancellationToken);
        var hasGlobalView = snapshot.GlobalPermissions.Any(ProgramRegistry.CompanyContextViewPermissions.Contains);
        var companies = await organizations.CompaniesAsync(snapshot.Companies.Select(x => x.CompanyId).ToArray(),
            hasGlobalView, cancellationToken);
        var companyAccess = companies.Select(company =>
        {
            var id = IdValue.Parse(company.Id);
            var permissions = snapshot.Companies.Where(x => x.CompanyId == id)
                .SelectMany(x => x.Permissions.Where(p => ProgramRegistry.AllowsScope(x.ScopeType, p)))
                .Concat(snapshot.GlobalPermissions.Where(p => !ProgramRegistry.GlobalOnlyPermissions.Contains(p)))
                .Distinct().Order().ToArray();
            return new CompanyAccessDto(company.Id, permissions);
        }).ToArray();
        var availablePermissions = companyAccess.SelectMany(x => x.Permissions).Concat(snapshot.GlobalPermissions).ToHashSet();
        var customMenus = await db.MenuItems.AsNoTracking()
            .ToDictionaryAsync(x => x.ProgramCode, StringComparer.Ordinal, cancellationToken);
        var navigation = ProgramRegistry.Programs
            .Select((program, index) => new
            {
                program,
                index,
                custom = customMenus.GetValueOrDefault(program.Code)
            })
            .Where(x => x.program.Implemented
                && (x.custom?.IsActive ?? true)
                && availablePermissions.Contains(x.program.Code + ".VIEW"))
            .OrderBy(x => x.custom?.SortOrder ?? (x.index + 1))
            .ThenBy(x => x.index)
            .Select(x => new NavigationDto(
                x.program.Code,
                x.custom is { DisplayName.Length: > 0 } ? x.custom.DisplayName : x.program.Name,
                x.program.Route))
            .ToArray();
        return new(userId.ToString(CultureInfo.InvariantCulture), user.UserName ?? "", false,
            companies, companyAccess, snapshot.GlobalPermissions, navigation);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var initial = await db.Users.AsNoTracking().SingleAsync(x => x.Id == userId, cancellationToken);
        // Screen before entering a database transaction. The scoped validator memoizes
        // only this request's screening result; Identity repeats all other checks.
        foreach (var validator in users.PasswordValidators)
            Ensure(await validator.ValidateAsync(users, initial, request.NewPassword));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var user = await LockedUserAsync(userId, cancellationToken);
        if (!user.IsActive || (user.MustChangePassword && !(user.TemporaryPasswordExpiresAtUtc > clock.GetUtcNow())))
            throw new RuleException("TEMPORARY_PASSWORD_EXPIRED", "รหัสชั่วคราวหมดอายุ กรุณาติดต่อผู้ดูแลบัญชี", 401);
        Ensure(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));
        user.MustChangePassword = false;
        user.TemporaryPasswordExpiresAtUtc = null;
        Ensure(await users.UpdateAsync(user));
        await db.Sessions.Where(x => x.UserId == userId).ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.Revoked, true), cancellationToken);
        AddSecurityEvent(userId, "PASSWORD_CHANGE", "SUCCESS");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task LogoutAsync(string sessionId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var userId = await db.Sessions.Where(x => x.Id == sessionId && !x.Revoked)
            .Select(x => (long?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        await db.Sessions.Where(x => x.Id == sessionId).ExecuteUpdateAsync(
            setters => setters.SetProperty(x => x.Revoked, true), cancellationToken);
        if (userId is not null) AddSecurityEvent(userId.Value, "LOGOUT", "SUCCESS");
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private Task<ApplicationUser> LockedUserAsync(long id, CancellationToken cancellationToken) =>
        db.Users.FromSqlInterpolated($"""SELECT * FROM "SuUser" WHERE "Id" = {id} FOR UPDATE""")
            .SingleAsync(cancellationToken);

    private void AddSecurityEvent(long userId, string action, string result) => db.AuditEvents.Add(new AuditEvent
    {
        ActorUserId = userId, ProgramCode = "AUTH", Action = action, Result = result,
        TraceId = actor.TraceId, OccurredAtUtc = clock.GetUtcNow()
    });

    private static RuleException LoginFailed() => new("LOGIN_FAILED", "ไม่สามารถเข้าสู่ระบบได้ กรุณาตรวจสอบข้อมูลหรือติดต่อผู้ดูแลบัญชี", 401);
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new RuleException("PASSWORD_REJECTED", "ไม่สามารถเปลี่ยนรหัสผ่านได้ กรุณาตรวจรหัสเดิมและใช้รหัสใหม่ที่ยาว 15–128 ตัวอักษรและไม่เคยรั่วไหล", 400);
    }
}
