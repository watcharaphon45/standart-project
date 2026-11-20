# Production Deployment & Operational Runbook

เอกสารนี้สรุปขั้นตอนเตรียมขึ้นระบบ Production, การตั้งค่า Environment/Secrets, Health & Logging, Migration Dry-Run, Backup/Restore Drill และแนวทาง Audit Retention สำหรับระบบ `clean` ตาม [นโยบายค่าใช้จ่าย](../cost-policy.md) และ [ADR-079](../adr/ADR-079-production-readiness.md)

---

## 1. Build Artifacts

รันคำสั่ง Build หลักเพื่อสร้างไฟล์พร้อม Deploy แยกส่วน API และ Angular SPA:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Build.ps1
```

| ส่วน | ตำแหน่ง Output | หมายเหตุ |
|---|---|---|
| Backend (.NET 10 Web API) | `.local/artifacts/api` | ใช้คู่กับ Environment Variables / Secret Store ของปลายทาง |
| Frontend (Angular 22 SPA) | `src/Web/spa/dist/clean/browser` | Static Assets; แก้ URL ของ API ผ่านไฟล์ `config.json` ได้โดยไม่ต้อง Rebuild |

---

## 2. Environment Variables & Configuration Matrix

ใช้ ASP.NET Core Environment `Production` เสมอ (โค้ด `LocalDevelopmentDatabase` จะไม่โหลด `.local/database.env` นอกโหมด `Development`)

| Config Key / Env Var | ค่าตัวอย่าง / คำอธิบาย | ข้อบังคับใน Production |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` | บังคับ `Production`; เปิด HSTS และบังคับ CORS `https://` |
| `ConnectionStrings__Database` | `Host=<db-host>;Port=5432;Database=<db>;Username=<app_user>;Password=<secret>;SSL Mode=VerifyFull` | เก็บใน Secret Manager / Vault / Service Env เท่านั้น ห้ามลง Git |
| `AllowedHosts` | `api.example.com` | เปลี่ยนจาก `localhost` เป็น Hostname จริงของ API |
| `Cors__AllowedOrigins__0` | `https://app.example.com` | ห้ามใช้ `*`, ห้ามใส่ path/query, ต้องเป็น `https://` ใน non-Development |
| `Cookies__SameSite` | `None` (ข้ามไซต์) หรือ `Lax` (ภายใต้โดเมนแม่เดียวกัน) | ค่าอนุญาต: `None`, `Lax`, `Strict` |
| `ReverseProxy__Enabled` | `true` เมื่อวางหลัง Nginx/Caddy/HAProxy/ALB | ตั้ง `false` หากรับ TLS ตรง |
| `ReverseProxy__ForwardLimit` | `1` (ช่วง `1`–`10`) | จำกัดจำนวน Proxy hop ที่เชื่อถือ |
| `ReverseProxy__KnownProxies__0` | เช่น `10.0.1.10` | ระบุ IP ของ Reverse Proxy จริง ป้องกัน IP/Proto Spoofing |
| `ReverseProxy__KnownNetworks__0` | เช่น `10.0.1.0/24` | ระบุ CIDR ของวง Proxy เฉพาะที่เชื่อถือ |
| `DataProtection__ApplicationName` | `clean` | ใช้ค่าเดียวกันทุก Instance |
| `DataProtection__KeyDirectory` | เช่น `/var/lib/clean/dpkeys` | จำเป็นเมื่อรันหลาย Instance เพื่อให้ Cookie/Antiforgery ถอดรหัสข้ามเครื่องได้ |

---

## 3. Cookie, CORS และ Browser Compatibility (Cross-Origin vs Same-Site)

- API ตั้ง Cookie `__Host-clean.session` และ `__Host-clean.csrf` ด้วย `Secure=true`, `HttpOnly=true` เสมอ
- **กรณีโดเมนคนละ Root Site** (เช่น SPA `https://erp.company.com` กับ API `https://api.vendor.net`):
  - ใช้ `Cookies__SameSite=None` (ค่าเริ่มต้น) และตรวจเบราว์เซอร์หลักขององค์กร (Edge/Chrome/Firefox/Safari) ว่าไม่บล็อก Third-Party Cookie หรือตั้ง Exception/Same-Site Domain
