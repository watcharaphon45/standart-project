using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using clean.Infrastructure.Identity;
using clean.Infrastructure.Persistence;
using clean.Domain.Entities.Organization;
using clean.Domain.Entities.System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Microsoft.Extensions.Logging;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace clean.Tests;

// Each fixture creates its own database. No existing application tables are cleared.
public sealed class ApiTests : IAsyncLifetime
{
    private readonly string database = "clean_test_" + Guid.NewGuid().ToString("N");
    private string adminConnection = "";
    private TestApplication app = null!;
    private long companyId;
    private long otherCompanyId;
    private long roleId;
    private long userId;
    private string testConnection = "";
    private const string Password = "Integration only password 4378!";
    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("CLEAN_TEST_DATABASE")
            ?? throw new InvalidOperationException("Set CLEAN_TEST_DATABASE to a local PostgreSQL connection with CREATE DATABASE permission.");
        await using (var connection = new NpgsqlConnection(adminConnection))
        {
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("CREATE DATABASE \"" + database + "\"", connection);
            await command.ExecuteNonQueryAsync();
        }
        var connectionString = new NpgsqlConnectionStringBuilder(adminConnection) { Database = database }.ConnectionString;
        testConnection = connectionString;
        app = new TestApplication(connectionString);
        using var scope = app.Services.CreateScope();
        var org = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        var su = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
        await org.Database.MigrateAsync(); await su.Database.MigrateAsync();
        var companies = new[] { Company.Create("ONE", "บริษัทหนึ่ง", 1, DateTimeOffset.UtcNow), Company.Create("TWO", "บริษัทสอง", 1, DateTimeOffset.UtcNow) };
        org.Companies.AddRange(companies); await org.SaveChangesAsync();
        companyId = companies[0].Id; otherCompanyId = companies[1].Id;
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = "tester", Email = "tester@example.test", IsActive = true, LockoutEnabled = true };
        Assert.True((await manager.CreateAsync(user, Password)).Succeeded); userId = user.Id;
        var role = new ApplicationRole { Name = "CompanyEditor", NormalizedName = "COMPANYEDITOR", CompanyId = companyId, IsActive = true };
        su.Roles.Add(role); await su.SaveChangesAsync(); roleId = role.Id;
        su.RolePermissions.AddRange(new RolePermission { RoleId = roleId, Permission = "DBRT01.VIEW" },
            new RolePermission { RoleId = roleId, Permission = "DBRT01.UPDATE" });
        su.CompanyAssignments.Add(new CompanyAssignment { UserId = userId, CompanyId = companyId, RoleId = roleId, IsActive = true });
        await su.SaveChangesAsync();
    }
    public async Task DisposeAsync()
    {
        await app.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("DROP DATABASE \"" + database + "\" WITH (FORCE)", connection);
        await command.ExecuteNonQueryAsync();
    }
    private HttpClient Client() => app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    private async Task Csrf(HttpClient client)
    {
        var result = await client.GetAsync("/api/auth/csrf");
        Assert.True(result.IsSuccessStatusCode, await result.Content.ReadAsStringAsync() + "\n" + app.Errors);
        var response = await result.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", response.GetProperty("token").GetString());
    }
    private async Task Login(HttpClient client)
    {
        await Csrf(client);
        var response = await client.PostAsJsonAsync("/api/auth/login", new { identifier = "tester", password = Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await Csrf(client);
    }
    [Fact] public async Task RejectsAnonymousAndMissingCsrf()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = companyId.ToString() })).StatusCode);
        await Login(client);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = companyId.ToString() })).StatusCode);
    }
    [Fact] public async Task RejectsCrossCompanyAndStaleUpdateAndAuditsAllowedChanges()
    {
        using var client = Client(); await Login(client);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = otherCompanyId.ToString() })).StatusCode);
        var response = await client.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = companyId.ToString() });
        var company = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.String, company.GetProperty("id").ValueKind);
        var update = new { id = companyId.ToString(), code = "ONE", name = "ชื่อใหม่", version = company.GetProperty("version").GetString() };
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/db/dbrt01/update", update)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/db/dbrt01/update", update)).StatusCode);
        using var scope = app.Services.CreateScope();
        var audit = await scope.ServiceProvider.GetRequiredService<SystemDbContext>().AuditEvents.SingleAsync(x => x.ProgramCode == "DBRT01");
        Assert.Equal("ชื่อใหม่", JsonDocument.Parse(audit.After!).RootElement.GetProperty("Name").GetString());
        Assert.DoesNotContain("Password", audit.After!);
    }
    [Fact] public async Task RevokesSessionOnNextRequestAfterRoleChange()
    {
        using var client = Client(); await Login(client);
        using(var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            await db.RolePermissions.Where(x => x.RoleId == roleId && x.Permission == "DBRT01.UPDATE").ExecuteDeleteAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact] public async Task LocksAccountAfterFiveFailuresAcrossUsernameAndEmail()
    {
        using var client = Client(); await Csrf(client);
        for(var index = 0; index < 5; index++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new {
                identifier = index % 2 == 0 ? "tester" : "tester@example.test", password = "incorrect password"
            })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { identifier = "tester", password = Password })).StatusCode);
    }
    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task ExpiredSessionIsRejected(bool absolute)
    {
        using var client = Client(); await Login(client);
        using(var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SystemDbContext>().Sessions
                .Where(x => x.UserId == userId).ExecuteUpdateAsync(set => set
                    .SetProperty(x => x.LastSeenAtUtc, absolute ? DateTimeOffset.UtcNow : DateTimeOffset.UtcNow.AddMinutes(-31))
                    .SetProperty(x => x.CreatedAtUtc, absolute ? DateTimeOffset.UtcNow.AddHours(-9) : DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact] public async Task TemporaryPasswordMustBeChangedAndChangeRevokesOtherSessions()
    {
        using(var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SystemDbContext>().Users.Where(x => x.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.MustChangePassword, true)
                    .SetProperty(x => x.TemporaryPasswordExpiresAtUtc, DateTimeOffset.UtcNow.AddHours(24)));
        using var first = Client(); using var second = Client();
        await Login(first); await Login(second);
        var me = await first.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.True(me.GetProperty("mustChangePassword").GetBoolean());
        Assert.Empty(me.GetProperty("companies").EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await first.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = companyId.ToString() })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await first.PostAsJsonAsync("/api/auth/change-password", new {
            currentPassword = Password, newPassword = "Changed fixture phrase 9357!"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/me")).StatusCode);
    }
    [Fact] public async Task ExpiredTemporaryPasswordCannotLogin()
    {
        using(var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SystemDbContext>().Users.Where(x => x.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.MustChangePassword, true)
                    .SetProperty(x => x.TemporaryPasswordExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
        using var client = Client(); await Csrf(client);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { identifier = "tester", password = Password })).StatusCode);
    }
    [Fact] public async Task RollsBackBusinessAndAuditTogetherAndRejectsDuplicateCode()
    {
        using(var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var role = new ApplicationRole { Name = "Registrar", NormalizedName = "REGISTRAR", IsActive = true };
            db.Roles.Add(role); await db.SaveChangesAsync();
            db.UserRoles.Add(new IdentityUserRole<long> { RoleId = role.Id, UserId = userId });
            db.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = "DBRT01.CREATE" });
            await db.SaveChangesAsync();
        }
        using var client = Client(); await Login(client);
        app.FailAudit = true;
        Assert.Equal(HttpStatusCode.InternalServerError, (await client.PostAsJsonAsync("/api/db/dbrt01/create", new { code = "NEW", name = "บริษัทใหม่" })).StatusCode);
        using(var scope = app.Services.CreateScope())
        {
            Assert.False(await scope.ServiceProvider.GetRequiredService<OrganizationDbContext>().Companies.AnyAsync(x => x.Code == "NEW"));
            Assert.False(await scope.ServiceProvider.GetRequiredService<SystemDbContext>().AuditEvents.AnyAsync(x => x.ProgramCode == "DBRT01"));
        }
        app.FailAudit = false;
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/db/dbrt01/create", new { code = " new ", name = "บริษัทใหม่" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/db/dbrt01/create", new { code = "NEW", name = "ซ้ำ" })).StatusCode);
        using(var scope = app.Services.CreateScope())
            Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<SystemDbContext>().AuditEvents.CountAsync(x => x.ProgramCode == "DBRT01"));
    }

    [Fact]
    public async Task EnforcesOrganizationHierarchyUniquenessScopeAndSharedMasterRules()
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var extraPermissions = clean.Domain.Security.ProgramRegistry.CompanyScopedOrganizationPermissions
                .Concat(["DBRT05.VIEW", "DBRT05.CREATE", "DBRT07.VIEW", "DBRT07.CREATE"])
                .Select(permission => new RolePermission { RoleId = roleId, Permission = permission });
            db.RolePermissions.AddRange(extraPermissions);
            await db.SaveChangesAsync();
        }

        using var client = Client();
        await Login(client);

        // DBRT02: Create branches BKK and CNX; verify company code becomes locked and duplicate branch code rejects.
        var bkkResp = await client.PostAsJsonAsync("/api/db/dbrt02/create", new { companyId = companyId.ToString(), code = " bkk ", name = "สาขากรุงเทพ" });
        Assert.Equal(HttpStatusCode.OK, bkkResp.StatusCode);
        var bkk = await bkkResp.Content.ReadFromJsonAsync<JsonElement>();
        var bkkId = bkk.GetProperty("id").GetString()!;

        var companyDetail = await (await client.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = companyId.ToString() }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(companyDetail.GetProperty("codeLocked").GetBoolean());

        var cnxResp = await client.PostAsJsonAsync("/api/db/dbrt02/create", new { companyId = companyId.ToString(), code = "cnx", name = "สาขาเชียงใหม่" });
        Assert.Equal(HttpStatusCode.OK, cnxResp.StatusCode);
        var cnx = await cnxResp.Content.ReadFromJsonAsync<JsonElement>();
        var cnxId = cnx.GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/db/dbrt02/create",
            new { companyId = companyId.ToString(), code = "BKK", name = "ซ้ำ" })).StatusCode);

        // DBRT04: Division uniqueness is company-wide across branches; unlocked division can reparent.
        var finResp = await client.PostAsJsonAsync("/api/db/dbrt04/create",
            new { companyId = companyId.ToString(), branchId = bkkId, code = "fin", name = "ฝ่ายการเงิน" });
        Assert.Equal(HttpStatusCode.OK, finResp.StatusCode);
        var fin = await finResp.Content.ReadFromJsonAsync<JsonElement>();
        var finId = fin.GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/db/dbrt04/create",
            new { companyId = companyId.ToString(), branchId = cnxId, code = "FIN", name = "ฝ่ายการเงินซ้ำต่างสาขา" })).StatusCode);

        var opsResp = await client.PostAsJsonAsync("/api/db/dbrt04/create",
            new { companyId = companyId.ToString(), branchId = bkkId, code = "ops", name = "ฝ่ายปฏิบัติการ" });
        Assert.Equal(HttpStatusCode.OK, opsResp.StatusCode);
        var ops = await opsResp.Content.ReadFromJsonAsync<JsonElement>();
        var opsId = ops.GetProperty("id").GetString()!;

        var movedOpsResp = await client.PutAsJsonAsync("/api/db/dbrt04/update",
            new { id = opsId, branchId = cnxId, code = "OPS", name = "ฝ่ายปฏิบัติการเชียงใหม่", version = ops.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.OK, movedOpsResp.StatusCode);

        // DBRT03: Department code uniqueness is checked per type (FIN allowed here), locks Division FIN.
        var deptResp = await client.PostAsJsonAsync("/api/db/dbrt03/create",
            new { companyId = companyId.ToString(), divisionId = finId, code = "fin", name = "แผนกการเงิน" });
        Assert.Equal(HttpStatusCode.OK, deptResp.StatusCode);
        var dept = await deptResp.Content.ReadFromJsonAsync<JsonElement>();
        var deptId = dept.GetProperty("id").GetString()!;

        var finLockedDetail = await (await client.PostAsJsonAsync("/api/db/dbrt04/detail", new { id = finId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(finLockedDetail.GetProperty("codeLocked").GetBoolean());

        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync("/api/db/dbrt04/update",
            new { id = finId, branchId = cnxId, code = "FIN", name = "ห้ามย้าย", version = finLockedDetail.GetProperty("version").GetString() })).StatusCode);

        // Bottom-up deactivation enforcement.
        var bkkDetail = await (await client.PostAsJsonAsync("/api/db/dbrt02/detail", new { id = bkkId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/db/dbrt02/deactivate",
            new { id = bkkId, version = bkkDetail.GetProperty("version").GetString() })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/db/dbrt04/deactivate",
            new { id = finId, version = finLockedDetail.GetProperty("version").GetString() })).StatusCode);

        var deptDeactResp = await client.PostAsJsonAsync("/api/db/dbrt03/deactivate",
            new { id = deptId, version = dept.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.OK, deptDeactResp.StatusCode);
        var deptDeact = await deptDeactResp.Content.ReadFromJsonAsync<JsonElement>();

        var finDeactResp = await client.PostAsJsonAsync("/api/db/dbrt04/deactivate",
            new { id = finId, version = finLockedDetail.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.OK, finDeactResp.StatusCode);
        var finDeact = await finDeactResp.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/db/dbrt03/activate",
            new { id = deptId, version = deptDeact.GetProperty("version").GetString() })).StatusCode);

        var finReactResp = await client.PostAsJsonAsync("/api/db/dbrt04/activate",
            new { id = finId, version = finDeact.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.OK, finReactResp.StatusCode);
        var deptReactResp = await client.PostAsJsonAsync("/api/db/dbrt03/activate",
            new { id = deptId, version = deptDeact.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.OK, deptReactResp.StatusCode);

        // DBRT06: CostCenter belongs to Company.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/db/dbrt06/create",
            new { companyId = companyId.ToString(), code = "cc01", name = "ศูนย์บัญชี" })).StatusCode);

        // DBRT05 / DBRT07: Company role cannot mutate shared masters even if assigned in role permissions.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/db/dbrt05/create",
            new { code = "DEV", name = "นักพัฒนา" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/db/dbrt07/create",
            new { code = "BU01", name = "หน่วยธุรกิจ" })).StatusCode);

        // Global SharedMasterAdministrator can mutate DBRT05 / DBRT07 with CompanyId = null in AuditEvent.
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var sharedRole = new ApplicationRole { Name = "SharedMasterAdmin", NormalizedName = "SHAREDMASTERADMIN", IsActive = true };
            db.Roles.Add(sharedRole);
            await db.SaveChangesAsync();
            db.UserRoles.Add(new IdentityUserRole<long> { RoleId = sharedRole.Id, UserId = userId });
            db.RolePermissions.AddRange(clean.Domain.Security.ProgramRegistry.SharedMasterPermissions
                .Select(p => new RolePermission { RoleId = sharedRole.Id, Permission = p }));
            await db.SaveChangesAsync();
        }
        using var adminClient = Client();
        await Login(adminClient);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync("/api/db/dbrt05/create",
            new { code = "dev", name = "นักพัฒนา" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsJsonAsync("/api/db/dbrt07/create",
            new { code = "bu01", name = "หน่วยธุรกิจหลัก" })).StatusCode);
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var posAudit = await db.AuditEvents.SingleAsync(x => x.ProgramCode == "DBRT05");
            var buAudit = await db.AuditEvents.SingleAsync(x => x.ProgramCode == "DBRT07");
            Assert.Null(posAudit.CompanyId);
            Assert.Null(buAudit.CompanyId);
        }

        // Branch-scoped assignment authorizes CNX branch/divisions/departments and rejects BKK + Company-only DBRT06.
        using (var scope = app.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var branchUser = new ApplicationUser { UserName = "brancher", Email = "brancher@example.test", IsActive = true, LockoutEnabled = true };
            Assert.True((await manager.CreateAsync(branchUser, Password)).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var branchRole = new ApplicationRole { Name = "BranchManager", NormalizedName = "BRANCHMANAGER", CompanyId = companyId, IsActive = true };
            db.Roles.Add(branchRole);
            await db.SaveChangesAsync();
            db.RolePermissions.AddRange(new[]
            {
                "DBRT02.VIEW", "DBRT04.VIEW", "DBRT04.CREATE", "DBRT03.VIEW", "DBRT03.CREATE", "DBRT05.VIEW", "DBRT06.VIEW"
            }.Select(p => new RolePermission { RoleId = branchRole.Id, Permission = p }));
            db.CompanyAssignments.Add(new CompanyAssignment
            {
                UserId = branchUser.Id, CompanyId = companyId, RoleId = branchRole.Id,
                ScopeType = "Branch", ScopeId = long.Parse(cnxId), IsActive = true
            });
            await db.SaveChangesAsync();
        }

        using var branchClient = Client();
        await Csrf(branchClient);
        Assert.Equal(HttpStatusCode.OK, (await branchClient.PostAsJsonAsync("/api/auth/login",
            new { identifier = "brancher", password = Password })).StatusCode);
        await Csrf(branchClient);

        Assert.Equal(HttpStatusCode.OK, (await branchClient.PostAsJsonAsync("/api/db/dbrt02/detail", new { id = cnxId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchClient.PostAsJsonAsync("/api/db/dbrt02/detail", new { id = bkkId })).StatusCode);
        var branchSearch = await (await branchClient.PostAsJsonAsync("/api/db/dbrt02/search",
            new { companyId = companyId.ToString() })).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, branchSearch.GetProperty("totalCount").GetInt32());
        Assert.Equal("CNX", branchSearch.GetProperty("items")[0].GetProperty("code").GetString());

        Assert.Equal(HttpStatusCode.Forbidden, (await branchClient.PostAsJsonAsync("/api/db/dbrt04/create",
            new { companyId = companyId.ToString(), branchId = bkkId, code = "NOPE", name = "ห้าม" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await branchClient.PostAsJsonAsync("/api/db/dbrt03/create",
            new { companyId = companyId.ToString(), divisionId = opsId, code = "TEAM1", name = "ทีมเชียงใหม่" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchClient.PostAsJsonAsync("/api/db/dbrt03/create",
            new { companyId = companyId.ToString(), divisionId = finId, code = "NOPE", name = "ห้าม" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await branchClient.PostAsJsonAsync("/api/db/dbrt05/search", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchClient.PostAsJsonAsync("/api/db/dbrt06/search",
            new { companyId = companyId.ToString() })).StatusCode);
    }

    [Fact]
    public async Task EnforcesSystemAdministrationUserRolePermissionGrantCeilingAndScopeRules()
    {
        // Setup: Global AccountAdmin for SURT01, CompanyAdmin for companyId (Company ONE), OtherCompanyAdmin for otherCompanyId (Company TWO).
        using (var scope = app.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var org = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();

            var b1 = Branch.Create(companyId, "B1", "สาขาหนึ่ง", 1, DateTimeOffset.UtcNow);
            var b2 = Branch.Create(companyId, "B2", "สาขาสอง", 1, DateTimeOffset.UtcNow);
            org.Branches.AddRange(b1, b2);
            await org.SaveChangesAsync();

            var div1 = Division.Create(companyId, b1.Id, "DV1", "ฝ่ายหนึ่ง", 1, DateTimeOffset.UtcNow);
            var div2 = Division.Create(companyId, b2.Id, "DV2", "ฝ่ายสอง", 1, DateTimeOffset.UtcNow);
            org.Divisions.AddRange(div1, div2);
            await org.SaveChangesAsync();

            var d1 = Department.Create(companyId, div1.Id, "DP1", "แผนกหนึ่ง", 1, DateTimeOffset.UtcNow);
            var d2 = Department.Create(companyId, div2.Id, "DP2", "แผนกสอง", 1, DateTimeOffset.UtcNow);
            org.Departments.AddRange(d1, d2);
            await org.SaveChangesAsync();

            var accountAdmin = new ApplicationUser { UserName = "accadmin", Email = "accadmin@example.test", IsActive = true, LockoutEnabled = true };
            Assert.True((await manager.CreateAsync(accountAdmin, Password)).Succeeded);
            var accountRole = new ApplicationRole { Name = "ACCOUNTADMIN", NormalizedName = "ACCOUNTADMIN", Description = "ผู้ดูแลบัญชี", IsActive = true };
            db.Roles.Add(accountRole);
            await db.SaveChangesAsync();
            db.UserRoles.Add(new IdentityUserRole<long> { UserId = accountAdmin.Id, RoleId = accountRole.Id });
            db.RolePermissions.AddRange(clean.Domain.Security.ProgramRegistry.UserManagementPermissions
                .Select(p => new RolePermission { RoleId = accountRole.Id, Permission = p }));

            var companyAdmin = new ApplicationUser { UserName = "compadmin", Email = "compadmin@example.test", IsActive = true, LockoutEnabled = true };
            Assert.True((await manager.CreateAsync(companyAdmin, Password)).Succeeded);
            var companyAdminRole = new ApplicationRole
            {
                Name = "COMPADMIN", NormalizedName = "COMPADMIN", CompanyId = companyId, Description = "ผู้ดูแลบริษัท 1", IsActive = true
            };
            db.Roles.Add(companyAdminRole);
            await db.SaveChangesAsync();
            db.RolePermissions.AddRange(clean.Domain.Security.ProgramRegistry.CompanyAccessAdminPermissions
                .Concat(["DBRT02.VIEW", "DBRT03.VIEW", "DBRT03.CREATE", "DBRT04.VIEW", "DBRT05.CREATE"])
                .Select(p => new RolePermission { RoleId = companyAdminRole.Id, Permission = p }));
            db.CompanyAssignments.Add(new CompanyAssignment
            {
                UserId = companyAdmin.Id, CompanyId = companyId, RoleId = companyAdminRole.Id, ScopeType = "Company", IsActive = true
            });

            var otherAdmin = new ApplicationUser { UserName = "otheradmin", Email = "otheradmin@example.test", IsActive = true, LockoutEnabled = true };
            Assert.True((await manager.CreateAsync(otherAdmin, Password)).Succeeded);
            var otherAdminRole = new ApplicationRole
            {
                Name = "COMPADMIN", NormalizedName = "COMPADMIN", CompanyId = otherCompanyId, Description = "ผู้ดูแลบริษัท 2", IsActive = true
            };
            db.Roles.Add(otherAdminRole);
            await db.SaveChangesAsync();
            db.RolePermissions.AddRange(clean.Domain.Security.ProgramRegistry.CompanyAccessAdminPermissions
                .Select(p => new RolePermission { RoleId = otherAdminRole.Id, Permission = p }));
            db.CompanyAssignments.Add(new CompanyAssignment
            {
                UserId = otherAdmin.Id, CompanyId = otherCompanyId, RoleId = otherAdminRole.Id, ScopeType = "Company", IsActive = true
            });

            await db.SaveChangesAsync();
        }

        // 1) SURT01: CompanyAdmin is forbidden; Global AccountAdmin manages full user lifecycle.
        using var compClient = Client();
        await Csrf(compClient);
        Assert.Equal(HttpStatusCode.OK, (await compClient.PostAsJsonAsync("/api/auth/login", new { identifier = "compadmin", password = Password })).StatusCode);
        await Csrf(compClient);
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PostAsJsonAsync("/api/su/surt01/search", new { })).StatusCode);

        using var accClient = Client();
        await Csrf(accClient);
        Assert.Equal(HttpStatusCode.OK, (await accClient.PostAsJsonAsync("/api/auth/login", new { identifier = "accadmin", password = Password })).StatusCode);
        await Csrf(accClient);

        // Reject '@' in UserName
        Assert.Equal(HttpStatusCode.BadRequest, (await accClient.PostAsJsonAsync("/api/su/surt01/create", new
        {
            userName = "alice@corp", email = "alice@example.test", temporaryPassword = "Alice Temp Phrase 1234!"
        })).StatusCode);

        const string tempPass1 = "Alice Temp Phrase 1234!";
        const string tempPass2 = "Alice Reset Phrase 5678!";
        const string aliceRealPass = "Alice Chosen Phrase 9012!";

        var createAlice = await accClient.PostAsJsonAsync("/api/su/surt01/create", new
        {
            userName = "alice", email = "alice@example.test", temporaryPassword = tempPass1
        });
        Assert.Equal(HttpStatusCode.OK, createAlice.StatusCode);
        var alice = await createAlice.Content.ReadFromJsonAsync<JsonElement>();
        var aliceId = alice.GetProperty("id").GetString()!;
        Assert.True(alice.GetProperty("mustChangePassword").GetBoolean());

        // Self-disable forbidden/conflict
        var accDetail = await (await accClient.PostAsJsonAsync("/api/su/surt01/search", new { keyword = "accadmin" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var accAdminDto = accDetail.GetProperty("items")[0];
        Assert.Equal(HttpStatusCode.Conflict, (await accClient.PostAsJsonAsync("/api/su/surt01/disable", new
        {
            id = accAdminDto.GetProperty("id").GetString(), version = accAdminDto.GetProperty("version").GetString()
        })).StatusCode);

        // Update Alice email and check concurrency
        var updateAlice = await accClient.PutAsJsonAsync("/api/su/surt01/update", new
        {
            id = aliceId, userName = "alice", email = "alice.updated@example.test", version = alice.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, updateAlice.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await accClient.PutAsJsonAsync("/api/su/surt01/update", new
        {
            id = aliceId, userName = "alice", email = "stale@example.test", version = alice.GetProperty("version").GetString()
        })).StatusCode);
        alice = await updateAlice.Content.ReadFromJsonAsync<JsonElement>();

        // Alice logs in with temp password, changes password, and holds an active session.
        using var aliceClient = Client();
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = tempPass1 })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.NoContent, (await aliceClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = tempPass1, newPassword = aliceRealPass
        })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = aliceRealPass })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.GetAsync("/api/auth/me")).StatusCode);

        // Reset temporary password revokes Alice's existing session.
        var resetAlice = await accClient.PostAsJsonAsync("/api/su/surt01/reset", new
        {
            id = aliceId, temporaryPassword = tempPass2, version = alice.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, resetAlice.StatusCode);
        alice = await resetAlice.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(alice.GetProperty("mustChangePassword").GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await aliceClient.GetAsync("/api/auth/me")).StatusCode);

        // Lockout and Unlock flow
        for (var i = 0; i < 5; i++)
        {
            using var lockAttempt = Client();
            await Csrf(lockAttempt);
            await lockAttempt.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = "wrong password 12345" });
        }
        var aliceLockedDetail = await (await accClient.PostAsJsonAsync("/api/su/surt01/detail", new { id = aliceId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(aliceLockedDetail.GetProperty("isLockedOut").GetBoolean());

        var unlockResp = await accClient.PostAsJsonAsync("/api/su/surt01/unlock", new
        {
            id = aliceId, version = aliceLockedDetail.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, unlockResp.StatusCode);
        alice = await unlockResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(alice.GetProperty("isLockedOut").GetBoolean());

        // Disable then Activate Alice; verify disable revokes active sessions.
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = tempPass2 })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.NoContent, (await aliceClient.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = tempPass2, newPassword = aliceRealPass
        })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = aliceRealPass })).StatusCode);
        await Csrf(aliceClient);

        var disableResp = await accClient.PostAsJsonAsync("/api/su/surt01/disable", new
        {
            id = aliceId, version = alice.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, disableResp.StatusCode);
        alice = await disableResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Unauthorized, (await aliceClient.GetAsync("/api/auth/me")).StatusCode);

        var activateResp = await accClient.PostAsJsonAsync("/api/su/surt01/activate", new
        {
            id = aliceId, version = alice.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, activateResp.StatusCode);

        // Verify SU audit never stores temporaryPassword text.
        using (var scope = app.Services.CreateScope())
        {
            var suAudits = await scope.ServiceProvider.GetRequiredService<SystemDbContext>().AuditEvents
                .Where(x => x.ProgramCode == "SURT01").ToListAsync();
            Assert.NotEmpty(suAudits);
            Assert.All(suAudits, a =>
            {
                Assert.DoesNotContain(tempPass1, a.After ?? "");
                Assert.DoesNotContain(tempPass2, a.After ?? "");
            });
        }

        // 2) SURT02: Same role code allowed across companies ONE & TWO; duplicate rejected within company.
        var finRoleResp = await compClient.PostAsJsonAsync("/api/su/surt02/create", new
        {
            companyId = companyId.ToString(), code = " fin_staff ", name = "เจ้าหน้าที่การเงิน"
        });
        Assert.Equal(HttpStatusCode.OK, finRoleResp.StatusCode);
        var finRole = await finRoleResp.Content.ReadFromJsonAsync<JsonElement>();
        var finRoleId = finRole.GetProperty("id").GetString()!;

        Assert.Equal(HttpStatusCode.Conflict, (await compClient.PostAsJsonAsync("/api/su/surt02/create", new
        {
            companyId = companyId.ToString(), code = "FIN_STAFF", name = "ซ้ำ"
        })).StatusCode);

        using var otherClient = Client();
        await Csrf(otherClient);
        Assert.Equal(HttpStatusCode.OK, (await otherClient.PostAsJsonAsync("/api/auth/login", new { identifier = "otheradmin", password = Password })).StatusCode);
        await Csrf(otherClient);
        var otherFinResp = await otherClient.PostAsJsonAsync("/api/su/surt02/create", new
        {
            companyId = otherCompanyId.ToString(), code = "FIN_STAFF", name = "การเงินบริษัทสอง"
        });
        Assert.Equal(HttpStatusCode.OK, otherFinResp.StatusCode);
        var otherFinRoleId = (await otherFinResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        // 3) SURT03: Grant ceiling and self-escalation prevention
        // Disallow permission not held by compadmin (DBRT06.CREATE)
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PutAsJsonAsync("/api/su/surt03/update", new
        {
            roleId = finRoleId, permissions = new[] { "DBRT06.CREATE" }, version = finRole.GetProperty("version").GetString()
        })).StatusCode);

        // Disallow GlobalOnlyPermission on company role even if compadmin holds DBRT05.CREATE globally or in role
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PutAsJsonAsync("/api/su/surt03/update", new
        {
            roleId = finRoleId, permissions = new[] { "DBRT05.CREATE" }, version = finRole.GetProperty("version").GetString()
        })).StatusCode);

        // Disallow self-escalation (compadmin editing their own role COMPADMIN)
        var rolesSearch = await (await compClient.PostAsJsonAsync("/api/su/surt02/search", new { companyId = companyId.ToString(), keyword = "COMPADMIN" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var ownRole = rolesSearch.GetProperty("items")[0];
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PutAsJsonAsync("/api/su/surt03/update", new
        {
            roleId = ownRole.GetProperty("id").GetString(), permissions = new[] { "SURT03.VIEW" }, version = ownRole.GetProperty("version").GetString()
        })).StatusCode);

        // Grant valid permissions within ceiling
        var permUpdate = await compClient.PutAsJsonAsync("/api/su/surt03/update", new
        {
            roleId = finRoleId,
            permissions = new[] { "DBRT02.VIEW", "DBRT04.VIEW", "DBRT03.VIEW", "DBRT03.CREATE" },
            version = finRole.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, permUpdate.StatusCode);

        // 4) SURT04: User scope assignment (Branch -> Department), locks role code & prevents cross-company / self edits.
        string b1Id, b2Id, d1Id, d2Id;
        using (var scope = app.Services.CreateScope())
        {
            var org = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            b1Id = (await org.Branches.AsNoTracking().SingleAsync(x => x.CompanyId == companyId && x.Code == "B1")).Id.ToString();
            b2Id = (await org.Branches.AsNoTracking().SingleAsync(x => x.CompanyId == companyId && x.Code == "B2")).Id.ToString();
            d1Id = (await org.Departments.AsNoTracking().SingleAsync(x => x.CompanyId == companyId && x.Code == "DP1")).Id.ToString();
            d2Id = (await org.Departments.AsNoTracking().SingleAsync(x => x.CompanyId == companyId && x.Code == "DP2")).Id.ToString();
        }

        // Cross-company role rejected (403)
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PutAsJsonAsync("/api/su/surt04/update", new
        {
            companyId = companyId.ToString(), userId = aliceId, roleId = otherFinRoleId, scopeType = "Company", scopeId = (string?)null, isActive = true
        })).StatusCode);

        // Self-assignment modification by non-global admin rejected (403)
        var compSearch = await (await compClient.PostAsJsonAsync("/api/su/surt04/search", new { companyId = companyId.ToString(), keyword = "compadmin" }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var compSelf = compSearch.GetProperty("items")[0];
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PutAsJsonAsync("/api/su/surt04/update", new
        {
            companyId = companyId.ToString(),
            userId = compSelf.GetProperty("userId").GetString(),
            roleId = finRoleId,
            scopeType = "Company",
            scopeId = (string?)null,
            isActive = true,
            version = compSelf.GetProperty("version").GetString()
        })).StatusCode);

        // Assign Alice to Branch B1
        var assignBranch = await compClient.PutAsJsonAsync("/api/su/surt04/update", new
        {
            companyId = companyId.ToString(), userId = aliceId, roleId = finRoleId, scopeType = "Branch", scopeId = b1Id, isActive = true
        });
        Assert.Equal(HttpStatusCode.OK, assignBranch.StatusCode);
        var scopeDetail = await assignBranch.Content.ReadFromJsonAsync<JsonElement>();
        var accessible = scopeDetail.GetProperty("accessibleDepartments").EnumerateArray().ToArray();
        Assert.Single(accessible);
        Assert.Equal(d1Id, accessible[0].GetProperty("id").GetString());

        // Assigned role is now locked and cannot change code or deactivate
        var lockedRole = await (await compClient.PostAsJsonAsync("/api/su/surt02/detail", new { id = finRoleId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(lockedRole.GetProperty("codeLocked").GetBoolean());
        Assert.Equal(HttpStatusCode.Conflict, (await compClient.PutAsJsonAsync("/api/su/surt02/update", new
        {
            id = finRoleId, code = "RENAMED", name = "เจ้าหน้าที่การเงิน", version = lockedRole.GetProperty("version").GetString()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await compClient.PostAsJsonAsync("/api/su/surt02/deactivate", new
        {
            id = finRoleId, version = lockedRole.GetProperty("version").GetString()
        })).StatusCode);

        // Alice logs in and sees D1 (allowed) but not D2 (forbidden)
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = aliceRealPass })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/db/dbrt03/detail", new { id = d1Id })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await aliceClient.PostAsJsonAsync("/api/db/dbrt03/detail", new { id = d2Id })).StatusCode);

        // Admin changes Alice's scope to Department DP2 -> Alice's session immediately revokes via AuthorizationFingerprint
        var assignDept = await compClient.PutAsJsonAsync("/api/su/surt04/update", new
        {
            companyId = companyId.ToString(),
            userId = aliceId,
            roleId = finRoleId,
            scopeType = "Department",
            scopeId = d2Id,
            isActive = true,
            version = scopeDetail.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, assignDept.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await aliceClient.GetAsync("/api/auth/me")).StatusCode);

        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/auth/login", new { identifier = "alice", password = aliceRealPass })).StatusCode);
        await Csrf(aliceClient);
        Assert.Equal(HttpStatusCode.Forbidden, (await aliceClient.PostAsJsonAsync("/api/db/dbrt03/detail", new { id = d1Id })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await aliceClient.PostAsJsonAsync("/api/db/dbrt03/detail", new { id = d2Id })).StatusCode);
    }

    [Fact]
    public async Task EnforcesSystemOperationsMenuDocNumberingWorkflowSettingsAndScopedAuditRules()
    {
        string compOpsRoleId, apprFinRoleId, otherApprRoleId;
        long branchAuditorUserId, branchAuditorRoleId;
        using (var scope = app.Services.CreateScope())
        {
            var su = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var sysUser = new ApplicationUser { UserName = "syscoord", Email = "syscoord@example.test", IsActive = true };
            var compUser = new ApplicationUser { UserName = "compops", Email = "compops@example.test", IsActive = true };
            var viewUser = new ApplicationUser { UserName = "auditview", Email = "auditview@example.test", IsActive = true };
            var branchUser = new ApplicationUser { UserName = "branchauditor", Email = "branchauditor@example.test", IsActive = true };
            Assert.True((await userManager.CreateAsync(sysUser, Password)).Succeeded);
            Assert.True((await userManager.CreateAsync(compUser, Password)).Succeeded);
            Assert.True((await userManager.CreateAsync(viewUser, Password)).Succeeded);
            Assert.True((await userManager.CreateAsync(branchUser, Password)).Succeeded);
            branchAuditorUserId = branchUser.Id;

            var sysRole = new ApplicationRole { Name = "SYS_COORD_TEST", NormalizedName = "SYS_COORD_TEST", IsActive = true };
            var compRole = new ApplicationRole { Name = "COMP_OPS_TEST", NormalizedName = "COMP_OPS_TEST", CompanyId = companyId, IsActive = true };
            var viewRole = new ApplicationRole { Name = "AUDIT_VIEW_ONLY", NormalizedName = "AUDIT_VIEW_ONLY", CompanyId = companyId, IsActive = true };
            var branchRole = new ApplicationRole { Name = "BRANCH_AUDITOR_TEST", NormalizedName = "BRANCH_AUDITOR_TEST", CompanyId = companyId, IsActive = true };
            var apprFin = new ApplicationRole { Name = "APPR_FIN", NormalizedName = "APPR_FIN", CompanyId = companyId, IsActive = true };
            var otherAppr = new ApplicationRole { Name = "OTHER_APPR", NormalizedName = "OTHER_APPR", CompanyId = otherCompanyId, IsActive = true };
            su.Roles.AddRange(sysRole, compRole, viewRole, branchRole, apprFin, otherAppr);
            await su.SaveChangesAsync();

            compOpsRoleId = compRole.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            apprFinRoleId = apprFin.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            otherApprRoleId = otherAppr.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            branchAuditorRoleId = branchRole.Id;

            su.UserRoles.Add(new IdentityUserRole<long> { UserId = sysUser.Id, RoleId = sysRole.Id });
            su.RolePermissions.AddRange(clean.Domain.Security.ProgramRegistry.SystemOperationsPermissions
                .Select(p => new RolePermission { RoleId = sysRole.Id, Permission = p }));

            var compPermissions = clean.Domain.Security.ProgramRegistry.DocumentSequencePermissions
                .Concat(clean.Domain.Security.ProgramRegistry.ApprovalWorkflowPermissions)
                .Concat(clean.Domain.Security.ProgramRegistry.SystemSettingPermissions)
                .Concat(clean.Domain.Security.ProgramRegistry.AuditLogPermissions)
                .Concat(["SURT05.VIEW", "SURT02.VIEW", "SURT02.DEACTIVATE", "DBRT02.VIEW", "DBRT02.CREATE"]);
            su.RolePermissions.AddRange(compPermissions
                .Select(p => new RolePermission { RoleId = compRole.Id, Permission = p }));
            su.RolePermissions.Add(new RolePermission { RoleId = viewRole.Id, Permission = "SURT09.VIEW" });
            su.RolePermissions.Add(new RolePermission { RoleId = branchRole.Id, Permission = "SURT09.VIEW" });

            su.CompanyAssignments.AddRange(
                new CompanyAssignment { UserId = compUser.Id, CompanyId = companyId, RoleId = compRole.Id, IsActive = true, ScopeType = "Company" },
                new CompanyAssignment { UserId = viewUser.Id, CompanyId = companyId, RoleId = viewRole.Id, IsActive = true, ScopeType = "Company" });
            await su.SaveChangesAsync();
        }

        using var sysClient = Client();
        await Csrf(sysClient);
        Assert.Equal(HttpStatusCode.OK, (await sysClient.PostAsJsonAsync("/api/auth/login", new { identifier = "syscoord", password = Password })).StatusCode);
        await Csrf(sysClient);

        using var compClient = Client();
        await Csrf(compClient);
        Assert.Equal(HttpStatusCode.OK, (await compClient.PostAsJsonAsync("/api/auth/login", new { identifier = "compops", password = Password })).StatusCode);
        await Csrf(compClient);

        using var viewClient = Client();
        await Csrf(viewClient);
        Assert.Equal(HttpStatusCode.OK, (await viewClient.PostAsJsonAsync("/api/auth/login", new { identifier = "auditview", password = Password })).StatusCode);
        await Csrf(viewClient);

        // 1) SURT05: Global-only Menu Configuration + UI navigation visibility without bypassing backend policies
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PostAsJsonAsync("/api/su/surt05/search", new { })).StatusCode);

        var menuSearch = await (await sysClient.PostAsJsonAsync("/api/su/surt05/search", new { pageSize = 50 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(16, menuSearch.GetProperty("totalCount").GetInt32());
        var menus = menuSearch.GetProperty("items").EnumerateArray().ToArray();
        var surt06Menu = menus.Single(x => x.GetProperty("programCode").GetString() == "SURT06");
        var surt08Menu = menus.Single(x => x.GetProperty("programCode").GetString() == "SURT08");

        var menuUpdateResp = await sysClient.PutAsJsonAsync("/api/su/surt05/update", new
        {
            id = surt06Menu.GetProperty("id").GetString(),
            displayName = "เลขที่เอกสารกำหนดเอง",
            sortOrder = 1,
            version = surt06Menu.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, menuUpdateResp.StatusCode);

        var menuDeactivateResp = await sysClient.PostAsJsonAsync("/api/su/surt05/deactivate", new
        {
            id = surt08Menu.GetProperty("id").GetString(),
            version = surt08Menu.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, menuDeactivateResp.StatusCode);

        var compMe = await (await compClient.GetAsync("/api/auth/me")).Content.ReadFromJsonAsync<JsonElement>();
        var navItems = compMe.GetProperty("navigation").EnumerateArray().ToArray();
        var navCodes = navItems.Select(x => x.GetProperty("code").GetString()!).ToArray();
        Assert.True(Array.IndexOf(navCodes, "SURT06") >= 0);
        Assert.True(Array.IndexOf(navCodes, "SURT06") < Array.IndexOf(navCodes, "SURT07"));
        Assert.Equal("เลขที่เอกสารกำหนดเอง", navItems.Single(x => x.GetProperty("code").GetString() == "SURT06").GetProperty("name").GetString());
        Assert.DoesNotContain("SURT08", navCodes);

        // Backend authorization still allows SURT08 even when its menu is hidden
        Assert.Equal(HttpStatusCode.OK, (await compClient.PostAsJsonAsync("/api/su/surt08/search", new
        {
            companyId = companyId.ToString()
        })).StatusCode);

        // 2) SURT06: Document Numbering & Code/Prefix/ResetPolicy lock after allocation
        var ym = DateTimeOffset.UtcNow.ToString("yyyyMM", System.Globalization.CultureInfo.InvariantCulture);
        var seqCreateResp = await compClient.PostAsJsonAsync("/api/su/surt06/create", new
        {
            companyId = companyId.ToString(),
            code = " inv ",
            name = "เลขที่ใบแจ้งหนี้",
            prefix = " inv ",
            resetPolicy = "year_month",
            paddingLength = 5
        });
        Assert.Equal(HttpStatusCode.OK, seqCreateResp.StatusCode);
        var seq = await seqCreateResp.Content.ReadFromJsonAsync<JsonElement>();
        var seqId = seq.GetProperty("id").GetString()!;
        Assert.Equal("INV", seq.GetProperty("code").GetString());
        Assert.Equal("INV", seq.GetProperty("prefix").GetString());
        Assert.Equal("YEAR_MONTH", seq.GetProperty("resetPolicy").GetString());
        Assert.False(seq.GetProperty("codeLocked").GetBoolean());
        Assert.Equal($"INV-{ym}-00001", seq.GetProperty("previewNextNumber").GetString());

        var alloc1Resp = await compClient.PostAsJsonAsync("/api/su/surt06/next", new
        {
            id = seqId,
            version = seq.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, alloc1Resp.StatusCode);
        var alloc1 = await alloc1Resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"INV-{ym}-00001", alloc1.GetProperty("documentNumber").GetString());
        Assert.Equal(1, alloc1.GetProperty("sequenceNumber").GetInt64());

        var alloc2Resp = await compClient.PostAsJsonAsync("/api/su/surt06/next", new
        {
            id = seqId,
            version = alloc1.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, alloc2Resp.StatusCode);
        var alloc2 = await alloc2Resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal($"INV-{ym}-00002", alloc2.GetProperty("documentNumber").GetString());
        Assert.Equal(2, alloc2.GetProperty("sequenceNumber").GetInt64());

        var lockedSeq = await (await compClient.PostAsJsonAsync("/api/su/surt06/detail", new { id = seqId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(lockedSeq.GetProperty("codeLocked").GetBoolean());
        Assert.Equal(2, lockedSeq.GetProperty("lastNumber").GetInt64());
        Assert.Equal($"INV-{ym}-00003", lockedSeq.GetProperty("previewNextNumber").GetString());

        var seqLockedUpdate = await compClient.PutAsJsonAsync("/api/su/surt06/update", new
        {
            id = seqId,
            code = "INV",
            name = "เปลี่ยนคำนำหน้าไม่ได้",
            prefix = "BILL",
            resetPolicy = "YEAR_MONTH",
            paddingLength = 5,
            version = lockedSeq.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.Conflict, seqLockedUpdate.StatusCode);
        Assert.Equal("CODE_LOCKED", (await seqLockedUpdate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var seqNameUpdate = await compClient.PutAsJsonAsync("/api/su/surt06/update", new
        {
            id = seqId,
            code = "INV",
            name = "ใบแจ้งหนี้ขาย",
            prefix = "INV",
            resetPolicy = "YEAR_MONTH",
            paddingLength = 6,
            version = lockedSeq.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, seqNameUpdate.StatusCode);
        var seqUpdated = await seqNameUpdate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ใบแจ้งหนี้ขาย", seqUpdated.GetProperty("name").GetString());
        Assert.Equal($"INV-{ym}-000003", seqUpdated.GetProperty("previewNextNumber").GetString());

        // 3) SURT07: Approval Workflow cross-company role rejection, same-role actor rule & role lock
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PostAsJsonAsync("/api/su/surt07/create", new
        {
            companyId = companyId.ToString(),
            code = "CROSS_WF",
            name = "อนุมัติข้ามบริษัท",
            documentType = "PR",
            steps = new[] { new { stepOrder = 1, stepName = "ขั้น 1", approverRoleId = otherApprRoleId, requireDifferentActor = true } }
        })).StatusCode);

        var sameActorBadResp = await compClient.PostAsJsonAsync("/api/su/surt07/create", new
        {
            companyId = companyId.ToString(),
            code = "BAD_WF",
            name = "อนุมัติซ้ำคนเดียว",
            documentType = "PR",
            steps = new[]
            {
                new { stepOrder = 1, stepName = "ขั้น 1", approverRoleId = apprFinRoleId, requireDifferentActor = true },
                new { stepOrder = 2, stepName = "ขั้น 2", approverRoleId = apprFinRoleId, requireDifferentActor = false }
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, sameActorBadResp.StatusCode);
        Assert.Equal("INVALID_WORKFLOW_STEPS", (await sameActorBadResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        var wfCreateResp = await compClient.PostAsJsonAsync("/api/su/surt07/create", new
        {
            companyId = companyId.ToString(),
            code = " pr_flow ",
            name = "อนุมัติใบขอซื้อ",
            documentType = " pr ",
            steps = new[]
            {
                new { stepOrder = 1, stepName = "ตรวจสอบคำขอ", approverRoleId = apprFinRoleId, requireDifferentActor = true },
                new { stepOrder = 2, stepName = "อนุมัติขั้นสุดท้าย", approverRoleId = apprFinRoleId, requireDifferentActor = true }
            }
        });
        Assert.Equal(HttpStatusCode.OK, wfCreateResp.StatusCode);
        var wf = await wfCreateResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("PR_FLOW", wf.GetProperty("code").GetString());
        Assert.Equal(2, wf.GetProperty("steps").GetArrayLength());

        var lockedApprRole = await (await compClient.PostAsJsonAsync("/api/su/surt02/detail", new { id = apprFinRoleId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(lockedApprRole.GetProperty("codeLocked").GetBoolean());
        var roleDeactivateResp = await compClient.PostAsJsonAsync("/api/su/surt02/deactivate", new
        {
            id = apprFinRoleId,
            version = lockedApprRole.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.Conflict, roleDeactivateResp.StatusCode);
        Assert.Equal("ACTIVE_ASSIGNMENTS", (await roleDeactivateResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        // 4) SURT08: System Settings validation & sensitive value masking in DTO and Audit
        var badIntResp = await compClient.PostAsJsonAsync("/api/su/surt08/create", new
        {
            companyId = companyId.ToString(),
            key = "MAX_RETRY",
            displayName = "จำนวนครั้งสูงสุด",
            dataType = "INTEGER",
            value = "not-an-integer",
            isSensitive = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, badIntResp.StatusCode);
        Assert.Equal("INVALID_SETTING_VALUE", (await badIntResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());

        const string secretVal = "super-secret-value-99";
        var secretCreateResp = await compClient.PostAsJsonAsync("/api/su/surt08/create", new
        {
            companyId = companyId.ToString(),
            key = " webhook_secret ",
            displayName = "คีย์เว็บฮุก",
            dataType = "STRING",
            value = secretVal,
            isSensitive = true
        });
        Assert.Equal(HttpStatusCode.OK, secretCreateResp.StatusCode);
        var secretSetting = await secretCreateResp.Content.ReadFromJsonAsync<JsonElement>();
        var secretId = secretSetting.GetProperty("id").GetString()!;
        Assert.Equal("WEBHOOK_SECRET", secretSetting.GetProperty("key").GetString());
        Assert.Equal("", secretSetting.GetProperty("value").GetString());
        Assert.True(secretSetting.GetProperty("isSensitive").GetBoolean());
        Assert.True(secretSetting.GetProperty("hasValue").GetBoolean());

        var secretDetail = await (await compClient.PostAsJsonAsync("/api/su/surt08/detail", new { id = secretId }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("", secretDetail.GetProperty("value").GetString());
        Assert.True(secretDetail.GetProperty("hasValue").GetBoolean());

        var secretUpdate = await compClient.PutAsJsonAsync("/api/su/surt08/update", new
        {
            id = secretId,
            displayName = "คีย์เว็บฮุกใหม่",
            value = "",
            version = secretDetail.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, secretUpdate.StatusCode);
        Assert.True((await secretUpdate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("hasValue").GetBoolean());

        using (var scope = app.Services.CreateScope())
        {
            var settingAudits = await scope.ServiceProvider.GetRequiredService<SystemDbContext>().AuditEvents
                .Where(x => x.ProgramCode == "SURT08").ToListAsync();
            Assert.NotEmpty(settingAudits);
            Assert.All(settingAudits, a =>
            {
                Assert.DoesNotContain(secretVal, a.Before ?? "");
                Assert.DoesNotContain(secretVal, a.After ?? "");
                Assert.Contains("***MASKED***", a.After ?? "");
            });
        }

        // 5) SURT09: Audit View/Export authorization separation, global log isolation, branch scope & CSV formula neutralization
        var br1Resp = await compClient.PostAsJsonAsync("/api/db/dbrt02/create", new { companyId = companyId.ToString(), code = "BR_OPS_1", name = "สาขาปฏิบัติการ 1" });
        Assert.Equal(HttpStatusCode.OK, br1Resp.StatusCode);
        var br1Id = (await br1Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        var br2Resp = await compClient.PostAsJsonAsync("/api/db/dbrt02/create", new { companyId = companyId.ToString(), code = "BR_OPS_2", name = "สาขาปฏิบัติการ 2" });
        Assert.Equal(HttpStatusCode.OK, br2Resp.StatusCode);
        var br2Id = (await br2Resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;

        string surt05AuditId, br1AuditId, br2AuditId;
        using (var scope = app.Services.CreateScope())
        {
            var su = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            su.CompanyAssignments.Add(new CompanyAssignment
            {
                UserId = branchAuditorUserId,
                CompanyId = companyId,
                RoleId = branchAuditorRoleId,
                IsActive = true,
                ScopeType = "Branch",
                ScopeId = long.Parse(br1Id, System.Globalization.CultureInfo.InvariantCulture)
            });
            su.AuditEvents.Add(new AuditEvent
            {
                OccurredAtUtc = DateTimeOffset.UtcNow,
                CompanyId = companyId,
                EntityId = companyId,
                ProgramCode = "DBRT01",
                Action = "UPDATE",
                Result = "SUCCESS",
                TraceId = "=1+1"
            });
            await su.SaveChangesAsync();

            var surt05Event = await su.AuditEvents.AsNoTracking().FirstAsync(x => x.ProgramCode == "SURT05");
            var br1Long = long.Parse(br1Id, System.Globalization.CultureInfo.InvariantCulture);
            var br2Long = long.Parse(br2Id, System.Globalization.CultureInfo.InvariantCulture);
            var br1Event = await su.AuditEvents.AsNoTracking().FirstAsync(x => x.ProgramCode == "DBRT02" && x.EntityId == br1Long);
            var br2Event = await su.AuditEvents.AsNoTracking().FirstAsync(x => x.ProgramCode == "DBRT02" && x.EntityId == br2Long);
            surt05AuditId = surt05Event.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            br1AuditId = br1Event.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            br2AuditId = br2Event.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        using var branchClient = Client();
        await Csrf(branchClient);
        Assert.Equal(HttpStatusCode.OK, (await branchClient.PostAsJsonAsync("/api/auth/login", new { identifier = "branchauditor", password = Password })).StatusCode);
        await Csrf(branchClient);

        // View-only role can search but gets 403 on export
        Assert.Equal(HttpStatusCode.OK, (await viewClient.PostAsJsonAsync("/api/su/surt09/search", new { companyId = companyId.ToString() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewClient.PostAsJsonAsync("/api/su/surt09/export", new { companyId = companyId.ToString() })).StatusCode);

        // Company Auditor cannot query global logs or inspect SURT05 audit detail
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PostAsJsonAsync("/api/su/surt09/search", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await compClient.PostAsJsonAsync("/api/su/surt09/detail", new { id = surt05AuditId })).StatusCode);

        var compAuditSearch = await (await compClient.PostAsJsonAsync("/api/su/surt09/search", new { companyId = companyId.ToString(), pageSize = 100 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var compAuditPrograms = compAuditSearch.GetProperty("items").EnumerateArray()
            .Select(x => x.GetProperty("programCode").GetString()!).ToArray();
        Assert.DoesNotContain("SURT05", compAuditPrograms);
        Assert.DoesNotContain("SURT01", compAuditPrograms);
        Assert.DoesNotContain("AUTH", compAuditPrograms);

        var compExportResp = await compClient.PostAsJsonAsync("/api/su/surt09/export", new { companyId = companyId.ToString() });
        Assert.Equal(HttpStatusCode.OK, compExportResp.StatusCode);
        var compExport = await compExportResp.Content.ReadFromJsonAsync<JsonElement>();
        var csv = compExport.GetProperty("csvContent").GetString()!;
        Assert.Contains("\"'=1+1\"", csv);
        Assert.DoesNotContain(",\"=1+1\"", csv);

        // Branch Auditor sees BR_OPS_1 audit event only and is blocked from BR_OPS_2 audit detail
        var branchAuditSearch = await (await branchClient.PostAsJsonAsync("/api/su/surt09/search", new { companyId = companyId.ToString(), programCode = "DBRT02", pageSize = 50 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        var branchAuditIds = branchAuditSearch.GetProperty("items").EnumerateArray()
            .Select(x => x.GetProperty("id").GetString()!).ToArray();
        Assert.Contains(br1AuditId, branchAuditIds);
        Assert.DoesNotContain(br2AuditId, branchAuditIds);
        Assert.Equal(HttpStatusCode.OK, (await branchClient.PostAsJsonAsync("/api/su/surt09/detail", new { id = br1AuditId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await branchClient.PostAsJsonAsync("/api/su/surt09/detail", new { id = br2AuditId })).StatusCode);

        // Global Auditor views & exports all including SURT05
        var globalSearch = await (await sysClient.PostAsJsonAsync("/api/su/surt09/search", new { programCode = "SURT05", pageSize = 50 }))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(globalSearch.GetProperty("totalCount").GetInt32() >= 2);
        Assert.Equal(HttpStatusCode.OK, (await sysClient.PostAsJsonAsync("/api/su/surt09/detail", new { id = surt05AuditId })).StatusCode);
        var globalExportResp = await sysClient.PostAsJsonAsync("/api/su/surt09/export", new { programCode = "SURT05" });
        Assert.Equal(HttpStatusCode.OK, globalExportResp.StatusCode);
        Assert.Contains("SURT05", (await globalExportResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("csvContent").GetString()!);
    }

    [Fact]
    public async Task HealthAndReadinessEndpointsReportStatusAndSecurityHeadersWithoutLeakingDetails()
    {
        using var client = Client();

        var livenessResp = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, livenessResp.StatusCode);
        AssertHasObservabilityAndSecurityHeaders(livenessResp);
        var liveness = await livenessResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", liveness.GetProperty("status").GetString());

        var readinessResp = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, readinessResp.StatusCode);
        AssertHasObservabilityAndSecurityHeaders(readinessResp);
        var readiness = await readinessResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ready", readiness.GetProperty("status").GetString());
        Assert.Equal("ready", readiness.GetProperty("checks").GetProperty("database").GetString());

        const string sentinel = "ReadinessSecret_84721!";
        await using var unavailableApp = new TestApplication(
            $"Host=127.0.0.1;Port=1;Database=missing;Username=nobody;Password={sentinel};Timeout=1;Command Timeout=1");
        using var unavailableClient = unavailableApp.CreateClient(
            new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

        var failedResp = await unavailableClient.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failedResp.StatusCode);
        AssertHasObservabilityAndSecurityHeaders(failedResp);
        var failedBody = await failedResp.Content.ReadAsStringAsync();
        Assert.DoesNotContain(sentinel, failedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1", failedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("nobody", failedBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", failedBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", failedBody, StringComparison.OrdinalIgnoreCase);

        var failedJson = JsonDocument.Parse(failedBody).RootElement;
        Assert.Equal("unavailable", failedJson.GetProperty("status").GetString());
        Assert.Equal("unavailable", failedJson.GetProperty("checks").GetProperty("database").GetString());
    }

    [Fact]
    public async Task HealthEndpointsBypassMustChangePasswordGate()
    {
        using (var scope = app.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<SystemDbContext>().Users.Where(x => x.Id == userId)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.MustChangePassword, true)
                    .SetProperty(x => x.TemporaryPasswordExpiresAtUtc, DateTimeOffset.UtcNow.AddHours(24)));

        using var client = Client();
        await Login(client);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/api/db/dbrt01/detail", new { id = companyId.ToString() })).StatusCode);
    }

    [Fact]
    public async Task MigrationSqlDryRunIsCleanAndIdempotent()
    {
        var dryRunDatabase = "clean_dryrun_" + Guid.NewGuid().ToString("N");
        await using (var admin = new NpgsqlConnection(adminConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand("CREATE DATABASE \"" + dryRunDatabase + "\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            string orgSql;
            string suSql;
            using (var scope = app.Services.CreateScope())
            {
                orgSql = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>()
                    .GetService<IMigrator>()
                    .GenerateScript(null, null, MigrationsSqlGenerationOptions.Idempotent);
                suSql = scope.ServiceProvider.GetRequiredService<SystemDbContext>()
                    .GetService<IMigrator>()
                    .GenerateScript(null, null, MigrationsSqlGenerationOptions.Idempotent);
            }

            Assert.False(string.IsNullOrWhiteSpace(orgSql));
            Assert.False(string.IsNullOrWhiteSpace(suSql));

            var dryRunConnection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = dryRunDatabase }.ConnectionString;
            await using var conn = new NpgsqlConnection(dryRunConnection);
            await conn.OpenAsync();

            for (var pass = 0; pass < 2; pass++)
            {
                await using (var orgCmd = new NpgsqlCommand(orgSql, conn))
                    await orgCmd.ExecuteNonQueryAsync();
                await using (var suCmd = new NpgsqlCommand(suSql, conn))
                    await suCmd.ExecuteNonQueryAsync();
            }

            await using var verifyCmd = new NpgsqlCommand("""
                SELECT
                    (SELECT count(*) FROM "__DbMigrationsHistory"),
                    (SELECT count(*) FROM "__SuMigrationsHistory"),
                    (SELECT count(*) FROM "DbCompany"),
                    (SELECT count(*) FROM "SuUser"),
                    (SELECT count(*) FROM "SuAuditEvent"),
                    (SELECT count(*) FROM "SuDocumentSequence"),
                    (SELECT count(*) FROM "SuApprovalWorkflow")
                """, conn);
            await using var reader = await verifyCmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.True(reader.GetInt64(0) >= 2);
            Assert.True(reader.GetInt64(1) >= 5);
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand("DROP DATABASE IF EXISTS \"" + dryRunDatabase + "\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static void AssertHasObservabilityAndSecurityHeaders(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("X-Trace-Id", out var traceValues));
        Assert.False(string.IsNullOrWhiteSpace(traceValues.Single()));
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal("camera=(), microphone=(), geolocation=()", response.Headers.GetValues("Permissions-Policy").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single(), StringComparison.Ordinal);
    }

    [BrowserFact]
    public async Task BrowserRunsCompanyFlowAgainstRealApi()
    {
        using(var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SystemDbContext>();
            var role = new ApplicationRole { Name = "BrowserRegistrar", NormalizedName = "BROWSERREGISTRAR", IsActive = true };
            db.Roles.Add(role); await db.SaveChangesAsync();
            db.UserRoles.Add(new IdentityUserRole<long> { RoleId = role.Id, UserId = userId });
            db.RolePermissions.AddRange(clean.Domain.Security.ProgramRegistry.AllPermissions
                .Select(permission => new RolePermission { RoleId = role.Id, Permission = permission }));
            await db.SaveChangesAsync();
        }
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
        using var serverCertificate = X509CertificateLoader.LoadPkcs12(
            certificate.Export(X509ContentType.Pkcs12), null, X509KeyStorageFlags.UserKeySet);
        var spaPort = GetFreeLoopbackPort();
        var spaOrigin = $"https://localhost:{spaPort}";
        await using var browserApp = new TestApplication(testConnection, spaOrigin);
        browserApp.UseKestrel(options => options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(serverCertificate)));
        using var starter = browserApp.CreateClient();
        var address = browserApp.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var processInfo = new ProcessStartInfo("node")
        {
            WorkingDirectory = Path.Combine(root, "src/Web/spa"),
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            CreateNoWindow = true
        };
        processInfo.ArgumentList.Add("e2e/browser-smoke.mjs");
        processInfo.Environment["CLEAN_BROWSER_API"] = address;
        processInfo.Environment["CLEAN_BROWSER_CERT"] = Convert.ToBase64String(certificate.Export(X509ContentType.Pkcs12));
        processInfo.Environment["CLEAN_BROWSER_PASSWORD"] = Password;
        processInfo.Environment["CLEAN_BROWSER_SPA_PORT"] = spaPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
        using var process = Process.Start(processInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch { process.Kill(true); throw; }
        Assert.True(process.ExitCode == 0, await output + "\n" + await errors + "\n" + browserApp.Errors);
    }

    private static int GetFreeLoopbackPort()
    {
        try
        {
            using var listener = new System.Net.Sockets.TcpListener(IPAddress.IPv6Loopback, 0);
            listener.Server.DualMode = true;
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        catch (System.Net.Sockets.SocketException)
        {
            using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }

    private sealed class TestApplication(string connection, string spaOrigin = "https://localhost:4200") : WebApplicationFactory<Program>
    {
        private readonly CaptureLogger logger = new();
        public string Errors => logger.Errors.ToString();
        public bool FailAudit { get; set; }
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logger));
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Database", connection);
            builder.UseSetting("Cors:AllowedOrigins:0", spaOrigin);
            builder.ConfigureServices(services => {
                services.RemoveAll<IPasswordScreening>();
                services.AddScoped<IPasswordScreening, AcceptTestPasswords>();
                services.RemoveAll<IAuditWriter>();
                services.AddScoped<IAuditWriter>(provider => new TestAuditWriter(
                    ActivatorUtilities.CreateInstance<AuditWriter>(provider), () => FailAudit));
            });
        }
    }
    private sealed class TestAuditWriter(AuditWriter writer, Func<bool> fail) : IAuditWriter
    {
        public async Task CompanyChangedAsync(string action, CompanySnapshot? before, Company company, CancellationToken cancellationToken)
        {
            await writer.CompanyChangedAsync(action, before, company, cancellationToken);
            if (fail()) throw new InvalidOperationException("Test-only audit failure");
        }

        public async Task RecordOrganizationAsync(
            string programCode,
            string action,
            long? companyId,
            long entityId,
            object? before,
            object after,
            CancellationToken cancellationToken)
        {
            await writer.RecordOrganizationAsync(programCode, action, companyId, entityId, before, after, cancellationToken);
            if (fail()) throw new InvalidOperationException("Test-only audit failure");
        }
    }
    private sealed class CaptureLogger : ILoggerProvider, ILogger
    {
        public System.Text.StringBuilder Errors { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel)) Errors.AppendLine(formatter(state, exception)).AppendLine(exception?.ToString());
        }
    }
    private sealed class AcceptTestPasswords : IPasswordScreening
    {
        public Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
