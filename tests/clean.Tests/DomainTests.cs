using clean.Application.Common.Models;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;
using clean.Domain.Security;

namespace clean.Tests;

public sealed class DomainTests
{
    private static Company Create() => Company.Create(" acme ", " บริษัทตัวอย่าง ", 1, DateTimeOffset.UtcNow);

    [Fact]
    public void NormalizesBusinessCodeAndKeepsBigintPrecision()
    {
        var company = Create();
        Assert.Equal("ACME", company.Code);
        Assert.Equal("บริษัทตัวอย่าง", company.Name);
        Assert.True(IdValue.IsValid("9223372036854775807"));
        Assert.False(IdValue.IsValid("9223372036854775808"));
        Assert.False(IdValue.IsValid("1e3"));
        Assert.False(IdValue.IsValid("-1"));
    }

    [Fact]
    public void StaleEditorCannotOverwriteNewerValues()
    {
        var company = Create();
        company.Update("ACME", "ใหม่", 1, 1, DateTimeOffset.UtcNow);
        var error = Assert.Throws<RuleException>(() => company.Update("ACME", "เก่า", 1, 2, DateTimeOffset.UtcNow));
        Assert.Equal("CONCURRENCY_CONFLICT", error.Code);
        Assert.Equal("ใหม่", company.Name);
    }

