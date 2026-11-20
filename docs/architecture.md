# โครงสร้างระบบ (Phase 1–3: Auth, DBRT01–DBRT07 & SURT01–SURT09)

Clean Architecture แบ่ง Domain → Application → Infrastructure → Web; ทุกโปรเจกต์ Target net10.0 ส่วน Angular 22 อยู่ `src/Web/spa` และให้บริการแยก Origin พร้อม Custom UI Component Library (`src/Web/spa/src/app/shared/ui/`) และ Instagram Design System

## การบันทึกข้อมูลองค์กรและสิทธิ์ระบบ (DBRT01–DBRT07 & SURT01–SURT09)

Controller → MediatR → Normalize (`INormalizableRequest`) → FluentValidation แบบ Async ตามลำดับ → Handler (`DB` หรือผ่าน `ISystemAdminService` ใน `SU`) ตรวจ Scope (`IAccessService` ร่วมกับ `ProgramRegistry.AllowsScope` และเพดานสิทธิ์ที่มอบต่อได้) → Transaction `Serializable` บน NpgsqlConnection เดียวกัน (`AtomicWriter`) → `OrganizationDbContext` / `SystemDbContext` บันทึก Entity → `SystemDbContext` บันทึก Audit → Commit ทั้งคู่

Update/Status ล็อกแถวด้วย `FOR UPDATE` (`Lock*Async` ใน `OrganizationDbContext` และ `LockUserAsync`/`LockRoleAsync`/`LockAssignmentAsync`/`LockMenuItemAsync`/`LockDocumentSequenceAsync`/`LockApprovalWorkflowAsync`/`LockSystemSettingAsync` ใน `SystemAdminService`) และตรวจ Version/Parent อีกครั้ง การตรวจ HIBP สำหรับรหัสผ่านชั่วคราว (`CreateUser`/`ResetTemporaryPassword`) รันก่อนเปิด Transaction เพื่อไม่ถือ DB Lock ระหว่างเรียก HTTP ภายนอก Unique Index ป้องกันรหัสซ้ำ Version/ID/FK เป็น `BIGINT` ในฐานข้อมูลและ JSON string ที่ API เมื่อ Conflict ตอบ 409 หน้าจอเก็บ Draft ดูอ้างอิงระหว่างโหลดล่าสุด

## ฐานข้อมูล

PostgreSQL 18 ฐานเดียว Schema public ใช้ชื่อตาราง PascalCase

| Context | ตาราง |
|---|---|
| OrganizationDbContext | `DbCompany`, `DbBranch`, `DbDivision`, `DbDepartment`, `DbCostCenter`, `DbPosition`, `DbBusinessUnit` |
| SystemDbContext | `SuUser`, `SuRole`, ตาราง Identity, `SuCompanyAssignment`, `SuRolePermission`, `SuSession`, `SuMenuItem`, `SuDocumentSequence`, `SuApprovalWorkflow`, `SuApprovalWorkflowStep`, `SuSystemSetting`, `SuAuditEvent` |

Migration History แยกตาม Context ต้อง Migrate DB ก่อน SU (`20261116062618_InitialOrganization` → `20261118065519_OrganizationMasters`) แล้ว SU (`20261116062619_InitialSystem` → `20261116999999_CompanyIntegrity` → `20261118999999_OrganizationIntegrity` → `20261119000000_SystemAdministration` → `20261119010000_SystemOperations`) เพิ่ม FK ข้ามโมดูล ดัชนีบทบาทแยกบริษัท และ Trigger ระดับฐานข้อมูล

โครงสร้างองค์กรใช้ Alternate Keys (`AK_DbBranch_Id_CompanyId`, `AK_DbDivision_Id_CompanyId`) และ Composite FKs เพื่อรับรองความสอดคล้องของ `CompanyId` ตลอดสาย `Company → Branch → Division → Department` ส่วน `DbCostCenter` ผูก `CompanyId` และ `DbPosition` / `DbBusinessUnit` เป็น Shared Master ไม่ผูก `CompanyId`

การอ่านข้อมูล DB จากฝั่ง SU ทำผ่าน `IOrganizationLookup` (`CompaniesAsync`, `CompanyAsync`, `BranchesByCompanyAsync`, `DepartmentsByCompanyAsync`, `DepartmentIdsByBranchesAsync`, `DivisionIdsByBranchesAsync`) โดยไม่ Inject `IOrganizationDbContext` เข้าสู่ SU Handlers โดยตรง

