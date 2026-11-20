using clean.Domain.Entities.Organization;
using clean.Domain.Entities.System;
using clean.Domain.Security;
using clean.Infrastructure.Identity;
using clean.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace clean.Infrastructure.Services;

public sealed class BootstrapService(OrganizationDbContext organization, SystemDbContext system,
    UserManager<ApplicationUser> users, RoleManager<ApplicationRole> roles, IConfiguration configuration, TimeProvider clock)
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (await system.Users.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Bootstrap requires an empty user store. Existing accounts were not changed.");
        var name = configuration["Bootstrap:UserName"] ?? throw new InvalidOperationException("Set Bootstrap__UserName.");
        var email = configuration["Bootstrap:Email"] ?? throw new InvalidOperationException("Set Bootstrap__Email.");
        var password = configuration["Bootstrap:Password"] ?? throw new InvalidOperationException("Set Bootstrap__Password.");
        var user = new ApplicationUser
        {
            UserName = name, Email = email, IsActive = true, LockoutEnabled = true,
            MustChangePassword = true, TemporaryPasswordExpiresAtUtc = clock.GetUtcNow().AddHours(24)
        };
        foreach (var validator in users.PasswordValidators)
            Require(await validator.ValidateAsync(users, user, password));

        await using var transaction = await organization.Database.BeginTransactionAsync(cancellationToken);
        await system.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
        Require(await users.CreateAsync(user, password));
        var company = Company.Create(configuration["Bootstrap:CompanyCode"] ?? "MAIN",
            configuration["Bootstrap:CompanyName"] ?? "บริษัทหลัก", user.Id, clock.GetUtcNow());
        organization.Companies.Add(company);
        await organization.SaveChangesAsync(cancellationToken);
        var companyRole = new ApplicationRole { Name = "CompanyRegistrar" };
        Require(await roles.CreateAsync(companyRole));
        system.RolePermissions.AddRange(ProgramRegistry.CompanyPermissions.Select(permission =>
            new RolePermission { RoleId = companyRole.Id, Permission = permission }));
        Require(await users.AddToRoleAsync(user, companyRole.Name));
        var organizationRole = new ApplicationRole { Name = "OrganizationAdministrator" };
        Require(await roles.CreateAsync(organizationRole));
        system.RolePermissions.AddRange(ProgramRegistry.CompanyScopedOrganizationPermissions.Select(permission =>
            new RolePermission { RoleId = organizationRole.Id, Permission = permission }));
        Require(await users.AddToRoleAsync(user, organizationRole.Name));
        var sharedMasterRole = new ApplicationRole { Name = "SharedMasterAdministrator" };
        Require(await roles.CreateAsync(sharedMasterRole));
        system.RolePermissions.AddRange(ProgramRegistry.SharedMasterPermissions.Select(permission =>
            new RolePermission { RoleId = sharedMasterRole.Id, Permission = permission }));
        Require(await users.AddToRoleAsync(user, sharedMasterRole.Name));
        var accountRole = new ApplicationRole { Name = "AccountAdministrator", Description = "ผู้ดูแลบัญชีผู้ใช้งาน" };
        Require(await roles.CreateAsync(accountRole));
        system.RolePermissions.AddRange(ProgramRegistry.UserManagementPermissions.Select(permission =>
            new RolePermission { RoleId = accountRole.Id, Permission = permission }));
        Require(await users.AddToRoleAsync(user, accountRole.Name));
        var accessRole = new ApplicationRole { Name = "AccessAdministrator", Description = "ผู้ดูแลบทบาทและสิทธิ์บริษัท" };
        Require(await roles.CreateAsync(accessRole));
        system.RolePermissions.AddRange(ProgramRegistry.CompanyAccessAdminPermissions.Select(permission =>
            new RolePermission { RoleId = accessRole.Id, Permission = permission }));
        Require(await users.AddToRoleAsync(user, accessRole.Name));
        var opsRole = new ApplicationRole { Name = "SystemCoordinator", Description = "ผู้ดูแลเมนู ตั้งค่า และประวัติระบบ" };
        Require(await roles.CreateAsync(opsRole));
        system.RolePermissions.AddRange(ProgramRegistry.SystemOperationsPermissions.Select(permission =>
            new RolePermission { RoleId = opsRole.Id, Permission = permission }));
        Require(await users.AddToRoleAsync(user, opsRole.Name));
        system.AuditEvents.Add(new AuditEvent
        {
            ActorUserId = user.Id, CompanyId = company.Id, EntityId = company.Id,
            ProgramCode = "BOOTSTRAP", Action = "INITIALIZE", Result = "SUCCESS",
            OccurredAtUtc = clock.GetUtcNow(), TraceId = "bootstrap"
        });
        await system.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void Require(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Bootstrap failed validation. Check username, email and password policy. No credentials were logged.");
    }
}
