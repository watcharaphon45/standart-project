import { Routes } from '@angular/router';
import { authGuard, pendingGuard, permissionGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'change-password',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/change-password.component').then((m) => m.ChangePasswordComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'db/dbrt01' },
      {
        path: 'db/dbrt01',
        loadComponent: () =>
          import('./features/db/dbrt01/company-list.component').then((m) => m.CompanyListComponent),
      },
      {
        path: 'db/dbrt01/create',
        data: { permission: 'DBRT01.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt01/company-form.component').then((m) => m.CompanyFormComponent),
      },
      {
        path: 'db/dbrt01/edit',
        data: { editing: true, permission: 'DBRT01.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt01/company-form.component').then((m) => m.CompanyFormComponent),
      },
      {
        path: 'db/dbrt02',
        data: { permission: 'DBRT02.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/db/dbrt02/branch-list.component').then((m) => m.BranchListComponent),
      },
      {
        path: 'db/dbrt02/create',
        data: { permission: 'DBRT02.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt02/branch-form.component').then((m) => m.BranchFormComponent),
      },
      {
        path: 'db/dbrt02/edit',
        data: { editing: true, permission: 'DBRT02.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt02/branch-form.component').then((m) => m.BranchFormComponent),
      },
      {
        path: 'db/dbrt03',
        data: { permission: 'DBRT03.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/db/dbrt03/department-list.component').then(
            (m) => m.DepartmentListComponent,
          ),
      },
      {
        path: 'db/dbrt03/create',
        data: { permission: 'DBRT03.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt03/department-form.component').then(
            (m) => m.DepartmentFormComponent,
          ),
      },
      {
        path: 'db/dbrt03/edit',
        data: { editing: true, permission: 'DBRT03.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt03/department-form.component').then(
            (m) => m.DepartmentFormComponent,
          ),
      },
      {
        path: 'db/dbrt04',
        data: { permission: 'DBRT04.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/db/dbrt04/division-list.component').then(
            (m) => m.DivisionListComponent,
          ),
      },
      {
        path: 'db/dbrt04/create',
        data: { permission: 'DBRT04.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt04/division-form.component').then(
            (m) => m.DivisionFormComponent,
          ),
      },
      {
        path: 'db/dbrt04/edit',
        data: { editing: true, permission: 'DBRT04.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt04/division-form.component').then(
            (m) => m.DivisionFormComponent,
          ),
      },
      {
        path: 'db/dbrt05',
        data: { permission: 'DBRT05.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/db/dbrt05/position-list.component').then(
            (m) => m.PositionListComponent,
          ),
      },
      {
        path: 'db/dbrt05/create',
        data: { permission: 'DBRT05.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt05/position-form.component').then(
            (m) => m.PositionFormComponent,
          ),
      },
      {
        path: 'db/dbrt05/edit',
        data: { editing: true, permission: 'DBRT05.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt05/position-form.component').then(
            (m) => m.PositionFormComponent,
          ),
      },
      {
        path: 'db/dbrt06',
        data: { permission: 'DBRT06.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/db/dbrt06/cost-center-list.component').then(
            (m) => m.CostCenterListComponent,
          ),
      },
      {
        path: 'db/dbrt06/create',
        data: { permission: 'DBRT06.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt06/cost-center-form.component').then(
            (m) => m.CostCenterFormComponent,
          ),
      },
      {
        path: 'db/dbrt06/edit',
        data: { editing: true, permission: 'DBRT06.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt06/cost-center-form.component').then(
            (m) => m.CostCenterFormComponent,
          ),
      },
      {
        path: 'db/dbrt07',
        data: { permission: 'DBRT07.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/db/dbrt07/business-unit-list.component').then(
            (m) => m.BusinessUnitListComponent,
          ),
      },
      {
        path: 'db/dbrt07/create',
        data: { permission: 'DBRT07.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt07/business-unit-form.component').then(
            (m) => m.BusinessUnitFormComponent,
          ),
      },
      {
        path: 'db/dbrt07/edit',
        data: { editing: true, permission: 'DBRT07.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/db/dbrt07/business-unit-form.component').then(
            (m) => m.BusinessUnitFormComponent,
          ),
      },
      {
        path: 'su/surt01',
        data: { permission: 'SURT01.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt01/user-list.component').then((m) => m.UserListComponent),
      },
      {
        path: 'su/surt01/create',
        data: { permission: 'SURT01.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt01/user-form.component').then((m) => m.UserFormComponent),
      },
      {
        path: 'su/surt01/edit',
        data: { editing: true, permission: 'SURT01.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt01/user-form.component').then((m) => m.UserFormComponent),
      },
      {
        path: 'su/surt02',
        data: { permission: 'SURT02.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt02/role-list.component').then((m) => m.RoleListComponent),
      },
      {
        path: 'su/surt02/create',
        data: { permission: 'SURT02.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt02/role-form.component').then((m) => m.RoleFormComponent),
      },
      {
        path: 'su/surt02/edit',
        data: { editing: true, permission: 'SURT02.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt02/role-form.component').then((m) => m.RoleFormComponent),
      },
      {
        path: 'su/surt03',
        data: { permission: 'SURT03.VIEW' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt03/permission-matrix.component').then(
            (m) => m.PermissionMatrixComponent,
          ),
      },
      {
        path: 'su/surt04',
        data: { permission: 'SURT04.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt04/user-scope-list.component').then(
            (m) => m.UserScopeListComponent,
          ),
      },
      {
        path: 'su/surt04/edit',
        data: { permission: 'SURT04.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt04/user-scope-form.component').then(
            (m) => m.UserScopeFormComponent,
          ),
      },
      {
        path: 'su/surt05',
        data: { permission: 'SURT05.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt05/menu-list.component').then((m) => m.MenuListComponent),
      },
      {
        path: 'su/surt05/edit',
        data: { editing: true, permission: 'SURT05.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt05/menu-form.component').then((m) => m.MenuFormComponent),
      },
      {
        path: 'su/surt06',
        data: { permission: 'SURT06.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt06/doc-sequence-list.component').then(
            (m) => m.DocSequenceListComponent,
          ),
      },
      {
        path: 'su/surt06/create',
        data: { permission: 'SURT06.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt06/doc-sequence-form.component').then(
            (m) => m.DocSequenceFormComponent,
          ),
      },
      {
        path: 'su/surt06/edit',
        data: { editing: true, permission: 'SURT06.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt06/doc-sequence-form.component').then(
            (m) => m.DocSequenceFormComponent,
          ),
      },
      {
        path: 'su/surt07',
        data: { permission: 'SURT07.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt07/workflow-list.component').then(
            (m) => m.WorkflowListComponent,
          ),
      },
      {
        path: 'su/surt07/create',
        data: { permission: 'SURT07.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt07/workflow-form.component').then(
            (m) => m.WorkflowFormComponent,
          ),
      },
      {
        path: 'su/surt07/edit',
        data: { editing: true, permission: 'SURT07.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt07/workflow-form.component').then(
            (m) => m.WorkflowFormComponent,
          ),
      },
      {
        path: 'su/surt08',
        data: { permission: 'SURT08.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt08/setting-list.component').then((m) => m.SettingListComponent),
      },
      {
        path: 'su/surt08/create',
        data: { permission: 'SURT08.CREATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt08/setting-form.component').then((m) => m.SettingFormComponent),
      },
      {
        path: 'su/surt08/edit',
        data: { editing: true, permission: 'SURT08.UPDATE' },
        canActivate: [permissionGuard],
        canDeactivate: [pendingGuard],
        loadComponent: () =>
          import('./features/su/surt08/setting-form.component').then((m) => m.SettingFormComponent),
      },
      {
        path: 'su/surt09',
        data: { permission: 'SURT09.VIEW' },
        canActivate: [permissionGuard],
        loadComponent: () =>
          import('./features/su/surt09/audit-log-list.component').then(
            (m) => m.AuditLogListComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
