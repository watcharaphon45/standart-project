export interface Company {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface Branch {
  id: string;
  companyId: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface Division {
  id: string;
  companyId: string;
  branchId: string;
  branchCode: string;
  branchName: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface Department {
  id: string;
  companyId: string;
  branchId: string;
  branchCode: string;
  divisionId: string;
  divisionCode: string;
  divisionName: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface CostCenter {
  id: string;
  companyId: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface Position {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface BusinessUnit {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  [key: string]: string | boolean;
}

export interface LookupOption {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface DivisionLookupOption {
  id: string;
  branchId: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface DepartmentOptions {
  branches: LookupOption[];
  divisions: DivisionLookupOption[];
}

export interface Page<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface CompanyOption {
  id: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface NavigationItem {
  code: string;
  name: string;
  route: string;
}

export interface CurrentUser {
  id: string;
  userName: string;
  mustChangePassword: boolean;
  companies: CompanyOption[];
  companyAccess: { companyId: string; permissions: string[] }[];
  globalPermissions: string[];
  navigation: NavigationItem[];
}

export interface ApiProblem {
  code?: string;
  title?: string;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}

export interface UserAccount {
  id: string;
  userName: string;
  email: string;
  isActive: boolean;
  mustChangePassword: boolean;
  temporaryPasswordExpiresAtUtc: string | null;
  isLockedOut: boolean;
  lockoutEndUtc: string | null;
  accessFailedCount: number;
  version: string;
}

export interface RoleItem {
  id: string;
  companyId: string | null;
  code: string;
  name: string;
  isActive: boolean;
  codeLocked: boolean;
  permissionCount: number;
  assignedUserCount: number;
  version: string;
}

export interface PermissionActionItem {
  permission: string;
  action: string;
  label: string;
  globalOnly: boolean;
}

export interface ProgramPermissionGroup {
  code: string;
  name: string;
  items: PermissionActionItem[];
}

export interface PermissionCatalog {
  roles: RoleItem[];
  catalog: ProgramPermissionGroup[];
  grantablePermissions: string[];
}

export interface RolePermissionsDetail {
  roleId: string;
  companyId: string | null;
  roleCode: string;
  roleName: string;
  isActive: boolean;
  version: string;
  permissions: string[];
  grantablePermissions: string[];
}

export interface DepartmentScopeOption {
  id: string;
  branchId: string;
  branchCode: string;
  branchName: string;
  divisionId: string;
  divisionCode: string;
  divisionName: string;
  code: string;
  name: string;
  isActive: boolean;
}

export interface UserScopeListItem {
  userId: string;
  userName: string;
  email: string;
  userIsActive: boolean;
  hasAssignment: boolean;
  roleId: string | null;
  roleCode: string | null;
  roleName: string | null;
  scopeType: string | null;
  scopeId: string | null;
  scopeCode: string | null;
  scopeName: string | null;
  isActive: boolean;
  version: string;
}

export interface UserScopeOptions {
  roles: LookupOption[];
  branches: LookupOption[];
  departments: DepartmentScopeOption[];
}

export interface UserScopeDetail {
  userId: string;
  userName: string;
  email: string;
  userIsActive: boolean;
  companyId: string;
  roleId: string | null;
  scopeType: 'Company' | 'Branch' | 'Department';
  scopeId: string | null;
  isActive: boolean;
  version: string;
  accessibleDepartments: DepartmentScopeOption[];
}

export interface MenuItemDto {
  id: string;
  moduleCode: string;
  programCode: string;
  defaultName: string;
  displayName: string;
  route: string;
  requiredPermission: string;
  sortOrder: number;
  isActive: boolean;
  version: string;
}

export interface DocumentSequenceDto {
  id: string;
  companyId: string;
  code: string;
  name: string;
  prefix: string;
  resetPolicy: 'NONE' | 'YEAR' | 'YEAR_MONTH';
  paddingLength: number;
  currentPeriod: string;
  lastNumber: number;
  previewNextNumber: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
}

export interface DocumentNumberAllocation {
  id: string;
  documentNumber: string;
  periodKey: string;
  sequenceNumber: number;
  version: string;
}

export interface ApprovalWorkflowStepDto {
  id: string;
  stepOrder: number;
  stepName: string;
  approverRoleId: string;
  approverRoleCode: string;
  approverRoleName: string;
  requireDifferentActor: boolean;
}

export interface ApprovalWorkflowDto {
  id: string;
  companyId: string;
  code: string;
  name: string;
  documentType: string;
  stepCount: number;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
}

export interface ApprovalWorkflowDetailDto {
  id: string;
  companyId: string;
  code: string;
  name: string;
  documentType: string;
  isActive: boolean;
  codeLocked: boolean;
  version: string;
  steps: ApprovalWorkflowStepDto[];
}

export interface ApprovalWorkflowOptions {
  roles: LookupOption[];
}

export interface SystemSettingDto {
  id: string;
  companyId: string | null;
  key: string;
  displayName: string;
  dataType: 'STRING' | 'INTEGER' | 'BOOLEAN';
  value: string;
  isSensitive: boolean;
  hasValue: boolean;
  version: string;
}

export interface AuditEventItem {
  id: string;
  occurredAtUtc: string;
  actorUserId: string | null;
  actorUserName: string;
  companyId: string | null;
  entityId: string | null;
  programCode: string;
  action: string;
  result: string;
  traceId: string;
}

export interface AuditEventDetail extends AuditEventItem {
  before: string | null;
  after: string | null;
}

export interface AuditExportResult {
  fileName: string;
  contentType: string;
  csvContent: string;
  totalCount: number;
}
