# clean

โครงเริ่มต้นตาม [project-plan.md](project-plan.md) ใช้ ASP.NET Core Identity, MediatR/CQRS, EF Core และ Angular พัฒนา Login/Session, **DBRT01–DBRT07 ฐานข้อมูลองค์กร** (บริษัท สาขา แผนก ฝ่าย ตำแหน่งงาน ศูนย์ต้นทุน หน่วยธุรกิจ) และ **SURT01–SURT09 ระบบบริหารจัดการสิทธิ์ ผู้ใช้งาน เมนู เอกสาร การอนุมัติ และ Audit** พร้อม Custom UI Components และ Instagram Design System จากหน้าจอถึง PostgreSQL/Audit

ทุกโปรแกรมทั้ง 16 รายการ (`DBRT01`–`DBRT07` และ `SURT01`–`SURT09`), Shell ระยะที่ 4.1–4.2 และความพร้อมระดับ Production ระยะที่ 4.3 (Health/Readiness `/health` และ `/health/ready`, `X-Trace-Id`, Structured JSON Logging, Security Headers, Migration Dry-Run และ Backup/Restore Drill) พร้อมใช้งานแล้ว ดู [สถานะงาน](docs/development-status.md), [คู่มือ Production Deployment](docs/operations/deployment.md), [Architecture](docs/architecture.md), [API](docs/api.md) และ [ADR-079](docs/adr/ADR-079-production-readiness.md) ก่อนขึ้นระบบจริงยังต้องตั้งค่า Hosting/Domain ขององค์กร, ทดสอบ Browser ข้ามไซต์, ตรวจ Screen Reader จริง/Load Test และอนุมัตินโยบาย Audit Retention

## เครื่องมือ

.NET SDK 11.0.100 ตาม global.json พร้อม Runtime .NET/ASP.NET Core 10 (ทุกโปรเจกต์ Target net10.0), Node 26.13.0/npm 11.20.0 และ PostgreSQL 18 โดยติดตั้งโดยตรงหรือใช้ Podman แบบโอเพนซอร์ส

ใช้เฉพาะซอฟต์แวร์/บริการที่ไม่มีค่า License, Subscription หรือค่าตามการใช้งานสำหรับโปรเจกต์ ตาม [นโยบายค่าใช้จ่าย](docs/cost-policy.md) MediatR คง 12.5.0 / Apache-2.0 และไม่กำหนด Docker Desktop เป็นเครื่องมือบังคับ

Angular 22.2.6, CLI/CDK 22.2.7, Tailwind 4.3.5, MediatR 12.5.0, EF Core 10.0.15, Npgsql Provider 10.0.3

## เริ่มใช้งานในเครื่อง

รันจากโฟลเดอร์โปรเจกต์ คำสั่ง Bypass มีผลเฉพาะ Process ของ Script

สำหรับเครื่องใหม่ ให้ติดตั้ง Podman และเตรียม Machine ด้วย podman machine init / podman machine start ก่อนใช้ Start-Database.ps1 หรือใช้ PostgreSQL ติดตั้งตรงแล้วกำหนด ConnectionStrings__Database เอง หากมีฐานข้อมูล Docker เดิมอยู่ ต้องสำรองและย้ายข้อมูลก่อนสลับ Engine สคริปต์จะไม่ลบหรือเขียนทับฐานข้อมูลเดิม ปัจจุบันยังไม่ได้ติดตั้ง Podman หรือย้ายฐานข้อมูลของเครื่องนี้

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Database.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Restore.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Migrate.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Setup-Https.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Bootstrap.ps1 -UserName admin -Email admin@example.com
```

เปลี่ยน Email เป็นบัญชีจริงก่อน Bootstrap สคริปต์ให้กรอกรหัสชั่วคราวแบบซ่อนอักขระ 15–128 ตัว ตรวจ HIBP และสร้างบัญชีเฉพาะฐานข้อมูลที่ยังไม่มีผู้ใช้ รหัสอายุ 24 ชั่วโมง บังคับเปลี่ยนก่อนเข้าทำงาน หาก HIBP ไม่พร้อมให้ลองใหม่ภายหลัง

Bootstrap สร้างบริษัท MAIN และ Role ส่วนกลาง ได้แก่ CompanyRegistrar (สิทธิ์ DBRT01), OrganizationAdministrator (สิทธิ์ DBRT02–04 และ DBRT06), SharedMasterAdministrator (สิทธิ์ DBRT05 และ DBRT07), AccountAdministrator (สิทธิ์ SURT01), AccessAdministrator (สิทธิ์ SURT02–04) และ SystemCoordinator (สิทธิ์ SURT05–09) ดู [ADR-078](docs/adr/ADR-078-company-slice.md)

เริ่ม API และ Frontend คนละ Terminal:

ใน Visual Studio ให้เลือก Web เป็น Startup Project และ Profile https แล้วกด Run ได้ โหมด Development จะอ่าน .local/database.env จาก Workspace อัตโนมัติ หากตั้ง ConnectionStrings__Database หรือ ConnectionStrings:Database ไว้แล้ว จะใช้ค่าที่กำหนดก่อนเสมอ โหมดอื่นต้องกำหนด Connection String เอง

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Api.ps1
npm.cmd start --prefix src/Web/spa
```