    [Fact]
    public void ReferencedCodeCannotChangeButNameCan()
    {
        var company = Create();
        company.LockCode();
        Assert.Throws<RuleException>(() => company.Update("OTHER", "ชื่อ", company.Version, 1, DateTimeOffset.UtcNow));
        company.Update("ACME", "ชื่อใหม่", company.Version, 1, DateTimeOffset.UtcNow);
        Assert.Equal("ชื่อใหม่", company.Name);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CannotDeactivateWithActiveReferences(bool children, bool assignments)
    {
        var company = Create();
        Assert.Throws<RuleException>(() => company.ChangeStatus(false, 1, children, assignments, 1, DateTimeOffset.UtcNow));
        Assert.True(company.IsActive);
    }

    [Fact]
    public void BranchDivisionAndDepartmentEnforceHierarchyCodeAndReparentLocks()
    {
        var now = DateTimeOffset.UtcNow;
        var branch = Branch.Create(1, " bkk ", " สาขากรุงเทพ ", 1, now);
        Assert.Equal("BKK", branch.Code);
        Assert.Equal("สาขากรุงเทพ", branch.Name);

        branch.LockCode();
        Assert.Equal("CODE_LOCKED", Assert.Throws<RuleException>(() =>
            branch.Update("CNX", "สาขากรุงเทพ", branch.Version, 1, now)).Code);
        Assert.Equal("ACTIVE_CHILDREN", Assert.Throws<RuleException>(() =>
            branch.ChangeStatus(false, branch.Version, true, true, false, 1, now)).Code);
        Assert.Equal("ACTIVE_ASSIGNMENTS", Assert.Throws<RuleException>(() =>
            branch.ChangeStatus(false, branch.Version, true, false, true, 1, now)).Code);

        var division = Division.Create(1, 10, " fin ", " ฝ่ายการเงิน ", 1, now);
        division.Update(20, "FIN2", "ฝ่ายบัญชี", division.Version, 1, now);
        Assert.Equal(20, division.BranchId);
        Assert.Equal("FIN2", division.Code);
        division.LockCode();
        Assert.Equal("REPARENT_LOCKED", Assert.Throws<RuleException>(() =>
            division.Update(30, "FIN2", "ฝ่ายบัญชี", division.Version, 1, now)).Code);
        Assert.Equal("CODE_LOCKED", Assert.Throws<RuleException>(() =>
            division.Update(20, "OTHER", "ฝ่ายบัญชี", division.Version, 1, now)).Code);

        var department = Department.Create(1, 100, " ap ", " แผนกเจ้าหนี้ ", 1, now);
        department.LockCode();
        Assert.Equal("REPARENT_LOCKED", Assert.Throws<RuleException>(() =>
            department.Update(200, "AP", "แผนกเจ้าหนี้", department.Version, 1, now)).Code);
        department.ChangeStatus(false, department.Version, true, false, 1, now);
        Assert.Equal("DEPARTMENT_INACTIVE", Assert.Throws<RuleException>(() =>
            department.Update(100, "AP", "แก้", department.Version, 1, now)).Code);
        Assert.Equal("PARENT_INACTIVE", Assert.Throws<RuleException>(() =>
            department.ChangeStatus(true, department.Version, false, false, 1, now)).Code);
    }

    [Fact]
    public void CostCenterPositionAndBusinessUnitEnforceMasterRules()
    {
        var now = DateTimeOffset.UtcNow;
        var costCenter = CostCenter.Create(1, " cc01 ", " ศูนย์ต้นทุน ", 1, now);
        Assert.Equal("CC01", costCenter.Code);
        costCenter.ChangeStatus(false, costCenter.Version, true, 1, now);
        Assert.Equal("PARENT_INACTIVE", Assert.Throws<RuleException>(() =>
            costCenter.ChangeStatus(true, costCenter.Version, false, 1, now)).Code);

        var position = Position.Create(" dev ", " นักพัฒนา ", 1, now);
        Assert.Equal("DEV", position.Code);
        position.Update("dev2", "นักพัฒนาอาวุโส", position.Version, 1, now);
        Assert.Equal("DEV2", position.Code);
        Assert.Equal("CONCURRENCY_CONFLICT", Assert.Throws<RuleException>(() =>
            position.Update("DEV2", "เก่า", 1, 1, now)).Code);

        var unit = BusinessUnit.Create(" bu-1 ", " หน่วยธุรกิจหลัก ", 1, now);
        Assert.Equal("BU-1", unit.Code);
        unit.LockCode();
        Assert.Equal("CODE_LOCKED", Assert.Throws<RuleException>(() =>
            unit.Update("BU-2", "หน่วยธุรกิจหลัก", unit.Version, 1, now)).Code);
        unit.ChangeStatus(false, unit.Version, 1, now);
        Assert.Equal("BUSINESS_UNIT_INACTIVE", Assert.Throws<RuleException>(() =>
            unit.Update("BU-1", "แก้", unit.Version, 1, now)).Code);
    }

    [Fact]
    public void ProgramRegistrySeparatesUserManagementFromCompanyAccessAdmin()
    {
        var implementedSu = ProgramRegistry.Programs
            .Where(x => x.Implemented && x.Code.StartsWith("SURT", StringComparison.Ordinal))
            .Select(x => x.Code)
            .ToArray();
        Assert.Equal(["SURT01", "SURT02", "SURT03", "SURT04", "SURT05", "SURT06", "SURT07", "SURT08", "SURT09"], implementedSu);

        foreach (var permission in ProgramRegistry.UserManagementPermissions.Concat(ProgramRegistry.MenuConfigPermissions))
        {
            Assert.Contains(permission, ProgramRegistry.GlobalOnlyPermissions);
            Assert.DoesNotContain(permission, ProgramRegistry.CompanyGrantablePermissions);
            Assert.False(ProgramRegistry.AllowsScope("Company", permission));
        }

        foreach (var permission in ProgramRegistry.CompanyAccessAdminPermissions
            .Concat(ProgramRegistry.DocumentSequencePermissions)
            .Concat(ProgramRegistry.ApprovalWorkflowPermissions)
            .Concat(ProgramRegistry.SystemSettingPermissions))
        {
            Assert.Contains(permission, ProgramRegistry.CompanyGrantablePermissions);
            Assert.True(ProgramRegistry.AllowsScope("Company", permission));
            Assert.False(ProgramRegistry.AllowsScope("Branch", permission));
            Assert.False(ProgramRegistry.AllowsScope("Department", permission));
        }

        foreach (var permission in ProgramRegistry.AuditLogPermissions)
        {
            Assert.Contains(permission, ProgramRegistry.CompanyGrantablePermissions);
            Assert.True(ProgramRegistry.AllowsScope("Company", permission));
            Assert.True(ProgramRegistry.AllowsScope("Branch", permission));
            Assert.True(ProgramRegistry.AllowsScope("Department", permission));
        }
    }
}