Assignment Key `(UserId, CompanyId)` บังคับหนึ่ง Role ต่อบริษัท FK `(RoleId, CompanyId)` ป้องกัน Role ข้ามบริษัท ดัชนี `SuRole` แยกเป็น `RoleNameIndex` (`WHERE "CompanyId" IS NULL`) สำหรับบทบาทส่วนกลาง และ `IX_SuRole_CompanyId_NormalizedName` (`WHERE "CompanyId" IS NOT NULL`) เพื่อให้ต่างบริษัทตั้งรหัส Role ชื่อเดียวกันได้โดยไม่ชนกัน ส่วนกลางใช้ Identity UserRole กับ Role ที่ `CompanyId` เป็น null เท่านั้น `AccessService` รองรับ Scope `Company`, `Branch` และ `Department` ร่วมกับ `ProgramRegistry.AllowsScope` (`DemandGlobalAsync`, `DemandSharedViewAsync`, `DemandCompanyAsync`, `DemandBranchAsync`, `DemandDepartmentAsync`, `GetScopeFilterAsync`) ส่วน API/UI ระบบผู้ดูแลและการตั้งค่าระบบเปิดใช้งานครบทั้ง `SURT01`–`SURT09`

Trigger ใน `CompanyIntegrity`, `OrganizationIntegrity`, `SystemAdministration` และ `SystemOperations` ได้แก่:
- ล็อก Code และ Parent (`REPARENT_LOCKED`/`CODE_LOCKED` ผ่าน `clean_guard_company`, `clean_guard_branch`, `clean_guard_division`, `clean_guard_department`, `clean_guard_role`) เมื่อถูกอ้างอิงจากตารางลูก, `SuUserRole`, `SuCompanyAssignment`, `SuDocumentSequence`, `SuApprovalWorkflow` หรือ `SuApprovalWorkflowStep` (`division_branch_reference`, `department_division_reference`, `company_cost_center_reference`, `assignment_scope_reference`, `role_assignment_reference`, `role_user_role_reference`, `company_doc_sequence_reference`, `company_workflow_reference`, `workflow_step_role_reference`) พร้อมเพิ่ม `Version` ของแถวแม่
- ป้องกันการปิด Parent หรือบทบาทที่มีลูก, Assignment/UserRole, ชุดเลขที่เอกสาร หรือขั้นตอนอนุมัติที่ Active ทั้งใน Handler และระดับ Trigger ฐานข้อมูล (`PARENT_INACTIVE`, `ACTIVE_CHILDREN`, `ACTIVE_ASSIGNMENTS`)

## Frontend Design System และ UI

- `src/Web/spa/src/styles.css`: กำหนด Instagram Design System Tokens (`--surface`, `--canvas`, `--surface-subtle`, `--text`, `--muted`, `--border`, `--brand`, `--brand-hover`, `--accent-rose`, `--ig-gradient` และ Tailwind `@theme`)
- `src/Web/spa/src/app/shared/ui/`: รวม Custom Standalone UI Components (`app-button`, `app-input`, `app-textarea`, `app-select`, `app-checkbox`, `app-radio-group`, `app-date-picker`, `app-form-field`, `app-data-table`, `app-table-toolbar`, `app-table-pagination`, `app-page-header`, `app-confirm-dialog`, `app-notification`, `app-loading`, `app-empty-state`, `app-error-state`)
- ทุก Feature Component แยก 3 ไฟล์ (`.component.ts`, `.component.html`, `.component.scss`), ใช้ Base Class ร่วม (`OrganizationListBase`, `OrganizationFormBase` ใน `features/db/shared/organization-crud.ts`) และเก็บ Edit ID ใน `sessionStorage` แทนการส่งผ่าน URL

## Security

Cookie __Host-clean.session เป็น Secure/HttpOnly/SameSite=None ใช้ CSRF Cookie แยกและ Header X-CSRF-TOKEN ทุก POST/PUT รวม Login และ POST Search ต้องผ่าน CSRF Filter หลัง Login ต้องขอ Token ใหม่ CORS ระบุ Origin ชัดเจนและ AllowCredentials

ทุก Request ตรวจ User Active, SecurityStamp, Revoked, Idle 30 นาที, Absolute 8 ชั่วโมง, อายุรหัสชั่วคราว และ Fingerprint Role/Permission/Scope ปัจจุบัน Cache ของ Fingerprint อยู่เฉพาะ Request เปลี่ยนสิทธิ์แล้ว Session เดิมใช้ต่อไม่ได้ตั้งแต่ Request ถัดไป เปลี่ยนรหัสผ่านเพิกถอนทุก Session

Username/Email Normalize และไม่ซ้ำทั้งระบบ Username ไม่มี @ รหัสผิด 5 ครั้งล็อก 15 นาที นับรวม Username/Email ของบัญชีเดียวกัน มี Rate Limit ต่อ IP เพิ่มเติม