หน้าเว็บ https://localhost:4200 ใช้ API https://localhost:7240 ทั้งคู่ใช้ ASP.NET Core localhost Development Certificate ที่เชื่อถือในเครื่อง npm start จะเตรียมใบรับรองผ่าน Setup-Https.ps1 อัตโนมัติ หาก Windows ถามให้ยืนยันการเชื่อถือ ใบรับรองและ Private Key ที่ Angular ใช้อยู่ใน .local/https ซึ่ง Git Ignore ไว้ หากเปลี่ยนใบรับรองให้หยุดแล้วเริ่ม API/Frontend และ Browser ใหม่

URL Frontend อยู่ src/Web/spa/public/config.json และ CORS อยู่ src/Web/appsettings.Development.json ต้องกำหนดให้ตรงกัน PostgreSQL เปิดเฉพาะ localhost:55432 รหัสสุ่มอยู่ใน .local/database.env หรือกำหนด ConnectionStrings__Database เอง

หากเครื่องอื่นต้องเชื่อม PostgreSQL ให้ตั้ง Port Binding และ Firewall บนเครื่องที่รัน Container ตาม [การเชื่อมฐานข้อมูลผ่าน LAN](docs/database-lan.md) เครื่องลูกข่ายใช้ Port 55432 และรหัสฐานข้อมูลของเครื่องแม่

## ทดสอบและ Build

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test.ps1
npx.cmd --prefix src/Web/spa playwright install chromium
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Browser.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Export-Migrations.ps1 -VerifyOnTempDb
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Backup-Database.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Restore-Database.ps1 -BackupPath .local/backups/<file>.dump -Drill
```

API Test สร้างฐานข้อมูล clean_test_<random> แยกต่อ Test และลบทิ้งเมื่อจบ ต้องใช้บัญชี PostgreSQL ท้องถิ่นที่มี CREATE DATABASE ชุดทดสอบใช้ Password Screening จำลองเฉพาะ Test DI ระบบที่รันจริงตรวจ HIBP เสมอ

Browser Test ใช้ Chromium, HTTPS API จริง และ PostgreSQL ชั่วคราว โดยจองพอร์ต Localhost ที่ว่างให้อัตโนมัติ (ไม่ชน `ng serve` บนพอร์ต 4200) ตรวจ Login/Shell/Create/Update/Conflict/Status/Hierarchy/SU Operations/Logout, Desktop 1440×1000, Tablet 1024×768, Mobile 390×844 และ axe WCAG 2.1 AA เก็บภาพใน src/Web/spa/test-results ใบรับรอง Test สร้างชั่วคราวในหน่วยความจำ

Build สร้าง API ใน .local/artifacts/api, Migration SQL scripts ใน .local/artifacts/migrations, สำรองข้อมูลใน .local/backups และ Frontend ใน src/Web/spa/dist/clean/browser ยังต้องกำหนด Hosting, Domain จริง, Audit Retention และการตรวจรับขั้นสุดท้ายตาม [คู่มือ Deployment](docs/operations/deployment.md) ก่อน Production

## โครงสร้าง

- src/Domain: Entity กฎข้อมูลองค์กร และทะเบียนโปรแกรม (`ProgramRegistry`)
- src/Application: CQRS, Validation Pipeline, Interface
- src/Infrastructure: DB/SU Context, Identity, Session, Transaction/Audit
- src/Web: API/Authorization/CSRF และ Angular ใน spa
- tests/clean.Tests: Domain/Security/API Integration/Browser Tests

ข้อมูลลับและผล Build อยู่ใน .local ที่ Git Ignore ไว้ ไม่มีรหัสผ่านเริ่มต้นฝังในโปรเจกต์