- **กรณีโดเมนภายใต้ Root Site เดียวกัน** (เช่น SPA `https://app.example.com` กับ API `https://api.example.com`):
  - แนะนำปรับ `Cookies__SameSite=Lax` เพื่อลดการพึ่งพา Third-Party Cookie บนเบราว์เซอร์ที่เข้มงวด (เช่น Safari ITP / Privacy Mode)

---

## 4. SPA Static Hosting & Reverse Proxy Checklist

1. **SPA Fallback Routing:** ตั้งให้ Static Web Server คืน `index.html` (HTTP 200) สำหรับทุก Route ของ Angular ยกเว้นไฟล์ Static จริง (`.js`, `.css`, `.ico`, `.json`) และอย่ากลืน `/api/*` หรือ `/health*`
2. **Cache Control:**
   - ไฟล์ Bundle ที่มี Hash (`main-*.js`, `chunk-*.js`, `*.css`): `Cache-Control: public, max-age=31536000, immutable`
   - `index.html` และ `config.json`: `Cache-Control: no-cache, no-store, must-revalidate` เพื่อไม่ให้ผู้ใช้ติด JS เวอร์ชันเก่าหลัง Deploy
3. **Runtime `config.json`:** ชี้ `{ "apiBaseUrl": "https://<api-domain>" }` โดยไม่มี Credential ใดๆ ใน Frontend
4. **TLS & HSTS:** เปิด TLS 1.2/1.3 และให้ Reverse Proxy ส่ง Header `X-Forwarded-For` และ `X-Forwarded-Proto: https` (หากเปิด `ReverseProxy__Enabled=true`)

---

## 5. Health Probes, Structured Logging & Alerting

### Health Endpoints

| Endpoint | หน้าที่ | Status Code | JSON Response |
|---|---|---|---|
| `GET /health` | Liveness Probe (Process ทำงานอยู่) | `200 OK` | `{"status":"ok"}` |
| `GET /health/ready` | Readiness Probe (ทดลองเชื่อม PostgreSQL `SELECT 1` Timeout 5 วินาที) | `200 OK` หรือ `503 Service Unavailable` | `{"status":"ready","checks":{"database":"ready"}}` หรือ `"unavailable"` |

> `/health` และ `/health/ready` อนุญาต Anonymous (`AllowAnonymous`), ตั้ง `Cache-Control: no-store`, ข้ามการบล็อก `must_change_password` และ **ไม่ส่ง** Connection String, Host, Port, Username, Password หรือ Exception Message กลับใน Response Body

### Structured JSON Logging & Correlation (`TraceId`)

- ในโหมด `Production` (`src/Web/appsettings.json`) ระบบ Log เป็น JSON พร้อม UTC Timestamp (`yyyy-MM-ddTHH:mm:ss.fffZ`) และ Scope `TraceId`
- Middleware `RequestObservabilityMiddleware` แนบ Header `X-Trace-Id` ในทุก Response (และ Expose ผ่าน CORS `WithExposedHeaders("X-Trace-Id")`) ให้ทีม Support ใช้ค้นร่วมกับ `SuAuditEvent.TraceId`
- Log เฉพาะ HTTP Method, Request Path (ไม่รวม Query String/Body/Cookie/Authorization/CSRF Header), Status Code, ElapsedMs และตัวเลข `UserId` (ไม่ Log Username/Email/Password)
- `HttpClient` ของ `PwnedPasswordScreening` (HIBP) ยังคงปิด Log เพื่อไม่ให้ Hash Prefix รั่วเข้า Log

### Security Headers ที่ API ส่งกลับทุกคำขอ

`Cache-Control: no-store`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer`, `Permissions-Policy: camera=(), microphone=(), geolocation=()`, `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'`

---

## 6. Database Migration Dry-Run & Rollback Plan

ลำดับ Apply Migration ต้องเริ่มจาก `OrganizationDbContext` ก่อน `SystemDbContext` เสมอ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Export-Migrations.ps1 -VerifyOnTempDb
```

- สร้าง Idempotent SQL Script ได้ที่ `.local/artifacts/migrations/01-organization.sql` และ `02-system.sql` พร้อม SHA-256 Checksum เพื่อส่งให้ DBA ตรวจสอบก่อนรันจริง
- `-VerifyOnTempDb` จะทดสอบรัน SQL ทั้งสองไฟล์ซ้ำ 2 รอบบนฐานข้อมูลชั่วคราวแล้วลบทิ้ง เพื่อยืนยัน Idempotency
- **แนวทาง Rollback:**
  1. ก่อน Migrate Production ให้รัน Backup เสมอ
  2. หาก Migration ล้มเหลวระหว่างรันหรือมีปัญหา Functional ร้ายแรงหลัง Deploy: หยุดทราฟฟิก API ชั่วคราว → Restore จากไฟล์ Backup ล่าสุดก่อน Migrate (`Restore-Database.ps1`) → ถอย Binary API กลับเวอร์ชันก่อนหน้า หรือทำ Forward-fix Migration เมื่อมีการบันทึกข้อมูลใหม่ที่ห้ามสูญเสีย

---

## 7. Backup & Restore Drill SOP

### สร้างไฟล์สำรอง (`.dump` + `.sha256`)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Backup-Database.ps1
```

