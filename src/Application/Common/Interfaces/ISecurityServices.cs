using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT01;
using clean.Application.Features.SU.SURT02;
using clean.Application.Features.SU.SURT03;
using clean.Application.Features.SU.SURT04;
using clean.Application.Features.SU.SURT05;
using clean.Application.Features.SU.SURT06;
using clean.Application.Features.SU.SURT07;
using clean.Application.Features.SU.SURT08;
using clean.Application.Features.SU.SURT09;
using clean.Domain.Entities.Organization;

namespace clean.Application.Common.Interfaces;

public interface ICurrentActor
{
    long UserId { get; }
    string TraceId { get; }
}

public interface IAccessService
{
    Task DemandGlobalAsync(string permission, CancellationToken cancellationToken);
    Task DemandSharedViewAsync(string permission, CancellationToken cancellationToken);
    Task DemandCompanyAsync(long companyId, string permission, CancellationToken cancellationToken);
    Task DemandBranchAsync(long companyId, long branchId, string permission, CancellationToken cancellationToken);
    Task DemandDepartmentAsync(long companyId, long branchId, long departmentId, string permission, CancellationToken cancellationToken);
    Task<ScopeFilter> GetScopeFilterAsync(long companyId, string permission, CancellationToken cancellationToken);
    Task<ScopeFilter> GetScopeFilterAnyAsync(long companyId, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken);
    Task<bool> HasActiveAssignmentsAsync(long companyId, CancellationToken cancellationToken);
    Task<bool> HasActiveBranchAssignmentsAsync(long companyId, long branchId, CancellationToken cancellationToken);
    Task<bool> HasActiveDepartmentAssignmentsAsync(long companyId, long departmentId, CancellationToken cancellationToken);
}

public interface IAtomicWriter
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
}

public interface IAuditWriter
{
    Task CompanyChangedAsync(string action, CompanySnapshot? before, Company company, CancellationToken cancellationToken);
    Task RecordOrganizationAsync(
        string programCode,
        string action,
        long? companyId,
        long entityId,
        object? before,
        object after,
        CancellationToken cancellationToken);
}

public interface IOrganizationLookup
{
    Task<IReadOnlyList<CompanyOption>> CompaniesAsync(IReadOnlyCollection<long> companyIds, bool all, CancellationToken cancellationToken);
    Task<CompanyOption?> CompanyAsync(long companyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<LookupOptionDto>> BranchesByCompanyAsync(long companyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DepartmentScopeOptionDto>> DepartmentsByCompanyAsync(long companyId, CancellationToken cancellationToken);
    Task<IReadOnlySet<long>> DepartmentIdsByBranchesAsync(long companyId, IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken);
    Task<IReadOnlySet<long>> DivisionIdsByBranchesAsync(long companyId, IReadOnlyCollection<long> branchIds, CancellationToken cancellationToken);
}

public interface IIdentityService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<SessionUser?> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken);
    Task<CurrentUserDto> CurrentAsync(long userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(string sessionId, CancellationToken cancellationToken);
}

public interface ISystemAdminService
{
    Task<Page<UserDto>> SearchUsersAsync(SearchUsersRequest request, CancellationToken cancellationToken);
    Task<UserDto> GetUserDetailAsync(GetUserDetailRequest request, CancellationToken cancellationToken);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> ResetTemporaryPasswordAsync(ResetTemporaryPasswordRequest request, CancellationToken cancellationToken);
    Task<UserDto> UnlockUserAsync(UnlockUserRequest request, CancellationToken cancellationToken);
    Task<UserDto> ChangeUserStatusAsync(ChangeUserStatusRequest request, CancellationToken cancellationToken);

    Task<Page<RoleDto>> SearchRolesAsync(SearchRolesRequest request, CancellationToken cancellationToken);
    Task<RoleDto> GetRoleDetailAsync(GetRoleDetailRequest request, CancellationToken cancellationToken);
    Task<RoleDto> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleDto> UpdateRoleAsync(UpdateRoleRequest request, CancellationToken cancellationToken);
    Task<RoleDto> ChangeRoleStatusAsync(ChangeRoleStatusRequest request, CancellationToken cancellationToken);

    Task<PermissionCatalogDto> GetPermissionOptionsAsync(GetPermissionOptionsRequest request, CancellationToken cancellationToken);
    Task<RolePermissionsDto> GetRolePermissionsDetailAsync(GetRolePermissionsDetailRequest request, CancellationToken cancellationToken);
    Task<RolePermissionsDto> UpdateRolePermissionsAsync(UpdateRolePermissionsRequest request, CancellationToken cancellationToken);

