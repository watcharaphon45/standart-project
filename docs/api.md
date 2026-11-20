# API เอกสารอ้างอิง (Auth, DBRT01–DBRT07 & SURT01–SURT09)

ใช้ HTTPS และ Explicit Credentialed CORS ทุก ID/FK/Version ใน JSON เป็น string ไม่มี ID ใน URL หรือ Hard Delete Endpoint

GET /api/auth/csrf ด้วย credentials: include แล้วส่ง token ใน X-CSRF-TOKEN สำหรับทุก POST/PUT หลัง Login ต้องขอ Token ใหม่

## Auth และ Session

| Method | Endpoint | Body / ผลลัพธ์ |
|---|---|---|
| GET | /api/auth/csrf | { token } |
| POST | /api/auth/login | { identifier, password } → Cookie และ mustChangePassword |
| GET | /api/auth/me | User, Companies, CompanyAccess, GlobalPermissions, Navigation |
| POST | /api/auth/change-password | { currentPassword, newPassword } → 204 และ Sign out |
| POST | /api/auth/logout | {} → 204 |

## DBRT01–DBRT07 ฐานข้อมูลองค์กร

| โปรแกรม | Base Route | Endpoints | Response DTO และ SortField |
|---|---|---|---|
| DBRT01 บริษัท | /api/db/dbrt01 | `search`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, code, name, isActive, codeLocked, version }` (`code`, `name`, `isActive`) |
| DBRT02 สาขา | /api/db/dbrt02 | `search`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, companyId, code, name, isActive, codeLocked, version }` (`code`, `name`, `isActive`) |
| DBRT04 ฝ่าย | /api/db/dbrt04 | `search`, `options`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, companyId, branchId, branchCode, branchName, code, name, isActive, codeLocked, version }` (`code`, `name`, `branchCode`, `isActive`) |
| DBRT03 แผนก | /api/db/dbrt03 | `search`, `options`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, companyId, branchId, branchCode, divisionId, divisionCode, divisionName, code, name, isActive, codeLocked, version }` (`code`, `name`, `branchCode`, `divisionCode`, `isActive`) |
| DBRT06 ศูนย์ต้นทุน | /api/db/dbrt06 | `search`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, companyId, code, name, isActive, codeLocked, version }` (`code`, `name`, `isActive`) |
| DBRT05 ตำแหน่งงาน | /api/db/dbrt05 | `search`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, code, name, isActive, codeLocked, version }` (`code`, `name`, `isActive`) |
| DBRT07 หน่วยธุรกิจ | /api/db/dbrt07 | `search`, `detail`, `create`, `update`, `activate`, `deactivate` | `{ id, code, name, isActive, codeLocked, version }` (`code`, `name`, `isActive`) |

### Request Payloads (DBRT01–DBRT07)

- **Search**:
  - `DBRT01`, `DBRT02`, `DBRT06`: `{ companyId, keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`
  - `DBRT04`: `{ companyId, branchId?, keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`
  - `DBRT03`: `{ companyId, branchId?, divisionId?, keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`
  - `DBRT05`, `DBRT07` (Shared Master): `{ keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`
  - ตอบกลับทุก Search เป็น `{ items, totalCount, pageNumber, pageSize }`, `PageSize` 1–100, `Keyword` สูงสุด 200 ตัวอักษร
- **Options** (เฉพาะ `[Authorize]`, ตรวจสิทธิ์ `VIEW`/`CREATE`/`UPDATE` ตาม Scope ภายใน Handler):
  - `POST /api/db/dbrt04/options` `{ companyId }` → `LookupOptionDto[]` (`{ id, code, name, isActive }`)
  - `POST /api/db/dbrt03/options` `{ companyId }` → `{ branches: LookupOptionDto[], divisions: DivisionLookupOptionDto[] }`
- **Detail / Status**:
  - `detail`: `{ id }`
  - `activate` / `deactivate`: `{ id, version }`
- **Create / Update**:
  - `DBRT01`, `DBRT05`, `DBRT07`: `create` `{ code, name }`, `update` `{ id, code, name, version }`
  - `DBRT02`, `DBRT06`: `create` `{ companyId, code, name }`, `update` `{ id, code, name, version }`
  - `DBRT04`: `create` `{ companyId, branchId, code, name }`, `update` `{ id, branchId, code, name, version }`
  - `DBRT03`: `create` `{ companyId, divisionId, code, name }`, `update` `{ id, divisionId, code, name, version }`