ตั้งรหัสตรวจความยาว 15–128 ตัวและรายการพบบ่อย/HIBP ผ่าน HTTPS ส่ง SHA-1 Prefix เพียง 5 ตัวพร้อม Padding ไม่ส่ง Plaintext/Full Hash ไม่ Log URL ของ Client นี้ หากตรวจไม่ได้ให้หยุดการตั้งรหัส Login เดิมไม่เรียก HIBP ไม่มี Test Bypass ใน Production DI

## Audit

DBRT01–DBRT07 และ SURT01–SURT08 บันทึกผ่าน `AuditWriter.RecordOrganizationAsync` โดยใช้ Allowlist Snapshot ก่อน/หลังเฉพาะฟิลด์ที่กำหนด (`CompanySnapshot`, `BranchSnapshot`, `DivisionSnapshot`, `DepartmentSnapshot`, `CostCenterSnapshot`, `PositionSnapshot`, `BusinessUnitSnapshot`, `UserSnapshot`, `RoleSnapshot`, `RolePermissionSnapshot`, `AssignmentSnapshot`, `MenuItemSnapshot`, `DocumentSequenceSnapshot`, `ApprovalWorkflowSnapshot`, `SystemSettingSnapshot` ซึ่งแทนค่า Secret ด้วย `"***MASKED***"`) พร้อม Metadata (`ActorUserId`, `OccurredAtUtc`, `CompanyId` หรือ null สำหรับ Shared Master/Global Program, `EntityId`, `ProgramCode`, `Action`, `Result`, `TraceId`) ไม่มี Password/Hash/Secret/ข้อมูลติดต่อ

ข้อมูลองค์กร ข้อมูลสิทธิ์/ตั้งค่า SU และ Success Audit ใช้ Transaction เดียวกัน หากล้มเหลว Rollback ทั้งคู่ Conflict ไม่สร้าง Success Audit Login/Password Change เป็น Security Event แยกใน SU และการดู/ส่งออก CSV ตาม Scope เปิดใช้งานผ่าน `SURT09` พร้อม Neutralize สูตร Spreadsheet (`=`, `+`, `-`, `@`, `\t`) ส่วน Retention Policy และขั้นตอน Archive/Purge กำหนดแนวทางปฏิบัติไว้ใน [Deployment Runbook](operations/deployment.md)

## Production Readiness, Health & Observability (Phase 4.3)

- **Health & Readiness (`src/Web/Health/`):** แยก `GET /health` (Liveness) ออกจาก `GET /health/ready` (`DatabaseReadinessHealthCheck` รัน `SELECT 1` ภายใน 5 วินาที) คืนเฉพาะสถานะ `"ready"` / `"unavailable"` และรหัส `200` / `503` โดยปิดบังรายละเอียด Database และ Connection String ทั้งหมด
- **Observability & Correlation (`src/Web/Middleware/RequestObservabilityMiddleware.cs`):** ใช้ `ResolveTraceId` ร่วมกันทั้ง `X-Trace-Id` Response Header, `CurrentActor.TraceId`, `ValidateCsrfFilter`, `GlobalExceptionHandler` และ `ProblemDetails` Log เฉพาะ Method, Path (ไม่เก็บ QueryString/Body/Cookie/Username), StatusCode, ElapsedMs, Numeric `UserId` และ `TraceId` โดย `appsettings.json` ใช้ UTC JSON Console Formatter ใน Production
- **Security Headers & Deployment Hardening (`src/Web/Middleware/SecurityHeadersMiddleware.cs`, `src/Web/Configuration/ProductionReadiness.cs`):** แนบ `Cache-Control: no-store`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy` และ `Content-Security-Policy` บนทุกคำขอของ API, ตรวจความถูกต้องของ `Cors:AllowedOrigins` (ห้าม `*`/Path และบังคับ `https://` นอก Development), รองรับการตั้ง `Cookies:SameSite` (`None`/`Lax`/`Strict`), Optional Reverse Proxy `ForwardedHeaders` (`KnownProxies`/`KnownIPNetworks`) และ Optional `DataProtection:KeyDirectory`
- **Operations & Drills (`scripts/`):** เพิ่ม `Export-Migrations.ps1` (สร้าง Idempotent SQL พร้อมตรวจซ้ำ 2 รอบ), `Backup-Database.ps1` (Custom Dump + SHA-256) และ `Restore-Database.ps1 -Drill` (ตรวจ Checksum, กู้คืนบนฐานชั่วคราว, นับแถวยืนยันแล้วลบทิ้ง) ดูรายละเอียดและ [ADR-079](adr/ADR-079-production-readiness.md)