    Task<Page<UserScopeListItemDto>> SearchUserScopesAsync(SearchUserScopesRequest request, CancellationToken cancellationToken);
    Task<UserScopeOptionsDto> GetUserScopeOptionsAsync(GetUserScopeOptionsRequest request, CancellationToken cancellationToken);
    Task<UserScopeDetailDto> GetUserScopeDetailAsync(GetUserScopeDetailRequest request, CancellationToken cancellationToken);
    Task<UserScopeDetailDto> UpdateUserScopeAsync(UpdateUserScopeRequest request, CancellationToken cancellationToken);

    Task<Page<MenuItemDto>> SearchMenuItemsAsync(SearchMenuItemsRequest request, CancellationToken cancellationToken);
    Task<MenuItemDto> GetMenuItemDetailAsync(GetMenuItemDetailRequest request, CancellationToken cancellationToken);
    Task<MenuItemDto> UpdateMenuItemAsync(UpdateMenuItemRequest request, CancellationToken cancellationToken);
    Task<MenuItemDto> ChangeMenuItemStatusAsync(ChangeMenuItemStatusRequest request, CancellationToken cancellationToken);

    Task<Page<DocumentSequenceDto>> SearchDocumentSequencesAsync(SearchDocumentSequencesRequest request, CancellationToken cancellationToken);
    Task<DocumentSequenceDto> GetDocumentSequenceDetailAsync(GetDocumentSequenceDetailRequest request, CancellationToken cancellationToken);
    Task<DocumentSequenceDto> CreateDocumentSequenceAsync(CreateDocumentSequenceRequest request, CancellationToken cancellationToken);
    Task<DocumentSequenceDto> UpdateDocumentSequenceAsync(UpdateDocumentSequenceRequest request, CancellationToken cancellationToken);
    Task<DocumentSequenceDto> ChangeDocumentSequenceStatusAsync(ChangeDocumentSequenceStatusRequest request, CancellationToken cancellationToken);
    Task<DocumentNumberAllocationDto> AllocateDocumentNumberAsync(AllocateDocumentNumberRequest request, CancellationToken cancellationToken);

    Task<Page<ApprovalWorkflowDto>> SearchApprovalWorkflowsAsync(SearchApprovalWorkflowsRequest request, CancellationToken cancellationToken);
    Task<ApprovalWorkflowOptionsDto> GetApprovalWorkflowOptionsAsync(GetApprovalWorkflowOptionsRequest request, CancellationToken cancellationToken);
    Task<ApprovalWorkflowDetailDto> GetApprovalWorkflowDetailAsync(GetApprovalWorkflowDetailRequest request, CancellationToken cancellationToken);
    Task<ApprovalWorkflowDetailDto> CreateApprovalWorkflowAsync(CreateApprovalWorkflowRequest request, CancellationToken cancellationToken);
    Task<ApprovalWorkflowDetailDto> UpdateApprovalWorkflowAsync(UpdateApprovalWorkflowRequest request, CancellationToken cancellationToken);
    Task<ApprovalWorkflowDto> ChangeApprovalWorkflowStatusAsync(ChangeApprovalWorkflowStatusRequest request, CancellationToken cancellationToken);

    Task<Page<SystemSettingDto>> SearchSystemSettingsAsync(SearchSystemSettingsRequest request, CancellationToken cancellationToken);
    Task<SystemSettingDto> GetSystemSettingDetailAsync(GetSystemSettingDetailRequest request, CancellationToken cancellationToken);
    Task<SystemSettingDto> CreateSystemSettingAsync(CreateSystemSettingRequest request, CancellationToken cancellationToken);
    Task<SystemSettingDto> UpdateSystemSettingAsync(UpdateSystemSettingRequest request, CancellationToken cancellationToken);

    Task<Page<AuditEventItemDto>> SearchAuditLogsAsync(SearchAuditLogsRequest request, CancellationToken cancellationToken);
    Task<AuditEventDetailDto> GetAuditLogDetailAsync(GetAuditLogDetailRequest request, CancellationToken cancellationToken);
    Task<AuditExportDto> ExportAuditLogsAsync(ExportAuditLogsRequest request, CancellationToken cancellationToken);
}