### สิทธิ์และ Scope Authorization

- แยกสิทธิ์ 5 Action ต่อโปรแกรม: `VIEW`, `CREATE`, `UPDATE`, `ACTIVATE`, `DEACTIVATE`
- `DBRT01`: `CREATE` ต้องมี Explicit Global Permission ส่วน Action อื่นใช้ Global หรือ `Company` Scope
- `DBRT06`: ต้องมี Global หรือ `Company` Scope
- `DBRT02`: `CREATE` ต้องมี Global หรือ `Company` Scope ส่วน `VIEW`/`UPDATE`/`ACTIVATE`/`DEACTIVATE` รองรับ Global, `Company` หรือ `Branch` Scope ของสาขานั้น
- `DBRT04`: รองรับ Global, `Company` หรือ `Branch` Scope ของสาขาที่ฝ่ายสังกัด (และตรวจสาขาปลายทางเมื่อย้ายสาขาใน `UPDATE`)
- `DBRT03`: `CREATE` รองรับ Global, `Company` หรือ `Branch` Scope ของฝ่ายเป้าหมาย ส่วน Action อื่นรองรับ Global, `Company`, `Branch` หรือ `Department` Scope (หากย้ายฝ่ายใน `UPDATE` ต้องมีสิทธิ์ระดับ `Branch`/`Company`/Global ที่ฝ่ายปลายทางด้วย)
- `DBRT05`, `DBRT07`: `CREATE`/`UPDATE`/`ACTIVATE`/`DEACTIVATE` เป็น Global-only (`DemandGlobalAsync`) ส่วน `VIEW` ใช้ได้ทั้ง Global และ Company Assignment ที่มีสิทธิ์ `.VIEW`

## SURT01–SURT04 ระบบบริหารจัดการผู้ใช้งาน บทบาท และสิทธิ์

| โปรแกรม | Base Route | Endpoints | Response DTO และ SortField |
|---|---|---|---|
| SURT01 ผู้ใช้งาน | /api/su/surt01 | `search`, `detail`, `create`, `update`, `reset`, `unlock`, `activate`, `disable` | `UserDto` (`{ id, userName, email, isActive, mustChangePassword, temporaryPasswordExpiresAtUtc, isLockedOut, lockoutEndUtc, accessFailedCount, version }`) (`userName`, `email`, `isActive`) |
| SURT02 บทบาท | /api/su/surt02 | `search`, `detail`, `create`, `update`, `activate`, `deactivate` | `RoleDto` (`{ id, companyId, code, name, isActive, codeLocked, permissionCount, assignedUserCount, version }`) (`code`, `name`, `isActive`) |
| SURT03 สิทธิ์ | /api/su/surt03 | `options`, `detail`, `update` | `options` → `PermissionCatalogDto` (`{ roles, catalog, grantablePermissions }`); `detail`/`update` → `RolePermissionsDto` (`{ roleId, companyId, roleCode, roleName, isActive, version, permissions, grantablePermissions }`) |
| SURT04 ขอบเขตผู้ใช้งาน | /api/su/surt04 | `search`, `options`, `detail`, `update` | `search` → `UserScopeListItemDto`; `options` → `{ roles, branches, departments }`; `detail`/`update` → `UserScopeDetailDto` (`userName`, `email`, `roleCode`, `scopeType`, `isActive`) |
| SURT05 เมนูระบบ | /api/su/surt05 | `search`, `detail`, `update`, `activate`, `deactivate` | `MenuItemDto` (`{ id, moduleCode, programCode, defaultName, displayName, route, requiredPermission, sortOrder, isActive, version }`) (`sortOrder`, `programCode`, `displayName`, `isActive`) |
| SURT06 เลขที่เอกสาร | /api/su/surt06 | `search`, `detail`, `create`, `update`, `activate`, `deactivate`, `next` | `DocumentSequenceDto` (`{ id, companyId, code, name, prefix, resetPolicy, paddingLength, currentPeriod, lastNumber, previewNextNumber, isActive, codeLocked, version }`); `next` → `DocumentNumberAllocationDto` |
| SURT07 ขั้นตอนอนุมัติ | /api/su/surt07 | `search`, `options`, `detail`, `create`, `update`, `activate`, `deactivate` | `search` → `ApprovalWorkflowDto`; `options` → `{ roles }`; `detail`/`create`/`update` → `ApprovalWorkflowDetailDto` (`steps`) |
| SURT08 ตั้งค่าระบบ | /api/su/surt08 | `search`, `detail`, `create`, `update` | `SystemSettingDto` (`{ id, companyId, key, displayName, dataType, value, isSensitive, hasValue, version }`) (`key`, `displayName`, `dataType`) |
| SURT09 ประวัติการใช้งาน | /api/su/surt09 | `search`, `detail`, `export` | `search` → `AuditEventItemDto`; `detail` → `AuditEventDetailDto`; `export` → `AuditExportDto` (`fileName`, `contentType`, `csvContent`, `totalCount`) |

