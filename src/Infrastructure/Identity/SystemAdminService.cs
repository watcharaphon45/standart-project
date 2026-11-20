using System.Globalization;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT01;
using clean.Application.Features.SU.SURT02;
using clean.Application.Features.SU.SURT03;
using clean.Application.Features.SU.SURT04;
using clean.Domain.Entities.Organization;
using clean.Domain.Entities.System;
using clean.Domain.Exceptions;
using clean.Domain.Security;
using clean.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace clean.Infrastructure.Identity;

public sealed partial class SystemAdminService(
    SystemDbContext db,
    UserManager<ApplicationUser> users,
    AccessService access,
    IOrganizationLookup organizations,
    IAtomicWriter atomic,
    IAuditWriter audit,
    ICurrentActor actor,
    TimeProvider clock) : ISystemAdminService
{
    public async Task<Page<UserDto>> SearchUsersAsync(SearchUsersRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT01.VIEW", cancellationToken);
        var query = db.Users.AsNoTracking().AsQueryable();
        if (request.IsActive is not null)
            query = query.Where(x => x.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                (x.NormalizedUserName != null && x.NormalizedUserName.Contains(upper))
                || (x.NormalizedEmail != null && x.NormalizedEmail.Contains(upper)));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("email", true) => query.OrderBy(x => x.NormalizedEmail),
            ("email", false) => query.OrderByDescending(x => x.NormalizedEmail),
            ("isActive", true) => query.OrderBy(x => x.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.IsActive),
            (_, true) => query.OrderBy(x => x.NormalizedUserName),
            _ => query.OrderByDescending(x => x.NormalizedUserName)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        return new(rows.Select(x => ToUserDto(x, now)).ToArray(), count, request.PageNumber, request.PageSize);
    }

    public async Task<UserDto> GetUserDetailAsync(GetUserDetailRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT01.VIEW", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
        return ToUserDto(user, clock.GetUtcNow());
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT01.CREATE", cancellationToken);
        var candidate = new ApplicationUser { UserName = request.UserName, Email = request.Email };
        await ScreenPasswordAsync(candidate, request.TemporaryPassword);

        return await atomic.ExecuteAsync(async () =>
        {
            var normalizedName = users.NormalizeName(request.UserName);
            var normalizedEmail = users.NormalizeEmail(request.Email);
            if (await db.Users.AnyAsync(x => x.NormalizedUserName == normalizedName, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "ชื่อผู้ใช้งานนี้มีอยู่ในระบบแล้ว", 409);
            if (await db.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "อีเมลนี้มีอยู่ในระบบแล้ว", 409);

            var now = clock.GetUtcNow();
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                IsActive = true,
                LockoutEnabled = true,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = now.AddHours(24),
                Version = 1
            };
            EnsureIdentity(await users.CreateAsync(user, request.TemporaryPassword), "ไม่สามารถสร้างผู้ใช้งานได้");
            await audit.RecordOrganizationAsync(
                "SURT01", "CREATE", null, user.Id, null, ToUserSnapshot(user, now), cancellationToken);
            return ToUserDto(user, now);
        }, cancellationToken);
    }

    public async Task<UserDto> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT01.UPDATE", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);

        return await atomic.ExecuteAsync(async () =>
        {
            var user = await LockUserAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
            OrganizationRules.CheckVersion(expectedVersion, user.Version);

            var normalizedName = users.NormalizeName(request.UserName);
            var normalizedEmail = users.NormalizeEmail(request.Email);
            if (await db.Users.AnyAsync(x => x.Id != id && x.NormalizedUserName == normalizedName, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "ชื่อผู้ใช้งานนี้มีอยู่ในระบบแล้ว", 409);
            if (await db.Users.AnyAsync(x => x.Id != id && x.NormalizedEmail == normalizedEmail, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "อีเมลนี้มีอยู่ในระบบแล้ว", 409);

            var now = clock.GetUtcNow();
            var before = ToUserSnapshot(user, now);
            user.UserName = request.UserName;
            user.NormalizedUserName = normalizedName;
            user.Email = request.Email;
            user.NormalizedEmail = normalizedEmail;
            user.Version++;
            EnsureIdentity(await users.UpdateAsync(user), "ไม่สามารถแก้ไขข้อมูลผู้ใช้งานได้");
            await audit.RecordOrganizationAsync(
                "SURT01", "UPDATE", null, user.Id, before, ToUserSnapshot(user, now), cancellationToken);
            return ToUserDto(user, now);
        }, cancellationToken);
    }

    public async Task<UserDto> ResetTemporaryPasswordAsync(
        ResetTemporaryPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT01.RESET", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var initial = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
        await ScreenPasswordAsync(initial, request.TemporaryPassword);

        return await atomic.ExecuteAsync(async () =>
        {
            var user = await LockUserAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
            OrganizationRules.CheckVersion(expectedVersion, user.Version);
            var now = clock.GetUtcNow();
            var before = ToUserSnapshot(user, now);

            user.PasswordHash = users.PasswordHasher.HashPassword(user, request.TemporaryPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.MustChangePassword = true;
            user.TemporaryPasswordExpiresAtUtc = now.AddHours(24);
            user.Version++;
            EnsureIdentity(await users.UpdateAsync(user), "ไม่สามารถรีเซ็ตรหัสผ่านได้");
            await RevokeUserSessionsAsync(user.Id, cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT01", "RESET", null, user.Id, before, ToUserSnapshot(user, now), cancellationToken);
            return ToUserDto(user, now);
        }, cancellationToken);
    }

    public async Task<UserDto> UnlockUserAsync(UnlockUserRequest request, CancellationToken cancellationToken)
    {
        await access.DemandGlobalAsync("SURT01.UNLOCK", cancellationToken);
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);

        return await atomic.ExecuteAsync(async () =>
        {
            var user = await LockUserAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
            OrganizationRules.CheckVersion(expectedVersion, user.Version);
            var now = clock.GetUtcNow();
            var before = ToUserSnapshot(user, now);

            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.Version++;
            EnsureIdentity(await users.UpdateAsync(user), "ไม่สามารถปลดล็อกบัญชีได้");
            await audit.RecordOrganizationAsync(
                "SURT01", "UNLOCK", null, user.Id, before, ToUserSnapshot(user, now), cancellationToken);
            return ToUserDto(user, now);
        }, cancellationToken);
    }

    public async Task<UserDto> ChangeUserStatusAsync(ChangeUserStatusRequest request, CancellationToken cancellationToken)
    {
        var action = request.IsActive ? "ACTIVATE" : "DISABLE";
        await access.DemandGlobalAsync("SURT01." + action, cancellationToken);
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);

        return await atomic.ExecuteAsync(async () =>
        {
            var user = await LockUserAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
            OrganizationRules.CheckVersion(expectedVersion, user.Version);
            if (!request.IsActive && user.Id == actor.UserId)
                throw new RuleException("CANNOT_DISABLE_SELF", "ไม่สามารถปิดใช้งานบัญชีที่กำลังใช้งานอยู่ได้", 409);
            if (user.IsActive == request.IsActive)
                throw new RuleException("STATUS_UNCHANGED", "สถานะข้อมูลเปลี่ยนไปแล้ว กรุณาโหลดข้อมูลล่าสุด", 409);

            var now = clock.GetUtcNow();
            var before = ToUserSnapshot(user, now);
            user.IsActive = request.IsActive;
            if (!request.IsActive)
                user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.Version++;
            EnsureIdentity(await users.UpdateAsync(user), "ไม่สามารถเปลี่ยนสถานะบัญชีได้");
            if (!request.IsActive)
                await RevokeUserSessionsAsync(user.Id, cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT01", action, null, user.Id, before, ToUserSnapshot(user, now), cancellationToken);
            return ToUserDto(user, now);
        }, cancellationToken);
    }

    public async Task<Page<RoleDto>> SearchRolesAsync(SearchRolesRequest request, CancellationToken cancellationToken)
    {
        long? companyId = string.IsNullOrEmpty(request.CompanyId) ? null : IdValue.Parse(request.CompanyId);
        await DemandRoleAccessAsync(companyId, "VIEW", cancellationToken);

        var query = db.Roles.AsNoTracking().Where(x => x.CompanyId == companyId);
        if (request.IsActive is not null)
            query = query.Where(x => x.IsActive == request.IsActive);
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            query = query.Where(x =>
                (x.NormalizedName != null && x.NormalizedName.Contains(upper))
                || x.Description.ToUpper().Contains(upper));
        }

        var count = await query.CountAsync(cancellationToken);
        var ascending = request.SortDirection == "asc";
        var sorted = (request.SortField, ascending) switch
        {
            ("name", true) => query.OrderBy(x => x.Description).ThenBy(x => x.NormalizedName),
            ("name", false) => query.OrderByDescending(x => x.Description).ThenByDescending(x => x.NormalizedName),
            ("isActive", true) => query.OrderBy(x => x.IsActive),
            ("isActive", false) => query.OrderByDescending(x => x.IsActive),
            (_, true) => query.OrderBy(x => x.NormalizedName),
            _ => query.OrderByDescending(x => x.NormalizedName)
        };
        var rows = await sorted.ThenBy(x => x.Id)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = await BuildRoleDtosAsync(rows, cancellationToken);
        return new(items, count, request.PageNumber, request.PageSize);
    }

    public async Task<RoleDto> GetRoleDetailAsync(GetRoleDetailRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);
        await DemandRoleAccessAsync(role.CompanyId, "VIEW", cancellationToken);
        return (await BuildRoleDtosAsync([role], cancellationToken)).Single();
    }

    public async Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        long? companyId = string.IsNullOrEmpty(request.CompanyId) ? null : IdValue.Parse(request.CompanyId);
        await DemandRoleAccessAsync(companyId, "CREATE", cancellationToken);
        var (code, name) = OrganizationRules.NormalizeAndValidate(request.Code, request.Name);

        return await atomic.ExecuteAsync(async () =>
        {
            if (companyId is long cid)
            {
                var company = await organizations.CompanyAsync(cid, cancellationToken)
                    ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
                if (!company.IsActive)
                    throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน", 409);
            }

            if (await db.Roles.AnyAsync(x => x.CompanyId == companyId && x.NormalizedName == code, cancellationToken))
                throw new RuleException("DUPLICATE_CODE", "รหัสบทบาทนี้มีอยู่ในขอบเขตเดียวกันแล้ว", 409);

            var role = new ApplicationRole
            {
                CompanyId = companyId,
                Name = code,
                NormalizedName = code,
                Description = name,
                IsActive = true,
                CodeLocked = false,
                Version = 1,
                ConcurrencyStamp = Guid.NewGuid().ToString("N")
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT02", "CREATE", role.CompanyId, role.Id, null, ToRoleSnapshot(role), cancellationToken);
            return new RoleDto(
                role.Id.ToString(CultureInfo.InvariantCulture),
                role.CompanyId?.ToString(CultureInfo.InvariantCulture),
                role.Name ?? "",
                role.Description,
                role.IsActive,
                role.CodeLocked,
                0,
                0,
                role.Version.ToString(CultureInfo.InvariantCulture));
        }, cancellationToken);
    }

    public async Task<RoleDto> UpdateRoleAsync(UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var target = await db.Roles.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);

        await DemandRoleAccessAsync(target.CompanyId, "UPDATE", cancellationToken);
        var snapshot = await access.SnapshotAsync(actor.UserId, cancellationToken);
        if (!snapshot.GlobalPermissions.Contains("SURT02.UPDATE")
            && snapshot.Companies.Any(x => x.RoleId == id))
        {
            throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์แก้ไขบทบาทที่ตนเองใช้งานอยู่", 403);
        }

        var (code, name) = OrganizationRules.NormalizeAndValidate(request.Code, request.Name);

        var updated = await atomic.ExecuteAsync(async () =>
        {
            var role = await LockRoleAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);
            OrganizationRules.CheckVersion(expectedVersion, role.Version);
            OrganizationRules.EnsureActiveForUpdate(role.IsActive, "ROLE_INACTIVE");
            OrganizationRules.EnsureCodeUnlocked(role.CodeLocked, code, role.NormalizedName ?? role.Name ?? "");

            if (await db.Roles.AnyAsync(
                x => x.Id != id && x.CompanyId == role.CompanyId && x.NormalizedName == code,
                cancellationToken))
            {
                throw new RuleException("DUPLICATE_CODE", "รหัสบทบาทนี้มีอยู่ในขอบเขตเดียวกันแล้ว", 409);
            }

            var before = ToRoleSnapshot(role);
            role.Name = code;
            role.NormalizedName = code;
            role.Description = name;
            role.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            role.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT02", "UPDATE", role.CompanyId, role.Id, before, ToRoleSnapshot(role), cancellationToken);
            return role;
        }, cancellationToken);

        return (await BuildRoleDtosAsync([updated], cancellationToken)).Single();
    }

    public async Task<RoleDto> ChangeRoleStatusAsync(ChangeRoleStatusRequest request, CancellationToken cancellationToken)
    {
        var id = IdValue.Parse(request.Id);
        var expectedVersion = IdValue.Parse(request.Version);
        var action = request.IsActive ? "ACTIVATE" : "DEACTIVATE";
        var target = await db.Roles.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);

        await DemandRoleAccessAsync(target.CompanyId, action, cancellationToken);
        var snapshot = await access.SnapshotAsync(actor.UserId, cancellationToken);
        if (!snapshot.GlobalPermissions.Contains("SURT02." + action)
            && snapshot.Companies.Any(x => x.RoleId == id))
        {
            throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์เปลี่ยนสถานะบทบาทที่ตนเองใช้งานอยู่", 403);
        }

        var updated = await atomic.ExecuteAsync(async () =>
        {
            var role = await LockRoleAsync(id, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);
            OrganizationRules.CheckVersion(expectedVersion, role.Version);

            var parentActive = role.CompanyId is null
                || ((await organizations.CompanyAsync(role.CompanyId.Value, cancellationToken))?.IsActive ?? false);
            var hasActiveAssignments = await db.CompanyAssignments.AnyAsync(
                    x => x.RoleId == id && x.IsActive, cancellationToken)
                || await db.UserRoles.AnyAsync(x => x.RoleId == id, cancellationToken)
                || await db.ApprovalWorkflowSteps.AnyAsync(
                    x => x.ApproverRoleId == id && db.ApprovalWorkflows.Any(w => w.Id == x.WorkflowId && w.IsActive),
                    cancellationToken);

            OrganizationRules.EnsureCanChangeStatus(request.IsActive, role.IsActive, parentActive, false, hasActiveAssignments);
            var before = ToRoleSnapshot(role);
            role.IsActive = request.IsActive;
            role.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            role.Version++;
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT02", action, role.CompanyId, role.Id, before, ToRoleSnapshot(role), cancellationToken);
            return role;
        }, cancellationToken);

        return (await BuildRoleDtosAsync([updated], cancellationToken)).Single();
    }

    public async Task<PermissionCatalogDto> GetPermissionOptionsAsync(
        GetPermissionOptionsRequest request,
        CancellationToken cancellationToken)
    {
        long? companyId = string.IsNullOrEmpty(request.CompanyId) ? null : IdValue.Parse(request.CompanyId);
        await DemandPermissionAccessAsync(companyId, "VIEW", cancellationToken);

        var snapshot = await access.SnapshotAsync(actor.UserId, cancellationToken);
        var roles = await db.Roles.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.NormalizedName)
            .ToListAsync(cancellationToken);
        var roleDtos = await BuildRoleDtosAsync(roles, cancellationToken);
        var grantable = GetGrantablePermissions(snapshot, companyId).Order().ToArray();

        return new PermissionCatalogDto(roleDtos, BuildPermissionCatalog(), grantable);
    }

    public async Task<RolePermissionsDto> GetRolePermissionsDetailAsync(
        GetRolePermissionsDetailRequest request,
        CancellationToken cancellationToken)
    {
        var roleId = IdValue.Parse(request.RoleId);
        var role = await db.Roles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == roleId, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);
        await DemandPermissionAccessAsync(role.CompanyId, "VIEW", cancellationToken);

        var snapshot = await access.SnapshotAsync(actor.UserId, cancellationToken);
        var permissions = await db.RolePermissions.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .OrderBy(x => x.Permission)
            .Select(x => x.Permission)
            .ToArrayAsync(cancellationToken);
        var grantable = GetGrantablePermissions(snapshot, role.CompanyId).Order().ToArray();

        return new RolePermissionsDto(
            role.Id.ToString(CultureInfo.InvariantCulture),
            role.CompanyId?.ToString(CultureInfo.InvariantCulture),
            role.Name ?? "",
            string.IsNullOrWhiteSpace(role.Description) ? role.Name ?? "" : role.Description,
            role.IsActive,
            role.Version.ToString(CultureInfo.InvariantCulture),
            permissions,
            grantable);
    }

    public async Task<RolePermissionsDto> UpdateRolePermissionsAsync(
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var roleId = IdValue.Parse(request.RoleId);
        var expectedVersion = IdValue.Parse(request.Version);
        var target = await db.Roles.AsNoTracking()
            .Where(x => x.Id == roleId)
            .Select(x => new { x.CompanyId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);

        await DemandPermissionAccessAsync(target.CompanyId, "UPDATE", cancellationToken);
        var snapshot = await access.SnapshotAsync(actor.UserId, cancellationToken);

        if (target.CompanyId is long companyId)
        {
            if (!snapshot.GlobalPermissions.Contains("SURT03.UPDATE")
                && snapshot.Companies.Any(x => x.CompanyId == companyId && x.RoleId == roleId))
            {
                throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์แก้ไขสิทธิ์ของบทบาทที่ตนเองใช้งานอยู่", 403);
            }
        }
        else if (await db.UserRoles.AnyAsync(x => x.UserId == actor.UserId && x.RoleId == roleId, cancellationToken))
        {
            var otherGlobalPerms = await GetActorGlobalPermissionsExcludingRoleAsync(actor.UserId, roleId, cancellationToken);
            if (request.Permissions.Distinct(StringComparer.Ordinal).Any(p => !otherGlobalPerms.Contains(p)))
                throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์เพิ่มสิทธิ์ให้บทบาทส่วนกลางที่ตนเองถืออยู่เกินกว่าสิทธิ์อื่นที่มี", 403);
        }

        var desired = request.Permissions.Distinct(StringComparer.Ordinal).Order().ToArray();
        var grantable = GetGrantablePermissions(snapshot, target.CompanyId);
        if (desired.Any(p => !grantable.Contains(p)))
            throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์มอบสิทธิ์ที่อยู่นอกเหนือขอบเขตของคุณ", 403);

        var updatedRole = await atomic.ExecuteAsync(async () =>
        {
            var role = await LockRoleAsync(roleId, cancellationToken)
                ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบทบาท", 404);
            OrganizationRules.CheckVersion(expectedVersion, role.Version);
            OrganizationRules.EnsureActiveForUpdate(role.IsActive, "ROLE_INACTIVE");

            var existingRows = await db.RolePermissions
                .Where(x => x.RoleId == roleId)
                .OrderBy(x => x.Permission)
                .ToListAsync(cancellationToken);
            var existingPerms = existingRows.Select(x => x.Permission).ToArray();
            var before = new RolePermissionSnapshot(
                role.Id.ToString(CultureInfo.InvariantCulture),
                role.Name ?? "",
                role.CompanyId?.ToString(CultureInfo.InvariantCulture),
                existingPerms);

            var toRemove = existingRows.Where(x => !desired.Contains(x.Permission, StringComparer.Ordinal)).ToArray();
            var toAdd = desired
                .Where(p => !existingPerms.Contains(p, StringComparer.Ordinal))
                .Select(p => new RolePermission { RoleId = roleId, Permission = p })
                .ToArray();

            if (toRemove.Length > 0) db.RolePermissions.RemoveRange(toRemove);
            if (toAdd.Length > 0) db.RolePermissions.AddRange(toAdd);
            role.ConcurrencyStamp = Guid.NewGuid().ToString("N");
            role.Version++;
            await db.SaveChangesAsync(cancellationToken);

            var after = new RolePermissionSnapshot(
                role.Id.ToString(CultureInfo.InvariantCulture),
                role.Name ?? "",
                role.CompanyId?.ToString(CultureInfo.InvariantCulture),
                desired);
            await audit.RecordOrganizationAsync(
                "SURT03", "UPDATE", role.CompanyId, role.Id, before, after, cancellationToken);
            return role;
        }, cancellationToken);

        return new RolePermissionsDto(
            updatedRole.Id.ToString(CultureInfo.InvariantCulture),
            updatedRole.CompanyId?.ToString(CultureInfo.InvariantCulture),
            updatedRole.Name ?? "",
            string.IsNullOrWhiteSpace(updatedRole.Description) ? updatedRole.Name ?? "" : updatedRole.Description,
            updatedRole.IsActive,
            updatedRole.Version.ToString(CultureInfo.InvariantCulture),
            desired,
            grantable.Order().ToArray());
    }

    public async Task<Page<UserScopeListItemDto>> SearchUserScopesAsync(
        SearchUserScopesRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT04.VIEW", cancellationToken);

        var joined = await (from user in db.Users.AsNoTracking()
                            join assignment in db.CompanyAssignments.AsNoTracking()
                                .Where(x => x.CompanyId == companyId)
                                on user.Id equals assignment.UserId into assignmentGroup
                            from assignment in assignmentGroup.DefaultIfEmpty()
                            join role in db.Roles.AsNoTracking()
                                on assignment.RoleId equals role.Id into roleGroup
                            from role in roleGroup.DefaultIfEmpty()
                            select new
                            {
                                user.Id,
                                UserName = user.UserName ?? "",
                                Email = user.Email ?? "",
                                UserIsActive = user.IsActive,
                                Assignment = assignment,
                                Role = role
                            }).ToListAsync(cancellationToken);

        var branches = (await organizations.BranchesByCompanyAsync(companyId, cancellationToken))
            .ToDictionary(x => x.Id, StringComparer.Ordinal);
        var departments = (await organizations.DepartmentsByCompanyAsync(companyId, cancellationToken))
            .ToDictionary(x => x.Id, StringComparer.Ordinal);

        var mapped = joined.Select(row =>
        {
            var hasAssignment = row.Assignment is not null;
            var scopeType = row.Assignment?.ScopeType;
            var scopeIdStr = row.Assignment?.ScopeId?.ToString(CultureInfo.InvariantCulture);
            string? scopeCode = null;
            string? scopeName = null;
            if (scopeType == "Company")
            {
                scopeName = "ทั้งบริษัท";
            }
            else if (scopeType == "Branch" && scopeIdStr is not null && branches.TryGetValue(scopeIdStr, out var branch))
            {
                scopeCode = branch.Code;
                scopeName = branch.Name;
            }
            else if (scopeType == "Department" && scopeIdStr is not null && departments.TryGetValue(scopeIdStr, out var dept))
            {
                scopeCode = dept.Code;
                scopeName = dept.Name;
            }

            return new UserScopeListItemDto(
                row.Id.ToString(CultureInfo.InvariantCulture),
                row.UserName,
                row.Email,
                row.UserIsActive,
                hasAssignment,
                row.Role?.Id.ToString(CultureInfo.InvariantCulture),
                row.Role?.Name,
                row.Role is null ? null : (string.IsNullOrWhiteSpace(row.Role.Description) ? row.Role.Name : row.Role.Description),
                scopeType,
                scopeIdStr,
                scopeCode,
                scopeName,
                row.Assignment?.IsActive ?? false,
                row.Assignment?.Version.ToString(CultureInfo.InvariantCulture) ?? "");
        });

        if (!string.IsNullOrEmpty(request.ScopeType))
            mapped = mapped.Where(x => x.ScopeType == request.ScopeType);
        if (request.IsActive is not null)
        {
            mapped = request.IsActive.Value
                ? mapped.Where(x => x.HasAssignment && x.IsActive)
                : mapped.Where(x => !x.HasAssignment || !x.IsActive);
        }
        var keyword = request.Keyword?.Trim();
        if (!string.IsNullOrEmpty(keyword))
        {
            var upper = keyword.ToUpperInvariant();
            mapped = mapped.Where(x =>
                x.UserName.ToUpperInvariant().Contains(upper)
                || x.Email.ToUpperInvariant().Contains(upper)
                || (x.RoleCode?.ToUpperInvariant().Contains(upper) ?? false)
                || (x.RoleName?.ToUpperInvariant().Contains(upper) ?? false));
        }

        var filtered = mapped.ToList();
        var ascending = request.SortDirection == "asc";
        IOrderedEnumerable<UserScopeListItemDto> ordered = (request.SortField, ascending) switch
        {
            ("email", true) => filtered.OrderBy(x => x.Email, StringComparer.OrdinalIgnoreCase),
            ("email", false) => filtered.OrderByDescending(x => x.Email, StringComparer.OrdinalIgnoreCase),
            ("roleCode", true) => filtered.OrderBy(x => x.RoleCode ?? "", StringComparer.OrdinalIgnoreCase),
            ("roleCode", false) => filtered.OrderByDescending(x => x.RoleCode ?? "", StringComparer.OrdinalIgnoreCase),
            ("scopeType", true) => filtered.OrderBy(x => x.ScopeType ?? "", StringComparer.OrdinalIgnoreCase),
            ("scopeType", false) => filtered.OrderByDescending(x => x.ScopeType ?? "", StringComparer.OrdinalIgnoreCase),
            ("isActive", true) => filtered.OrderBy(x => x.IsActive),
            ("isActive", false) => filtered.OrderByDescending(x => x.IsActive),
            (_, true) => filtered.OrderBy(x => x.UserName, StringComparer.OrdinalIgnoreCase),
            _ => filtered.OrderByDescending(x => x.UserName, StringComparer.OrdinalIgnoreCase)
        };

        var count = filtered.Count;
        var pageItems = ordered
            .ThenBy(x => long.Parse(x.UserId, CultureInfo.InvariantCulture))
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();
        return new(pageItems, count, request.PageNumber, request.PageSize);
    }

    public async Task<UserScopeOptionsDto> GetUserScopeOptionsAsync(
        GetUserScopeOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        await access.DemandCompanyAsync(companyId, "SURT04.VIEW", cancellationToken);

        var roles = await db.Roles.AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.NormalizedName)
            .Select(x => new LookupOptionDto(
                x.Id.ToString(CultureInfo.InvariantCulture),
                x.Name ?? "",
                string.IsNullOrWhiteSpace(x.Description) ? x.Name ?? "" : x.Description,
                x.IsActive))
            .ToArrayAsync(cancellationToken);
        var branches = await organizations.BranchesByCompanyAsync(companyId, cancellationToken);
        var departments = await organizations.DepartmentsByCompanyAsync(companyId, cancellationToken);
        return new UserScopeOptionsDto(roles, branches, departments);
    }

    public async Task<UserScopeDetailDto> GetUserScopeDetailAsync(
        GetUserScopeDetailRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var userId = IdValue.Parse(request.UserId);
        await access.DemandCompanyAsync(companyId, "SURT04.VIEW", cancellationToken);

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
        _ = await organizations.CompanyAsync(companyId, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);

        var assignment = await db.CompanyAssignments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId && x.CompanyId == companyId, cancellationToken);
        var departments = await organizations.DepartmentsByCompanyAsync(companyId, cancellationToken);
        var accessible = FilterAccessibleDepartments(
            departments,
            assignment?.IsActive ?? false,
            assignment?.ScopeType ?? "Company",
            assignment?.ScopeId?.ToString(CultureInfo.InvariantCulture));

        return new UserScopeDetailDto(
            user.Id.ToString(CultureInfo.InvariantCulture),
            user.UserName ?? "",
            user.Email ?? "",
            user.IsActive,
            companyId.ToString(CultureInfo.InvariantCulture),
            assignment?.RoleId.ToString(CultureInfo.InvariantCulture),
            assignment?.ScopeType ?? "Company",
            assignment?.ScopeId?.ToString(CultureInfo.InvariantCulture),
            assignment?.IsActive ?? true,
            assignment?.Version.ToString(CultureInfo.InvariantCulture) ?? "",
            accessible);
    }

    public async Task<UserScopeDetailDto> UpdateUserScopeAsync(
        UpdateUserScopeRequest request,
        CancellationToken cancellationToken)
    {
        var companyId = IdValue.Parse(request.CompanyId);
        var userId = IdValue.Parse(request.UserId);
        var roleId = IdValue.Parse(request.RoleId);
        long? scopeId = string.IsNullOrEmpty(request.ScopeId) ? null : IdValue.Parse(request.ScopeId);

        await access.DemandCompanyAsync(companyId, "SURT04.UPDATE", cancellationToken);
        var snapshot = await access.SnapshotAsync(actor.UserId, cancellationToken);
        var isGlobalScopeAdmin = snapshot.GlobalPermissions.Contains("SURT04.UPDATE");
        if (!isGlobalScopeAdmin && userId == actor.UserId)
            throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์แก้ไขขอบเขตการใช้งานของตนเอง", 403);

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลผู้ใช้งาน", 404);
        var company = await organizations.CompanyAsync(companyId, cancellationToken)
            ?? throw new RuleException("NOT_FOUND", "ไม่พบข้อมูลบริษัท", 404);
        var targetRoleInfo = await db.Roles.AsNoTracking()
            .Where(x => x.Id == roleId)
            .Select(x => new { x.CompanyId, x.IsActive })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RuleException("ROLE_NOT_FOUND", "ไม่พบบทบาทที่เลือก", 404);
        if (targetRoleInfo.CompanyId != companyId)
            throw new RuleException("FORBIDDEN", "ไม่สามารถมอบบทบาทข้ามบริษัทหรือบทบาทส่วนกลางในขอบเขตบริษัทได้", 403);
        if (request.IsActive && !targetRoleInfo.IsActive)
            throw new RuleException("ROLE_INACTIVE", "กรุณาเปิดใช้งานบทบาทก่อนกำหนดให้ผู้ใช้งาน", 409);

        var grantable = GetGrantablePermissions(snapshot, companyId);
        var targetRolePermissions = await db.RolePermissions.AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.Permission)
            .ToArrayAsync(cancellationToken);
        if (targetRolePermissions.Any(p => !grantable.Contains(p)))
            throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์มอบบทบาทที่มีสิทธิ์เกินขอบเขตของคุณ", 403);

        var existingSnapshot = await db.CompanyAssignments.AsNoTracking()
            .Where(x => x.UserId == userId && x.CompanyId == companyId)
            .Select(x => new { x.RoleId })
            .SingleOrDefaultAsync(cancellationToken);
        if (existingSnapshot is not null && !isGlobalScopeAdmin && existingSnapshot.RoleId != roleId)
        {
            var previousPermissions = await db.RolePermissions.AsNoTracking()
                .Where(x => x.RoleId == existingSnapshot.RoleId)
                .Select(x => x.Permission)
                .ToArrayAsync(cancellationToken);
            if (previousPermissions.Any(p => !grantable.Contains(p)))
                throw new RuleException("FORBIDDEN", "คุณไม่มีสิทธิ์ปรับเปลี่ยนผู้ใช้งานที่ถือบทบาทเกินขอบเขตของคุณ", 403);
        }

        var branches = await organizations.BranchesByCompanyAsync(companyId, cancellationToken);
        var departments = await organizations.DepartmentsByCompanyAsync(companyId, cancellationToken);
        if (request.ScopeType == "Company")
        {
            if (request.IsActive && !company.IsActive)
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อน", 409);
        }
        else if (request.ScopeType == "Branch")
        {
            var branch = branches.SingleOrDefault(x => x.Id == scopeId!.Value.ToString(CultureInfo.InvariantCulture))
                ?? throw new RuleException("BRANCH_NOT_FOUND", "ไม่พบสาขาในบริษัทที่เลือก", 404);
            if (request.IsActive && (!company.IsActive || !branch.IsActive))
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทและสาขาก่อน", 409);
        }
        else if (request.ScopeType == "Department")
        {
            var department = departments.SingleOrDefault(x => x.Id == scopeId!.Value.ToString(CultureInfo.InvariantCulture))
                ?? throw new RuleException("DEPARTMENT_NOT_FOUND", "ไม่พบแผนกในบริษัทที่เลือก", 404);
            if (request.IsActive && (!company.IsActive || !department.IsActive))
                throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานบริษัทและแผนกก่อน", 409);
        }

        var saved = await atomic.ExecuteAsync(async () =>
        {
            var role = await LockRoleAsync(roleId, cancellationToken)
                ?? throw new RuleException("ROLE_NOT_FOUND", "ไม่พบบทบาทที่เลือก", 404);
            if (!role.CodeLocked)
            {
                role.CodeLocked = true;
                role.Version++;
            }

            var existing = await LockAssignmentAsync(userId, companyId, cancellationToken);
            AssignmentSnapshot? before = null;
            CompanyAssignment entity;
            string action;

            if (existing is not null)
            {
                if (string.IsNullOrEmpty(request.Version))
                    throw new RuleException("CONCURRENCY_CONFLICT", "ข้อมูลถูกแก้ไขแล้ว กรุณาโหลดข้อมูลล่าสุดและแก้ใหม่", 409);
                OrganizationRules.CheckVersion(IdValue.Parse(request.Version), existing.Version);
                before = ToAssignmentSnapshot(existing);
                existing.RoleId = roleId;
                existing.ScopeType = request.ScopeType;
                existing.ScopeId = scopeId;
                existing.IsActive = request.IsActive;
                existing.Version++;
                entity = existing;
                action = "UPDATE";
            }
            else
            {
                entity = new CompanyAssignment
                {
                    UserId = userId,
                    CompanyId = companyId,
                    RoleId = roleId,
                    ScopeType = request.ScopeType,
                    ScopeId = scopeId,
                    IsActive = request.IsActive,
                    Version = 1
                };
                db.CompanyAssignments.Add(entity);
                action = "CREATE";
            }

            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordOrganizationAsync(
                "SURT04", action, companyId, userId, before, ToAssignmentSnapshot(entity), cancellationToken);
            return entity;
        }, cancellationToken);

        var accessible = FilterAccessibleDepartments(
            departments,
            saved.IsActive,
            saved.ScopeType,
            saved.ScopeId?.ToString(CultureInfo.InvariantCulture));

        return new UserScopeDetailDto(
            user.Id.ToString(CultureInfo.InvariantCulture),
            user.UserName ?? "",
            user.Email ?? "",
            user.IsActive,
            companyId.ToString(CultureInfo.InvariantCulture),
            saved.RoleId.ToString(CultureInfo.InvariantCulture),
            saved.ScopeType,
            saved.ScopeId?.ToString(CultureInfo.InvariantCulture),
            saved.IsActive,
            saved.Version.ToString(CultureInfo.InvariantCulture),
            accessible);
    }

    private Task DemandRoleAccessAsync(long? companyId, string action, CancellationToken cancellationToken) =>
        companyId is long cid
            ? access.DemandCompanyAsync(cid, "SURT02." + action, cancellationToken)
            : access.DemandGlobalAsync("SURT02." + action, cancellationToken);

    private Task DemandPermissionAccessAsync(long? companyId, string action, CancellationToken cancellationToken) =>
        companyId is long cid
            ? access.DemandCompanyAsync(cid, "SURT03." + action, cancellationToken)
            : access.DemandGlobalAsync("SURT03." + action, cancellationToken);

    private async Task ScreenPasswordAsync(ApplicationUser user, string password)
    {
        foreach (var validator in users.PasswordValidators)
        {
            var result = await validator.ValidateAsync(users, user, password);
            if (!result.Succeeded)
                throw new RuleException("PASSWORD_REJECTED", "รหัสผ่านชั่วคราวไม่ผ่านเกณฑ์ความปลอดภัยหรือเคยรั่วไหล กรุณาใช้รหัสใหม่", 400);
        }
    }

    private Task<ApplicationUser?> LockUserAsync(long id, CancellationToken cancellationToken) =>
        db.Users.FromSqlInterpolated($"""SELECT * FROM "SuUser" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<ApplicationRole?> LockRoleAsync(long id, CancellationToken cancellationToken) =>
        db.Roles.FromSqlInterpolated($"""SELECT * FROM "SuRole" WHERE "Id" = {id} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task<CompanyAssignment?> LockAssignmentAsync(long userId, long companyId, CancellationToken cancellationToken) =>
        db.CompanyAssignments.FromSqlInterpolated(
            $"""SELECT * FROM "SuCompanyAssignment" WHERE "UserId" = {userId} AND "CompanyId" = {companyId} FOR UPDATE""")
            .SingleOrDefaultAsync(cancellationToken);

    private Task RevokeUserSessionsAsync(long userId, CancellationToken cancellationToken) =>
        db.Sessions.Where(x => x.UserId == userId && !x.Revoked)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Revoked, true), cancellationToken);

    private async Task<IReadOnlyList<RoleDto>> BuildRoleDtosAsync(
        IReadOnlyList<ApplicationRole> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Count == 0) return [];
        var roleIds = roles.Select(x => x.Id).ToArray();
        var permCounts = await db.RolePermissions.AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .GroupBy(x => x.RoleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var assignCounts = await db.CompanyAssignments.AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId) && x.IsActive)
            .GroupBy(x => x.RoleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var globalUserCounts = await db.UserRoles.AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId))
            .GroupBy(x => x.RoleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        return roles.Select(role =>
        {
            var permissions = permCounts.GetValueOrDefault(role.Id);
            var assigned = role.CompanyId is null
                ? globalUserCounts.GetValueOrDefault(role.Id)
                : assignCounts.GetValueOrDefault(role.Id);
            var displayName = string.IsNullOrWhiteSpace(role.Description) ? role.Name ?? "" : role.Description;
            return new RoleDto(
                role.Id.ToString(CultureInfo.InvariantCulture),
                role.CompanyId?.ToString(CultureInfo.InvariantCulture),
                role.Name ?? "",
                displayName,
                role.IsActive,
                role.CodeLocked,
                permissions,
                assigned,
                role.Version.ToString(CultureInfo.InvariantCulture));
        }).ToArray();
    }

    private HashSet<string> GetGrantablePermissions(AccessSnapshot snapshot, long? companyId)
    {
        if (companyId is long cid)
        {
            var heldInCompany = snapshot.Companies
                .Where(x => x.CompanyId == cid && x.ScopeType == "Company")
                .SelectMany(x => x.Permissions);
            return snapshot.GlobalPermissions
                .Concat(heldInCompany)
                .Where(p => ProgramRegistry.CompanyGrantablePermissions.Contains(p, StringComparer.Ordinal))
                .ToHashSet(StringComparer.Ordinal);
        }

        return snapshot.GlobalPermissions.ToHashSet(StringComparer.Ordinal);
    }

    private async Task<HashSet<string>> GetActorGlobalPermissionsExcludingRoleAsync(
        long userId,
        long excludedRoleId,
        CancellationToken cancellationToken)
    {
        var perms = await (from membership in db.UserRoles.AsNoTracking()
                           join role in db.Roles.AsNoTracking() on membership.RoleId equals role.Id
                           join rp in db.RolePermissions.AsNoTracking() on role.Id equals rp.RoleId
                           where membership.UserId == userId
                               && role.CompanyId == null
                               && role.IsActive
                               && role.Id != excludedRoleId
                           select rp.Permission).ToArrayAsync(cancellationToken);
        return perms.ToHashSet(StringComparer.Ordinal);
    }

    private static IReadOnlyList<ProgramPermissionGroupDto> BuildPermissionCatalog()
    {
        return ProgramRegistry.Programs
            .Where(program => program.Implemented)
            .Select(program =>
            {
                var actions = program.Code switch
                {
                    "SURT01" => ["VIEW", "CREATE", "UPDATE", "RESET", "UNLOCK", "ACTIVATE", "DISABLE"],
                    "SURT03" or "SURT04" => ["VIEW", "UPDATE"],
                    _ => ProgramRegistry.Actions
                };
                var items = actions.Select(action =>
                {
                    var permission = $"{program.Code}.{action}";
                    var label = action switch
                    {
                        "VIEW" => "ดูข้อมูล",
                        "CREATE" => "สร้าง",
                        "UPDATE" => "แก้ไข",
                        "ACTIVATE" => "เปิดใช้งาน",
                        "DEACTIVATE" => "ปิดใช้งาน",
                        "RESET" => "รีเซ็ตรหัสชั่วคราว",
                        "UNLOCK" => "ปลดล็อก",
                        "DISABLE" => "ปิดใช้งานบัญชี",
                        _ => action
                    };
                    return new PermissionActionItemDto(
                        permission,
                        action,
                        label,
                        ProgramRegistry.GlobalOnlyPermissions.Contains(permission, StringComparer.Ordinal));
                }).ToArray();
                return new ProgramPermissionGroupDto(program.Code, program.Name, items);
            }).ToArray();
    }

    private static IReadOnlyList<DepartmentScopeOptionDto> FilterAccessibleDepartments(
        IReadOnlyList<DepartmentScopeOptionDto> departments,
        bool isActive,
        string scopeType,
        string? scopeId)
    {
        if (!isActive) return [];
        return scopeType switch
        {
            "Company" => departments,
            "Branch" when scopeId is not null => departments.Where(x => x.BranchId == scopeId).ToArray(),
            "Department" when scopeId is not null => departments.Where(x => x.Id == scopeId).ToArray(),
            _ => []
        };
    }

    private static UserSnapshot ToUserSnapshot(ApplicationUser user, DateTimeOffset now) => new(
        user.UserName ?? "",
        user.Email ?? "",
        user.IsActive,
        user.MustChangePassword,
        user.LockoutEnabled && user.LockoutEnd is DateTimeOffset end && end > now);

    private static UserDto ToUserDto(ApplicationUser user, DateTimeOffset now)
    {
        var locked = user.LockoutEnabled && user.LockoutEnd is DateTimeOffset end && end > now;
        return new UserDto(
            user.Id.ToString(CultureInfo.InvariantCulture),
            user.UserName ?? "",
            user.Email ?? "",
            user.IsActive,
            user.MustChangePassword,
            user.TemporaryPasswordExpiresAtUtc,
            locked,
            locked ? user.LockoutEnd : null,
            user.AccessFailedCount,
            user.Version.ToString(CultureInfo.InvariantCulture));
    }

    private static RoleSnapshot ToRoleSnapshot(ApplicationRole role) => new(
        role.Name ?? "",
        string.IsNullOrWhiteSpace(role.Description) ? role.Name ?? "" : role.Description,
        role.CompanyId?.ToString(CultureInfo.InvariantCulture),
        role.IsActive);

    private static AssignmentSnapshot ToAssignmentSnapshot(CompanyAssignment assignment) => new(
        assignment.UserId.ToString(CultureInfo.InvariantCulture),
        assignment.CompanyId.ToString(CultureInfo.InvariantCulture),
        assignment.RoleId.ToString(CultureInfo.InvariantCulture),
        assignment.ScopeType,
        assignment.ScopeId?.ToString(CultureInfo.InvariantCulture),
        assignment.IsActive);

    private static void EnsureIdentity(IdentityResult result, string message)
    {
        if (!result.Succeeded)
            throw new RuleException("IDENTITY_OPERATION_FAILED", message, 400);
    }
}