### ซ้อมกู้คืนฐานข้อมูลบนฐานชั่วคราว (Restore Drill)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Restore-Database.ps1 -BackupPath .local/backups/<file>.dump -Drill
```

สคริปต์จะตรวจ SHA-256 Checksum ก่อน จากนั้นสร้างฐานข้อมูลชั่วคราว `clean_restore_drill_*`, กู้คืนด้วย `pg_restore --exit-on-error`, ตรวจจำนวนแถวของตารางหลัก (`__DbMigrationsHistory`, `__SuMigrationsHistory`, `DbCompany`, `SuUser`, `SuAuditEvent`) แล้วลบฐานชั่วคราวทิ้งอัตโนมัติ (หากต้องการกู้คืนทับฐานจริง ต้องระบุ `-TargetDatabase <name> -Force` เท่านั้น)

---

## 8. Audit Retention Policy Baseline

ตาราง `SuAuditEvent` เก็บเหตุการณ์ทางธุรกิจและ Security แบบ Append-only (ไม่มี Delete ผ่าน UI/API) ก่อนขึ้น Production องค์กรควรอนุมัตินโยบายจัดเก็บอย่างเป็นทางการ ตัวอย่างแนวทางเริ่มต้น:

| ระดับข้อมูล | ระยะแนะนำใน `SuAuditEvent` (Hot DB) | แนวทางหลังครบกำหนด |
|---|---|---|
| เหตุการณ์ทั่วไป (`DBRT01`–`07`, `SURT05`–`08`) | อย่างน้อย 1–2 ปี (ตามข้อกำหนดองค์กร/กฎหมาย) | Export เป็นไฟล์ JSON/CSV หรือ Partition Archive พร้อม Checksum เก็บใน Cold Storage ก่อน Purge |
| เหตุการณ์สิทธิ์/บัญชี (`AUTH`, `BOOTSTRAP`, `SURT01`–`04`) | อย่างน้อย 2–7 ปี | แยก Archive ที่เข้ารหัสและตรวจสิทธิ์เข้าถึงก่อนลบข้อมูลเก่า |

**กฎความปลอดภัย:** ห้ามลบข้อมูลใน `SuAuditEvent` จนกว่าจะ Export ลงสื่อจัดเก็บถาวรและตรวจ Hash ยืนยันความครบถ้วนแล้วเท่านั้น

---

## 9. Pre-Go-Live Sign-Off Checklist

- [ ] เลือก Hosting, ตั้งโดเมนจริง (`AllowedHosts`, `Cors:AllowedOrigins`, `config.json`) และเปิด TLS + HSTS
- [ ] ยืนยัน `Cookies:SameSite` (`None` หรือ `Lax`) ด้วยการ Login/CSRF/Refresh จริงบน Browser ขององค์กร
- [ ] ตั้ง Reverse Proxy Forwarded Headers (`ReverseProxy:*`) และ Data Protection `KeyDirectory` (หากมี >1 Instance)
- [ ] ซ้อม `Export-Migrations.ps1` และ `Restore-Database.ps1 -Drill` ผ่านบน Environment เป้าหมาย
- [ ] รัน Bootstrap บัญชีผู้ดูแลแรก (`scripts/Bootstrap.ps1`) ด้วยรหัสชั่วคราวจริง (ห้ามใช้บัญชีตัวอย่าง) และเปลี่ยนรหัสผ่านทันที
- [ ] ตรวจ Screen Reader จริง (NVDA/Narrator/VoiceOver) และ Load/Stress Test ตาม Workload องค์กร
- [ ] อนุมัตินโยบาย Audit Retention (`SuAuditEvent`) และตั้ง Alert บน `/health/ready` 503 / Log Level `Error`
