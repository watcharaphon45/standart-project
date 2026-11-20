using System.Globalization;
using clean.Domain.Entities.Organization;
using clean.Domain.Exceptions;

namespace clean.Application.Common.Models;

public sealed record CompanySnapshot(string Code, string Name, bool IsActive)
{
    public static CompanySnapshot Of(Company company) => new(company.Code, company.Name, company.IsActive);
}

public sealed record CompanyDto(string Id, string Code, string Name, bool IsActive, bool CodeLocked, string Version)
{
    public static CompanyDto Of(Company company) => new(
        company.Id.ToString(CultureInfo.InvariantCulture),
        company.Code,
        company.Name,
        company.IsActive,
        company.CodeLocked,
        company.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record BranchSnapshot(string Code, string Name, string CompanyId, bool IsActive)
{
    public static BranchSnapshot Of(Branch branch) => new(
        branch.Code,
        branch.Name,
        branch.CompanyId.ToString(CultureInfo.InvariantCulture),
        branch.IsActive);
}

public sealed record BranchDto(
    string Id,
    string CompanyId,
    string Code,
    string Name,
    bool IsActive,
    bool CodeLocked,
    string Version)
{
    public static BranchDto Of(Branch branch) => new(
        branch.Id.ToString(CultureInfo.InvariantCulture),
        branch.CompanyId.ToString(CultureInfo.InvariantCulture),
        branch.Code,
        branch.Name,
        branch.IsActive,
        branch.CodeLocked,
        branch.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record DivisionSnapshot(string Code, string Name, string BranchId, bool IsActive)
{
    public static DivisionSnapshot Of(Division division) => new(
        division.Code,
        division.Name,
        division.BranchId.ToString(CultureInfo.InvariantCulture),
        division.IsActive);
}

public sealed record DivisionDto(
    string Id,
    string CompanyId,
    string BranchId,
    string BranchCode,
    string BranchName,
    string Code,
    string Name,
    bool IsActive,
    bool CodeLocked,
    string Version)
{
    public static DivisionDto Of(Division division, string branchCode, string branchName) => new(
        division.Id.ToString(CultureInfo.InvariantCulture),
        division.CompanyId.ToString(CultureInfo.InvariantCulture),
        division.BranchId.ToString(CultureInfo.InvariantCulture),
        branchCode,
        branchName,
        division.Code,
        division.Name,
        division.IsActive,
        division.CodeLocked,
        division.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record DepartmentSnapshot(string Code, string Name, string DivisionId, bool IsActive)
{
    public static DepartmentSnapshot Of(Department department) => new(
        department.Code,
        department.Name,
        department.DivisionId.ToString(CultureInfo.InvariantCulture),
        department.IsActive);
}

public sealed record DepartmentDto(
    string Id,
    string CompanyId,
    string BranchId,
    string BranchCode,
    string DivisionId,
    string DivisionCode,
    string DivisionName,
    string Code,
    string Name,
    bool IsActive,
    bool CodeLocked,
    string Version)
{
    public static DepartmentDto Of(
        Department department,
        long branchId,
        string branchCode,
        string divisionCode,
        string divisionName) => new(
            department.Id.ToString(CultureInfo.InvariantCulture),
            department.CompanyId.ToString(CultureInfo.InvariantCulture),
            branchId.ToString(CultureInfo.InvariantCulture),
            branchCode,
            department.DivisionId.ToString(CultureInfo.InvariantCulture),
            divisionCode,
            divisionName,
            department.Code,
            department.Name,
            department.IsActive,
            department.CodeLocked,
            department.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record CostCenterSnapshot(string Code, string Name, string CompanyId, bool IsActive)
{
    public static CostCenterSnapshot Of(CostCenter costCenter) => new(
        costCenter.Code,
        costCenter.Name,
        costCenter.CompanyId.ToString(CultureInfo.InvariantCulture),
        costCenter.IsActive);
}

public sealed record CostCenterDto(
    string Id,
    string CompanyId,
    string Code,
    string Name,
    bool IsActive,
    bool CodeLocked,
    string Version)
{
    public static CostCenterDto Of(CostCenter costCenter) => new(
        costCenter.Id.ToString(CultureInfo.InvariantCulture),
        costCenter.CompanyId.ToString(CultureInfo.InvariantCulture),
        costCenter.Code,
        costCenter.Name,
        costCenter.IsActive,
        costCenter.CodeLocked,
        costCenter.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record PositionSnapshot(string Code, string Name, bool IsActive)
{
    public static PositionSnapshot Of(Position position) => new(position.Code, position.Name, position.IsActive);
}

public sealed record PositionDto(string Id, string Code, string Name, bool IsActive, bool CodeLocked, string Version)
{
    public static PositionDto Of(Position position) => new(
        position.Id.ToString(CultureInfo.InvariantCulture),
        position.Code,
        position.Name,
        position.IsActive,
        position.CodeLocked,
        position.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record BusinessUnitSnapshot(string Code, string Name, bool IsActive)
{
    public static BusinessUnitSnapshot Of(BusinessUnit unit) => new(unit.Code, unit.Name, unit.IsActive);
}

public sealed record BusinessUnitDto(string Id, string Code, string Name, bool IsActive, bool CodeLocked, string Version)
{
    public static BusinessUnitDto Of(BusinessUnit unit) => new(
        unit.Id.ToString(CultureInfo.InvariantCulture),
        unit.Code,
        unit.Name,
        unit.IsActive,
        unit.CodeLocked,
        unit.Version.ToString(CultureInfo.InvariantCulture));
}

public sealed record LookupOptionDto(string Id, string Code, string Name, bool IsActive);
public sealed record DivisionLookupOptionDto(string Id, string BranchId, string Code, string Name, bool IsActive);
public sealed record DepartmentOptionsDto(
    IReadOnlyList<LookupOptionDto> Branches,
    IReadOnlyList<DivisionLookupOptionDto> Divisions);

public sealed record ScopeFilter(
    bool FullCompany,
    IReadOnlySet<long> BranchIds,
    IReadOnlySet<long> DepartmentIds);

public sealed record Page<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);
public sealed record CompanyOption(string Id, string Code, string Name, bool IsActive);
public sealed record LoginRequest(string Identifier, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record LoginResult(string SessionId, string UserId, bool MustChangePassword);
public sealed record SessionUser(long UserId, string UserName, bool MustChangePassword);
public sealed record CompanyAccessDto(string CompanyId, string[] Permissions);
public sealed record NavigationDto(string Code, string Name, string Route);
public sealed record CurrentUserDto(
    string Id,
    string UserName,
    bool MustChangePassword,
    IReadOnlyList<CompanyOption> Companies,
    IReadOnlyList<CompanyAccessDto> CompanyAccess,
    string[] GlobalPermissions,
    IReadOnlyList<NavigationDto> Navigation);

public sealed record UserSnapshot(string UserName, string Email, bool IsActive, bool MustChangePassword, bool IsLockedOut);

public sealed record UserDto(
    string Id,
    string UserName,
    string Email,
    bool IsActive,
    bool MustChangePassword,
    DateTimeOffset? TemporaryPasswordExpiresAtUtc,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    int AccessFailedCount,
    string Version);

public sealed record RoleSnapshot(string Code, string Name, string? CompanyId, bool IsActive);

public sealed record RoleDto(
    string Id,
    string? CompanyId,
    string Code,
    string Name,
    bool IsActive,
    bool CodeLocked,
    int PermissionCount,
    int AssignedUserCount,
    string Version);

public sealed record RolePermissionSnapshot(string RoleId, string RoleCode, string? CompanyId, string[] Permissions);

public sealed record PermissionActionItemDto(string Permission, string Action, string Label, bool GlobalOnly);

public sealed record ProgramPermissionGroupDto(
    string Code,
    string Name,
    IReadOnlyList<PermissionActionItemDto> Items);

public sealed record PermissionCatalogDto(
    IReadOnlyList<RoleDto> Roles,
    IReadOnlyList<ProgramPermissionGroupDto> Catalog,
    string[] GrantablePermissions);

public sealed record RolePermissionsDto(
    string RoleId,
    string? CompanyId,
    string RoleCode,
    string RoleName,
    bool IsActive,
    string Version,
    string[] Permissions,
    string[] GrantablePermissions);

public sealed record AssignmentSnapshot(
    string UserId,
    string CompanyId,
    string RoleId,
    string ScopeType,
    string? ScopeId,
    bool IsActive);

public sealed record DepartmentScopeOptionDto(
    string Id,
    string BranchId,
    string BranchCode,
    string BranchName,
    string DivisionId,
    string DivisionCode,
    string DivisionName,
    string Code,
    string Name,
    bool IsActive);

public sealed record UserScopeListItemDto(
    string UserId,
    string UserName,
    string Email,
    bool UserIsActive,
    bool HasAssignment,
    string? RoleId,
    string? RoleCode,
    string? RoleName,
    string? ScopeType,
    string? ScopeId,
    string? ScopeCode,
    string? ScopeName,
    bool IsActive,
    string Version);

public sealed record UserScopeOptionsDto(
    IReadOnlyList<LookupOptionDto> Roles,
    IReadOnlyList<LookupOptionDto> Branches,
    IReadOnlyList<DepartmentScopeOptionDto> Departments);

public sealed record UserScopeDetailDto(
    string UserId,
    string UserName,
    string Email,
    bool UserIsActive,
    string CompanyId,
    string? RoleId,
    string ScopeType,
    string? ScopeId,
    bool IsActive,
    string Version,
    IReadOnlyList<DepartmentScopeOptionDto> AccessibleDepartments);

public sealed record MenuItemSnapshot(string ProgramCode, string DisplayName, int SortOrder, bool IsActive);

public sealed record MenuItemDto(
    string Id,
    string ModuleCode,
    string ProgramCode,
    string DefaultName,
    string DisplayName,
    string Route,
    string RequiredPermission,
    int SortOrder,
    bool IsActive,
    string Version);

public sealed record DocumentSequenceSnapshot(
    string Code,
    string Name,
    string CompanyId,
    string Prefix,
    string ResetPolicy,
    int PaddingLength,
    string CurrentPeriod,
    long LastNumber,
    bool IsActive);

public sealed record DocumentSequenceDto(
    string Id,
    string CompanyId,
    string Code,
    string Name,
    string Prefix,
    string ResetPolicy,
    int PaddingLength,
    string CurrentPeriod,
    long LastNumber,
    string PreviewNextNumber,
    bool IsActive,
    bool CodeLocked,
    string Version);

public sealed record DocumentNumberAllocationDto(
    string Id,
    string DocumentNumber,
    string PeriodKey,
    long SequenceNumber,
    string Version);

public sealed record ApprovalWorkflowStepSnapshot(
    int StepOrder,
    string StepName,
    string ApproverRoleId,
    bool RequireDifferentActor);

public sealed record ApprovalWorkflowSnapshot(
    string Code,
    string Name,
    string CompanyId,
    string DocumentType,
    bool IsActive,
    IReadOnlyList<ApprovalWorkflowStepSnapshot> Steps);

public sealed record ApprovalWorkflowStepDto(
    string Id,
    int StepOrder,
    string StepName,
    string ApproverRoleId,
    string ApproverRoleCode,
    string ApproverRoleName,
    bool RequireDifferentActor);

public sealed record ApprovalWorkflowDto(
    string Id,
    string CompanyId,
    string Code,
    string Name,
    string DocumentType,
    int StepCount,
    bool IsActive,
    bool CodeLocked,
    string Version);

public sealed record ApprovalWorkflowDetailDto(
    string Id,
    string CompanyId,
    string Code,
    string Name,
    string DocumentType,
    bool IsActive,
    bool CodeLocked,
    string Version,
    IReadOnlyList<ApprovalWorkflowStepDto> Steps);

public sealed record ApprovalWorkflowOptionsDto(IReadOnlyList<LookupOptionDto> Roles);

public sealed record SystemSettingSnapshot(
    string Key,
    string DisplayName,
    string? CompanyId,
    string DataType,
    string Value,
    bool IsSensitive);

public sealed record SystemSettingDto(
    string Id,
    string? CompanyId,
    string Key,
    string DisplayName,
    string DataType,
    string Value,
    bool IsSensitive,
    bool HasValue,
    string Version);

public sealed record AuditEventItemDto(
    string Id,
    DateTimeOffset OccurredAtUtc,
    string? ActorUserId,
    string ActorUserName,
    string? CompanyId,
    string? EntityId,
    string ProgramCode,
    string Action,
    string Result,
    string TraceId);

public sealed record AuditEventDetailDto(
    string Id,
    DateTimeOffset OccurredAtUtc,
    string? ActorUserId,
    string ActorUserName,
    string? CompanyId,
    string? EntityId,
    string ProgramCode,
    string Action,
    string Result,
    string TraceId,
    string? Before,
    string? After);

public sealed record AuditExportDto(
    string FileName,
    string ContentType,
    string CsvContent,
    int TotalCount);

public static class IdValue
{
    public static bool IsValid(string? value) => value is { Length: > 0 and <= 19 }
        && value.All(char.IsAsciiDigit)
        && long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0;

    public static long Parse(string value)
    {
        if (!IsValid(value)) throw new RuleException("INVALID_ID", "รหัสอ้างอิงไม่ถูกต้อง", 400);
        return long.Parse(value, CultureInfo.InvariantCulture);
    }
}