### Request Payloads & Authorization (SURT01–SURT09)

- **SURT01 (Global-only ทุก Action)**:
  - `create`: `{ userName, email, temporaryPassword }` (รหัสผ่านชั่วคราว 15–128 ตัวอักษร อายุ 24 ชม. ตรวจ HIBP ก่อนเปิด Transaction, `userName` ห้ามมี `@`)
  - `update`: `{ id, userName, email, version }`; `reset`: `{ id, temporaryPassword, version }` (เพิกถอน Session ทั้งหมดของบัญชีนั้น); `unlock` / `activate` / `disable`: `{ id, version }` (ห้ามปิดตนเอง)
- **SURT02 (`Global` เมื่อ `companyId = null` หรือ `Company` Scope เมื่อระบุ `companyId`)**:
  - `search`: `{ companyId?, keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`
  - `create`: `{ companyId?, code, name }`, `update`: `{ id, code, name, version }`, `activate` / `deactivate`: `{ id, version }` (ล็อกรหัสเมื่อมีผู้ใช้/Assignment อ้างอิง และแยก Unique `(CompanyId, NormalizedName)` ต่อบริษัท)
- **SURT03 (`Global` สำหรับ Role ส่วนกลาง หรือ `Company` Scope สำหรับ Role บริษัท)**:
  - `options`: `{ companyId? }`, `detail`: `{ roleId }`, `update`: `{ roleId, permissions, version }`
  - บังคับเพดานสิทธิ์ (`CompanyGrantablePermissions` + สิทธิ์ที่ผู้มอบถืออยู่) ห้ามมอบสิทธิ์ใน `GlobalOnlyPermissions` ให้บทบาทบริษัท และห้ามผู้ดูแลบริษัทเพิ่มสิทธิ์ให้บทบาทที่ตนเองถืออยู่
- **SURT04 (`Company` Scope / Global Admin)**:
  - `search`: `{ companyId, keyword?, scopeType?, isActive?, pageNumber, pageSize, sortField, sortDirection }`
  - `options`: `{ companyId }`, `detail`: `{ companyId, userId }` (คืน `accessibleDepartments` ที่คำนวณจากสิทธิ์สืบทอด)
  - `update`: `{ companyId, userId, roleId, scopeType, scopeId?, isActive, version? }` (`scopeType` เป็น `"Company"` | `"Branch"` | `"Department"` บังคับ 1 Role ต่อ User ต่อ Company ห้ามมอบบทบาทข้ามบริษัท และเพิกถอน Session เดิมทันทีผ่าน `AuthorizationFingerprint`)
- **SURT05 (Global-only ทุก Action)**:
  - `search`: `{ keyword?, moduleCode?, isActive?, pageNumber, pageSize, sortField, sortDirection }`, `detail`: `{ id }`
  - `update`: `{ id, displayName, sortOrder, version }`, `activate` / `deactivate`: `{ id, version }` (ควบคุมชื่อแสดง/ลำดับ/การมองเห็นเมนูบน `/api/auth/me` โดยไม่ลัดข้าม Backend Policy)
- **SURT06 (`Company` Scope / Global Admin)**:
  - `search`: `{ companyId, keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`, `detail`: `{ id }`
  - `create`: `{ companyId, code, name, prefix, resetPolicy, paddingLength }`, `update`: `{ id, code, name, prefix, resetPolicy, paddingLength, version }` (`resetPolicy` เป็น `"NONE"` | `"YEAR"` | `"YEAR_MONTH"`, `paddingLength` 3–10)
  - `next` (`SURT06.UPDATE`): `{ id, version }` จองเลขถัดไปแบบ Atomic (`FOR UPDATE`) และตั้ง `codeLocked = true` ป้องกันการเปลี่ยน `code`/`prefix`/`resetPolicy` ในภายหลัง
- **SURT07 (`Company` Scope / Global Admin)**:
  - `search`: `{ companyId, keyword?, isActive?, pageNumber, pageSize, sortField, sortDirection }`, `options`: `{ companyId }`, `detail`: `{ id }`
  - `create`: `{ companyId, code, name, documentType, steps: [{ stepOrder, stepName, approverRoleId, requireDifferentActor }] }`, `update`: เพิ่ม `id`, `version`
  - ห้ามใช้ Role ข้ามบริษัทหรือ Global (`403`), บังคับลำดับขั้นต่อเนื่อง `1..N` และขั้นต่อเนื่องที่ใช้ Role เดียวกันต้องตั้ง `requireDifferentActor = true` (`400 INVALID_WORKFLOW_STEPS`) พร้อมล็อกบทบาทผู้อนุมัติ
- **SURT08 (`Global` เมื่อ `companyId = null` หรือ `Company` Scope เมื่อระบุ `companyId`)**:
  - `search`: `{ companyId?, keyword?, pageNumber, pageSize, sortField, sortDirection }`, `detail`: `{ id }`
  - `create`: `{ companyId?, key, displayName, dataType, value, isSensitive }` (`dataType` เป็น `"STRING"` | `"INTEGER"` | `"BOOLEAN"`), `update`: `{ id, displayName, value, version }`
  - ค่าที่เป็น `isSensitive = true` จะซ่อน `value = ""` พร้อม `hasValue` ที่ API และแทนค่าด้วย `"***MASKED***"` ใน Audit
- **SURT09 (`VIEW` / `EXPORT` แยกสิทธิ์; Global หรือ `Company`/`Branch`/`Department` Scope)**:
  - `search` (`SURT09.VIEW`): `{ companyId?, programCode?, action?, keyword?, pageNumber, pageSize, sortField, sortDirection }`, `detail` (`SURT09.VIEW`): `{ id }`, `export` (`SURT09.EXPORT`): `{ companyId?, programCode?, action?, keyword?, sortField, sortDirection }`
  - ผู้ใช้ระดับ Company/Branch/Department ไม่สามารถคิวรีหรือดู Audit ของ Global Programs (`AUTH`, `BOOTSTRAP`, `SURT01`, `DBRT05`, `DBRT07`, `SURT05`) และ CSV Export ป้องกัน Formula Injection (`=`, `+`, `-`, `@`, `\t`)

## Error Response

ข้อผิดพลาดเป็น ProblemDetails พร้อม `code`/`traceId` Validation เพิ่ม `errors` ตามชื่อ Field รหัสหลัก: `VALIDATION_ERROR` 400, `INVALID_ID` 400, `CSRF_VALIDATION_FAILED` 400, `PASSWORD_REJECTED` 400, `INVALID_WORKFLOW_STEPS` 400, `INVALID_SETTING_VALUE` 400, `PARENT_INACTIVE` 409, `ACTIVE_CHILDREN` 409, `ACTIVE_ASSIGNMENTS` 409, `CODE_LOCKED` 409, `REPARENT_LOCKED` 409, `ROLE_INACTIVE` 409, `CANNOT_DISABLE_SELF` 409, `STATUS_UNCHANGED` 409, `LOGIN_FAILED`/`LOGIN_REQUIRED` 401, `FORBIDDEN` 403, `NOT_FOUND`/`BRANCH_NOT_FOUND`/`DIVISION_NOT_FOUND`/`DEPARTMENT_NOT_FOUND`/`ROLE_NOT_FOUND` 404, `DUPLICATE_CODE` 409, `CONCURRENCY_CONFLICT` 409, `PASSWORD_SCREENING_UNAVAILABLE` 503 ไม่มี SQL/Secret ใน Response

## Health & Response Headers

- `GET /health`: Liveness Probe (Process ทำงานปกติ) → `200 OK` (`{"status":"ok"}`)
- `GET /health/ready`: Readiness Probe (ตรวจเชื่อม PostgreSQL `SELECT 1` Timeout 5 วินาที) → `200 OK` (`{"status":"ready","checks":{"database":"ready"}}`) หรือ `503 Service Unavailable` (`{"status":"unavailable","checks":{"database":"unavailable"}}`) โดยไม่เปิดเผย Connection String, Host, Username หรือ Exception ภายใน ทั้งคู่เปิดเป็น Anonymous และไม่ถูกบล็อกโดย `must_change_password`
- Response Headers มาตรฐาน: `X-Trace-Id` (Expose ผ่าน CORS เพื่อใช้อ้างอิงร่วมกับ `SuAuditEvent.TraceId`), `Cache-Control: no-store`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy: camera=(), microphone=(), geolocation=()`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`
