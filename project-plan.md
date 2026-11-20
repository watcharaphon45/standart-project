# Project Plan — clean / Enterprise Web Application Starter

> **มติล่าสุดเรื่องค่าใช้จ่าย:** เลือกเฉพาะ Library/เครื่องมือ/บริการที่ใช้ได้โดยไม่มีค่า License, Subscription หรือค่าตามการใช้งานสำหรับขอบเขตนี้ รวมการใช้งานเชิงพาณิชย์ขององค์กร ไม่ใช้ Trial หรือ Community Tier ที่ต้องผ่านเกณฑ์องค์กร คง MediatR 12.5.0 / Apache-2.0 และตรวจ License ทั้ง Dependency ตรง/ทางอ้อมก่อนเพิ่มหรืออัปเกรด ดู [นโยบายและรายการที่เลือก](docs/cost-policy.md)

> **เริ่มพัฒนาแล้ว — 16 พฤศจิกายน 2026:** ใช้ MediatR 12.5.0 / Apache-2.0 ตามคำตอบล่าสุด ชุดแรกเป็น Login/Session และ DBRT01 Full Vertical Slice ดู [สถานะงานและผลตรวจ](docs/development-status.md), [ADR-077](docs/adr/ADR-077-development-baseline.md) และ [ADR-078](docs/adr/ADR-078-company-slice.md) Checklist ท้ายแผนยังใช้ติดตามขอบเขตทั้งระบบ จึงไม่ถือว่าครบเพียงเพราะชุดแรกผ่าน

> **สถานะเอกสาร:** Architecture & Implementation Blueprint (ฉบับสรุปจากบทสนทนาที่ตกลงกัน)
>
> **วันที่รวบรวม:** 16 พฤศจิกายน 2026  
> **ปรับปรุงการตัดสินใจ:** 16 พฤศจิกายน 2026 — รวมคำตอบที่ผู้ใช้เลือกในบทสนทนา; Solution/Namespace: `clean`
> **ชื่อไฟล์:** `project-plan.md`  
> **ภาษาเอกสาร:** ไทย โดยใช้ชื่อเทคนิคและตัวอย่างโค้ดภาษาอังกฤษ  
> **ขอบเขต:** แผนสำหรับเริ่มต้นพัฒนา Web Application เชิงธุรกิจด้วย Angular + ASP.NET Core; เริ่มระบบด้วย Master กลุ่ม Organization (`DB`) และ System Administration (`SU`)

---

## 0. วิธีอ่านเอกสารและระดับการตัดสินใจ

เอกสารนี้รวบรวมรายละเอียดจากการสนทนาทั้งหมดในชุดนี้ โดย **ไม่ตีความข้อเสนอของผู้ช่วยว่าเป็นสิ่งที่ผู้ใช้ยืนยันโดยอัตโนมัติ** ให้แยกสามสถานะดังนี้:

| สัญลักษณ์ | ความหมาย | วิธีนำไปใช้ |
|---|---|---|
| **[ยืนยัน]** | ผู้ใช้ระบุชัดเจน หรือยอมรับแนวทางที่เสนอ | ถือเป็นข้อกำหนดหลักของงาน |
| **[แนวทางแนะนำ]** | เป็นรายละเอียดออกแบบ/ตัวอย่างที่เสนอ เพื่อให้ลงมือพัฒนาได้ แต่ยังไม่กำหนดตายตัว | นำไปใช้เป็นค่าเริ่มต้น; เปลี่ยนได้เมื่อมีเหตุผล |
| **[รอตัดสินใจ]** | ยังไม่มีคำตอบชัดเจน / มีหลายทางเลือก | ต้องตัดสินใจก่อนทำงานที่พึ่งพาเรื่องนั้น |

**กติกาสำคัญ:** ข้อกำหนดเชิงพฤติกรรม เช่น *ไม่ใช้ Browser Alert*, *ไม่ส่ง ID ผ่าน URL path*, *ใช้ FluentValidation*, *Angular อยู่ `Web/spa`*, *ป้องกันผู้ใช้กดทำรายการซ้ำระหว่างรอ API* ต้องไม่ถูกเปลี่ยนเพียงเพราะ Scaffold หรือ Library ตั้งค่าเริ่มต้นมาอีกแบบหนึ่ง

### 0.1 Executive summary

- **[ยืนยัน]** Backend แบ่งสี่ส่วนหลัก: `Application`, `Domain`, `Infrastructure`, `Web` (Web ทำหน้าที่ API และเป็นที่อยู่ของ Frontend)
- **[ยืนยัน]** Angular SPA อยู่ที่ `Web/spa`; ไฟล์ Component แยก `.ts`, `.html`, `.scss`; มี Feature Routing ชัดเจน และไม่ย่อโค้ดจนอ่าน/บำรุงรักษายาก
- **[ยืนยัน]** Logic ของแต่ละเมนู/Use Case อยู่ใน `Application/Features/<Module>/<Program>/<Operation>` เป็นหลัก
- **[ยืนยัน]** Entity แทนข้อมูลตารางฐานข้อมูล; มี EF Core Entity Configuration และ DbContext สำหรับ Mapping/Access ฐานข้อมูล
- **[ยืนยัน]** Web API รับ Request Model/DTO โดย ID เป็น Field ในข้อมูลที่ส่ง ไม่เป็นส่วนหนึ่งของ Route Path
- **[ยืนยัน]** Backend ใช้ FluentValidation 12.x ผ่าน MediatR Validation Pipeline กลางที่เรียก `ValidateAsync()`; Frontend ใช้ Form Validation ด้วย
- **[ยืนยัน]** Error ที่ผู้ใช้เห็นต้องอ่านเข้าใจ ห้ามส่ง Raw Error/SQL/Stack Trace/DB Error ตรง ๆ; มีระบบ Notification และ Error Handling กลาง
- **[ยืนยัน]** ใช้ Loading/Processing State เพื่อ **ป้องกันการทำรายการซ้ำ** ขณะส่ง/ประมวลผล API โดยเฉพาะ Create/Update/Delete/Approve/Submit
- **[ยืนยัน]** ไม่ใช้ `alert()`, `confirm()` หรือ `prompt()` ของ Browser; ใช้ Central Confirm Modal และ Service กลาง
- **[ยืนยัน]** ทุกหน้าต้อง Responsive และออกแบบตาม Semantic HTML, Accessibility, UX มาตรฐาน; ใช้ **Tailwind CSS 4.x + Angular CDK + Custom UI Components** โดยแยก CSS กลางสำหรับ Tailwind ออกจาก SCSS ของ Component
- **[ยืนยัน]** ออกแบบ Sidebar, Header, Enterprise Data Table แบบ Reusable ใช้งานร่วมกันทุกเมนู
- **[ยืนยัน]** Master กลุ่มเริ่มต้นคือ **Organization Master (Group A)** ใช้ Module `DB`; ระบบบริหารจัดการส่วนกลางใช้ Module `SU`
- **[ยืนยัน]** Program Code รูปแบบ `{MODULE}RT{NN}` (เลข 2 หลัก) เช่น `DBRT01`, `SURT01`; รหัสแทนชื่อโปรแกรมในตัวระบุทางเทคนิค แต่ UI แสดงชื่อที่ผู้ใช้เข้าใจ
- **[ยืนยันล่าสุด]** Login ด้วย **ASP.NET Core Identity + Secure HttpOnly Cookie + Policy-Based Authorization; ไม่ใช้ MFA** และเตรียมรองรับ OpenID Connect/SSO ภายหลัง
- **[ยืนยันล่าสุด]** PostgreSQL ฐานข้อมูลเดียว Schema เดียว; แยก DbContext ตามโมดูล; ชื่อตารางรูปแบบ `DbBranch`, `DbCostCenter`, `SuUser`, `SuUserPermission`
- **[ยืนยันล่าสุด]** ข้อมูลหลักรวมถึง User/Role ใช้ BIGINT ที่ฐานข้อมูลสร้าง; ID แบบ BIGINT ส่งผ่าน JSON เป็น string; Application ใช้ MediatR/CQRS และเข้าถึง EF Core ผ่าน DbContext Interface ใน Application
- **[ยืนยันล่าสุด]** รองรับหลายบริษัท เลือกทำงานทีละบริษัทและสลับได้; Role งานภายในบริษัทเป็นของแต่ละบริษัท และผู้ใช้มีหนึ่ง Role ต่อบริษัท ส่วนสิทธิ์ระบบส่วนกลางแยกขอบเขตตามส่วน 17.4
- **[ยืนยันล่าสุด]** Frontend/API คนละโดเมน; ยังไม่เลือก Hosting; API ใช้ Program Code และยังไม่ใส่ Version ใน URL
- **[ยืนยันล่าสุด]** .NET 10 LTS + EF Core 10 + Npgsql EF Provider 10; Angular 22; PostgreSQL 18 พร้อมแพตช์ที่ออกจริงและเข้ากันได้ภายในรุ่นหลักที่เลือก
- **[รอตัดสินใจ]** เวอร์ชัน/เงื่อนไขแพ็กเกจเสริมและรายละเอียด Data Model/Authorization; Hosting/โดเมนจริง และ Audit Retention ตัดสินใจก่อน Production

### 0.2 วัตถุประสงค์ของโครงการ

สร้างโครงสร้างเริ่มต้นที่นำไปพัฒนา ERP, HRM, CRM, Inventory, Back Office และ SaaS แบบเพิ่มโมดูลได้ โดย:

1. แยก Business Use Case ออกจาก HTTP, Database และหน้าจอ
2. มอบประสบการณ์ใช้งานสม่ำเสมอ มี Validation/Error/Confirm/Loading ทุก Feature
3. รองรับการขยาย Feature โดยไม่สร้างโค้ดซ้ำหรือสร้าง Dependency กลับทิศ
4. มี Program Registry, Role/Permission, Security, Audit และมาตรฐานงานข้อมูลหลัก
5. เริ่มได้ด้วย Monolith ที่เป็นระเบียบ ไม่รีบบังคับ Microservices
6. กำหนดเงื่อนไขตรวจรับที่ทดสอบได้ ไม่ใช้เพียงคำว่า “ดูดี/ปลอดภัย/Responsive”

### 0.3 สิ่งที่ยังไม่ยืนยันหรือเลือกเลื่อนไว้

- ผู้ใช้เลือก .NET 10 LTS, EF Core 10, Npgsql EF Provider 10, Angular 22, PostgreSQL 18, FluentValidation 12.x และ Tailwind 4.x แล้ว; เลือกแพตช์ที่ออกจริงและเข้ากันได้ก่อนติดตั้ง
- MediatR ยืนยันเพิ่มเติมเมื่อเริ่มพัฒนา: ใช้ 12.5.0 / Apache-2.0; วางแผนอัปเกรดภายหลังโดยตรวจ License ใหม่ก่อนเปลี่ยนรุ่น
- Hosting/เครื่องปลายทาง/Cloud และชื่อโดเมนจริงยังไม่เลือก; เตรียม Build สำหรับ Frontend/API แยกกันตามเวอร์ชันที่ยืนยัน
- Audit Retention ยังไม่กำหนด ต้องตัดสินใจก่อน Production; Allowlist ของ Organization Master ยืนยัน Code, Name, ID หน่วยงานแม่ และ IsActive แล้ว ส่วน TaxId/ข้อมูลติดต่อ/ฟิลด์อื่นและข้อมูล SU ต้องกำหนดแยกก่อนเปิดบันทึกค่าจริง
- ขอบเขตผู้ดูแลบัญชี/Role/Permission และ Audit ยืนยันแล้วตามส่วน 0.4/17.4; รายละเอียดชุดสิทธิ์ที่ส่วนกลางอนุญาตให้แต่ละบริษัทมอบต่อและการ Mapping เป็น Policy/Assignment ต้องจัดทำก่อน Implement
- รหัสโปรแกรมยังเป็นเลข 2 หลัก; วิธีรองรับเกิน 99 ตัดสินใจเมื่อใกล้เต็ม
- ยังไม่ใส่ API Version ใน URL; กำหนด Versioning/Compatibility ก่อนเปิดให้ External Clients
- Multi-Tenant SaaS, Microservices, Event Broker และ Advanced Data Grid เป็นส่วนขยายภายหลัง
- ยังไม่เชื่อม CI และไม่ส่ง Email/SMS ในระยะแรก; เตรียมคำสั่ง Build/Test สำหรับรันในเครื่อง
- ชื่อ Solution/Namespace คือ `clean`; ชื่อแสดงบนหน้าเว็บและข้อมูลบริษัทจริงยังไม่กำหนด
- รายละเอียดที่ระบุเป็น `[แนวทางแนะนำ]` ยังเป็นข้อเสนอ เว้นแต่มีคำตอบยืนยันไว้ในส่วน 0.4 หรือ ADR Register

### 0.4 มติจากการเลือกตัวเลือก — 16 พฤศจิกายน 2026

คำตอบล่าสุดของผู้ใช้มีผลแทนข้อกำหนดเดิมที่ขัดกัน โดยเฉพาะการ **ไม่ใช้ MFA** รายการนี้ยืนยันการออกแบบ ไม่ได้หมายความว่าพัฒนาหรือทดสอบแล้ว

| เรื่อง | มติที่ยืนยัน |
|---|---|
| ชื่อโครงการ | Solution และ Namespace ใช้ `clean` |
| Database | PostgreSQL; ฐานข้อมูลเดียว Schema เดียว |
| Context / Table | แยก DbContext ตามโมดูล; ตารางใช้รูปแบบ `DbBranch`, `DbCostCenter`, `SuUser`, `SuUserPermission` |
| Identity Ownership | ตารางบัญชีผู้ใช้/Role ของ ASP.NET Core Identity อยู่ใน SU DbContext เดียวกับข้อมูลระบบ; แยกจาก Context ของโมดูล DB |
| การอ่านข้อมูลข้ามโมดูล | SU อ่านบริษัท/แผนกผ่านบริการอ่านข้อมูลของโมดูล DB เช่น IOrganizationLookup; ไม่เรียก IOrganizationDbContext ตรงจาก Handler ของ SU |
| Primary Key | BIGINT ที่ฐานข้อมูลสร้างสำหรับข้อมูลหลักและ User/Role; แยกจาก Business Code |
| ID ใน API | BIGINT ID ส่งและรับผ่าน JSON เป็น string เช่น `"123"`; Angular ใช้ string และ Backend ตรวจ/แปลงเป็น long |
| รหัสฝ่าย/แผนก | ไม่ซ้ำภายในบริษัท โดยตรวจแยกตามประเภทข้อมูล; ไม่ห้ามรหัสฝ่ายตรงกับรหัสแผนก |
| รูปแบบรหัสข้อมูลหลัก | ยาว 1–50 ตัวอักษรหลังตัดช่องว่างหัวท้ายและแปลงเป็นตัวพิมพ์ใหญ่; ใช้เฉพาะ `A–Z`, `0–9`, `-`, `_` และไม่มีช่องว่างภายใน เช่น ` ab01 ` → `AB01` |
| การแก้รหัสข้อมูลหลัก | แก้ Code ได้เฉพาะก่อนมีข้อมูลอื่นอ้างอิง; เมื่อถูกใช้อ้างอิงแล้วล็อก Code โดย ID คงเดิม และยังตรวจรหัสซ้ำ/บันทึก Audit |
| ชื่อข้อมูลหลัก | ใช้ Name ช่องเดียว บังคับกรอก สูงสุด 200 ตัวอักษร; รองรับชื่อภาษาไทยหรืออังกฤษ ไม่แยกชื่อแปลในระยะแรก |
| รหัสสาขา/ศูนย์ต้นทุน | ไม่ซ้ำภายในบริษัท โดยตรวจแยกตามประเภทข้อมูล; คนละบริษัทใช้รหัสเดียวกันได้ |
| การเลิกใช้ข้อมูลหลักองค์กร | ปิดใช้งานและเปิดกลับได้; ไม่มีลบจริงผ่านหน้าจอ/API รวมถึงรายการที่สร้างผิดหรือยังไม่เคยใช้ |
| ปิดหน่วยงานแม่ | ต้องปิดหน่วยงานลูกก่อน จึงปิดบริษัท/สาขา/ฝ่ายได้; ไม่ปิดลูกอัตโนมัติ |
| สิทธิ์ก่อนปิดหน่วยงาน | Admin ต้องย้ายหรือยกเลิกสิทธิ์ผู้ใช้ที่ผูกตรงกับหน่วยงานก่อน; การปิดหน่วยงานไม่ปิดบัญชีผู้ใช้ทั้งระบบ |
| ย้ายหน่วยงานที่มีรายการอ้างอิง | ระยะแรกไม่รองรับการย้ายฝ่ายข้ามสาขา/แผนกข้ามฝ่ายภายในบริษัทหลังมีรายการธุรกิจอ้างอิง; ใช้หน่วยงานใหม่และรหัสใหม่เมื่อปรับองค์กร |
| Application | MediatR/CQRS; EF Core ผ่าน DbContext Interface ที่กำหนดใน Application |
| MediatR Version / License | ยืนยันเพิ่มเติมก่อนติดตั้ง: 12.5.0 / Apache-2.0 สำหรับเริ่มพัฒนา; วางแผนอัปเกรดภายหลัง |
| Backend Validation | FluentValidation 12.x พร้อมแพ็กเกจ DI รุ่นที่เข้ากันได้; Pipeline กลางผ่าน MediatR เรียก ValidateAsync ก่อน Handler สำหรับทุก Command/Query ที่มี Validator |
| Styling Version / Build | Tailwind 4.x ผ่าน CSS กลาง; SCSS สำหรับสไตล์เฉพาะ Component แยกจากขั้นตอน Tailwind; Component ยังแยก .ts/.html/.scss |
| API | Program Code เช่น `/api/db/dbrt01/search`; ID ใน Body; ยังไม่ใส่ Version |
| บริษัทและสิทธิ์ | หลายบริษัทในระบบเดียว; เลือกบริษัทที่กำลังทำงานและสลับได้; Role/สิทธิ์ต่างกันได้รายบริษัท |
| ขอบเขตข้อมูล | กำหนดได้ระดับบริษัท/สาขา/แผนก; สิทธิ์ระดับสาขาครอบคลุมแผนกใต้สาขา |
| การมอบสิทธิ์ทำรายการ | ผ่าน Role เท่านั้น; ไม่เพิ่ม Permission ให้ผู้ใช้โดยตรงเป็นรายบุคคล |
| เจ้าของชุด Role | Role งานภายในบริษัทแยกตามบริษัท; แก้สิทธิ์ Role ในบริษัท A ไม่กระทบชุด Role ของบริษัท B; Role ผู้ดูแลข้อมูลส่วนกลางแยกต่างหาก |
| จำนวน Role ต่อผู้ใช้ | หนึ่ง Role งานภายในบริษัทต่อผู้ใช้ต่อบริษัท; ผู้ใช้ยังมี Scope หลายสาขา/แผนกภายใต้ Role เดียวกันได้; สิทธิ์ระบบส่วนกลางแยกขอบเขต ไม่เพิ่ม Role งานบริษัทที่สอง |
| ผู้ดูแลบัญชี | ผู้ดูแลบัญชีส่วนกลางเท่านั้นที่สร้าง/แก้ข้อมูล Login/รีเซ็ตรหัส/ปลดล็อก/ปิดบัญชี; สิทธิ์นี้แยกจากผู้ดูแลข้อมูลส่วนกลาง |
| ผู้ดูแลสิทธิ์บริษัท | ผู้ดูแลบริษัทสร้าง/ปรับ Role และกำหนด Role/Scope ได้ภายในบริษัทและขอบเขตที่ส่วนกลางอนุญาต; ห้ามเพิ่มสิทธิ์ตนเองเกินขอบเขตนั้น |
| สิทธิ์ Audit | ให้ VIEW/EXPORT ผ่าน Role บริษัทได้ตามบริษัท/สาขา/แผนกที่ได้รับ; แยกสิทธิ์ดู/ส่งออกและตรวจ Scope ทั้งหน้าจอ/ไฟล์; ข้อมูลบัญชีส่วนกลางและ Login Log เฉพาะผู้มีสิทธิ์ตรวจสอบส่วนกลาง |
| ข้อมูลถูกแก้ไขพร้อมกัน | ตรวจเวอร์ชันและป้องกันเขียนทับ; แจ้งให้โหลดข้อมูลล่าสุดและแก้ใหม่ พร้อมเก็บค่าที่กรอกไว้ดูอ้างอิง; ระยะแรกไม่มีหน้าจอเปรียบเทียบ/รวมการแก้ไข |
| โครงสร้างองค์กร | บริษัท → สาขา → ฝ่าย → แผนก |
| ข้อมูลร่วม | ตำแหน่งงานและหน่วยธุรกิจใช้ร่วมกัน โดย Role ผู้ดูแลข้อมูลส่วนกลางเท่านั้นที่สร้าง/แก้ไข/ปิดใช้งาน; ศูนย์ต้นทุนแยกตามบริษัท |
| Authentication | บัญชีภายในผ่าน ASP.NET Core Identity + Secure Cookie + Policy Authorization; รองรับ SSO ภายหลัง; ไม่ใช้ MFA |
| Login Identifier | Username หรือ Email; แต่ละค่าต้องไม่ซ้ำทั้งระบบ; Username ห้ามมี `@` |
| สร้าง/กู้คืนบัญชี | Admin ตั้งรหัสผ่านชั่วคราวเองตามกฎรหัสผ่าน เพื่อสร้างบัญชีหรือรีเซ็ต; อายุ 24 ชั่วโมงนับจากออก; บังคับเปลี่ยนก่อนใช้ระบบ; ไม่ส่ง Email/SMS |
| ส่งมอบรหัสชั่วคราว | Admin ส่งมอบโดยตรงหลังยืนยันตัวตน ต่อหน้าหรือโทรกลับเบอร์ที่องค์กรยืนยันไว้; ไม่เพิ่มบริการส่งรหัส |
| กฎรหัสผ่าน | 15–128 ตัวอักษร ทั้งรหัสชั่วคราวและรหัสปกติ; รองรับวลี/ช่องว่าง ไม่บังคับผสมชนิดอักขระ และปฏิเสธรหัสที่พบบ่อยหรือทราบว่ารั่วไหล |
| แหล่งตรวจรหัสผ่าน | รายการรหัสพบบ่อยในระบบร่วมกับ HIBP API โดยส่งเฉพาะ Hash Prefix; หากตรวจไม่ได้ให้หยุดการสร้าง/รีเซ็ต/เปลี่ยนรหัสและให้ลองใหม่ ไม่ผูก Login ด้วยรหัสเดิมกับความพร้อมของ HIBP |
| อายุรหัสผ่านปกติ | ไม่บังคับเปลี่ยนตามรอบ; บังคับเปลี่ยนเมื่อมีหลักฐานว่ารหัสถูกเปิดเผย |
| Login Lockout | ผิดติดต่อกัน 5 ครั้ง ล็อก 15 นาที; รวม Username/Email ของบัญชีเดียวกัน; มี Rate Limit เพิ่มเติม และ Admin ปลดล็อกได้ตามสิทธิ์ |
| Session | ไม่ใช้งาน 30 นาที หรือครบ 8 ชั่วโมงนับจาก Login อย่างใดถึงก่อน |
| Session Revocation | เปลี่ยน Role/สิทธิ์ ปิดบัญชี หรือเปลี่ยนรหัสผ่าน มีผลกับ Session เดิมตั้งแต่ API Request ถัดไป; ตรวจฝั่ง Server ทุก Request |
| Audit | เก็บค่าก่อนและหลังเฉพาะฟิลด์ที่อนุญาต พร้อมปกปิดข้อมูลอ่อนไหว; Retention ตัดสินใจก่อน Production |
| Organization Master Audit Allowlist | Code, Name, ID หน่วยงานแม่ตามความสัมพันธ์ของ Entity และ IsActive; TaxId/ข้อมูลติดต่อ/ฟิลด์อื่นต้องกำหนดแยกก่อนเพิ่มเข้า Allowlist |
| Business Data / Audit Commit | รายการสร้าง/แก้ไข/ปิดใช้งานที่สำเร็จบันทึกข้อมูลธุรกิจและ Audit ใน Transaction เดียว; สำเร็จพร้อมกันหรือย้อนกลับทั้งหมด; ไม่รวม Log ของ Login/ความพยายามที่ล้มเหลว |
| Frontend/API | คนละโดเมน; ต้องออกแบบ CORS, Cookie และ CSRF ให้รองรับ; Hosting ยังไม่เลือก |
| Program Code | คง `DBRT01` / `SURT01`; เรื่องเกิน 99 ตัดสินใจเมื่อใกล้เต็ม |
| เวอร์ชันเทคโนโลยี | .NET 10 LTS + EF Core 10 + Npgsql EF Provider 10; Angular 22; PostgreSQL 18; ใช้แพตช์ที่ออกจริงและเข้ากันได้ภายในรุ่นหลักที่เลือก |
| Build/Test/CI | เตรียมคำสั่งรันในเครื่อง; ยังไม่เชื่อม CI หรือ Deploy อัตโนมัติ |

### 0.5 เวอร์ชันที่ยืนยันเพิ่มเติม — 16 พฤศจิกายน 2026

- Backend: .NET 10 LTS / ASP.NET Core 10, EF Core 10 และ Npgsql EF Provider 10; ไม่ใช้ Provider 11 รุ่นก่อนออกจริง
- Frontend: Angular 22 พร้อม CLI/CDK ที่เข้ากันได้; เลือก TypeScript ในช่วงที่ Angular 22 รองรับ
- Database: PostgreSQL 18 ใช้แพตช์ล่าสุดของสาย 18 ณ วันที่ติดตั้ง
- Validation: FluentValidation 12.x พร้อม `FluentValidation.DependencyInjectionExtensions` รุ่นที่เข้ากันได้; เรียก `ValidateAsync()` ผ่าน MediatR Pipeline กลางก่อน Handler
- Styling: Tailwind CSS 4.x พร้อม PostCSS Integration ที่เข้ากันได้; ใช้ CSS กลางสำหรับ Tailwind และแยก Component SCSS ออกจากขั้นตอน Tailwind
- MediatR: ผู้ใช้ยืนยัน 12.5.0 / Apache-2.0 เมื่อเริ่มพัฒนา; คง CQRS และ Validation Pipeline กลาง
- ทุกชุดใช้รุ่นที่ออกจริง; เลือกและล็อกแพตช์ที่เข้ากันได้ก่อน Restore/Build และบันทึกเวอร์ชันที่ใช้จริงใน Repository
- ข้อมูลประกอบการเลือกตรวจจาก [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [Angular compatibility](https://angular.dev/reference/versions), [Npgsql EF Provider 10](https://www.npgsql.org/efcore/release-notes/10.0.html) และ [PostgreSQL version policy](https://www.postgresql.org/support/versioning/)
- แพ็กเกจเสริมที่ยังไม่ระบุรุ่นไม่ถือว่าผู้ใช้อนุมัติรุ่นล่าสุดโดยอัตโนมัติ; เสนอรุ่น/เงื่อนไขที่เกี่ยวข้องก่อนติดตั้ง

---

## 1. ขอบเขตผลิตภัณฑ์และแนวทาง Architecture

### 1.1 กลุ่มผลิตภัณฑ์ที่ใช้ร่วมกับ Template ได้

| ประเภท | ความเหมาะสมเชิงแนวคิด | ข้อที่ต้องเพิ่มตามธุรกิจ |
|---|---|---|
| ERP / Accounting / Fixed Assets | สูงมาก | Transaction, Period Close, Audit, Domain Rules |
| HRM / HRIS | สูงมาก | Employee, Leave, Payroll, Privacy |
| CRM / Sales Back Office | สูงมาก | Customer, Sales Pipeline, Activities |
| Inventory / WMS | สูงมาก | Stock Concurrency, Warehouse, Barcode |
| Admin/Management Dashboard | สูงมาก | Reporting, Aggregation |
| Multi-Tenant SaaS | สูง แต่เป็นส่วนขยาย | Tenant Isolation, Provisioning, Subscription/Billing |
| Booking / Queue Management | สูง | Calendar/Capacity/Realtime ตามโจทย์ |
| LIS / HIS ภายในองค์กร | ปรับใช้ได้ | Privacy, Medical Data Protection, Integrations |
| Landing Page อย่างเดียว | อาจใหญ่เกินจำเป็น | ลด Architecture ลงได้ |
| Realtime Social/Chat หรือ Distributed Compute | ต้องเสริม Architecture | Messaging, Workers, Caching, Streaming |

ขนาดระบบไม่ได้ตัดสินด้วยจำนวนเมนูหรือจำนวนผู้ใช้เพียงตัวเดียว แต่ดู **ความซับซ้อนทางธุรกิจ, จำนวนทีม, รูปแบบ Deploy, คอขวด, Consistency, Availability และค่าใช้จ่ายการดูแล**

### 1.2 Architecture ของโปรเจกต์แรก

**[ยืนยัน] Clean Architecture 4 ส่วนหลัก + Feature-Based Application + Angular SPA ใน Web**

```text
Angular SPA (Web/spa)
    |
    | HTTP JSON + secure authentication cookie
    v
ASP.NET Core Web API (Web/Controllers)
    |
    v
Application/Features (Use Cases, DTOs, Validation, Workflow)
    |                 ^
    | Abstractions    | Implementations registered via DI
    v                 |
Domain            Infrastructure (EF Core / External Services)
                        |
                        v
                   Relational Database
```

> ผังนี้อธิบายการเรียกใช้งาน (Runtime Flow) แบบย่อ ไม่ใช่ทิศทางอ้างอิง Project ทุกลูกศร โดย Source Code Dependency ต้องสอดคล้องกับ Clean Architecture

### 1.3 Project References / Dependency Rule

| Project | อ้างอิง Project ใน Solution ได้ | ไม่ควรทำ |
|---|---|---|
| `Domain` | ปกติไม่ต้องอ้างอิง Layer อื่น | ห้ามอ้างอิง Web/Infrastructure/Application |
| `Application` | `Domain` | ห้ามอ้างอิง `Infrastructure` โดยตรง |
| `Infrastructure` | `Application`, `Domain` ตามความจำเป็น | ไม่วาง Business Workflow ที่ไม่เกี่ยวข้องกับ Persistence |
| `Web` | `Application`, `Infrastructure` เพื่อประกอบ DI | ไม่วาง Business Rule ส่วนใหญ่ใน Controller |
| `Web/spa` | Angular app แยกขอบเขตจาก .NET ผ่าน HTTP | ไม่เรียก DB หรืออิมพอร์ต .NET Entity มาตรง ๆ |

**[แนวทางแนะนำ]** ใช้ `Web` เป็น Composition Root, ทำ Registration ของ Persistence/Identity/Validators/Exception Handling จากชั้นที่เหมาะสม และใช้งาน Application Interface เพื่อลดการผูกกัน

### 1.4 ความต่างของ Architecture ที่เคยพิจารณา

| รูปแบบ | จุดเด่น | ตำแหน่งในแผน |
|---|---|---|
| Layered Architecture | Controller → Service → Repository → DB ตรงไปตรงมา | ทางเลือกเมื่อโปรเจกต์เล็ก แต่ไม่ใช่โครงสร้างหลักที่ตกลง |
| Clean Architecture | แยก Use Case/Domain จาก Infra | **ใช้เป็นโครงสร้างหลัก** |
| Modular Monolith | แยกโมดูลธุรกิจโดย Deploy ร่วม | **ทิศทางขยายที่แนะนำ**, ยังไม่บังคับให้แยก Project ต่อ Module |
| Microservices | Deploy/Scale อิสระราย Service | **ไม่ใช้เป็นค่าเริ่มต้น**; พิจารณาเมื่อมีเหตุผลด้านทีมและการ Scale |
| Event-Driven | สื่อสารแบบ Event, Worker/Async | ใช้เพิ่มเฉพาะกรณี เช่น งานประมวลผลใหญ่หรือ Integration |

Clean Architecture, Modular Monolith และ Event-Driven **ใช้ร่วมกันได้** ไม่ใช่ตัวเลือกที่ขัดกันทั้งหมด

---

## 2. โครงสร้าง Solution/Repository ฉบับอ้างอิง

> **[ยืนยัน]** Solution/Namespace ใช้ `clean`; คงบทบาทของ 4 Layer และ `Web/spa` ตามเดิม ชื่อ Class/Context ในผังเป็นแนวทางออกแบบ; ชื่อตารางใช้ Convention ที่ยืนยันในส่วน 4.4

```text
<solution-root>/
├── clean.sln                          # รูปแบบไฟล์ Solution ตาม SDK ที่ผู้ใช้เลือก
├── README.md
├── project-plan.md
├── .editorconfig
├── .gitignore
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── api/
│   ├── ui-design-system/
│   ├── security/
│   └── operations/
├── tests/
│   ├── Domain.Tests/
│   ├── Application.Tests/
│   ├── Infrastructure.IntegrationTests/
│   ├── Web.ApiTests/
│   └── Web.E2ETests/                   # Test Runner ตามที่เลือก
└── src/
    ├── Domain/
    │   ├── Entities/
    │   │   ├── Organization/
    │   │   │   ├── Company.cs
    │   │   │   ├── Branch.cs
    │   │   │   ├── Department.cs
    │   │   │   ├── Division.cs
    │   │   │   ├── Position.cs
    │   │   │   ├── CostCenter.cs
    │   │   │   └── BusinessUnit.cs
    │   │   └── ...
    │   ├── Enums/
    │   ├── ValueObjects/
    │   ├── DomainEvents/                 # เมื่อมีความจำเป็น
    │   ├── Services/                     # Domain Services / Rules
    │   └── Exceptions/
    │
    ├── Application/
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── ICurrentUserService.cs
    │   │   │   ├── IIdentityService.cs
    │   │   │   ├── IOrganizationDbContext.cs # ชื่อเสนอสำหรับโมดูล DB
    │   │   │   ├── IOrganizationLookup.cs    # Contract อ่านข้อมูล DB สำหรับโมดูลอื่น
    │   │   │   ├── IAuditWriter.cs           # ชื่อเสนอ; เข้าร่วม Transaction ของ Use Case
    │   │   │   └── ISystemDbContext.cs       # ชื่อเสนอสำหรับโมดูล SU
    │   │   ├── Exceptions/
    │   │   │   └── BusinessRuleException.cs
    │   │   ├── Security/
    │   │   ├── Models/
    │   │   └── Behaviors/                 # MediatR Pipeline ตามแนวทางที่ออกแบบ
    │   └── Features/
    │       ├── DB/
    │       │   ├── DBRT01/                # Company
    │       │   │   ├── CreateCompany/
    │       │   │   │   ├── CreateCompanyRequest.cs
    │       │   │   │   ├── CreateCompanyValidator.cs
    │       │   │   │   └── CreateCompanyHandler.cs
    │       │   │   ├── UpdateCompany/
    │       │   │   ├── SearchCompanies/
    │       │   │   ├── GetCompanyDetail/
    │       │   │   └── ChangeCompanyStatus/
    │       │   ├── DBRT02/                # Branch
    │       │   ├── DBRT03/                # Department
    │       │   ├── DBRT04/                # Division
    │       │   ├── DBRT05/                # Position
    │       │   ├── DBRT06/                # Cost Center
    │       │   └── DBRT07/                # Business Unit
    │       └── SU/
    │           ├── SURT01/                # User Management
    │           ├── SURT02/                # Role Management
    │           ├── SURT03/                # Permission Management
    │           ├── SURT04/                # User Department Mapping
    │           ├── SURT05/                # Menu Configuration
    │           ├── SURT06/                # Document Numbering
    │           ├── SURT07/                # Approval Workflow
    │           ├── SURT08/                # System Settings
    │           └── SURT09/                # Audit Log
    │
    ├── Infrastructure/
    │   ├── Persistence/
    │   │   ├── OrganizationDbContext.cs   # โมดูล DB
    │   │   ├── SystemDbContext.cs         # โมดูล SU รวม Identity User/Role/ตารางที่เกี่ยวข้อง
    │   │   ├── Configurations/
    │   │   │   ├── Organization/
    │   │   │   │   ├── CompanyConfiguration.cs
    │   │   │   │   ├── BranchConfiguration.cs
    │   │   │   │   └── ...
    │   │   │   └── ...
    │   │   └── Migrations/                # แยกตาม Context และแยก History Table
    │   ├── Identity/
    │   │   ├── ApplicationUser.cs
    │   │   ├── ApplicationRole.cs        # ชื่อเสนอ; Identity ใช้ SystemDbContext
    │   │   └── IdentityService.cs
    │   ├── Services/
    │   ├── Integrations/
    │   └── DependencyInjection.cs
    │
    └── Web/
        ├── Controllers/
        │   ├── DB/
        │   │   ├── DBRT01Controller.cs
        │   │   ├── DBRT02Controller.cs
        │   │   └── ...
        │   ├── SU/
        │   │   ├── SURT01Controller.cs
        │   │   └── ...
        │   └── AuthController.cs
        ├── ExceptionHandling/
        │   └── GlobalExceptionHandler.cs
        ├── Middleware/
        ├── Authorization/
        ├── spa/                           # Angular source (ชื่อยืนยัน)
        ├── Program.cs
        └── appsettings.json
```

**ข้อควรจำ:** ชื่อ Program Code (`DBRT01`) เป็นตัวระบุเมนูและตำแหน่ง Feature ที่ค้นหาได้ง่าย ส่วน Class และ Method ใช้ชื่อที่สื่อความหมาย (`CreateCompanyRequest`) ไม่ควรแทนชื่อทุกตัวด้วยรหัสที่อ่านไม่รู้เรื่อง

---

## 3. Module & Program Naming Convention [ยืนยัน]

### 3.1 รูปแบบรหัส

```text
{MODULE}{TYPE}{SEQUENCE}
  DB      RT       01   => DBRT01
  SU      RT       01   => SURT01
```

- `MODULE`: รหัสโมดูล เช่น `DB`, `SU`
- `TYPE`: `RT` สำหรับรายการเมนูเริ่มต้นที่กำหนดชุดนี้ (ใช้เป็น **Program Type ตาม Convention ของระบบ**)
- `SEQUENCE`: เลข 2 หลัก เริ่มจาก `01`, เพิ่มภายในแต่ละชุด `Module + Type`
- Pattern ทางเทคนิคของชุดปัจจุบัน: `^(DB|SU)RT\d{2}$`; เมื่อเพิ่มโมดูลใหม่ให้ขยาย Registry ไม่ hardcode Regex นี้ไว้ถาวร
- **ห้ามนำ Program Code เดิมที่เคยใช้งานไปใช้กับเมนูคนละความหมาย** เพื่อรักษา Permission/Audit History
- Program Code ต้อง Unique ใน Registry
- รหัสเมนูควรเป็น Stable Identifier; Display Name สามารถแปลภาษา/เปลี่ยนภายหลังได้
- `DB` ในที่นี้คือ **Module Code ของ Organization Master** ไม่ได้หมายถึง Database หรือ DbContext
- `RT` ในชุด SU ใช้เป็น **รหัสประเภทของหน้าจอ/โปรแกรมตามที่ตกลง** แม้เนื้อหาบางหน้าไม่ใช่ Master CRUD เช่น Audit Log
- **[ยืนยันให้เลื่อนการตัดสินใจ]** คงเลข 2 หลัก; เลือกวิธีรองรับเมื่อ Sequence ใกล้เต็ม 99 ก่อนใช้รหัสที่เกิน Convention

### 3.2 Program Registry — DB: Organization Master (Group A) [ยืนยัน]

| Code | Program / Menu | ชื่อไทยตัวอย่าง | หน้าที่ |
|---|---|---|---|
| **DBRT01** | Company | ข้อมูลบริษัท | จัดการบริษัท |
| **DBRT02** | Branch | ข้อมูลสาขา | จัดการสาขาภายใต้บริษัท |
| **DBRT03** | Department | ข้อมูลแผนก | จัดการแผนก |
| **DBRT04** | Division | ข้อมูลฝ่าย/หน่วยงาน | จัดการโครงสร้างฝ่าย/Division |
| **DBRT05** | Position | ข้อมูลตำแหน่งงาน | จัดการตำแหน่ง |
| **DBRT06** | Cost Center | ข้อมูลศูนย์ต้นทุน | จัดการศูนย์ต้นทุน |
| **DBRT07** | Business Unit | ข้อมูลหน่วยธุรกิจ | จัดการหน่วยธุรกิจ |

**ขอบเขตเริ่มต้นของ Business Master = Group A เท่านั้น**: Customer, Supplier, Product, Warehouse, Currency, UOM, Asset Category, Chart of Accounts และข้อมูลอื่นที่เคยเปรียบเทียบเป็นกลุ่ม B–F **ยังไม่อยู่ใน Initial Scope** ให้เพิ่มเป็น Optional Modules เมื่อมีโจทย์จริง

### 3.3 Program Registry — SU: System Administration [ยืนยัน]

| Code | Program / Menu | ชื่อไทยตัวอย่าง | หน้าที่ |
|---|---|---|---|
| **SURT01** | User Management | จัดการผู้ใช้งาน | User, Account Status, Assignment |
| **SURT02** | Role Management | จัดการบทบาท | Role & Assignment |
| **SURT03** | Permission Management | จัดการสิทธิ์ | Program/Action Permissions |
| **SURT04** | User Department Mapping | กำหนดแผนกผู้ใช้งาน | User ↔ Department Scope |
| **SURT05** | Menu Configuration | ตั้งค่าเมนู | Program Registry, Navigation, Visibility |
| **SURT06** | Document Numbering | เลขที่เอกสาร | Document Number Rules/Sequences |
| **SURT07** | Approval Workflow | ขั้นตอนอนุมัติ | Approval Steps & Roles |
| **SURT08** | System Settings | ตั้งค่าระบบ | Application Configuration ที่แก้ได้ตามสิทธิ์ |
| **SURT09** | Audit Log | ประวัติการใช้งาน | อ่าน/ค้น/ส่งออก Audit Log ตามสิทธิ์ |

**รวม 16 เมนูเริ่มต้น**: DB 7 + SU 9. ไม่บังคับ CRUD ทุกเมนู; `SURT09` ควรเป็น Read-Only/Export สำหรับผู้มีสิทธิ์ และระบบบางหน้าควรเปิดเฉพาะ Admin

### 3.4 Permission & Navigation Contract

```text
DBRT01.VIEW
DBRT01.CREATE
DBRT01.UPDATE
DBRT01.ACTIVATE
DBRT01.DEACTIVATE
DBRT01.EXPORT        # ถ้ารองรับ

SURT01.VIEW
SURT01.CREATE
SURT01.UPDATE
SURT01.DISABLE       # หากแตกต่างจาก DELETE

SURT09.VIEW
SURT09.EXPORT
```

- Backend ต้องเป็นตัวตัดสินสิทธิ์จริง; การซ่อนเมนูใน Angular เป็นเพียง UX
- Permission ตรวจทั้งระดับ **Program/Action** และ **Data Scope**; Role/สิทธิ์ของผู้ใช้ต่างกันได้ในแต่ละบริษัท และต้องใช้บริบทบริษัทที่กำลังทำงาน
- **[ยืนยัน]** มอบ Program/Action Permissions ผ่าน Role เท่านั้น; รองรับขอบเขตข้อมูลระดับบริษัท/สาขา/แผนก และไม่มอบ Action Permission โดยตรงให้ผู้ใช้รายบุคคล
- Navigation Registry ควรมี: `moduleCode`, `programCode`, `displayName`, `route`, `icon`, `parentCode`, `sortOrder`, `isActive`, `requiredPermission`
- Route UI ตัวอย่าง `/db/dbrt01`, `/su/surt01`; **[แนวทางแนะนำ]** ไม่ได้บังคับให้ URL ทุกตัวต้องเป็น Code ตลอดไป หากเปลี่ยนต้องยังคง Program Registry ที่เสถียร
- โปรแกรม Login เป็นหน้า Public Entry/Authentication ไม่ต้องใช้ `SURTxx` เพียงเพื่อให้เปิดหน้าได้

### 3.5 ตัวอย่าง Menu Configuration

```json
{
  "moduleCode": "DB",
  "programCode": "DBRT01",
  "programName": "Company",
  "displayName": "ข้อมูลบริษัท",
  "route": "/db/dbrt01",
  "isActive": true,
  "requiredPermission": "DBRT01.VIEW"
}
```

---

## 4. Domain, Application และ Data Access

### 4.1 Domain Layer

**[ยืนยัน]** มี `Entities` แทนตาราง/ข้อมูลธุรกิจเป็นแกนแรกของการออกแบบเดิม

**[แนวทางแนะนำ]** แยกให้ถูกต้องระหว่าง:

- Domain Entity ที่มี Identity, State, Invariants และอาจมี Behavior/Business Rules
- EF Core Configuration ที่ทำ Mapping ของ Entity ไปสู่ Table/Column/Key/Index/Relationship (อยู่ Infrastructure)
- Request/Response DTO สำหรับ API (ไม่ใช่ EF Entity)
- Read Model หรือ Persistence Model แยกต่างหากเมื่อความซับซ้อนจำเป็นจริง

ตัวอย่าง Domain Business Rule เช่น สินทรัพย์ที่จำหน่ายแล้วห้ามโอน; Logic อย่างคำนวณค่าเสื่อมราคาอาจไปใน Domain Service หากใช้ซ้ำหลาย Use Cases ขณะที่ขั้นตอน Query/Validate/Commit เป็น Application Workflow

> ห้ามเหมารวมว่า Logic ทุกบรรทัดต้องอยู่ Domain หรือทุก Logic ต้องอยู่ Application; พิจารณาความหมายและการนำกลับใช้ซ้ำ

### 4.2 Application Layer

**[ยืนยัน]** จัด Feature ตามเมนู/งาน เช่น `Application/Features/DB/DBRT01/CreateCompany/` และแยก Operation สำหรับ Create, Update, Search, Detail, Activate/Deactivate

หลักการ:

1. หนึ่ง Use Case มี Request/Response ของตนเอง
2. มี FluentValidator ของ Request ที่รับเข้าจาก API
3. Application เป็นผู้ประสาน Domain Rules, Persistence Abstraction, Transaction, Audit
4. Controller ไม่ควร Query DB หรือทำกฎธุรกิจที่ซับซ้อนเอง
5. Query ใช้ Projection ออก DTO; Command มี Save/Transaction เมื่อเปลี่ยนข้อมูล
6. งานที่ล้มเหลวจาก Business Rule แจ้งเป็น Error ที่มี Code ไม่ส่ง Raw Technical Exception
7. จัดการ Cancellation Token ต่อเนื่องจาก API ถึง DB เมื่อเหมาะสม

**[ยืนยัน]** ใช้ **MediatR/CQRS** แยก Command/Query และ Handler ตาม Use Case; Handler เข้าถึง EF Core ผ่าน DbContext Interface ที่ประกาศใน Application แยกตามโมดูล

### 4.3 Infrastructure: Entity Configuration + DbContext [ยืนยัน]

```csharp
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("DbCompany");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
```

```csharp
public sealed class OrganizationDbContext : DbContext, IOrganizationDbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
        : base(options) { }

    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Register เฉพาะ Configurations ของโมดูล DB ไม่ดึง Mapping ของ SU/Identity
        modelBuilder.ApplyConfiguration(new CompanyConfiguration());
    }
}
```

*โค้ดข้างต้นเป็น Reference Pattern ไม่ใช่ Implementation ครบชุด; ข้อมูลหลักใช้ BIGINT ที่ฐานข้อมูลสร้าง (`long` ใน C#) ตามมติล่าสุด ส่วน Fields/Indexes ที่เหลือต้องออกแบบต่อ*

**แนวทางเชื่อม DB:**

- Register DbContext ผ่าน Dependency Injection ของ ASP.NET Core
- Connection String อยู่ใน Configuration/Environment Variables/Secret Store ไม่ hardcode Password
- EF Core เป็นตัวอย่าง ORM หลักที่ตรงกับโครงสร้าง Entity + Configurations + DbContext ที่เล่ามา
- สามารถใช้ Dapper/ADO.NET/Stored Procedure หรือ PostgreSQL Functions ในบางงานที่เหมาะสมโดยไม่เปลี่ยน Clean Architecture; แต่ไม่ให้ Service ข้าม Layer ไปใช้ Connection String เองแบบกระจัดกระจาย
- Query ปริมาณมากควรใช้ Projection, Pagination, Index และ `AsNoTracking()` สำหรับ Read-Only ตามความเหมาะสม
- คำสั่งเปลี่ยนข้อมูลต้องมี Transaction Boundary ที่เหมาะกับ Use Case และระวัง Concurrency/Race Conditions

### 4.4 ฐานข้อมูลและ Isolation

**[ยืนยัน]** ใช้ PostgreSQL **ฐานข้อมูลเดียว Schema เดียว และแยก DbContext ตามโมดูล**

- ตารางใช้ PascalCase พร้อม Prefix ตามโมดูล เช่น `DbCompany`, `DbBranch`, `DbCostCenter`, `SuUser`, `SuUserPermission`
- `SuUserPermission` เป็นตัวอย่างรูปแบบชื่อจากผู้ใช้ ไม่ใช่มติให้สร้าง Direct User Permission; ตามมติล่าสุดสิทธิ์ทำรายการต้องมาจาก Role เท่านั้น ชื่อตารางจริงสำหรับ Role/Permission/Assignment ให้กำหนดตามความสัมพันธ์นี้
- ชื่อ Schema จริงยังเป็นรายละเอียด Configuration; ไม่แยก Schema ตามโมดูล
- Domain Class อาจใช้ชื่อธุรกิจ เช่น `Company`; EF Configuration ต้อง Map ไปยังชื่อตารางตาม Convention ที่ยืนยัน
- **[ยืนยัน]** ตาราง Identity User/Role และตาราง Identity ที่เกี่ยวข้องอยู่ใน SU DbContext เดียวกับข้อมูลระบบ; ไม่แยก Identity DbContext เพิ่ม ส่วน Organization Master อยู่ใน Context ของโมดูล DB
- **[แนวทางนำไปใช้]** SU Context สืบทอด IdentityDbContext ที่ใช้ User/Role Key แบบ long และใช้ชุด Migration ของ SU; หนึ่งตารางมีเจ้าของ Migration เดียว แม้อยู่ Schema เดียวกัน
- แยก Migrations/History Table ต่อ Context; จำกัดการ Register Entity Configurations ให้ตรงโมดูล
- **[ยืนยัน]** SU อ่านข้อมูลบริษัท/แผนกผ่านบริการอ่านข้อมูลของโมดูล DB เช่น IOrganizationLookup โดยคืน DTO ตาม Contract และตรวจสิทธิ์บริษัท/ขอบเขตข้อมูลฝั่ง Server; ไม่ Inject IOrganizationDbContext ตรงใน Handler ของ SU
- **[ข้อกำหนดการนำไปใช้]** Contract อ่านข้อมูลข้ามโมดูลไม่คืน DbContext, IQueryable หรือ Entity ที่ติดตามการเปลี่ยนแปลงให้ผู้เรียก; รายละเอียดเมธอด/DTO และ FK Ownership ให้กำหนดก่อน Migration ที่เกี่ยวข้อง
- **[ยืนยัน]** ข้อมูลธุรกิจและ Audit ของรายการสร้าง/แก้ไข/ปิดใช้งานที่สำเร็จอยู่ใน Transaction เดียวกัน ตามส่วน 18; Identity ยังคงใช้ตารางชื่อกลุ่ม `Su` ตาม Convention

### 4.5 Master Data Governance

- เก็บ `Id` (Technical PK) แยกจาก `Code` (Business Code) และ `Name` (ชื่อแสดงผล)
- `Code` ไม่ซ้ำตามขอบเขตที่กำหนด เช่น Global หรือ Company Scope
- **[ยืนยัน]** Code ของ Organization Master ยาว 1–50 ตัวอักษรหลังตัดช่องว่างหัวท้ายและแปลงเป็นตัวพิมพ์ใหญ่; อนุญาตเฉพาะ `A–Z`, `0–9`, `-`, `_` ไม่อนุญาตช่องว่างภายในหรือรหัสภาษาไทย; ใช้กฎเดียวกันทุกช่องทางที่รับ Code รวม Create/Update และ Import หากมี
- **[ยืนยัน]** แก้ Code ได้ก่อนมีข้อมูลอื่นอ้างอิงเท่านั้น; หลังถูกใช้อ้างอิงแล้วล็อก Code โดยไม่เปลี่ยน ID ของรายการ และยังตรวจ Unique Scope/Permission/Concurrency กับบันทึก Audit ตามปกติ
- **[ยืนยัน]** ชื่อ Organization Master ใช้ `Name` ช่องเดียว บังคับกรอก สูงสุด 200 ตัวอักษร รองรับภาษาไทยหรืออังกฤษ; ไม่แยก `NameTh`/`NameEn` ในระยะแรก
- **[ข้อกำหนดการนำไปใช้]** ตรวจเงื่อนไขล็อก Code ฝั่ง Server และป้องกัน Race Condition ระหว่างแก้รหัสกับการเพิ่มข้อมูลอ้างอิง; ไม่ปลดล็อกเพียงเพราะข้อมูลที่อ้างอิงถูกปิดใช้งาน
- **[รายละเอียดออกแบบ]** ระบุรายการ Reference ที่ทำให้ล็อก Code เช่น หน่วยงานลูก, Scope Assignment และรายการธุรกิจ; แยก Audit/Technical Logs ออกจากการใช้อ้างอิงทางธุรกิจ เพื่อไม่ให้ Creation Audit ทำให้แก้รหัสก่อนใช้งานไม่ได้
- มี `IsActive`/Status เพื่อเลิกใช้ข้อมูลอย่างปลอดภัย
- **[ยืนยัน]** Organization Master `DBRT01`–`DBRT07` ใช้ Activate/Deactivate เท่านั้น ไม่มี Hard Delete ผ่านหน้าจอ/API แม้รายการยังไม่เคยใช้; คงข้อมูลและความสัมพันธ์เดิมไว้สำหรับอ้างอิงย้อนหลัง
- มี `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` ตามนโยบาย Audit
- เมื่อมีเอกสารธุรกรรมที่ต้องรักษาค่าตามวันที่เกิดรายการ อาจเก็บ Snapshot ของข้อมูลแสดงผลบางส่วน
- Validate ทั้งระดับ UI, FluentValidation, Domain และ DB Constraint ตามหน้าที่
- ไม่ควรเอา Master ซับซ้อนทั้งหมดไปยัดตาราง Generic `MasterType/MasterCode/MasterValue` เดียว
- Lookup Table รวมใช้ได้เฉพาะ Reference List เล็ก ๆ ที่ไม่มีความสัมพันธ์ซับซ้อน
- **[ยืนยัน]** โครงสร้างหลักเป็น `Company → Branch → Division → Department`; Position/BusinessUnit ใช้ร่วมกันทั้งระบบ และ CostCenter อยู่ใต้ Company

### 4.6 Primary Key — BIGINT สำหรับข้อมูลหลักและ User/Role [ยืนยัน]

ข้อมูลหลัก เช่น Company/Branch/Division/Department และ **บัญชีผู้ใช้/Role** ใช้ **BIGINT ที่ฐานข้อมูลสร้าง** และแยกจาก Business Code; ภายใน C# ใช้ `long` ส่วน API ส่งและรับ BIGINT ID เป็น JSON string; ชนิด Key ของตารางอื่นและ Composite Keys ให้กำหนดตามความสัมพันธ์จริง ไม่บังคับเปลี่ยนทุกตาราง Identity เป็น Surrogate Key เดี่ยว

ตารางต่อไปเป็นบริบทของตัวเลือกที่เคยพิจารณา:

| ประเด็น | `int` | `Guid` / UUID |
|---|---|---|
| ขนาด Key | 4 bytes | 16 bytes |
| EF Core Schema Migration | สร้าง/แก้ Schema ได้ง่ายพอ ๆ กัน | สร้าง/แก้ Schema ได้ง่ายพอ ๆ กัน |
| รวมข้อมูลหลายแหล่ง | ต้องแก้ ID Collision/Mapping | โอกาส Collision ต่ำมาก |
| Index / Foreign Key | เล็กกว่า | ใหญ่กว่า |
| Generate ID ที่ Application | ต้องวางวิธีจัดสรร | ทำได้โดยไม่รอ Database |
| Debug ด้วยตา | อ่านง่าย | ยาวกว่า |

- **[ยืนยัน]** ไม่ใช้ GUID/UUID เป็นค่าเริ่มต้นสำหรับข้อมูลหลักในโปรเจกต์นี้; ใช้ BIGINT ตามคำตอบผู้ใช้
- อย่าผสมชนิด Key แบบไร้หลักเกณฑ์; ต้องเขียน ADR เมื่อเลือก
- เปลี่ยน PK จาก INT → GUID หลังใช้งานจริงมีต้นทุนสูง เพราะต้องย้าย PK, FK, Index, Mapping และ API/Code
- ถ้าย้ายข้อมูลระหว่าง DB ต้องวาง ID Mapping, FK Integrity และ Reconciliation Plan; **Schema Migration ≠ Data Migration**

### 4.7 Migration / Seed / DB Changes

- Migration ทุกครั้งต้องเก็บใน Version Control, Review และทดสอบบน DB สำเนาที่เหมาะสม
- เปลี่ยน Schema แบบมีข้อมูลอยู่แล้วต้องรองรับ Backfill, Constraint, Index และ Rollback/Recovery Plan
- ไม่ฝัง Production Connection String ใน Repository
- Seed Program Registry `DBRT01–DBRT07`, `SURT01–SURT09` และ Permission Key ตาม Version
- Seed ค่าเริ่มต้นที่เกี่ยวกับ Security อย่างระมัดระวัง; ห้าม Commit Admin Password
- หากมี Multiple DbContexts ให้แยก Migrations และ History Table/Schema อย่างชัดเจน
- Unique Index และ FK Constraint ต้องอยู่ใน DB จริง ไม่พึ่ง Validation Client/Server เพียงอย่างเดียว

---
## 5. API Design Standard [ยืนยัน]

### 5.1 หลักการส่งข้อมูล

**กติกาที่ผู้ใช้ยืนยัน:** **ไม่ส่ง ID ไปกับ URL Path**; ส่ง ID และข้อมูลประกอบเป็น **Request Model (DTO)** ไปยัง API

```http
# ไม่ใช่รูปแบบที่เลือก
GET /api/companies/1001
PUT /api/companies/1001
DELETE /api/companies/1001

# ตัวอย่างรูปแบบที่เลือก
POST /api/db/dbrt01/detail
Content-Type: application/json

{"id": "<company-id>", "includeBranches": true}
```

แนวปฏิบัติ:

1. Web Controller รับ `[FromBody]` เป็น Request DTO เช่น `GetCompanyDetailRequest`, `UpdateCompanyRequest`
2. Request Model ต้องมี `Id` ที่ต้องใช้ พร้อม Field ที่เกี่ยวข้อง ไม่ผูกกับ Route Parameter ของ ID
3. ใช้ `POST` พร้อม JSON Body สำหรับ Search/Detail ที่ต้องส่ง Search/Filter/ID ตาม Convention นี้
4. ใช้ `POST` สำหรับ Create/Business Actions, `PUT` หรือ `PATCH` พร้อม JSON Body สำหรับ Update ตามความเหมาะสม
5. หากโมดูลอื่นมี Delete ที่ยืนยันให้รองรับ และต้องมี Body หรือส่งเหตุผลประกอบ เลือก `POST /delete` ตาม Convention นี้เพื่อหลีกเลี่ยงข้อจำกัดของ Body บน DELETE ในบางระบบ; Organization Master `DBRT01`–`DBRT07` ไม่มี Delete Endpoint ตามมติล่าสุด
6. `[FromQuery]` สำหรับ GET ที่ไม่มี Sensitive/Complex Body เป็นไปได้เชิง HTTP แต่ **ไม่ใช้ส่ง ID ผ่าน URL Path ตามข้อกำหนดที่ตกลง**
7. ไม่ใช้ EF Core Entity เป็น Request DTO ตรง ๆ เพื่อป้องกัน Over-posting
8. การซ่อน ID ใน Body **ไม่ใช่มาตรการรักษาความลับหรือสิทธิ์**; ต้องมี Authorization + Object-Level Access Check ที่ Backend
9. ระบุ HTTP Method ให้ชัด อย่าเปลี่ยนทุก Operation เป็น POST โดยไม่จำเป็น; บันทึก Trade-off เรื่อง Caching/Semantics

### 5.2 Naming & Endpoint Convention [แนวทางแนะนำ]

| Use Case | Method | ตัวอย่าง Endpoint | Body |
|---|---|---|---|
| Search Company | `POST` | `/api/db/dbrt01/search` | `SearchCompaniesRequest` |
| Get Company Detail | `POST` | `/api/db/dbrt01/detail` | `GetCompanyDetailRequest` |
| Create Company | `POST` | `/api/db/dbrt01/create` | `CreateCompanyRequest` |
| Update Company | `PUT` | `/api/db/dbrt01/update` | `UpdateCompanyRequest` |
| Deactivate Company | `POST` | `/api/db/dbrt01/deactivate` | `ChangeCompanyStatusRequest` |
| Activate Company | `POST` | `/api/db/dbrt01/activate` | `ChangeCompanyStatusRequest` |
| Approve | `POST` | `/api/<module>/<program>/approve` | `<Program>ApproveRequest` |

**[ยืนยัน]** ใช้ Program Code ใน URL เช่น `/api/db/dbrt01/search`; ID อยู่ใน Request Body และยังไม่ใส่ Version ใน URL ส่วนชื่อ Operation/HTTP Method ย่อยในตารางเป็นแนวทางแนะนำ

### 5.3 Request/Response Models

```csharp
public sealed record SearchCompaniesRequest(
    string? Keyword,
    int PageNumber = 1,
    int PageSize = 25,
    string? SortField = "code",
    string? SortDirection = "asc"
);

public sealed record GetCompanyDetailRequest(
    string Id,
    bool IncludeBranches
);

public sealed record UpdateCompanyRequest(
    string Id,
    string Code,
    string Name,
    bool IsActive
);
```

> **[ยืนยัน]** BIGINT ID ใน Request/Response JSON ใช้ string เช่น `{"id":"123"}`; Angular เก็บเป็น string โดยไม่แปลงผ่าน `number` ส่วน Backend ตรวจรูปแบบ/ช่วงค่าด้วย Validation แล้วแปลงเป็น `long` ก่อนใช้กับ Entity/Query ฐานข้อมูล หลักนี้ครอบคลุม Foreign Key IDs ด้วย และไม่เปลี่ยนตัวเลขอย่าง PageSize/TotalCount ให้เป็น string โดยอัตโนมัติ

```json
{
  "keyword": "COMP",
  "pageNumber": 1,
  "pageSize": 25,
  "sortField": "code",
  "sortDirection": "asc"
}
```

**Response ของ Search [แนวทางแนะนำ]**

```json
{
  "items": [
    { "id": "<id>", "code": "COMP01", "name": "Company A", "isActive": true }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 25
}
```

API ที่เปลี่ยนข้อมูลควรตอบ DTO แสดงข้อมูลที่ผู้ใช้ต้องการใช้งานต่อ ไม่จำเป็นต้องส่ง Entity ทั้งก้อน และต้องไม่ส่ง Sensitive Fields (PasswordHash, Tokens, Internal Settings)

### 5.4 ตัวอย่าง Controller / Application Boundary

```csharp
[ApiController]
[Route("api/db/dbrt01")]
[Authorize]
public sealed class DBRT01Controller : ControllerBase
{
    private readonly ISender _sender;

    public DBRT01Controller(ISender sender) => _sender = sender;

    [Authorize(Policy = "DBRT01.VIEW")]
    [HttpPost("detail")]
    public async Task<IActionResult> Detail(
        [FromBody] GetCompanyDetailRequest request,
        CancellationToken cancellationToken)
    {
        // สมมติว่าระบบมี Validation ก่อนเข้า Use Case ตามมาตรฐานที่เลือก
        var query = new GetCompanyDetailQuery(request.Id, request.IncludeBranches);
        var result = await _sender.Send(query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
```

- เปิดใช้ `[ApiController]` สำหรับ Binding/Framework Validation ขั้นต้น
- ตัวอย่างใช้ `ISender` ของ MediatR; `GetCompanyDetailQuery`/Handler/Response ต้องประกาศใน Application พร้อม Validation ก่อนใช้จริง; ตัวอย่าง Query รับ string ID เพื่อตรวจแล้วแปลงเป็น long ใน Handler ก่อนเข้าฐานข้อมูล
- FluentValidation เป็นตัวกำหนด Request Rules; อย่าสับสนกับ C# Compiler หรือ DB Constraint
- Authorization ต้องเกิด **ก่อน** เปิดเผยข้อมูลหรือแก้ไขข้อมูล
- คำตอบข้อผิดพลาดใช้ Error Contract กลางด้านล่าง ไม่สร้าง Message รูปแบบแตกต่างในทุก Controller
- มี Cancellation Token, Correlation/TraceId และ Logging ที่ไม่รั่วข้อมูล

### 5.5 API Hardening [แนวทางแนะนำ]

- จำกัด `PageSize` สูงสุดและขนาด Request Body
- Sorting ใช้ Allowlist ของ Column/Field; ห้ามนำค่าจาก Client ต่อ SQL `ORDER BY` ตรง ๆ
- Filter ตรวจชนิดข้อมูลและ Scope ป้องกัน Query หนักโดยไม่จำเป็น
- พิจารณา Rate Limit แยก Login, Search, Export, Expensive Operations
- Server ต้อง enforce Company/Branch/Department Scope แม้ Client ส่ง ID อื่นมา
- ใช้ HTTPS และ Request/Response Content Type ชัดเจน
- กำหนด API Versioning/Compatibility Strategy ก่อนเผยแพร่ให้ External Clients (**[รอตัดสินใจ]**)

---

## 6. Backend Validation — FluentValidation [ยืนยัน]

### 6.1 กติกา

- Request ที่เข้าจาก API ทุก Write Use Case และ Read Use Case ที่มี Parameter ต้องผ่านกฎตรวจสอบที่เกี่ยวข้องก่อนประมวลผล
- Validator อยู่ **ใกล้ Feature** เช่น `Application/Features/DB/DBRT01/CreateCompany/CreateCompanyValidator.cs`
- Controller ไม่ควรเต็มไปด้วย `if (string.IsNullOrEmpty(...))`
- ใช้ `ValidateAsync()` เมื่อ Validator อาจมี Async Rules; ไม่พึ่ง Sync Auto-Validation ที่ไม่รองรับ Async Rules
- Response Validation แบบ Field-level ต้องมี Mapping ให้ Angular แสดงใต้ Field
- FluentValidation ไม่ใช่เครื่องมือเดียวสำหรับ Business Rules, Authorization หรือ Unique Constraints
- ห้ามตรวจความซ้ำด้วยการ Query ก่อน Insert เพียงขั้นเดียวแล้วถือว่าปลอดภัย: เพิ่ม Unique Index/DB Constraint

### 6.2 ตัวอย่าง Request + Validator

ตัวอย่างนี้รับ `Code` ที่ผ่านการตัดช่องว่างหัวท้ายและแปลงเป็นตัวพิมพ์ใหญ่แล้ว; **[ข้อกำหนดการนำไปใช้]** ให้ Normalize Code ก่อน Validation อย่างสม่ำเสมอทุกช่องทาง แล้วใช้ค่าที่ตรวจผ่านเดียวกันในการตรวจซ้ำและบันทึก ไม่ใช้กฎแปลง Code กับ Name/Username/Email

```csharp
public sealed class CreateCompanyRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("กรุณาระบุรหัสบริษัท")
            .MaximumLength(50).WithMessage("รหัสบริษัทต้องไม่เกิน 50 ตัวอักษร")
            .Matches(@"\A[A-Z0-9_-]+\z").WithMessage("รหัสบริษัทใช้ได้เฉพาะ A–Z, 0–9, - และ _ โดยไม่มีช่องว่างภายใน");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("กรุณาระบุชื่อบริษัท")
            .MaximumLength(200).WithMessage("ชื่อบริษัทต้องไม่เกิน 200 ตัวอักษร");
    }
}
```

### 6.3 จุดเรียก Validation [ยืนยัน: Pipeline กลางผ่าน MediatR]

- **[ยืนยัน]** ใช้ FluentValidation 12.x และแพ็กเกจ DI ที่เข้ากันได้; MediatR Pipeline Behavior เรียก `ValidateAsync()` ก่อนเข้า Handler สำหรับทุก Command/Query ที่มี Validator รองรับทั้งกฎแบบ Sync และ Async
- หากแยก HTTP Request DTO ออกจาก Command/Query ให้ Mapping และ Validation ครอบคลุมข้อมูลทั้งหมดตาม Contract
- Controller ไม่ควรเป็นจุดเดียวที่ค้ำประกัน Validation; Caller อื่นที่ส่งผ่าน MediatR ต้องผ่านกฎเดียวกัน
- ใช้ MediatR 12.5.0 / Apache-2.0 ตามมติเมื่อเริ่มพัฒนา; Registration และ Error Mapping ต้องตรงกับรุ่นนี้ โดยคงพฤติกรรม Pipeline กลาง
- **[ข้อกำหนดการนำไปใช้]** ไม่เรียก Validator ที่ใช้ DbContext instance เดียวกันแบบขนาน; ส่ง CancellationToken ให้ครบ และไม่ใช้ Validation แทน Authorization/Domain Rules/DB Constraints

### 6.4 แยกระดับของ Validation

| ระดับ | ตรวจอะไร | ตัวอย่าง |
|---|---|---|
| Angular Reactive Forms | ให้ Feedback ก่อนส่ง | Required, MaxLength, Pattern, Cross-field |
| FluentValidation | เช็ก Request Contract ฝั่ง Server | Code Required, Length, Range, Date Rules |
| Application / Domain | Business Invariants & Workflow | ห้ามแก้ข้อมูลเมื่อสถานะถูกปิด/อนุมัติแล้ว |
| Authorization | สิทธิ์ Action/Record/Scope | ดูได้แต่แก้ไม่ได้; ไม่อยู่บริษัทนี้ |
| Database | Data Integrity / Concurrency | Unique, FK, Check Constraints, Transaction |

---

## 7. Backend Exception Handling & API Error Contract [ยืนยัน]

### 7.1 เป้าหมาย

- **ไม่แสดง Raw Backend/DB Error** ให้ผู้ใช้ (SQL Exception, Connection String, Stack Trace, Npgsql/MySql Error, File Path ภายใน)
- ข้อผิดพลาดที่คาดการณ์ได้แจ้ง **ประโยคภาษาไทยที่ผู้ใช้เข้าใจและแก้ไขต่อได้**
- มี `GlobalExceptionHandler` ตอบกลับ Error Format สม่ำเสมอ
- จัดเก็บ Technical Diagnostics ภายใน Log พร้อม `traceId`/Correlation ID
- Frontend แยก Field Validation, Business Error, Auth Error, Unexpected Error โดย `code/status` ไม่ใช่อาศัยข้อความภาษาไทยอย่างเดียว

### 7.2 Response Format [แนวทางแนะนำ: RFC 7807/9457 Problem Details]

```json
{
  "type": "about:blank",
  "title": "ไม่สามารถทำรายการได้",
  "status": 409,
  "detail": "ข้อมูลบริษัทนี้ถูกปิดใช้งานแล้ว ไม่สามารถแก้ไขข้อมูลได้",
  "code": "COMPANY_INACTIVE",
  "traceId": "<trace-id>"
}
```

Validation:

```json
{
  "title": "ข้อมูลไม่ถูกต้อง",
  "status": 400,
  "code": "VALIDATION_ERROR",
  "errors": {
    "Code": ["กรุณาระบุรหัสบริษัท"],
    "Name": ["กรุณาระบุชื่อบริษัท"]
  },
  "traceId": "<trace-id>"
}
```

Unexpected Technical Error:

```json
{
  "title": "เกิดข้อผิดพลาดของระบบ",
  "status": 500,
  "detail": "ไม่สามารถดำเนินการได้ในขณะนี้ กรุณาลองใหม่อีกครั้ง",
  "code": "INTERNAL_ERROR",
  "traceId": "<trace-id>"
}
```

### 7.3 Status/Display Policy

| กรณี | Status ที่เสนอ | วิธีแสดง |
|---|---:|---|
| Request Validation | 400 | ข้อความใต้ Field |
| Login Required/Session Expired | 401 | นำกลับ Login หรือ Renewal Flow |
| Permission Denied | 403 | ข้อความไม่มีสิทธิ์ พร้อม Navigation ที่ปลอดภัย |
| ไม่พบข้อมูล | 404 | Page/Section Error, Retry/Back ตามบริบท |
| ข้อมูลซ้ำ/State Conflict | 409 | Notification/Dialog บอกเงื่อนไขธุรกิจ |
| Business Rule ไม่ผ่าน | 422 | บอกเหตุผลที่ผู้ใช้แก้ได้ |
| Unhandled Exception | 500 | ข้อความกลางและรหัสอ้างอิง (ไม่แสดง Stack) |
| Network Unreachable | ไม่มี Response/Angular `status = 0` | ข้อความเชื่อมต่อระบบไม่ได้ |

> 409 และ 422 เป็น Convention ที่เสนอ; ทีมต้องเลือกให้สอดคล้องแล้วใช้แบบเดียวกันตลอด

### 7.4 Exception Classes / Mapping

**[แนวทางแนะนำ]**

```text
Application/Common/Exceptions/
├── BusinessRuleException.cs
├── NotFoundException.cs
└── ConflictException.cs

Web/ExceptionHandling/
└── GlobalExceptionHandler.cs
```

แนวคิด:

- Expected Business Failure อาจใช้ `BusinessRuleException` ที่มี `Code` และ `Message` หรือ Result Pattern; ต้องไม่ใช้ Throw เพื่อควบคุม Every Normal Branch อย่างพร่ำเพรื่อ
- `FluentValidation.ValidationException` → 400 แบบ Field Dictionary
- Error ที่รู้จักและปลอดภัย → แปลงเป็นข้อความผู้ใช้เข้าใจ
- DB Error, Unknown Exception → Log เต็มในระบบภายใน, Response กลาง 500
- แปลง Exception ที่ขอบ Web แทนการ `try/catch` กลับ JSON ด้วยมือทุก Controller
- อย่า Log Password, Authentication Token, Full Sensitive Payload หรือ Personal Data เกินจำเป็น

### 7.5 Acceptance

- [ ] ไม่มี Raw SQL/ORM Exception ใน Browser, Toast, Network Error Detail ที่ผู้ใช้เห็น
- [ ] ทุก Error มี Status, Code และ TraceId ตาม Contract ที่กำหนด
- [ ] FluentValidation Error Map ไป Form Control ถูกต้อง
- [ ] Error ไม่ทำให้ Loading ค้าง
- [ ] Unauthorized กับ Forbidden ต่างกันชัดเจน
- [ ] 500 ไม่คืน `exception.Message` ที่ไม่คาดการณ์
- [ ] Log มีรายละเอียดเพียงพอให้ Developer ติดตาม โดยกำหนดสิทธิ์เข้าถึง Log

---

## 8. Angular Frontend — `Web/spa` [ยืนยัน]

### 8.1 เทคโนโลยี Frontend

| ส่วน | แนวทาง |
|---|---|
| Framework | Angular 22; ใช้ CLI/CDK และ TypeScript ในรุ่นที่เข้ากันได้ |
| Source Root | **`Web/spa`** |
| Component Structure | **แยก `.component.ts`, `.component.html`, `.component.scss`**; ไม่ย่อโค้ดจนอ่านยาก |
| Styling | **Tailwind CSS 4.x + Custom UI Components**; CSS กลางสำหรับ Tailwind และ SCSS เฉพาะ Component |
| Overlay/Accessibility Utility | **Angular CDK** |
| Forms | Reactive Forms |
| Routing | Feature Routes + Lazy Loading |
| API Calls | Angular Services + HttpClient + Interceptors |
| Error UI | Global NotificationService + FormErrorService |
| Confirmation | Central ConfirmDialogService; ห้าม Browser Alert/Confirm/Prompt |
| Loading | Form/Action Processing State ป้องกัน Duplicate Submission; Read Loading เพื่อ Feedback |
| Responsive | Mobile-first, CSS Grid/Flexbox, Responsive Breakpoints/Container Queries ตามความเหมาะสม |

Bootstrap ยังใช้ต่อได้ในโปรเจกต์เดิมหากมีอยู่ แต่ **สำหรับ Template ใหม่เลือก Tailwind เป็นหลัก** ไม่ลง Bootstrap + Angular Material + PrimeNG ทั้งหมดพร้อมกันโดยไม่มีเหตุผล; PrimeNG/Data Grid พิจารณาทีหลังเฉพาะ Feature ที่ซับซ้อน

**[ยืนยัน]** Tailwind 4.x ใช้ CSS กลางที่ผ่าน Tailwind/PostCSS โดยแยกจาก Sass; ไฟล์ `.component.scss` ใช้สไตล์เฉพาะ Component และอ้างอิง Design Tokens ได้ **[แนวทางนำไปใช้]** วาง Tailwind Import/Theme ใน `src/styles.css`, ใช้ Utility Classes ใน HTML และไม่ใส่ Tailwind Import/Directives เช่น `@apply` ใน SCSS ของ Component เพื่อให้ขั้นตอน Build แยกกันชัดเจน

### 8.2 Angular Folder Structure

```text
Web/spa/
├── angular.json
├── package.json
├── tsconfig.json
└── src/
    ├── main.ts
    ├── styles.css                    # Tailwind 4.x entry ผ่าน PostCSS
    ├── styles.scss                   # Optional custom global Sass; ไม่ import Tailwind
    ├── styles/
    │   ├── _tokens.scss
    │   ├── _colors.scss
    │   ├── _typography.scss
    │   ├── _spacing.scss
    │   ├── _utilities.scss
    │   └── _components.scss
    ├── environments/
    └── app/
        ├── core/
        │   ├── guards/
        │   │   ├── auth.guard.ts
        │   │   ├── permission.guard.ts
        │   │   └── unsaved-changes.guard.ts
        │   ├── interceptors/
        │   │   ├── api-error.interceptor.ts
        │   │   ├── loading.interceptor.ts
        │   │   └── auth.interceptor.ts
        │   ├── services/
        │   │   ├── auth.service.ts
        │   │   ├── loading.service.ts
        │   │   ├── notification.service.ts
        │   │   ├── confirm-dialog.service.ts
        │   │   └── current-user.service.ts
        │   ├── navigation/
        │   │   ├── sidebar/
        │   │   │   ├── sidebar.component.ts
        │   │   │   ├── sidebar.component.html
        │   │   │   └── sidebar.component.scss
        │   │   ├── header/
        │   │   │   ├── header.component.ts
        │   │   │   ├── header.component.html
        │   │   │   └── header.component.scss
        │   │   ├── navigation.config.ts
        │   │   └── navigation.service.ts
        │   └── layouts/
        │       └── main-layout/
        │           ├── main-layout.component.ts
        │           ├── main-layout.component.html
        │           └── main-layout.component.scss
        │
        ├── shared/
        │   ├── ui/
        │   │   ├── button/
        │   │   ├── input/
        │   │   ├── select/
        │   │   ├── checkbox/
        │   │   ├── radio/
        │   │   ├── textarea/
        │   │   ├── date-picker/
        │   │   ├── form-field/
        │   │   ├── data-table/
        │   │   ├── table-toolbar/
        │   │   ├── table-pagination/
        │   │   ├── page-header/
        │   │   ├── confirm-dialog/
        │   │   │   ├── confirm-dialog.component.ts
        │   │   │   ├── confirm-dialog.component.html
        │   │   │   ├── confirm-dialog.component.scss
        │   │   │   └── confirm-dialog.model.ts
        │   │   ├── notification/
        │   │   ├── loading/
        │   │   ├── empty-state/
        │   │   └── error-state/
        │   ├── directives/
        │   ├── pipes/
        │   └── services/
        │       └── form-error.service.ts
        │
        ├── features/
        │   ├── auth/
        │   │   ├── login/
        │   │   └── access-denied/
        │   ├── db/
        │   │   ├── dbrt01/
        │   │   │   ├── pages/
        │   │   │   │   ├── company-list/
        │   │   │   │   │   ├── company-list.component.ts
        │   │   │   │   │   ├── company-list.component.html
        │   │   │   │   │   └── company-list.component.scss
        │   │   │   │   ├── company-create/
        │   │   │   │   └── company-detail/
        │   │   │   ├── components/
        │   │   │   │   └── company-form/
        │   │   │   ├── models/
        │   │   │   ├── services/
        │   │   │   │   └── company-api.service.ts
        │   │   │   └── dbrt01.routes.ts
        │   │   ├── dbrt02/
        │   │   ├── dbrt03/
        │   │   ├── dbrt04/
        │   │   ├── dbrt05/
        │   │   ├── dbrt06/
        │   │   └── dbrt07/
        │   └── su/
        │       ├── surt01/
        │       ├── surt02/
        │       ├── surt03/
        │       ├── surt04/
        │       ├── surt05/
        │       ├── surt06/
        │       ├── surt07/
        │       ├── surt08/
        │       └── surt09/
        │
        ├── app.component.ts
        ├── app.component.html
        ├── app.config.ts
        └── app.routes.ts
```

> ให้สร้าง Component สำหรับเฉพาะ Feature เมื่อมีการใช้งานจริง ไม่ต้องสร้างไฟล์ว่างล่วงหน้าทุกเมนู

### 8.3 Component Separation Rule

- `*.component.ts`: State, Event Handlers, DI, ประสาน Service, ไม่เก็บ HTML ยาวใน Decorator
- `*.component.html`: Semantic Markup, Angular Template, Error/Loading/Empty/Content States
- `*.component.scss`: Styles เฉพาะ Component เมื่อ Utility Class อย่างเดียวไม่พอ; ใช้ Design Tokens และแยกจาก Tailwind 4.x ที่อยู่ใน CSS กลาง
- `features/<module>/<program>/services`: API Client ของ Feature
- `features/.../models`: Request/Response/UI Model ไม่ใช้ `any` ไร้เหตุผล
- `shared/ui`: Reusable Presentational Components ไม่ผูกกับ Domain/Module
- `core`: Singleton Services, Global Interceptor/Guard/Layout/Auth/Navigation
- `pages`: Route-level Components; `components`: Subcomponents/Reusable Forms ภายใน Feature

### 8.4 Routing & Lazy Loading

```typescript
export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'db',
        loadChildren: () => import('./features/db/db.routes').then(m => m.DB_ROUTES)
      },
      {
        path: 'su',
        loadChildren: () => import('./features/su/su.routes').then(m => m.SU_ROUTES)
      }
    ]
  },
  { path: 'login', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) },
  { path: '**', redirectTo: '' }
];
```

และใน DB Module:

```typescript
export const DB_ROUTES: Routes = [
  {
    path: 'dbrt01',
    canActivate: [permissionGuard],
    data: { permission: 'DBRT01.VIEW' },
    loadChildren: () => import('./dbrt01/dbrt01.routes').then(m => m.DBRT01_ROUTES)
  }
];
```

- Route หลักแยกจาก Route ของเมนู
- ใช้ Lazy Loading เฉพาะส่วนที่ช่วยลด Initial Bundle; ไม่บังคับ Lazy ทุก Component จนมีต้นทุนเกินจำเป็น
- Route Guard ใช้ช่วย UX แต่ไม่แทน Backend Authorization
- Unsaved Changes Guard ใช้ ConfirmDialogService กลาง (ไม่ใช้ Browser Confirm)
- Browser Back/Refresh/Deep Link ต้องใช้งานได้ตาม Hosting/Server Fallback ที่ตั้งค่า

---
## 9. Frontend Forms, Loading และ Duplicate Submission Protection [ยืนยัน]

### 9.1 ความหมายของ Loading ตามที่ผู้ใช้แก้ไขยืนยัน

**Loading ในงาน Submit มีหน้าที่ป้องกันการทำรายการเดิมซ้ำขณะที่ Frontend กำลังส่ง Request หรือรอ API ตอบกลับ** ไม่ใช่เพียงป้องกันการโหลดหน้า/ดึงข้อมูลซ้ำโดยทั่วไป

แบ่งสถานะอย่างน้อยสองชนิด:

1. `isLoading`: อยู่ระหว่างดึงข้อมูลเพื่อแสดงผล เช่น List, Detail, Search — แสดง Skeleton/Spinner/Empty/Error ตามบริบท
2. `isSubmitting` หรือ `isProcessing`: อยู่ระหว่างบันทึก/แก้ไข/ลบ/อนุมัติ/ส่งอนุมัติ/คำนวณและบันทึก — **Disable Action**, ป้องกันกดซ้ำทางโค้ด, แสดง Processing Feedback

### 9.2 Submit State Machine

```text
Idle
  |
  | User clicks Save
  v
Client Form Validation
  | invalid                     | valid
  v                             v
Show Field Errors           Optional Confirm Dialog
                                | cancel         | confirm
                                v                v
                               Idle          isSubmitting=true
                                               |
                                               v
                                            API Call
                                          /         \
                                       Success     Error
                                         |           |
                                         v           v
                                    Notification  Field/Business/System Error
                                          \        /
                                           v      v
                                     isSubmitting=false
                                              |
                                              v
                                             Idle
```

- **Frontend Guard:** `if (isSubmitting) return;` และ Button `[disabled]="isSubmitting || form.invalid"`
- คำสั่งเดียวกันที่ถูกกระตุ้นผ่านหลายจุดควรใช้ Action Stream/`exhaustMap` หรือ State Gate ที่แชร์ร่วมกัน
- `finalize()` หรือโครงสร้างเทียบเท่าต้องปิดสถานะไม่ว่า Success/Error/Cancel
- ปุ่มที่เกี่ยวข้อง เช่น Confirm/Delete/Submit ควรถูก Disable ตาม Scope ของ Request
- หากกด Close/Back ขณะ Pending API ต้องกำหนด UX ไม่ให้ผู้ใช้เข้าใจผิดว่าทำรายการไม่เกิดขึ้น
- ไม่จำเป็นต้องแสดง Fullscreen Blocker สำหรับ GET ทั่วไปหรือปิดทั้งระบบหากไม่มีเหตุผล

### 9.3 ตัวอย่าง Code Pattern

```typescript
export class CompanyCreateComponent {
  private readonly api = inject(CompanyApiService);
  private readonly notifications = inject(NotificationService);

  isSubmitting = false;
  readonly form = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50)]
    }),
    name: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)]
    })
  });

  save(): void {
    if (this.isSubmitting) return;
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.api.create(this.form.getRawValue()).pipe(
      finalize(() => this.isSubmitting = false)
    ).subscribe({
      next: () => this.notifications.success('บันทึกข้อมูลเรียบร้อยแล้ว'),
      error: error => {
        // 400 Field Errors: ส่งให้ FormErrorService
        // Business/System Errors: ส่งให้ API Interceptor/NotificationService
      }
    });
  }
}
```

**หมายเหตุ:** โค้ดตัวอย่างแสดงโครงสร้างเท่านั้น ไม่ใช่ Implementation เสร็จพร้อม Deploy; `FormErrorService`, Error Mapping และ Async Lifecycle ต้องทำครบในงานจริง

### 9.4 ป้องกันรายการซ้ำระดับ Backend ด้วย

**[แนวทางแนะนำที่สำคัญ]** การ Disable Button ไม่ได้ป้องกันการส่ง Request ซ้ำข้าม Tab, Refresh, Timeout, Proxy Retry หรือการเรียก API โดยตรง

- สำหรับคำสั่งสำคัญ เช่น Create, Approve, Posting, Generate Document, Depreciation Posting ให้พิจารณา **Idempotency Key** (สร้างจาก Client, บันทึก/ตรวจที่ Server)
- มี DB Unique Constraint/Business Key/Transaction และ Concurrency Protection
- หาก HTTP Timeout แต่ DB Commit สำเร็จ ต้องออกแบบ Query Status/Retry ด้วย Key เดิม แทนการสร้างรายการใหม่โดยไม่รู้ผล
- ต้องแยก **Preview/Calculate Only** ออกจาก **Commit/Post Transaction** เมื่อ Process มีผลจริงต่อข้อมูล

---

## 10. Frontend Form Validation & FormErrorService [ยืนยัน]

### 10.1 Required Behavior

- Angular Reactive Forms ตรวจข้อมูลก่อนส่ง API
- Field ที่ Required ต้องแสดง Label และสถานะ Required อย่างเข้าถึงได้
- ผูก Error Message กับ Field ที่ผิด (ไม่แสดงเพียง Toast รวม)
- ใช้ Error Text ที่ผู้ใช้เข้าใจ เป็นภาษาไทยตามการตั้งค่า Locale
- Validation Server เป็นแหล่งตรวจสอบสุดท้ายเสมอ; Client Validation ไม่ใช่มาตรการด้าน Security
- มี Cross-field Validation เช่น Start Date ต้องไม่เกิน End Date, Confirm Password ตรงกัน, Amount สอดคล้องกับ Business Rule
- แสดง Error เมื่อ Touched/Submitted ตาม UX ไม่ทำให้ฟอร์มแดงทั้งหน้าทันทีที่เปิด
- เมื่อ Validation Fail ต้องเลื่อน/ย้าย Focus ไปตำแหน่งที่เหมาะสมหรือ Summary ของข้อผิดพลาดในฟอร์มยาว
- เมื่อข้อมูลแก้ไขแล้วต้องมีวิธี Clear Server Error เก่าที่ไม่สอดคล้อง
- Submit Disabled/Processing ขณะส่ง Request; ไม่ให้เกิด Duplicate Submission

### 10.2 FormErrorService กลาง [แนวทางแนะนำ]

หน้าที่:

1. แปลง Property Name ที่ Backend ส่ง (`Code`, `Name`) ไปยัง Angular Control Name (`code`, `name`) ผ่าน Convention/Map ที่ชัดเจน
2. รับ `errors: Record<string, string[]>` จาก 400 ValidationProblem
3. ใส่ Error ลง Control (`setErrors`) โดยไม่ทำ Validation ภายในของ Angular หาย
4. รองรับ Nested Form/Array Field Mapping ตามรูปแบบมาตรฐาน
5. จัดการข้อความ Field/Group/Global อย่างเหมาะสม
6. ป้องกัน Toast ซ้ำกับข้อความใต้ Field เดียวกัน

### 10.3 ข้อกำหนด Password/Identity Form

- Password Input ต้องแสดง/ซ่อนรหัสผ่านได้โดยไม่เผยแพร่ค่าไป Log
- รองรับ Password Manager และ Autocomplete ตามวัตถุประสงค์ (`username`, `current-password`, `new-password`)
- **[ยืนยัน]** รหัสผ่านยาว 15–128 ตัวอักษร รองรับวลีและช่องว่าง ไม่บังคับผสมตัวพิมพ์ใหญ่/เล็ก/ตัวเลข/สัญลักษณ์; ปฏิเสธรหัสที่พบบ่อยหรือทราบว่ารั่วไหล ใช้กฎเดียวกันกับรหัสชั่วคราวที่ Admin ตั้งและรหัสที่ผู้ใช้ตั้งเอง
- **[ยืนยัน]** ตรวจรายการรหัสพบบ่อยภายในร่วมกับ HIBP API เมื่อสร้าง/รีเซ็ต/เปลี่ยนรหัส; หากบริการตรวจไม่ได้ ให้แจ้งว่าตรวจรหัสผ่านไม่ได้ชั่วคราวและลองใหม่ ไม่บันทึกรหัสใหม่หรือแสดงว่าสำเร็จ และไม่ใช้ HIBP เป็นเงื่อนไขของ Login ด้วยรหัสเดิม
- **[ยืนยัน]** รหัสผ่านปกติไม่หมดอายุตามรอบ; บังคับเปลี่ยนเมื่อมีหลักฐานว่ารหัสถูกเปิดเผย ส่วนรหัสชั่วคราวต้องเปลี่ยนก่อนใช้ระบบและมีอายุ 24 ชั่วโมงตามมติเดิม
- Error Login ต้องไม่บอกว่ามี/ไม่มีบัญชีนี้อยู่จริง
- **[ยืนยัน]** ไม่ใช้ MFA; มี Flow บังคับเปลี่ยนรหัสผ่านชั่วคราวหลัง Admin สร้าง/รีเซ็ตบัญชี พร้อม Accessibility

---

## 11. Central Notification / Error Display [ยืนยัน]

### 11.1 NotificationService

- เป็น Singleton Service กลางที่ทุก Feature เรียกใช้
- ประเภท: `success`, `error`, `warning`, `info` (ใช้เท่าที่จำเป็น)
- UI Component กลางอยู่ `shared/ui/notification/` และ render ที่ Main Layout
- มีมาตรฐาน Title/Message/Icon/Duration/Dismiss ตามประเภทและความรุนแรง
- ข้อความธุรกิจต้องสั้น ตรงประเด็น และบอกผู้ใช้ทำอะไรต่อได้
- ห้ามใช้ Native Browser `alert()` แทน Notification UI
- Toast เหมาะกับ Feedback สั้น ๆ; ข้อผิดพลาดหลาย Field แสดงที่ Form; การสูญเสียข้อมูล/เหตุการณ์สำคัญอาจใช้ Dialog แทน
- ไม่ให้ Error Message หรือ Toast ซ้อนกันจนทับปุ่ม/ข้อความสำคัญ
- จัดการ Accessibility เช่น `aria-live` อย่างเหมาะสม ไม่อ่านประกาศซ้ำพร่ำเพรื่อ

### 11.2 HTTP Error Interceptor

หน้าที่:

1. รับ `HttpErrorResponse` จาก HttpClient
2. แยก `status === 0` (network), 400 + `errors` (Field), 401/403 (Auth), 409/422 (Business), 500 (System)
3. รับ `code`, `detail`, `traceId` จาก API Contract
4. แสดงข้อความที่ Backend คัดกรองมาแล้ว หรือ Fallback ฝั่ง Frontend ที่เป็นมิตร
5. ส่ง Validation Errors ต่อให้ FormErrorService; ไม่แสดง Toast ที่ซ้ำกับ Field Errors
6. สามารถกำหนด Error Handling ต่อ Request ใน Feature ที่ต้องแสดงรูปแบบเฉพาะได้ โดยไม่ให้ทุก Interceptor กลืน Error จน Feature จัดการไม่ได้
7. Throw/Propagate Error ต่อไปให้ Caller เมื่อเหมาะสม เพื่อให้ Lifecycle เช่น `finalize()` ทำงานถูกต้อง

### 11.3 Message Catalog [แนวทางแนะนำ]

```text
COMMON.SAVE_SUCCESS       = "บันทึกข้อมูลเรียบร้อยแล้ว"
COMMON.DELETE_SUCCESS     = "ลบข้อมูลเรียบร้อยแล้ว"
COMMON.PERMISSION_DENIED  = "คุณไม่มีสิทธิ์ทำรายการนี้"
COMMON.NETWORK_ERROR      = "ไม่สามารถเชื่อมต่อกับระบบได้ กรุณาลองใหม่อีกครั้ง"
COMMON.SYSTEM_ERROR       = "ระบบขัดข้อง กรุณาลองใหม่อีกครั้ง"
DBRT01.COMPANY_INACTIVE   = "บริษัทนี้ถูกปิดใช้งานแล้ว ไม่สามารถแก้ไขข้อมูลได้"
```

- Error Code แยกจากข้อความเพื่อรองรับ Localization ในอนาคต
- หลีกเลี่ยงการทำ Message Catalog ที่ซับซ้อนเกินไปก่อนมีหลายภาษา

---

## 12. Central Confirm Modal — No Browser Alerts [ยืนยัน]

### 12.1 ข้อห้ามชัดเจน

- **ห้ามใช้** `window.alert()`
- **ห้ามใช้** `window.confirm()`
- **ห้ามใช้** `window.prompt()`
- ห้ามใช้ `alert()`, `confirm()`, `prompt()` แบบ Global ที่แมปไป Browser Dialog ใน Application UI
- ทุกการยืนยันใช้ `ConfirmDialogService` + `ConfirmDialogComponent` กลาง
- หาก Developer ไม่กำหนด Title/Button Text ให้ใช้ **ค่า Default ของ Modal กลาง** ไม่ fallback เป็น Browser Alert/Confirm

### 12.2 การออกแบบ Component

```text
Web/spa/src/app/
├── core/services/confirm-dialog.service.ts
└── shared/ui/confirm-dialog/
    ├── confirm-dialog.component.ts
    ├── confirm-dialog.component.html
    ├── confirm-dialog.component.scss
    └── confirm-dialog.model.ts
```

**[แนวทางแนะนำ]** ใช้ Angular CDK Dialog เป็นตัวจัด Overlay, Focus Trap, Focus Restoration และ Keyboard; Custom Visual ด้วย Tailwind/SCSS และ Theme Tokens

### 12.3 ConfirmDialogData

```typescript
export interface ConfirmDialogData {
  title?: string;                   // default "ยืนยันการทำรายการ"
  message: string;                  // ข้อความที่เข้าใจง่าย
  confirmText?: string;             // default "ยืนยัน"
  cancelText?: string;              // default "ยกเลิก"
  type?: 'primary' | 'warning' | 'danger';
}
```

API ที่ใช้จาก Feature:

```typescript
this.confirmDialog.confirm({
  title: 'ยืนยันการลบข้อมูล',
  message: 'คุณต้องการลบบริษัท COMP01 ใช่หรือไม่?',
  confirmText: 'ลบข้อมูล',
  type: 'danger'
}).subscribe(confirmed => {
  if (!confirmed) return;
  this.deleteCompany();
});
```

### 12.4 ข้อกำหนด UX/A11y

- Modal มีหัวข้อและข้อความบอก **สิ่งที่จะเกิดขึ้นจริง** (ไม่ใช้เพียง "คุณแน่ใจหรือไม่")
- มีปุ่ม `ยกเลิก` และปุ่ม Action ที่แยกสถานะชัดเจน
- งานอันตราย เช่น Delete ใช้ Danger Visual; ไม่ใช้สีเป็นตัวบอกความหมายอย่างเดียว
- ปิดด้วย Escape เมื่อยังไม่เริ่มกระบวนการ และเมื่อยกเลิกได้อย่างปลอดภัย
- Focus อยู่ภายใน Dialog ขณะเปิด; คืน Focus สู่ Trigger เมื่อปิด
- ไม่ให้เปิด Modal ซ้อนกันโดยไม่จำเป็น
- ป้องกันกดยืนยันซ้ำ; ระหว่าง API ทำงาน Disable Actions และแสดง Processing
- ถ้าเป็นงาน Irreversible/มีผลสำคัญต้องพิจารณาข้อความผลกระทบ และ Require Stronger Confirmation เมื่อจำเป็นจริง
- ไม่แสดง Confirm ทุกงานโดยอัตโนมัติอย่างพร่ำเพรื่อ; งาน Save ทั่วไปใช้ Confirm เฉพาะที่ Workflow ต้องการ

### 12.5 แผนการใช้ Confirm

| Action | แนวทาง |
|---|---|
| Create ธรรมดา | อาจไม่ต้อง Confirm หาก Form ชัดเจน |
| Update ข้อมูลสำคัญ | Confirm ตาม Business Impact |
| Delete | Confirm เสมอเมื่อทำรายการลบจริง; ตรวจ Backend Permission/Referential Integrity |
| Approve/Reject | Confirm พร้อมผลของการเปลี่ยน State |
| Post/Calculate Depreciation | Confirm โดยระบุขอบเขต/งวด/ผลต่อข้อมูลก่อน Commit |
| Deactivate | Confirm พร้อมแจ้งผลการปิดใช้งาน |
| Unsaved Changes | Confirm ผ่าน CanDeactivate Guard |
| Success/Error Notification | ใช้ NotificationService ไม่ใช่ Confirm |

---

## 13. HTML, Accessibility และ Custom UI Design System [ยืนยัน]

### 13.1 หลักการ

**ทุก HTML Tag/Control ที่ใช้จริงต้องผ่านการตรวจ Semantic Correctness, Accessibility, Responsive Behavior, Interaction States และ User-Friendliness** แต่ **ไม่จำเป็นต้องสร้าง Angular Wrapper ใหม่ทุก Tag** หาก Native Element ทำหน้าที่ได้ดี

ตัวอย่าง:

- ใช้ `<button>` สำหรับ Action ไม่ใช้ `<div (click)>` ที่ไม่มี Keyboard Support
- ใช้ `<a href>` หรือ `routerLink` สำหรับ Navigation ไม่ใช้ Button แทน Link โดยไม่มีเหตุผล
- ใช้ `<label for>` คู่ Input และข้อความ Error
- ใช้ `<nav>`, `<main>`, `<header>`, `<section>` และ Heading Hierarchy ตามบริบท
- ใช้ Native `<table>` สำหรับข้อมูลตาราง; ใช้ ARIA Grid แบบซับซ้อนเฉพาะเมื่อมี Interaction แบบ Spreadsheet จริง
- ใช้ Native Input Type (`email`, `number`, `date`, `password`, ฯลฯ) ที่เหมาะสม และมี Backend Validation เสมอ

### 13.2 มาตรฐานต่อ Control

| Control / Tag | UX + Accessibility Requirements |
|---|---|
| Button | Type ถูก (`button`/`submit`), Focus/Hover/Disabled/Loading, Hit Area, Duplicate Submit Guard |
| Input | Label, Input Type, Autocomplete เมื่อเหมาะสม, Validation, Clear Error, Help Text |
| Select | Label, Keyboard, Clear/Placeholder ตามความหมาย, Searchable สำหรับรายการยาวถ้าคุ้มค่า |
| Textarea | Label, Length Limit/Counter ถ้าจำกัด, Resize/Auto-Grow ตาม UX |
| Checkbox | Label กดเลือกได้, Checked/Disabled/Indeterminate ตามกรณี |
| Radio | Fieldset/Legend สำหรับกลุ่ม, มีคำอธิบายความต่างของตัวเลือก |
| Date Picker | รูปแบบวันที่/Timezone ชัด, Keyboard & Format Validation, ปฏิทินเข้าถึงได้ |
| Table | Header/Caption เมื่อจำเป็น, Sort State, Empty/Loading/Error, Responsive Table Container |
| Form | Logical Grouping, Required Indicator, Submit Handling, Error Summary สำหรับฟอร์มยาว |
| Dialog | ARIA Label/Title, Focus Trap, Cancel/Confirm, Focus Restore, Escape เมื่อปลอดภัย |
| Link | ข้อความบอกปลายทาง, Focus, Target Behavior ชัด |
| Image | `alt` ตามความหมาย; ภาพตกแต่งใช้ `alt=""` |
| Navigation | `aria-current`, Collapse Button `aria-expanded`, Keyboard Navigation |
| Status/Toast | Live Region เหมาะสม ไม่แสดง Error ด้วยสีอย่างเดียว |

### 13.3 Design Tokens [แนวทางแนะนำ]

```text
colors: primary / surface / text / muted / border / success / warning / danger
spacing: 4px scale หรือชุด Token ที่ทีมอนุมัติ
radius: control / card / modal
shadow: card / overlay
font: system-compatible Thai + Latin, scale สำหรับ title / body / caption
density: comfortable default; compact table mode เป็นทางเลือก
z-index: header / drawer / overlay / toast กำหนดลำดับร่วมกัน
motion: short, predictable; รองรับ prefers-reduced-motion
```

- อย่าฝังค่าสี/ขนาดไม่สอดคล้องกันทุก Feature
- Tailwind Utility Classes ใช้ Layout/Responsive; ชุด UI กลางควบคุม Visual/Behavior/States
- หาก Custom Control ต้องผูก Reactive Forms ให้ใช้ ControlValueAccessor (CVA) เมื่อเหมาะสม
- เลือก Native Control พร้อม Styling หาก Functionality เพียงพอ แทนสร้าง Complex Widget เองโดยไม่จำเป็น

### 13.4 UI States ขั้นต่ำ

- Default, Hover, Focus Visible, Active/Selected
- Disabled, Read-only, Loading/Processing
- Validation Error, Business Error, Success, Warning
- Empty, No Results, Network Error, Retry
- Responsive: Narrow/Medium/Wide
- Interaction: Keyboard, Mouse, Touch และ Screen Reader

### 13.5 Accessibility Targets

- **[แนวทางที่ตกลงใช้ในการออกแบบ]** WCAG 2.2 ระดับ AA เป็นเป้าหมายการตรวจรับ
- Contrast ข้อความปกติ ≥ 4.5:1; ตัวอักษรใหญ่ตามเกณฑ์ ≥ 3:1
- Keyboard Focus Visible; Focus Order ถูกต้อง
- Target Size ตาม WCAG 2.2 AA อย่างน้อย 24×24 CSS px ภายใต้ข้อยกเว้น และออกแบบปุ่มสำคัญให้กดง่ายขึ้นเมื่อเหมาะสม (เช่นเป้าหมาย 44×44)
- ไม่บอกสถานะด้วยสีเพียงอย่างเดียว
- Responsive และ Zoom ไม่ทำให้ UI สำคัญใช้ไม่ได้
- ใช้ Semantic HTML ก่อนเพิ่ม ARIA; ARIA ไม่ทำให้ `<div>` กลายเป็น Button ที่สมบูรณ์เอง
- ทดสอบ Screen Reader + Keyboard จริงสำหรับ Dialog, Dropdown, Menu, Complex Table
- ถ้ามี Animation ต้องเคารพ Reduced Motion

### 13.6 แนวทางตรวจ HTML Tag ก่อน Merge

1. ตรวจความหมาย/Browser Semantics จาก HTML Specification/MDN
2. ตรวจ WAI/WCAG สำหรับ Interaction และ Keyboard
3. ตรวจว่า Shared UI Component มีอยู่แล้วหรือไม่
4. เลือก Native Tag, Directive หรือ Angular Component ให้เหมาะกับความซับซ้อน
5. ตรวจ Focus, Hover, Disabled, Validation, Loading, Error
6. ตรวจ Breakpoint และ Content Overflow
7. ตรวจ Screen Reader/Keyboard และ Automation Accessibility
8. อัปเดต Story/Docs ของ Component ถ้าเป็น Reusable UI ใหม่

---

## 14. Responsive Strategy [ยืนยัน]

### 14.1 หลักการ

- **Mobile-first** กับทุกหน้า/ทุก Modal/ทุก Form/ทุก Table
- มี Layout ที่ใช้งานดีบน Mobile, Tablet, Laptop, Desktop, Large Desktop
- Responsive ไม่ได้แปลว่าทุก Device ต้องแสดง Layout เหมือนกัน แต่ต้อง **ไม่สูญเสียข้อมูลและความสามารถที่จำเป็น**
- เปลี่ยน Layout อย่างมีเหตุผล เช่น Sidebar → Drawer, Multi-column Form → Single Column, Table → Scrollable/Table Card เมื่อเหมาะสม
- หลีกเลี่ยง Horizontal Scroll ของหน้าโดยรวม; Table/ข้อมูลสองมิติอาจ Scroll ภายใน Container ได้

### 14.2 Viewport Testing Matrix

| CSS Viewport Width | กลุ่มใช้งาน | สิ่งที่ตรวจ |
|---:|---|---|
| 320px | Mobile แคบ | ไม่มี Button/Message ล้น, ฟอร์มทำได้ครบ |
| 390px | Mobile ทั่วไป | Drawer/Modal/Form/Toast ไม่ทับเนื้อหา |
| 640px | Mobile/Small Tablet | Layout พลิกแนวเหมาะสม |
| 768px | Tablet | Sidebar/Toolbar/Form ใช้งานสะดวก |
| 1024px | Tablet/Laptop | Density/Overflow/Sort/Filter/Drawer |
| 1280px | Laptop/Desktop | Full Shell, Table Toolbar, Multi-column Form |
| 1440px | Desktop | ใช้พื้นที่เหมาะสม ไม่ยืดแบบไร้ขอบเขต |
| 1920px+ | Large Desktop | Content Width/Density/Readable Line Length |

- ตัวเลขเป็น **Test Targets และแนวทาง Breakpoints** ไม่ใช่รับประกันโดยไม่ทดสอบจริง
- ทดสอบ Orientation, Browser Zoom, Text Enlargement, Touch Keyboard, Long Thai Text, Error Messages, Permission-hidden Menus
- ใช้ CSS Grid/Flexbox/Container Queries ตามความเหมาะสม ไม่ยึด Media Query เพียงเพราะทราบชื่อ Device

### 14.3 Layout Grid สำหรับฟอร์ม

- Mobile: 1 คอลัมน์
- Tablet: 1–2 คอลัมน์ตาม Width ของ Input
- Desktop: 2–3 คอลัมน์สำหรับ Form ทั่วไป; 4 คอลัมน์เฉพาะ Field สั้น ๆ และมีความเหมาะสม
- Action Buttons ต้องแสดงครบและมี Touch Target พอเพียง; Mobile อาจ Stack หรือ Full Width
- Error Text/Help Text ต้องไม่ถูกตัดเมื่อ Label/Value ยาว

---

## 15. Application Shell — Sidebar & Header [ยืนยัน]

### 15.1 Sidebar: Collapsible Navigation

**Desktop:** Expanded Sidebar; Collapse ได้; Active Menu ชัด  
**Mobile:** Drawer/Overlay เปิดผ่าน Menu Button; ไม่แย่งพื้นที่ Content

ข้อกำหนด:

- Logo/System Name ด้านบน
- Group Module เช่น `DB`, `SU` พร้อมชื่อที่ผู้ใช้เข้าใจ
- Submenu Expand/Collapse; ลำดับไม่ซ้อนลึกจนใช้งานยาก
- Active Link มี Visual Indicator + `aria-current="page"`
- เมนูอ้างอิง Menu Registry/Permissions จาก Backend ไม่ hardcode สิทธิ์เฉพาะใน HTML
- Menu Search เป็น Option เมื่อจำนวนเมนูมาก
- Keyboard Tab Order, Button `aria-expanded`, Focus Restoration สำหรับ Drawer
- Drawer ปิดได้ด้วย Close/Escape ตามรูปแบบ Dialog และจัดการ Focus ให้ถูกต้อง
- Sidebar Width เริ่มต้นที่เสนอ: **256px expanded**, **72px collapsed** (เป็น Design Tokens ที่ปรับได้)
- Collapse State Persistence (session/local preference) **[แนวทางแนะนำ]**, ต้องไม่เก็บข้อมูลอ่อนไหว
- Mobile Navigation ไม่ใช้ Hover-only Submenu

### 15.2 Header: Responsive Application Header

ข้อกำหนด:

- ตำแหน่งอยู่ใน Main Layout ไม่ต้องเขียนทุก Feature
- ชื่อระบบ/บริบทหน้า เช่น Breadcrumb หรือ Page Title
- Sidebar Toggle / Hamburger
- User Menu + Profile / Logout
- Notification Entry Point หากมีระบบ Notification จริง
- **[ยืนยัน]** มี Company Switcher สำหรับเลือกบริษัทที่กำลังทำงานจากบริษัทที่มีสิทธิ์; Global Search เป็น Option
- Compact on Mobile; ลด Secondary Actions โดยไม่ซ่อน Action สำคัญ
- Keyboard/Focus/ARIA Label ให้ทุก Icon Button
- Sticky Header เป็น Option โดยต้องไม่บดบัง Focus/Content
- Header Height เริ่มต้น **56–64px** เป็น Token ปรับตาม Layout

### 15.3 Main Layout Structure

```text
MainLayout
├── Header (persistent)
├── Sidebar (desktop) / Drawer (mobile)
├── Content Region
│   ├── Page Header / Breadcrumb
│   └── <router-outlet>
├── Notification Outlet
└── Global Dialog Outlet
```

**Acceptance:** เปลี่ยน Route แล้ว Menu Active ตรงหน้า; Permission ที่เปลี่ยนมีผลต่อ Navigation; Mobile Drawer ไม่ทับ Focus; Back/Forward ยังแสดง Active Menu ได้ถูกต้อง

---

## 16. Enterprise Data Table Standard [ยืนยัน]

### 16.1 เป้าหมาย

สร้าง Data Table กลางที่ใช้ได้ใน DB/SU และโมดูลอื่น โดยรองรับ **Search, Filter, Sort, Pagination, Row Actions, Selection (เมื่อจำเป็น), Loading, Empty, Error, Responsive และ Accessibility**

### 16.2 Features

| Function | รายละเอียด |
|---|---|
| Search | ค้นจาก Keyword หลัก; Debounce เมื่อเหมาะสม; Clear Search ได้ |
| Filter | สถานะ/Company/Branch/Department/Date Range/Other ตาม Feature |
| Sorting | ASC/DESC + Visual State + Server Allowlist; เลือก Default Stable Order |
| Pagination | Server-Side สำหรับข้อมูลปริมาณมาก; Page Number/Page Size/Total Count |
| Row Actions | View, Edit, Delete/Deactivate/Approve ตาม Permission และสถานะรายการ |
| Selection | Checkbox และ Batch Actions เมื่อมีจริง; คุม Selected Items ข้ามหน้าตาม Policy |
| Loading | Skeleton/Table Loading แต่ยังรักษา Context ของหน้าจอ |
| Empty | ไม่มีข้อมูลในระบบ แยกจากกรองแล้วไม่พบ |
| Error | ข้อความไม่เทคนิค + Retry |
| Export | CSV/Excel เพิ่มเฉพาะเมนูที่อนุญาต; ต้องตรวจสิทธิ์/Filter/Encoding/Formula Injection |
| Column Format | ตัวเลข/เงินชิดขวา, วันที่สม่ำเสมอ, สถานะมีข้อความและสีประกอบ |
| Responsive | Scroll เฉพาะ Table หรือ Card/Detail View หากข้อมูลเหมาะ |
| Accessibility | Semantic Table, `<th scope>`, Sort Indicator/`aria-sort`, Keyboard |

### 16.3 Layout

```text
Page Title + Description + Primary Action
[Search ................................] [Filter] [Export?]
[Active Filter Chips / Clear Filters]

| Select? | Code ↓ | Name | Company | Status | Actions |
|---------|--------|------|---------|--------|---------|
| ...     | ...    | ...  | ...     | ...    | ...     |

Showing 1–25 of 340         [Page Size] [Prev] [1 2 3] [Next]
```

- Toolbar Search/Filter อยู่เหนือตาราง, Pagination อยู่ด้านล่าง
- หาก Table มี Horizontal Scroll ให้ Row Actions/Context ยังใช้งานได้
- ไม่บังคับทุกคอลัมน์ให้หดจนอ่านไม่ออกบน Mobile
- หากใช้ Card View สำหรับ Mobile ให้สามารถเข้าถึง Field สำคัญและ Action ได้เทียบเท่า
- ไม่ใช้ Virtual Scrolling เป็นค่าเริ่มต้นหาก Server Pagination เพียงพอ
- Column Resize/Pin/Customize/Saved Filters/Inline Editing เป็น Advanced Option **[ยังไม่ยืนยัน]**

### 16.4 Server-side Search Contract

```json
{
  "keyword": "COMP",
  "filters": { "isActive": true },
  "sort": { "field": "code", "direction": "asc" },
  "pageNumber": 1,
  "pageSize": 25
}
```

Response:

```json
{
  "items": [],
  "totalCount": 0,
  "pageNumber": 1,
  "pageSize": 25
}
```

- ตรวจ PageSize สูงสุด/Sort Field Allowlist ใน FluentValidator/Application
- Query DB ด้วย Server-Side Pagination; ส่งเฉพาะ DTO ที่ต้องใช้
- Feature เป็นผู้กำหนด Columns/Filters/Actions; Shared Table ไม่ควรรู้รายละเอียด Company/Asset โดยตรง
- Table ต้องไม่เรียก API ซ้ำโดยไม่จำเป็น: คุม Request Lifecycle, Caching/Deduplication เมื่อมี Use Case จริง
- Loading สำหรับ Read Operation **แยกจาก** Blocking Submit Loading

### 16.5 Criteria ทดสอบ Table

- [ ] Sorting สะท้อนสถานะที่กำลังใช้งานจริง
- [ ] Search/Filter เปลี่ยนหน้า/Reset หน้าอย่างถูกต้อง
- [ ] Page Size และ TotalCount ตรงผลจริง
- [ ] Backend ป้องกัน Sort/Filter ที่ไม่ได้รับอนุญาต
- [ ] Empty, No Results, Error และ Retry ต่างกันชัด
- [ ] Mobile Table ไม่ดันทั้งหน้าล้นด้านข้าง
- [ ] Keyboard/Screen Reader อ่าน Header/Sort/Actions ได้
- [ ] Row Action ใช้ ConfirmDialogService หากเป็นคำสั่งสำคัญ
- [ ] Row Action ที่ไม่มี Permission ไม่สามารถทำได้แม้เรียก API ตรง

---
## 17. Authentication, Authorization และ Security [ยืนยันแนวทางหลัก]

### 17.1 สถานะจากบทสนทนา

- **[ยืนยันล่าสุด]** ใช้บัญชีภายในผ่าน **ASP.NET Core Identity + Secure HttpOnly Cookie + Role/Policy-Based Authorization** และ **ไม่ใช้ MFA**; มตินี้แทนข้อกำหนด MFA เดิม
- **[ยืนยัน]** Login ด้วย Username หรือ Email โดยแต่ละค่าต้องไม่ซ้ำทั้งระบบ; Username ห้ามมี `@` ทั้งตอนสร้างและแก้ไข จึงแยกประเภท Identifier จาก Email ได้
- **[ยืนยัน]** งาน Admin สำหรับสร้างบัญชี แก้ข้อมูล Login รีเซ็ตรหัส ปลดล็อก และปิดบัญชีทำได้เฉพาะผู้ดูแลบัญชีส่วนกลาง; แยกสิทธิ์จากผู้ดูแลข้อมูลส่วนกลางและผู้ดูแลบริษัท ผู้ใช้ยังเปลี่ยนรหัสของตนเองผ่าน Flow เดิมได้
- **[ยืนยัน]** Admin ตั้งรหัสผ่านชั่วคราวเองตามกฎรหัสผ่านเพื่อสร้างบัญชีหรือรีเซ็ต; รหัสมีอายุ 24 ชั่วโมงนับจากออก และผู้ใช้ต้องเปลี่ยนก่อนเข้าถึงงานธุรกิจ
- **[ยืนยัน]** ลืมรหัสผ่านหรือรหัสชั่วคราวหมดอายุให้ติดต่อ Admin เพื่อออกรหัสผ่านชั่วคราวใหม่; Admin ส่งมอบโดยตรงหลังยืนยันตัวตน ต่อหน้าหรือโทรกลับเบอร์ที่องค์กรยืนยันไว้ และระยะแรกระบบไม่ส่ง Email/SMS
- เตรียมรองรับ OIDC/SSO ภายหลัง; Provider และการเปิดใช้จริงยังอยู่นอกระยะแรก

### 17.2 วิธี Login ที่เลือก

```text
Angular Login Form (Web/spa)
   |
   | HTTPS + Anti-CSRF ตามกลไกที่เลือก
   v
ASP.NET Core Auth Endpoints
   |
   v
ASP.NET Core Identity (Username/Email, Password Hash, Lockout)
   |
   v
SU DbContext รวม ASP.NET Core Identity / ตาราง SuUser ใน PostgreSQL Database และ Schema เดียวกัน
   |
   v
Issue Secure HttpOnly Authentication Cookie
   |
   v
Browser calls API on another domain (credentialed request + CORS + CSRF)
   |
   v
Authorize Policy + Data Scope Check
```

- Login UI อยู่ `features/auth/login` และ Controller อยู่ `Web/Controllers/AuthController.cs`
- Frontend/API คนละโดเมนตามมติล่าสุด; ต้องออกแบบและทดสอบการแนบ Cookie, CORS และ CSRF ตามโดเมนจริงก่อน Deploy
- ไม่เก็บ Access Token/Refresh Token ใน `localStorage` สำหรับรูปแบบเริ่มต้นนี้
- ไม่เขียน Password Hash/Token Cryptography เอง
- หากใช้ External SSO ให้ใช้มาตรฐาน **OpenID Connect Authorization Code Flow + PKCE** และออกแบบ Backend Session/Callback ให้ถูกต้อง

### 17.3 Security Controls ขั้นต่ำ

| Area | Mandatory Security Requirement |
|---|---|
| Transport | HTTPS สำหรับทุก Environment ที่เกี่ยวข้อง; ห้าม Login ผ่าน HTTP ใน Production |
| Password | 15–128 ตัวอักษร รองรับวลี/ช่องว่าง ไม่บังคับผสมชนิดอักขระ; มี Common/Compromised Password Blocklist; Hash ผ่าน Identity ไม่เก็บ Plaintext หรือ Password ลง Log; รหัสปกติไม่เปลี่ยนตามรอบ แต่บังคับเปลี่ยนเมื่อมีหลักฐานว่าถูกเปิดเผย |
| MFA | ไม่ใช้ตามมติล่าสุด; ไม่มี MFA Enrollment/Challenge/Recovery ใน Initial Scope |
| Brute Force | Login ผิดติดต่อกัน 5 ครั้ง ล็อก 15 นาที นับรวม Username/Email ของบัญชีเดียวกัน; Admin ปลดล็อกได้ตามสิทธิ์; มี Rate Limiting และ Monitoring เพิ่มเติม |
| Login Errors | ข้อความทั่วไป ไม่เปิดเผยว่าบัญชีมีอยู่/ไม่มีอยู่ |
| Cookie | `HttpOnly`, `Secure`, `SameSite` ตาม Flow จริง; จำกัด Path/Domain ที่จำเป็น |
| CSRF | Anti-forgery Protection สำหรับ State-changing Requests ที่อาศัย Cookie |
| XSS | Angular Template Escaping, No Unsafe HTML, CSP เมื่อเหมาะสม |
| Authorization | API ตรวจ Policy ทุก Endpoint ที่ต้อง Login |
| Object-level Authorization | ตรวจ Company/Branch/Department/Owner/Record Scope ใน Backend |
| Session | Idle 30 นาที / Absolute 8 ชั่วโมงนับจาก Login; ตรวจ Revocation ทุก API Request; Logout และ Password/Role Change Handling |
| Secrets | Secret Manager / Environment / Vault ไม่ Commit Password/API Key |
| DB Access | Least Privilege, ไม่มี Public DB Port โดยไม่จำเป็น |
| Error | ไม่รั่ว Technical Error, SQL, Tokens, Connection String |
| Logging | Audit Login & Security Events โดยไม่บันทึกรหัสผ่าน/โทเค็น |
| Dependency | Package/OS Security Updates, Vulnerability Scanning |
| Backup | Backup/Restore Test, Encryption/Access Control ตามความเหมาะสม |
| Headers | ปรับ Security Headers เช่น CSP, HSTS, Frame Policies ตามการ Host จริง |

**สำคัญ:** ไม่มีชุด Login ใดรับประกัน “แฮ็กไม่ได้” 100%; ต้องทดสอบ Security ต่อเนื่อง และทำ Defense in Depth ตั้งแต่ Browser ถึง Database/Infrastructure

### 17.4 Authentication vs Authorization vs Menu Visibility

1. **Authentication:** ผู้ใช้เป็นใครและ Session ใช้งานได้หรือไม่
2. **Authorization (Program + Action):** ทำ `DBRT01.VIEW`, `.CREATE`, `.UPDATE`, `.ACTIVATE`, `.DEACTIVATE` ได้หรือไม่
3. **Data Scope Authorization:** ผู้ใช้มีสิทธิ์เห็น Company/Branch/Department หรือ Record นี้หรือไม่
4. **Menu Visibility:** Sidebar แสดง Link ตามสิทธิ์เพื่อ UX แต่ไม่ใช่ Security Boundary

ตัวอย่าง:

```csharp
[Authorize(Policy = "DBRT01.UPDATE")]
[HttpPut("update")]
public async Task<IActionResult> Update([FromBody] UpdateCompanyRequest request)
{
    // Application Handler ต้องตรวจสิทธิ์ระดับ Record/Scope ต่ออีกชั้น
    // ...
    return Ok();
}
```

- `DBRT01.UPDATE` ต้องถูก Register ผ่าน Policy/Authorization Handler ที่ตรวจ Permission Store จริง
- Role เป็นชุดรวม Permissions แต่ไม่ควร Hardcode ทุกเงื่อนไขธุรกิจไว้แค่ Role String
- **[ยืนยัน]** Program/Action Permissions ของผู้ใช้มาจาก Role เท่านั้น; ไม่มีการเพิ่ม Action Permission ตรงรายบุคคล
- **[ยืนยัน]** Role งานภายในบริษัทเป็นของแต่ละบริษัท; แก้ชุดสิทธิ์ Role ในบริษัทหนึ่งไม่กระทบ Role ของอีกบริษัท และการตรวจ Policy ต้องใช้บริษัทที่กำลังทำงาน
- **[ยืนยัน]** ผู้ใช้ได้รับหนึ่ง Role งานภายในบริษัทต่อบริษัท และมี Role ต่างกันในแต่ละบริษัทได้; สิทธิ์ผู้ดูแลบัญชีส่วนกลาง ผู้ดูแลข้อมูลส่วนกลาง และผู้ตรวจสอบส่วนกลางเป็นขอบเขตระบบที่แยกจาก Role งานภายในบริษัท โดยไม่เพิ่ม Role งานบริษัทที่สอง
- **[ข้อกำหนดการนำไปใช้]** Assignment ต้องใช้ Role ของบริษัทเดียวกันและป้องกันการมี Role งานภายในบริษัทที่มีผลพร้อมกันมากกว่าหนึ่งต่อคู่ User/Company รวมถึงกรณีบันทึกพร้อมกัน; หลาย Scope ภายใต้ Role เดียวกันต้องไม่กลายเป็นช่องทางเพิ่ม Role ที่สอง
- **[ยืนยัน]** กำหนด Data Scope ได้ระดับบริษัท/สาขา/แผนก สำหรับข้อมูลที่มีขอบเขตดังกล่าว: ระดับบริษัทครอบคลุมหน่วยลูกในบริษัทนั้น, ระดับสาขาครอบคลุมฝ่าย/แผนกใต้สาขา, ระดับแผนกจำกัดแผนกที่ได้รับสิทธิ์
- **[ข้อกำหนดการนำไปใช้]** ตรวจ Action Permission และ Scope ที่ได้รับร่วมกันให้ถูกต้อง; ห้ามเอาสิทธิ์แก้ไขเฉพาะแผนกไปรวมกับสิทธิ์ดูทั้งบริษัทจนกลายเป็นแก้ไขทั้งบริษัทได้
- ตรวจสถานะผู้ใช้และ Session Revocation ฝั่ง Server ทุก API Request; การเปลี่ยน Role/สิทธิ์ ปิดบัญชี หรือเปลี่ยน Password ต้องมีผลต่อ Session เดิมตั้งแต่ Request ถัดไป
- Company Switcher แสดงเฉพาะบริษัทที่มีสิทธิ์; Backend ต้องตรวจซ้ำ ไม่เชื่อ CompanyId จาก Client โดยลำพัง
- **[ยืนยัน]** Position/BusinessUnit สร้าง แก้ไข และปิดใช้งานได้เฉพาะ Role ผู้ดูแลข้อมูลส่วนกลาง; การเป็น Admin ของบริษัทเพียงอย่างเดียวไม่ให้สิทธิ์นี้ ชื่อ Role/Permission จริงให้กำหนดใน Registry และตรวจผ่าน Policy
- **[ยืนยัน]** ผู้ดูแลบัญชีส่วนกลางเท่านั้นที่สร้างบัญชี แก้ข้อมูล Login รีเซ็ตรหัส ปลดล็อก และปิดบัญชี; Company Admin หรือผู้ดูแลข้อมูลส่วนกลางไม่ได้สิทธิ์จัดการบัญชีจากบทบาทนั้นโดยอัตโนมัติ
- **[ยืนยัน]** ผู้ดูแลบริษัทสร้าง/ปรับ Role และกำหนด Role/Scope ให้ผู้ใช้ในบริษัทได้ภายในขอบเขตที่ส่วนกลางอนุญาต; ยังบังคับหนึ่ง Role งานภายในบริษัทต่อผู้ใช้ต่อบริษัทและห้ามเพิ่มสิทธิ์ตนเองเกินขอบเขตที่ได้รับ
- **[ข้อกำหนดการนำไปใช้]** ตรวจสิทธิ์บริหารและขอบเขตที่มอบต่อได้ฝั่ง Server ทั้งตอนแก้ชุด Permission ของ Role และตอนเปลี่ยน Assignment; ห้ามเพิ่มสิทธิ์ส่วนกลาง เปลี่ยนบริษัทเจ้าของ Role หรือขยาย Scope เกินที่ได้รับ รวมถึงการเพิ่มสิทธิ์ตนเองทางอ้อมผ่าน Role ที่ตนเป็นสมาชิก
- **[รายละเอียดออกแบบ]** แยกสิทธิ์ที่ผู้ดูแลมีไว้ทำงานออกจากสิทธิ์ที่มอบต่อได้ และกำหนดผู้มีสิทธิ์ส่วนกลางในการตั้ง/เปลี่ยนขอบเขตดังกล่าว; Role ที่ใช้ร่วมกันภายในบริษัทต้องตรวจผลต่อ Assignment ที่เกี่ยวข้อง รวมทั้งกรณีลดขอบเขตที่เคยมอบไว้ โดยไม่มีสิทธิ์เดิมค้างข้ามข้อจำกัดใหม่
- **[ยืนยัน]** มอบสิทธิ์อ่าน/ส่งออก Audit ของบริษัทผ่าน Role บริษัทได้ตามบริษัท/สาขา/แผนกที่ได้รับ; VIEW และ EXPORT แยกกัน ส่วนข้อมูลบัญชีส่วนกลางและ Login Log ให้เฉพาะผู้มีสิทธิ์ตรวจสอบส่วนกลาง
- **[ข้อกำหนดการนำไปใช้]** สิทธิ์จัดการบัญชี จัดการข้อมูลร่วม และตรวจสอบส่วนกลางต้องตรวจแยกกัน; ชื่อ Role/รูปแบบ Assignment เป็นรายละเอียดออกแบบ การได้รับสิทธิ์ส่วนกลางด้านหนึ่งไม่ให้สิทธิ์อีกด้านหรือสิทธิ์ธุรกิจทุกบริษัทโดยอัตโนมัติ

### 17.5 Session & CSRF Specification

- **[ยืนยัน]** Session หมดอายุเมื่อไม่ใช้งาน 30 นาที หรือครบ 8 ชั่วโมงนับจาก Login อย่างใดถึงก่อน; การต่อ Idle Timeout ไม่ต่อ Absolute Lifetime
- **[ยืนยัน]** Session เดิมถูกเพิกถอนตั้งแต่ API Request ถัดไปหลังเปลี่ยน Role/สิทธิ์ ปิดบัญชี หรือเปลี่ยนรหัสผ่าน; ตรวจฝั่ง Server ทุก Request
- **[รายละเอียดออกแบบ]** กำหนดว่า Request ใดนับเป็นกิจกรรมผู้ใช้ และการออก Session ใหม่หลังเปลี่ยนรหัสผ่าน โดยไม่ทำให้ Session เดิมยังใช้งานได้
- Cookie ใช้ `Secure`, `HttpOnly` พร้อมชื่อ Cookie ที่จำเพาะ/เหมาะสม
- `SameSite` และ Domain/Path เลือกตามโดเมน Frontend/API จริง; ต้องแยกกรณีคนละ Origin กับคนละ Site และทดสอบ Browser Policy ก่อน Production
- กำหนด CORS Allowlist สำหรับ Frontend ที่อนุญาต, Credentialed Requests และช่องทางรับ/ส่ง Anti-CSRF Token ให้ Angular ใช้งานข้าม Origin ได้
- State-changing API (POST/PUT/PATCH/DELETE) ต้องมี Anti-forgery Mechanism เมื่อใช้ Cookie Authentication; กำหนดช่องทางส่ง Token ที่ Angular ใช้งานได้
- แยก Public Login/Health Endpoint ออกจาก Authenticated API ชัดเจน
- Response 401/403 ต้องเป็น JSON ที่ Frontend จัดการได้ ไม่ Redirect หน้า Login HTML แบบไม่เหมาะสมกับ XHR
- Rate Limit Login และการรีเซ็ตรหัสผ่าน; ตรวจสิทธิ์ Admin และไม่เปิดเผยข้อมูลบัญชีเกินจำเป็น
- **[ยืนยัน]** Login ผิดติดต่อกัน 5 ครั้งล็อกบัญชีสำหรับ Login เป็นเวลา 15 นาที; นับความผิดพลาดตามบัญชีเดียวกันแม้สลับ Username/Email และ Admin ปลดล็อกได้ตามสิทธิ์
- **[ข้อกำหนดการนำไปใช้]** เปิดการนับ Login Failure ใน Identity อย่างชัดเจน และตั้งค่า Lockout ตามมติ ไม่พึ่งค่าเริ่มต้นของ Library; ตรวจ Counter ฝั่ง Server และป้องกัน Request พร้อมกันหลบเกณฑ์ล็อก
- **[ยืนยัน]** รหัสผ่านชั่วคราวที่ Admin ตั้งเองตามกฎรหัสผ่านมีอายุ 24 ชั่วโมงนับจากออก ทั้งกรณีสร้างบัญชีและรีเซ็ต; เมื่อครบอายุต้องให้ Admin ออกใหม่ และก่อนเปลี่ยนรหัสห้ามเข้าถึงงานธุรกิจ
- **[ข้อกำหนดการนำไปใช้]** ตรวจอายุรหัสชั่วคราวฝั่ง Server ทั้งตอน Login และตอนยืนยันเปลี่ยนรหัส เพื่อไม่ให้ Session ที่เริ่มก่อนหมดอายุข้ามข้อจำกัดนี้; จัดเก็บด้วย Password Hash ของ Identity และ Audit โดยไม่เก็บรหัสผ่านลง Log
- **[ยืนยัน]** ทั้งรหัสชั่วคราวและรหัสปกติใช้กฎ 15–128 ตัวอักษร รองรับวลี/ช่องว่าง ไม่บังคับผสมชนิดอักขระ และมี Common/Compromised Password Blocklist; รหัสปกติไม่บังคับเปลี่ยนตามรอบ แต่บังคับเปลี่ยนเมื่อมีหลักฐานว่ารหัสถูกเปิดเผย
- **[ยืนยัน]** แหล่งตรวจรหัสผ่านคือรายการรหัสพบบ่อยในระบบร่วมกับ HIBP Pwned Passwords API; ใช้ทั้งการสร้างบัญชี/รีเซ็ตโดย Admin และการเปลี่ยนรหัสโดยผู้ใช้ รวมรหัสชั่วคราวและรหัสปกติ; ส่งเฉพาะ Hash Prefix ไม่ส่งรหัสผ่านหรือ Hash เต็มให้บริการภายนอก
- **[ยืนยัน]** ถ้า HIBP ตรวจไม่ได้ ให้หยุดการตั้งรหัสและให้ลองใหม่ ไม่ข้ามการตรวจแล้วบันทึก; Login ด้วยรหัสเดิมไม่ต้องเรียก HIBP แต่ยังตรวจสถานะบัญชี Lockout อายุรหัสชั่วคราว และข้อบังคับเปลี่ยนรหัสตามกฎเดิม
- **[ข้อกำหนดการนำไปใช้]** ตรวจให้ผ่านก่อนบันทึกการสร้างบัญชี/เปลี่ยน Password Hash/ต่ออายุรหัสชั่วคราวหรือเพิกถอน Session จากการเปลี่ยนรหัส; กรณีตรวจไม่ได้ต้องไม่ทำให้เกิดบัญชีหรือการรีเซ็ตที่สำเร็จเพียงบางส่วน และไม่บันทึก Success Audit ของรายการที่ล้มเหลว
- **[แนวทางนำไปใช้]** Backend ตรวจผ่าน HTTPS Range API โดยส่ง 5 ตัวแรกของ SHA-1 แล้วเทียบ Hash Suffix ใน Server; ขอ Response Padding และไม่ถือรายการ Padding ที่ Count เป็น 0 ว่าเป็นรหัสรั่วไหล ตรวจเมื่อส่งรหัสครบแล้ว ไม่เรียกทุกครั้งที่พิมพ์; SHA-1 ในขั้นตอนนี้มีไว้ค้นรายการรั่วไหลเท่านั้น การเก็บรหัสบัญชียังใช้ Password Hasher ของ Identity
- **[ข้อกำหนดการนำไปใช้]** ไม่เก็บรหัสผ่าน Hash เต็ม หรือค่าที่ใช้ค้น HIBP ลง Audit/Operational Logs; ตั้ง Timeout และจัดการ Network Error/Response ที่ตรวจสอบไม่ได้เป็นการตรวจไม่สำเร็จ โดยไม่เปิด Transaction ฐานข้อมูลรอ HTTP Call
- **[ยืนยัน]** Admin ส่งมอบรหัสชั่วคราวโดยตรงหลังยืนยันตัวตน ต่อหน้าหรือโทรกลับเบอร์ที่องค์กรยืนยันไว้; ไม่ใช้เบอร์ใหม่ที่ผู้ขอรีเซ็ตแจ้งโดยไม่มีการยืนยันเป็นหลักฐานเพียงอย่างเดียว และไม่เพิ่ม Email/SMS หรือเครื่องมือแชร์รหัสในระยะแรก
- **[รายละเอียดออกแบบ]** กำหนดชุดรายการรหัสพบบ่อยภายในและวิธีปรับปรุง, Timeout/การจัดการผลตอบกลับ HIBP และขั้นตอนตรวจตัวตน/แหล่งเบอร์ที่องค์กรยืนยันก่อนใช้งานจริง; ไม่ถือว่ามตินี้เพิ่มฟิลด์เบอร์โทรที่บังคับกรอกให้ทุกบัญชีโดยอัตโนมัติ
- If running multiple IIS Instances/Servers: Shared ASP.NET Core Data Protection Key Storage, consistent Clock/Key Rotation, Sticky Session ไม่ใช่คำตอบหลักเพียงอย่างเดียว

### 17.6 Program Management ภายใต้ SU

| Program | Access | Security Concerns |
|---|---|---|
| SURT01 User | ผู้ดูแลบัญชีส่วนกลางเท่านั้นสำหรับงานบริหารบัญชี | Create/Edit Login/Reset/Unlock/Disable; แยกจากผู้ดูแลบริษัทและผู้ดูแลข้อมูลส่วนกลาง |
| SURT02 Role | ผู้ดูแลบริษัท ภายในขอบเขตที่ส่วนกลางอนุญาต | Role แยกบริษัท, หนึ่ง Role ต่อผู้ใช้ต่อบริษัท, ป้องกันการมอบสิทธิ์เกินขอบเขต |
| SURT03 Permission | ผู้ดูแลบริษัทจัด Permission ใน Role ได้เฉพาะขอบเขตที่ส่วนกลางอนุญาต | ไม่แก้ Registry/สิทธิ์ส่วนกลางโดยอัตโนมัติ, Prevent Self-escalation, Audit Every Change |
| SURT04 User Department Mapping | ผู้ดูแลบริษัท ตามขอบเขตที่มอบต่อได้ | Company/Branch/Department Scope; ไม่เปลี่ยนบัญชีหรือ Assignment ของบริษัทอื่น |
| SURT05 Menu | Admin | Program Registry/Visibility ≠ Backend Permission |
| SURT06 Document Numbering | Authorized Operator | Unique/Atomic Sequence, Concurrency |
| SURT07 Approval Workflow | Authorized Admin | Separation of Duties / Versioning |
| SURT08 Settings | Highly Restricted | Secret Fields Not Displayed/Stored Insecurely |
| SURT09 Audit Log | VIEW/EXPORT ผ่าน Role บริษัทตาม Scope; บัญชีส่วนกลาง/Login Log เฉพาะผู้ตรวจสอบส่วนกลาง | แยกดู/ส่งออก, กรองทุก Query และไฟล์ส่งออก, Read-only, Retention |

> **[ยืนยัน]** ขอบเขตบัญชี/Role/Permission/Audit เป็นไปตามมติข้างต้น; **[รายละเอียดออกแบบ]** กำหนดชื่อ Role/Policy และชุดสิทธิ์ที่มอบต่อได้ให้ชัดเจน ส่วนคำว่า Admin ของเมนูอื่นต้องระบุ Policy ตามหน้าที่ก่อน Implement ไม่ตีความเป็นสิทธิ์ครอบจักรวาล

### 17.7 Auth Acceptance

- [ ] Login ถูก/ผิดทำงาน, Error ไม่เปิดเผย Username Enumeration
- [ ] Login ด้วย Username หรือ Email ไปยังบัญชีเดียวกันได้; ตรวจความซ้ำแต่ละ Identifier ทั้งระบบ และปฏิเสธ Username ที่มี `@` ทั้ง Create/Update
- [ ] Admin ตั้งรหัสผ่านชั่วคราวเองตามกฎสำหรับสร้าง/รีเซ็ตบัญชี; ต้องเปลี่ยนก่อนใช้ระบบ; ไม่ต้องผ่าน MFA หรือ Email/SMS
- [ ] ทั้ง Admin และผู้ใช้ตั้งรหัสได้เฉพาะความยาว 15–128 ตัวอักษร; รับวลี/ช่องว่างโดยไม่บังคับผสมชนิดอักขระ และปฏิเสธ Common/Compromised Password ตาม Blocklist
- [ ] สร้าง/รีเซ็ต/เปลี่ยนรหัสตรวจทั้งรายการภายในและ HIBP ตามเงื่อนไข; รหัสที่อยู่ในรายการใดรายการหนึ่งถูกปฏิเสธ และไม่ส่งรหัสผ่าน/Hash เต็มออกภายนอก
- [ ] HIBP Timeout/ล้มเหลว/ผลตอบกลับตรวจสอบไม่ได้ → ตั้งรหัสไม่สำเร็จและไม่มีการเปลี่ยนบัญชี/รหัส/อายุรหัส/Session บางส่วน; Login ด้วยรหัสเดิมยังทำงานตามกฎเดิมโดยไม่เรียก HIBP
- [ ] Admin สร้าง/รีเซ็ตสำเร็จแล้วส่งมอบรหัสโดยตรงหลังยืนยันตัวตน ตามขั้นตอนต่อหน้าหรือโทรกลับเบอร์ที่องค์กรยืนยัน; Audit บันทึกเหตุการณ์โดยไม่บันทึกรหัสหรือค่าที่ใช้ค้น HIBP
- [ ] รหัสปกติไม่มีการบังคับเปลี่ยนตามรอบ แต่มี Flow บังคับเปลี่ยนเมื่อมีหลักฐานว่าถูกเปิดเผย
- [ ] Login ผิด 5 ครั้งล็อก 15 นาที รวมการสลับ Username/Email ของบัญชีเดียวกัน; Rate Limit ยังทำงาน และ Admin ปลดล็อกได้เฉพาะเมื่อมีสิทธิ์
- [ ] รหัสชั่วคราวครบ 24 ชั่วโมงนับจากออกใช้ Login หรือยืนยันเปลี่ยนรหัสไม่ได้ แม้เริ่มขั้นตอนก่อนหมดอายุ; Admin ออกใหม่แล้วใช้กำหนดอายุของรหัสใหม่
- [ ] Session หมดอายุเมื่อ Idle 30 นาที หรือครบ 8 ชั่วโมง และ Activity ไม่ยืด Absolute Lifetime
- [ ] Session หมดอายุ → API คืน 401 และ Angular ไป Login Flow
- [ ] ไม่มีสิทธิ์ Program → API คืน 403 ไม่ใช่ข้อมูลจริง
- [ ] เปลี่ยน Body `Id` เป็นของบริษัทอื่น → ไม่ข้าม Data Scope
- [ ] CSRF Test ป้องกัน Cross-site State-changing Request ที่ไม่ได้รับอนุญาต
- [ ] ไม่เก็บ Password หรือ Cookie Secret ใน localStorage/Logs
- [ ] เปลี่ยน Password/Role/สิทธิ์ หรือ Disable User แล้ว Session เดิมใช้ไม่ได้ตั้งแต่ API Request ถัดไป
- [ ] ผู้ใช้ที่มีหลายบริษัทได้รับ Role/สิทธิ์ถูกต้องในบริษัทที่เลือก และสลับบริษัทแล้วไม่ค้างข้อมูล/สิทธิ์จากบริษัทเดิม
- [ ] Scope ระดับบริษัท/สาขาครอบคลุมหน่วยลูกถูกต้อง; Scope แผนกไม่ขยายสิทธิ์ไปแผนกอื่นหรือบริษัทอื่น
- [ ] Role งานภายในบริษัทแยกเจ้าของตามบริษัท; แก้ Role ของบริษัท A ไม่เปลี่ยน Role ของบริษัท B และไม่สามารถ Assign Role ข้ามเจ้าของบริษัท
- [ ] ผู้ใช้มี Role งานภายในบริษัทที่มีผลได้ไม่เกินหนึ่งต่อบริษัท แม้บันทึกพร้อมกัน; สิทธิ์ระบบส่วนกลางแยกขอบเขตและไม่เพิ่ม Role งานบริษัทที่สอง
- [ ] สิทธิ์ทำรายการมาจาก Role เท่านั้น; หลาย Scope ภายใต้ Role เดียวกันไม่ทำให้ได้ Action/Scope เกินที่ได้รับ และไม่ให้สิทธิ์ส่วนกลางโดยอัตโนมัติ
- [ ] Company Admin ที่ไม่มี Role ผู้ดูแลข้อมูลส่วนกลางสร้าง/แก้ไข/ปิดใช้งาน Position/BusinessUnit ไม่ได้
- [ ] Company Admin และผู้ดูแลข้อมูลส่วนกลางที่ไม่มีสิทธิ์ผู้ดูแลบัญชีส่วนกลางเรียก Create/Edit Login/Reset/Unlock/Disable บัญชีไม่ได้ แม้บัญชีนั้นใช้งานบริษัทเดียว; ผู้ใช้ยังเปลี่ยนรหัสตนเองผ่าน Flow ที่กำหนดได้
- [ ] ผู้ดูแลบริษัทแก้ Role/Scope ได้เฉพาะบริษัทและขอบเขตที่ส่วนกลางอนุญาต; ส่ง Request เพิ่มสิทธิ์ส่วนกลาง/ข้ามบริษัท/เพิ่มสิทธิ์ตนเองโดยตรงหรือผ่าน Role ไม่ได้
- [ ] VIEW Audit อย่างเดียวส่งออกไม่ได้; ทุก Search/Detail/Export คุม Company/Branch/Department Scope เดียวกัน และผู้มีสิทธิ์ Audit บริษัทอ่านข้อมูลบัญชีส่วนกลาง/Login Log ไม่ได้
- [ ] Cookie/CORS/CSRF ทำงานกับ Frontend/API คนละโดเมนตาม Environment จริง
- [ ] Logout เพิกถอน/สิ้นสุด Session อย่างมีประสิทธิภาพ
- [ ] Security Scan / Penetration Testing ตามระดับความเสี่ยงก่อน Production

---

## 18. Audit Trail, Logging, Observability และ Transaction

### 18.1 Audit Trail

**[ยืนยันใน Initial SU Scope]** มี `SURT09 — Audit Log`; **[แนวทางแนะนำ]** เก็บ Audit Event สำหรับ Create/Update/Delete/Deactivate/Approve/Permission Changes และ Login Events ที่สำคัญ

**[ยืนยันล่าสุด]** เก็บค่าก่อนและหลังเฉพาะฟิลด์ที่อนุญาต พร้อมปกปิดข้อมูลอ่อนไหว; **Audit Retention ยังไม่กำหนด และต้องตัดสินใจก่อน Production**

**[ยืนยัน]** Allowlist ของ Organization Master ระยะแรกคือ `Code`, `Name`, ID ของหน่วยงานแม่ตามความสัมพันธ์ของ Entity และ `IsActive`; มีผู้ทำรายการ เวลา โปรแกรม บริษัท/Scope ที่เกี่ยวข้อง และผลลัพธ์ตามกฎ Audit เดิม ส่วน `TaxId`/ข้อมูลติดต่อและฟิลด์อื่นยังไม่อนุญาตให้เก็บค่าก่อน/หลังจนกว่าจะกำหนดแยก มตินี้ไม่ใช่ Allowlist ของข้อมูลผู้ใช้หรือ Entity ใน SU

**[แนวทาง Mapping]** ID หน่วยงานแม่หมายถึง Company ของ Branch/CostCenter, Branch ของ Division และ Division ของ Department; Company/Position/BusinessUnit ไม่มีหน่วยงานแม่ตามโครงสร้างที่ยืนยัน จึงไม่มีค่าฟิลด์นี้ การ Mapping ตาม FK จริงให้ทำตอนออกแบบ ไม่เพิ่มคอลัมน์ซ้ำเพื่อ Audit โดยอัตโนมัติ

**[ยืนยัน]** Audit สำหรับรายการสร้าง/แก้ไข/ปิดใช้งานที่สำเร็จต้องบันทึกใน Transaction เดียวกับข้อมูลธุรกิจ หากบันทึก Audit ไม่ได้ต้อง Rollback ข้อมูลธุรกิจด้วย ไม่เลือก Outbox/การสร้าง Audit ภายหลังสำหรับรายการกลุ่มนี้ มตินี้ไม่รวม Login Log หรือ Log ของความพยายามที่ล้มเหลว

**[แนวทางนำไปใช้]** ให้ SU เป็นเจ้าของตาราง Audit และ Migration โดยโมดูลอื่นเขียนผ่าน IAuditWriter ใน Infrastructure ที่เข้าร่วม Transaction ปัจจุบัน; Audit ของธุรกิจที่สำเร็จต้องไม่ถูกแยก Commit ขณะที่ Security/Failure Logs ใช้เส้นทางบันทึกแยกจาก Transaction ธุรกิจที่อาจ Rollback

ข้อมูลที่แนะนำ:

```text
AuditId
OccurredAt (UTC)
ActorUserId / ActorDisplayName (ตามนโยบาย)
ModuleCode + ProgramCode
Action (CREATE/UPDATE/DELETE/APPROVE/LOGIN/...)
EntityType + EntityId
CompanyId/DepartmentId (ถ้าเกี่ยวข้อง)
Result (SUCCESS/FAILURE)
Reason/BusinessCode (ถ้ามี)
CorrelationId/TraceId
ChangedFields / Before-After (เฉพาะข้อมูลที่อนุญาต)
IP/UserAgent (ตามข้อกำหนด Privacy/Retention)
```

- ต้องไม่เก็บ Password/Secret/Token/ข้อมูลอ่อนไหวโดยไม่จำเป็น
- Admin ไม่ควรแก้ไข Audit Log ผ่าน UI ได้
- **[ยืนยัน]** Audit ของข้อมูลบริษัทให้สิทธิ์ VIEW/EXPORT ผ่าน Role บริษัทได้ตามบริษัท/สาขา/แผนกที่ได้รับ; ข้อมูลบัญชีส่วนกลางและ Login Log ให้เฉพาะผู้มีสิทธิ์ตรวจสอบส่วนกลาง และสิทธิ์ตรวจสอบแยกจากสิทธิ์บริหารบัญชี
- **[ข้อกำหนดการนำไปใช้]** ตรวจ Permission และ Scope ฝั่ง Server ใน Search/Filter/Detail/Export รวมผลรวมและไฟล์ที่สร้าง; การมี CompanyId แนบกับ Login Log ไม่ทำให้ Company Admin อ่านได้ และไม่ตีความ Scope ว่างเป็นเข้าถึงได้ทุกบริษัท
- **[รายละเอียดออกแบบ]** ระบุการจัดประเภทเหตุการณ์บริษัท/ส่วนกลางและ Scope ของเหตุการณ์ให้ตรวจย้อนหลังได้; Audit ของข้อมูลร่วมทุกบริษัทไม่ตกเป็น Audit ของบริษัทที่เลือกโดยอัตโนมัติ ต้องกำหนด Policy ตรวจสอบส่วนกลางตามประเภทข้อมูล
- กำหนด Storage, Access Policy และ Time Synchronization; ยังไม่กำหนดอายุหรือเปิดลบ Audit อัตโนมัติจนมีมติ Retention
- ใช้ Allowlist ของ Organization Master ที่ยืนยันข้างต้น; กำหนด Allowlist ของข้อมูล SU/ฟิลด์เพิ่มเติมและวิธี Mask ตาม Data Model ก่อนเปิดบันทึกค่าจริง ไม่เก็บทั้ง Entity โดยไม่คัดกรอง และไม่ให้ช่อง Reason/ข้อความ Log กลายเป็นช่องทางเก็บค่าที่ไม่ได้รับอนุญาต
- สำหรับ Financial Transactions ต้องระบุ Event/Document Reference และความสัมพันธ์กับ Posting ที่ย้อนตรวจได้

### 18.2 Operational Logging

- Structured Logs: Time, Severity, Program, Request, TraceId, User Context แบบไม่รั่วข้อมูล
- Correlation ID เชื่อม Angular Request, Web API, Application และ DB Operation
- Metrics/Tracing/Monitoring ตามการ Deploy จริง
- แจ้งเตือนระบบผิดปกติ เช่น Login Failure Spike, 5xx Spike, DB Connection Fail, Slow Query, Deadlock
- แยก Business Error ที่คาดการณ์ได้จาก Unexpected System Fault ใน Log Severity

### 18.3 Transaction Consistency

- คำสั่งหลายขั้นตอนที่ต้องสำเร็จทั้งหมดให้กำหนด Transaction Boundary ที่ Application/Infrastructure
- **[ยืนยัน]** การสร้าง/แก้ไข/ปิดใช้งานข้อมูลและ Success Audit เป็นหน่วย Commit เดียวกัน; ตอบสำเร็จหลังทั้งสองส่วน Commit แล้วเท่านั้น
- **[ข้อกำหนดการนำไปใช้]** เมื่อข้อมูลอยู่ใน DB Context และ Audit อยู่ใน SU Context ให้ประสาน DbConnection/DbTransaction เดียวกันอย่างชัดเจน ไม่ถือว่าการใช้ Connection String หรือฐานข้อมูลเดียวกันทำให้ Atomic โดยอัตโนมัติ; จัดการ Retry ทั้งชุดการบันทึกและป้องกัน Audit ซ้ำ
- **[ยืนยัน]** ตรวจเวอร์ชันข้อมูลเพื่อป้องกันการเขียนทับเมื่อมีผู้อื่นบันทึกไปก่อน; แจ้งผู้ใช้ให้โหลดข้อมูลล่าสุดและแก้ใหม่ พร้อมเก็บค่าที่กรอกไว้ดูอ้างอิง ระยะแรกไม่มีหน้าจอเปรียบเทียบหรือรวมการแก้ไขให้อัตโนมัติ
- **[ข้อกำหนดการนำไปใช้]** Detail Response ส่ง Version Token และคำสั่งแก้ไข/เปลี่ยนสถานะส่ง Token ของข้อมูลที่ผู้ใช้เปิดมา; ตรวจเทียบเวอร์ชันเดิมขณะบันทึก ห้ามใช้เวอร์ชันที่เพิ่งอ่านใหม่แทนเพื่อข้าม Conflict และห้าม Retry จนเขียนทับสำเร็จโดยผู้ใช้ไม่รับรู้
- **[แนวทางแนะนำ]** ส่ง Friendly Conflict ผ่าน Problem Details ตาม Error Contract (เสนอ HTTP 409); เก็บค่าฟอร์มเดิมไว้ในหน่วยความจำของหน้าเพื่อดูอ้างอิงเมื่อโหลดล่าสุด และตรวจสิทธิ์/Scope ใหม่ตามปกติ รูปแบบ Token และ Mapping สำหรับ PostgreSQL ให้กำหนดตอนออกแบบ
- **[ข้อกำหนดการนำไปใช้]** เมื่อเกิด Conflict ต้องไม่ Commit การแก้ไขหรือ Success Audit ของคำสั่งนั้น; Failure Log แยกจาก Transaction ตามแนวทางใน 18.1
- Idempotency สำหรับคำสั่งที่อาจถูก Retry และสร้างรายการซ้ำ
- Document Numbering ต้องจัดสรรแบบ Atomic ตาม Scope
- หากเพิ่ม Background Jobs/Events ภายหลัง พิจารณา Outbox Pattern, Retries, Dead-letter, Idempotent Consumers
- ห้ามแยกเป็นหลาย Database Service แล้วสมมติว่า Transaction ข้าม Service จะ Atomic ได้เอง

---

## 19. Initial Master Data Model — DB Group A

### 19.1 Organization Master Data Initial Scope

> **[ยืนยัน]** โครงสร้าง Company → Branch → Division → Department, ขอบเขตข้อมูลร่วม/รายบริษัท, BIGINT ของข้อมูลหลัก, Convention ชื่อตาราง, Code 1–50 ตัวอักษรตามกฎด้านล่างและล็อกหลังถูกใช้อ้างอิง, Name ช่องเดียวบังคับกรอกสูงสุด 200 ตัวอักษร, รหัสสาขา/ฝ่าย/แผนก/ศูนย์ต้นทุนไม่ซ้ำภายในบริษัทแยกตามประเภทข้อมูล, ปิดใช้งานแทนการลบจริงผ่านหน้าจอ/API และตรวจเวอร์ชันเมื่อแก้ไข โดยแจ้ง Conflict ให้โหลดล่าสุดและแก้ใหม่พร้อมเก็บค่าที่กรอกไว้อ้างอิง; Fields เสริม, Unique Scope ของข้อมูลชนิดอื่น, รายละเอียด Index และรูปแบบ Concurrency Token ยังเป็นรายละเอียดออกแบบ

| Program | Entity / Table | Core Fields (Code/Name ยืนยันแล้ว; Fields เสริมรอออกแบบ) | Relationships / Notes |
|---|---|---|---|
| DBRT01 | `Company` / `DbCompany` | Id, Code, Name, TaxId?, IsActive, Audit Fields | บริษัทที่ผู้ใช้มีสิทธิ์เลือกทำงาน |
| DBRT02 | `Branch` / `DbBranch` | Id, CompanyId, Code, Name, IsActive, Audit Fields | สาขาต้องอยู่ใต้บริษัท; Code ไม่ซ้ำภายในบริษัท [ยืนยัน] |
| DBRT03 | `Department` / `DbDepartment` | Id, DivisionId, Code, Name, IsActive | แผนกอยู่ใต้ฝ่าย; Code ไม่ซ้ำภายในบริษัท ไม่ใช่เฉพาะฝ่าย |
| DBRT04 | `Division` / `DbDivision` | Id, BranchId, Code, Name, IsActive | ฝ่ายอยู่ใต้สาขา; Code ไม่ซ้ำภายในบริษัท ไม่ใช่เฉพาะสาขา |
| DBRT05 | `Position` / `DbPosition` | Id, Code, Name, IsActive | ใช้ร่วมกันระหว่างบริษัท |
| DBRT06 | `CostCenter` / `DbCostCenter` | Id, CompanyId, Code, Name, IsActive | แยกตามบริษัท; Code ไม่ซ้ำภายในบริษัท [ยืนยัน] |
| DBRT07 | `BusinessUnit` / `DbBusinessUnit` | Id, Code, Name, IsActive | ใช้ร่วมกันระหว่างบริษัท |

หากเก็บ CompanyId/BranchId ซ้ำใน Entity ลูกเพื่อ Query/Authorization ต้องรักษาความสอดคล้องกับสาย FK ของโครงสร้างหลัก; รูปแบบคอลัมน์สุดท้ายให้ระบุใน Data Model ก่อน Migration

**[ยืนยัน]** Code ของ Branch, Division, Department และ CostCenter ไม่ซ้ำภายในบริษัท โดยตรวจแยกตามประเภทข้อมูล จึงใช้ Code เดียวกันข้ามประเภทหรือข้ามบริษัทได้ แต่สองรายการชนิดเดียวกันในบริษัทเดียวกันใช้ Code ซ้ำไม่ได้ ต้องบังคับความไม่ซ้ำในฐานข้อมูลจริง; **[รายละเอียดออกแบบ]** วิธีเก็บ CompanyId/Composite FK ให้กำหนดก่อน Migration

**[ยืนยัน]** Code ของข้อมูลหลักองค์กรทุกประเภทตัดช่องว่างหัวท้ายและแปลงตัวอักษรอังกฤษเป็นตัวพิมพ์ใหญ่ก่อนบันทึก/ตรวจซ้ำ เช่น ` ab01 ` เป็น `AB01`; ค่าหลัง Normalize ต้องยาว 1–50 ตัวอักษร ใช้เฉพาะ `A–Z`, `0–9`, `-`, `_` และไม่มีช่องว่างภายใน กฎนี้ใช้กับ Business Code ไม่เปลี่ยน Name, Username หรือ Email ให้เป็นตัวพิมพ์ใหญ่ตามไปด้วย

**[ยืนยัน]** Code แก้ได้ก่อนมีข้อมูลอื่นอ้างอิงเท่านั้น; หลังถูกใช้อ้างอิงแล้วล็อก โดย ID คงเดิมและยังตรวจรหัสซ้ำ/บันทึก Audit ตามกฎเดิม; รายการ Reference และการป้องกัน Race Condition ให้ออกแบบตามส่วน 4.5 การล็อก Code ไม่ได้ห้ามแก้ Name หรือฟิลด์อื่นที่ผ่านสิทธิ์และเงื่อนไขของฟิลด์นั้น

**[ยืนยัน]** Name ใช้ช่องเดียว บังคับกรอก สูงสุด 200 ตัวอักษร รองรับภาษาไทยหรืออังกฤษ; ระยะแรกไม่มีฟิลด์ชื่อแปลแยก

Fields ร่วมกัน (กฎที่ยืนยันมีผลตามข้อความด้านบน; Fields ทางเทคนิคอื่นเป็นแนวทางออกแบบ):

```text
Id                  BIGINT generated by database (C#: long)
Code                Required, 1–50 characters: A–Z / 0–9 / - / _; unique in scope; locked after use
Name                Required display name, max 200 characters; single field for Thai or English
IsActive            Active/Inactive State
CreatedAtUtc        UTC Timestamp
CreatedBy           Actor User ID
UpdatedAtUtc        Nullable UTC Timestamp
UpdatedBy           Nullable Actor User ID
VersionToken        Optimistic Concurrency [ยืนยันพฤติกรรม; รูปแบบ Token รอออกแบบ]
```

- Company/Branch/Department/Division/Position/CostCenter/BusinessUnit **ต้องเป็น Master ของจริง** มีการตรวจสิทธิ์/เงื่อนไขธุรกิจ ไม่ใช่เพียง Dropdown
- ระบุ Master Dropdown Lookup Endpoint แยกจาก Search Table หากมี UX/Permission ต้องต่างกัน
- **[ยืนยัน]** Company/Branch/Department/Division/Position/CostCenter/BusinessUnit ใช้ Activate/Deactivate ไม่มีลบจริงผ่านหน้าจอ/API รวมถึงรายการสร้างผิดหรือไม่เคยใช้; การปิดใช้งานไม่ลบข้อมูลอ้างอิงย้อนหลัง
- Sequence/Code ที่ผู้ใช้เห็นไม่ใช่ Technical PK โดยอัตโนมัติ
- การเปลี่ยนโครงสร้าง Master ต้องระวัง Referential Integrity ของ Transactions ที่อ้างอิงอยู่

### 19.2 Master Standard Operations

| Operation | Requirement |
|---|---|
| Search/List | Paging, Search, Sort, Filter, Permissions |
| Detail | รับ `Id` ใน Request Body ไม่ใช่ Route Path |
| Create | Request DTO + FluentValidation + Unique DB Constraint |
| Update | Validation, Permission, Version Check, Audit; Code แก้ได้เฉพาะก่อนมีข้อมูลอ้างอิง; Conflict ให้โหลดล่าสุดและแก้ใหม่พร้อมเก็บค่าที่กรอกไว้อ้างอิง |
| Activate/Deactivate | Confirm Modal; ก่อนปิดหน่วยงานแม่ต้องปิดลูกและจัดการสิทธิ์ที่ผูกตรงก่อน ตามกฎใน 19.3 |
| Delete | ไม่รองรับใน Organization Master ทั้งหน้าจอและ API; ใช้ Deactivate |
| Lookup | DTO ขนาดเล็ก สำหรับ Select/Autocomplete, Scope Check |
| Export | Optional ตาม Permission และความจำเป็น |

### 19.3 Organization Master กับ User/Permission

- `SURT04` อ้างอิง `Department` จาก `DBRT03` เพื่อกำหนด User-Department Mapping
- **[ยืนยัน]** ผู้ใช้หนึ่งคนเข้าถึงหลายบริษัทได้ เลือกทำงานทีละบริษัท และมี Role/สิทธิ์ต่างกันในแต่ละบริษัท
- **[ยืนยัน]** Role งานภายในบริษัทเป็นของแต่ละบริษัท และผู้ใช้ได้รับหนึ่ง Role ต่อบริษัท; สิทธิ์ระบบส่วนกลางแยกขอบเขตตามส่วน 17.4
- **[ยืนยัน]** Data Scope กำหนดได้ระดับบริษัท/สาขา/แผนก โดยสิทธิ์ระดับสาขาครอบคลุมแผนกใต้สาขา; มอบ Action Permissions ผ่าน Role เท่านั้น; Position/BusinessUnit ให้ Role ผู้ดูแลข้อมูลส่วนกลางเป็นผู้จัดการ
- **[ข้อกำหนดการนำไปใช้]** ตาราง Assignment ต้องบังคับหนึ่ง Role งานภายในบริษัทที่มีผลต่อคู่ User/Company และ Role ต้องเป็นของบริษัทนั้น; Scope หลายสาขา/แผนกยังทำได้ภายใต้ Role เดียวกัน โดยตรวจ Action กับ Scope ร่วมกัน
- **[ยืนยัน]** ผู้ดูแลบริษัทจัดการ Role/Scope ได้ในขอบเขตที่ส่วนกลางอนุญาต; การเปลี่ยนสิทธิ์ของบริษัทต้องไม่เปลี่ยนบัญชี Login หรือ Assignment ของบริษัทอื่น
- **[รายละเอียดออกแบบ]** โครงสร้างตาราง/Constraints/Identity Mapping และการจัดเก็บ Scope ต้องออกแบบให้บังคับมติข้างต้นได้จริง; ไม่ถือว่า User-Role Mapping เริ่มต้นของ Identity ครอบคลุมกฎรายบริษัทโดยอัตโนมัติ และต้องบังคับกฎปิด/ย้ายหน่วยงานด้านล่างร่วมด้วย
- Role/Permission ไม่ควรถูกเทียบกับ Department โดยตรง เพราะ Role ตอบ “ทำอะไรได้” ส่วน Department Scope ตอบ “ทำกับข้อมูลของใคร/ที่ไหนได้”
- **[ยืนยัน]** บริษัท/สาขา/ฝ่ายที่มีหน่วยงานลูกเปิดใช้งานอยู่ปิดใช้งานไม่ได้; ต้องปิดหน่วยงานลูกก่อนทีละลำดับ ไม่ Cascade ปิดลูกให้อัตโนมัติ
- **[ยืนยัน]** หากหน่วยงานที่จะปิดยังมีสิทธิ์ผู้ใช้ผูกตรงอยู่ Admin ต้องย้ายหรือยกเลิก Assignment ที่เกี่ยวข้องก่อน จึงปิดหน่วยงานได้; ไม่ยกเลิกสิทธิ์ให้อัตโนมัติพร้อมปิด และไม่ปิดบัญชีผู้ใช้ทั้งระบบ
- **[ยืนยัน]** ระยะแรกไม่รองรับการย้ายฝ่ายข้ามสาขาหรือแผนกข้ามฝ่ายภายในบริษัทเดียวกัน เมื่อมีรายการธุรกิจอ้างอิงแล้ว; หากปรับโครงสร้างให้สร้างหน่วยงานใหม่ด้วยรหัสใหม่ โดยเก็บหน่วยงานเดิมและข้อมูลอ้างอิงย้อนหลังไว้
- **[ข้อกำหนดการนำไปใช้]** ตรวจเงื่อนไขหน่วยงานลูก/Assignment/รายการธุรกิจฝั่ง Server และป้องกัน Race Condition ระหว่างตรวจและบันทึก; การตรวจรายการอ้างอิงก่อนย้ายต้องครอบคลุมหน่วยงานลูกที่ได้รับผลด้วย เพื่อไม่ให้ย้ายหน่วยงานแม่แล้วเปลี่ยนโครงสร้างของรายการย้อนหลังทางอ้อม
- **[รายละเอียดออกแบบ]** สำหรับรายการที่ยังไม่มีธุรกิจอ้างอิง ต้องกำหนดการตรวจสิทธิ์ต้นทาง/ปลายทางและผลต่อ Scope ก่อนเปิดการย้าย; ไม่ถือว่ามติจำกัดการย้ายเป็นการอนุญาตให้แก้ Parent ID โดยไม่มีการตรวจ

### 19.4 SU Initial Capabilities ในรายละเอียด

**SURT01 — User Management:** ผู้ดูแลบัญชีส่วนกลางเท่านั้นที่สร้าง/แก้ข้อมูล Login/ปิดบัญชี/ปลดล็อก/รีเซ็ตรหัส; แยกจากสิทธิ์ดูแลข้อมูลส่วนกลางและสิทธิ์ผู้ดูแลบริษัท ผู้ดูแลบัญชีตั้งรหัสผ่านชั่วคราวตามกฎ อายุ 24 ชั่วโมงและส่งมอบโดยตรงหลังยืนยันตัวตน; บังคับเปลี่ยนก่อนใช้ระบบ Login ด้วย Username/Email โดย Username ห้ามมี `@`; ไม่ใช้ MFA และไม่ส่ง Email/SMS ส่วน Lookup ผู้ใช้เพื่อมอบ Role ให้บริษัทต้องคืนข้อมูลเท่าที่ Policy อนุญาตโดยไม่ให้สิทธิ์บริหารบัญชีตามไปด้วย

**SURT02 — Role Management:** ผู้ดูแลบริษัทสร้าง/ปรับ Role และกำหนด Role ให้ผู้ใช้ได้ภายในขอบเขตที่ส่วนกลางอนุญาต; Role เป็นของแต่ละบริษัทและผู้ใช้มีหนึ่ง Role งานภายในบริษัทต่อบริษัท สิทธิ์ระบบส่วนกลางแยกขอบเขตตามส่วน 17.4; การปิด Role/เปลี่ยนชุดสิทธิ์ต้องตรวจผลต่อ Assignment และไม่ทำให้เกิดการเพิ่มสิทธิ์เกินขอบเขตโดยตรงหรือทางอ้อม

**SURT03 — Permission Management:** Program Registry + Action Permission ที่จัดเป็นชุดใน Role, Change Audit, Policy Mapping; ผู้ดูแลบริษัทปรับ Permission ของ Role ได้ภายในขอบเขตที่ส่วนกลางอนุญาต ไม่มี Direct User Action Permission และห้าม Self-escalation; การแก้ Registry/ขอบเขตที่มอบต่อได้/สิทธิ์ส่วนกลางให้กำหนด Policy ส่วนกลางแยกก่อน Implement

**SURT04 — User Department Mapping:** ผู้ดูแลบริษัทจัดการ Scope ระดับบริษัท/สาขา/แผนกได้เฉพาะขอบเขตที่ส่วนกลางอนุญาตให้มอบต่อ และแสดงแผนกที่เข้าถึงได้จริงรวมสิทธิ์สืบทอด; มี Audit และตรวจ Company/Branch Consistency โดยไม่เพิ่ม Action Permission ตรงรายบุคคลหรือแก้ Assignment บริษัทอื่น

**SURT05 — Menu Configuration:** Program Code, Module, Parent, Display Label, Icon, Route, Sort, Active, Permission; ไม่อนุญาตให้แก้ Program Code ได้ตามใจหลังใช้งานจริง

**SURT06 — Document Numbering:** Format, Prefix, Sequence, Reset Rules, Company/Period Scope, Concurrency-Safe Numbering; ไม่ใช้ Primary Key เป็นเลขเอกสารอย่างเดียว

**SURT07 — Approval Workflow:** Approval Steps, Role/Department, Status Transition, Version/Effective Date ตามความจำเป็น, Audit History

**SURT08 — System Settings:** Key/Value/Typed Settings ตาม Scope, Validation, Access Control, Secret Handling, Default Values, Cache Invalidation

**SURT09 — Audit Log:** Search/Filter/Detail/Export แบบ Read-only; VIEW/EXPORT แยกกันและมอบผ่าน Role บริษัทได้ตามบริษัท/สาขา/แผนกที่ได้รับ ต้องกรอง Scope ทั้งหน้าจอและไฟล์ส่งออก; ข้อมูลบัญชีส่วนกลางและ Login Log เฉพาะผู้มีสิทธิ์ตรวจสอบส่วนกลาง; Masked Sensitive Data และ Retention ตามมติที่เกี่ยวข้อง

---

## 20. Deployment & Hosting [ยังไม่ตัดสินใจขั้นสุดท้าย]

### 20.1 รูปแบบที่เลือกและส่วนที่เลื่อนไว้

- Angular Source อยู่ `Web/spa` **[ยืนยัน]**
- **[ยืนยัน]** Frontend และ API คนละโดเมน; เตรียม Build Artifact แยกกัน โดย API ใช้ Path `/api/*` บนโดเมนของ API
- **[เลื่อนการตัดสินใจ]** Hosting/เครื่องปลายทางและชื่อโดเมนจริงเลือกภายหลัง; ไม่ผูกกับ IIS หรือ Docker ในตอนนี้
- **[ยืนยัน]** เตรียมคำสั่ง Build/Test สำหรับรันในเครื่อง; ยังไม่เชื่อม CI และไม่ส่ง Email/SMS
- **[ยืนยัน]** Build ด้วย .NET 10 LTS / EF Core 10 / Npgsql EF Provider 10 และ Angular 22; PostgreSQL 18 เป็นฐานข้อมูลเป้าหมาย; แพ็กเกจเสริมต้องระบุรุ่นก่อนติดตั้ง

### 20.2 Environment Separation

```text
Local Development
  Angular Dev Server + ASP.NET Core API (กำหนด Dev Origins/Credentials/CSRF)
  ASP.NET Core API
  Development DB/Secrets

Test / Staging
  Production-like Build and Configuration
  Migration + Seed + Automated Tests
  Separate Test Credentials/DB

Production
  HTTPS / Hosting ตามที่จะเลือกภายหลัง
  Frontend Domain: Static Angular Assets
  API Domain: .NET API + Cookie/CORS/CSRF Configuration
  Secret Store / DB Restricted Access
  Backups / Monitoring / Audit / Security
```

- Angular Build Output Path ขึ้นกับ Angular Build Configuration/Version: ตรวจจริงก่อนจัดแพ็กเกจ Frontend สำหรับ Hosting ที่เลือก
- Deep Links ของ SPA ต้องตั้ง Fallback ให้กลับ Angular HTML อย่างถูกต้องโดยไม่กลืน `/api/*`
- Versioned Assets/Cache Headers ต้องไม่ทำให้ผู้ใช้ติดไฟล์ JS เก่าหลัง Deploy
- ไม่ฝัง Connection String, Password, API Key ใน Angular Environment Files (ไฟล์ Frontend ถูกผู้ใช้ดาวน์โหลดได้)
- API URL/Origins, Cookie และ Anti-CSRF ต้องกำหนดตามโดเมนจริง และทดสอบการ Login/เรียก API ข้าม Origin ใน Browser

### 20.3 Operations Checklist

- [ ] Production HTTPS/HSTS/Headers
- [ ] Secret/Configuration แยกตาม Environment
- [ ] DB Migration Plan + Backups + Restore Test
- [ ] Health/Readiness Endpoint ที่ไม่เปิดข้อมูลอ่อนไหว
- [ ] Security/Performance Monitoring
- [ ] Static SPA Routing/Fallback ทดสอบ
- [ ] Data Protection Key Storage หาก Scale หลาย Instance
- [ ] Session/Identity/Permission Validity ทดสอบหลัง Deploy
- [ ] Release/Rollback Plan; Automation พิจารณาเมื่อเลือก Hosting และเปิดใช้ CI/CD ภายหลัง

---

## 21. Testing Strategy & Definition of Done

### 21.1 Test Levels

| Test Type | ครอบคลุม | ตัวอย่าง |
|---|---|---|
| Domain Unit Tests | Business Rules/Invariants | Inactive Company/State Transition |
| Application Unit Tests | Use Case Logic, Validator, Error Mapping | CreateCompany Validation/Conflict |
| Infrastructure Integration Tests | DbContext, EF Config, Migration, Constraints | Unique CompanyCode, FK Integrity |
| API Integration Tests | HTTP Contract/Auth/Problem Details | Body ID, 400/401/403/409/500 |
| Angular Component Tests | Form, Table, Modal, Notification | Validation/Disable/Loading |
| Browser E2E Tests | Login → Navigation → CRUD → Confirm | DBRT01 End-to-End |
| Accessibility Tests | Semantic HTML/Keyboard/Focus/Dialog | WCAG 2.2 AA Target |
| Responsive Visual Tests | Target Viewports | 320/390/768/1024/1280/1920 |
| Security Tests | AuthN/AuthZ/CSRF/Object-level Access | Cross-company ID Access |
| Performance Tests | Large Lists/Concurrent Writes | Pagination & Concurrency |

### 21.2 Specific Test Cases จากข้อกำหนดสนทนา

**Authentication & SU**

- [ ] Login ถูกต้อง / ผิด / Locked / ต้องเปลี่ยนรหัสผ่านชั่วคราว; ไม่มี MFA ใน Initial Scope
- [ ] Permission Policy ระดับ Program/Action ถูกต้อง (`DBRT01.VIEW`, `.CREATE`, ...)
- [ ] ไม่มีสิทธิ์เข้าถึงเมนูและ API; URL/Body Manipulation ไม่ข้ามสิทธิ์
- [ ] เปลี่ยน Role/Permission ปิดบัญชี หรือเปลี่ยน Password แล้ว Session เดิมถูกเพิกถอนตั้งแต่ API Request ถัดไป
- [ ] Session Idle 30 นาที / Absolute 8 ชั่วโมง; Role/สิทธิ์แยกตามบริษัท และ Cookie/CORS/CSRF ข้าม Origin ทำงานตาม Configuration
- [ ] User Department Mapping คุม Data Scope ได้จริง
- [ ] SURT09 ค้น Audit Log ได้แต่แก้/ลบจาก UI ไม่ได้
- [ ] Audit ของ Organization Master เก็บ Before/After เฉพาะ Code, Name, ID หน่วยงานแม่ที่มีจริง และ IsActive; TaxId/ข้อมูลติดต่อ/ฟิลด์ที่ไม่ได้อนุญาตไม่รั่วผ่าน Payload หรือข้อความประกอบ

**Backend**

- [ ] Request Body DTO รองรับ ID + Additional Data, ไม่มี `/api/<resource>/{id}` สำหรับ API ตาม Convention นี้
- [ ] FluentValidation ปฏิเสธ Required/Length/Range/Invalid State
- [ ] Unique Constraint ช่วยป้องกัน Race Condition
- [ ] ปิดบริษัท/สาขา/ฝ่ายไม่ได้หากยังมีหน่วยงานลูกเปิดใช้งานอยู่; ไม่มีการปิดลูกอัตโนมัติ
- [ ] ปิดหน่วยงานไม่ได้หากยังมี Assignment สิทธิ์ผู้ใช้ที่ผูกตรงและมีผลอยู่; หลัง Admin จัดการครบจึงปิดได้ โดยบัญชีผู้ใช้ยังคงอยู่
- [ ] ปฏิเสธการย้ายฝ่าย/แผนกที่มีรายการธุรกิจอ้างอิง รวมกรณีอ้างอิงผ่านหน่วยลูกที่ได้รับผล; ประวัติและความสัมพันธ์เดิมไม่เปลี่ยน
- [ ] Global Exception Handler แสดง Friendly Error แทน DB Error
- [ ] มี TraceId/Correlation ID สำหรับ Debug
- [ ] DB Transaction Rollback เมื่อบันทึกหลายตารางไม่สำเร็จ
- [ ] บังคับให้บันทึก Audit ล้มเหลวแล้วข้อมูลธุรกิจต้อง Rollback; บังคับให้ข้อมูลธุรกิจล้มเหลวแล้วต้องไม่มี Success Audit ค้างอยู่
- [ ] User/Role/Identity Tables ใช้ SU Context และมี Migration Owner เดียว; SU อ่านบริษัท/แผนกผ่านบริการ DB ที่ตรวจ Scope ตาม Contract

**Frontend Form / UX**

- [ ] `.ts`, `.html`, `.scss` แยกชัดเจน
- [ ] Angular Router / Lazy Loading ตาม Feature ทำงาน
- [ ] Reactive Form แสดง Required/Format Errors ใกล้ Control
- [ ] Server Field Error แมปสู่ Form Control ถูกต้อง
- [ ] Save/Delete/Approve ระหว่าง Pending API ไม่สามารถกดยิงซ้ำ
- [ ] หลัง Success/Error Loading ไม่ค้าง
- [ ] ไม่มีการเรียก Browser alert/confirm/prompt
- [ ] Confirm Dialog ยืนยัน/ยกเลิก/Focus/Escape/Responsive ถูกต้อง
- [ ] Notification Service แสดง User-Friendly Message ไม่ซ้ำกับ Field Error
- [ ] Unsaved Changes Confirm ไม่ทำให้ข้อมูลสูญหายโดยไม่แจ้ง

**Sidebar/Header/Table**

- [ ] Active Sidebar Menu เปลี่ยนตาม Route
- [ ] Sidebar Collapse/Expand และ Mobile Drawer ถูกต้อง
- [ ] Header User/Logout/Breadcrumb ครบตาม Design
- [ ] Search/Filter/Sort/Pagination คุยกับ Backend ด้วย Request Model
- [ ] Empty/No Results/Loading/Error State มองออกชัดเจน
- [ ] Table จำนวนมากไม่ดาวน์โหลดทุกแถวมา Client โดยไม่จำเป็น
- [ ] Row Actions ซ่อน/Disable ตาม Permission และ Backend Reject ได้
- [ ] Desktop/Tablet/Mobile ใช้งานจริง ไม่มีการ Overflow ที่ไม่ตั้งใจ

### 21.3 Definition of Done ต่อ Feature

Feature ใหม่จะถือว่าเสร็จเมื่อ:

1. Program Code จัดสรรใน Registry ไม่ซ้ำ และนำไปผูก Permission/Menu/Audit
2. Use Case อยู่ใน Application/Features ที่ถูกตำแหน่ง, ไม่ฝัง Business Logic หนักใน Controller
3. มี Request/Response Contract + FluentValidation + Friendly Error
4. Auth/Authorization/Data Scope ตรวจที่ Backend
5. มี Unit/Integration/API Tests ตามความเสี่ยง
6. มี Angular Route + Page/Components `.ts/.html/.scss` ครบ
7. มี Form Validation + Loading Duplicate Submit Protection
8. มี Confirm Dialog/Notification ผ่าน Component กลาง ไม่ใช้ Native Browser Dialog
9. มี Responsive/Accessibility/Keyboard/Focus Testing
10. มี Logging/Audit สำหรับ Action สำคัญ
11. ผ่าน Code Review, Security Review, Migration Review และ Documentation Update

---

## 22. Implementation Roadmap (ลำดับการพัฒนาที่แนะนำ)

> ลำดับ Phase ต่อไปเป็น **ข้อเสนอเพื่อเริ่มลงมือพัฒนา** ไม่ใช่กำหนดเวลาหรือสัญญาว่าแต่ละ Phase ใช้จำนวนวันเท่าใด

### Phase 0 — Architecture Decisions & Repo Bootstrap

**Deliverables**

- [x] ผู้ใช้เลือก .NET 10 LTS + EF Core 10 + Npgsql EF Provider 10, Angular 22 และ PostgreSQL 18
- [ ] ระบุรุ่นแพ็กเกจเสริม ล็อกแพตช์ และตรวจความเข้ากันได้ก่อนติดตั้ง
- [ ] สร้าง Solution `clean` / Project 4 Layers + Angular `Web/spa` ตามเวอร์ชันที่ยืนยัน
- [ ] วาง MediatR/CQRS + DbContext Interface แยกโมดูลตามมติที่ยืนยัน
- [ ] ใช้ BIGINT ที่ฐานข้อมูลสร้างสำหรับข้อมูลหลัก/User/Role; Request/Response ID เป็น JSON string และตรวจแปลงเป็น long ใน Backend
- [ ] เตรียมคำสั่ง Build/Test ในเครื่อง โดยยังไม่เชื่อม CI
- [ ] กำหนด Naming/Folder Conventions, Source Control/Code Review
- [ ] กำหนด `ProgramRegistry` และ Seed DBRT01–07 / SURT01–09
- [ ] สร้าง Initial ADR สำหรับข้อที่ยังมีทางเลือก

### Phase 1 — Infrastructure & Cross-cutting Backend

- [ ] PostgreSQL ฐานข้อมูลเดียว Schema เดียว; EF Core Context แยกโมดูล + Table Prefix Db/Su + Migration/History แยกตามเจ้าของ
- [ ] FluentValidation 12.x + DI รุ่นที่เข้ากันได้; Validation Pipeline กลางเรียก ValidateAsync ก่อน Handler
- [ ] Global Exception Handler / Problem Details / TraceId
- [ ] Audit Fields + Logging Convention
- [ ] Standard API Request/Response Contracts + Pagination
- [ ] Health/Configuration/Secrets (environment-safe)
- [ ] Integration Tests สำหรับ DB/Validation/Error

### Phase 2 — Authentication & SU Security Foundation

- [ ] ASP.NET Core Identity ใน SU DbContext + Secure Cookies
- [ ] Login/Logout/Session Expiration/Password Recovery ตามนโยบาย
- [ ] Admin สร้าง/รีเซ็ตบัญชีด้วยรหัสผ่านชั่วคราวและบังคับเปลี่ยน; ไม่ใช้ MFA หรือ Email/SMS
- [ ] Idle 30 นาที / Absolute 8 ชั่วโมง และ Revocation มีผลกับ Request ถัดไป
- [ ] Cookie/CORS/CSRF สำหรับ Frontend/API คนละโดเมน
- [ ] AuthN/AuthZ Policies + Permission Store/Role Mapping
- [ ] Data Scope Architecture สำหรับ Company/Department
- [ ] SU Core: SURT01, SURT02, SURT03, SURT04, SURT05 ขั้นต้น
- [ ] Audit Login/Security Events
- [ ] CSRF/XSS/Rate Limiting/Session Tests

### Phase 3 — Angular Design System & Application Shell

- [ ] Tailwind 4.x ใน CSS กลาง + Angular CDK; Component SCSS แยกจากขั้นตอน Tailwind
- [ ] Design Tokens/Theme
- [ ] Shared Button/Input/Select/Form Field + Validation UI
- [ ] NotificationService + API Error Interceptor + FormErrorService
- [ ] ConfirmDialogService/Component (No Browser Alert)
- [ ] Loading/Processing State/Disable Actions
- [ ] Main Layout + Sidebar + Header + Company Switcher + Responsive Drawer
- [ ] Auth Route/Guards + Program Navigation
- [ ] Shared Enterprise Data Table + Toolbar + Pagination
- [ ] Responsive + WCAG Acceptance Baseline

### Phase 4 — DB Organization Master Initial Scope

Implement ตามลำดับตัวอย่าง (ปรับได้ตาม Dependency ของจริง):

- [ ] DBRT01 — Company
- [ ] DBRT02 — Branch
- [ ] DBRT04 — Division
- [ ] DBRT03 — Department
- [ ] DBRT05 — Position
- [ ] DBRT06 — Cost Center
- [ ] DBRT07 — Business Unit
- [ ] Lookup Endpoints สำหรับ Form Dropdown ที่จำเป็น
- [ ] Unique/Scope/Deactivate/Referential Integrity
- [ ] Master CRUD/Search/Detail/Form Standard
- [ ] Audit/Permission/Tests ทุกโปรแกรม

> โครงสร้าง Company → Branch → Division → Department ยืนยันแล้ว; ตรวจรายละเอียด FK/Constraints ก่อน Migration และพัฒนาตาม Dependency

### Phase 5 — Complete SU Administration Scope

- [ ] SURT01 — User Management (Full Management)
- [ ] SURT02 — Role Management
- [ ] SURT03 — Permission Management
- [ ] SURT04 — User Department Mapping
- [ ] SURT05 — Menu Configuration
- [ ] SURT06 — Document Numbering
- [ ] SURT07 — Approval Workflow
- [ ] SURT08 — System Settings
- [ ] SURT09 — Audit Log
- [ ] Rights/Approvals/Concurrency/Security Regression Tests

### Phase 6 — Production Readiness

- [ ] Deployment Environment + Angular SPA Hosting
- [ ] Monitoring/Log Retention/Alerts
- [ ] Database Backup/Restore Drill
- [ ] Migration Dry-run + Rollback Plan
- [ ] Security & Accessibility Audit
- [ ] Multi-viewport Regression and Browser Testing
- [ ] Load/Stress Test ตาม Workload จริง
- [ ] Operational Runbook / Support Troubleshooting

### Phase 7 — Optional Extension (ไม่อยู่ใน Initial Scope)

- [ ] ขยาย Modular Boundaries เมื่อมีความจำเป็น; ระยะแรกแยก DbContext ตามโมดูลแล้ว แต่ใช้ Database/Schema เดียว
- [ ] SaaS Multi-Tenant Isolation/Billing/Subscription
- [ ] Enterprise SSO (Entra ID/Keycloak/OIDC)
- [ ] Background Worker + Events/Message Broker/Outbox
- [ ] Advanced Table Grid (Virtual Scroll, Saved Filters, Column Pinning)
- [ ] Business Masters กลุ่ม B–F เมื่อมีระบบที่ใช้จริง
- [ ] Microservices **เฉพาะเมื่อ** มีเหตุผลด้าน Deploy/Scale/Team ที่ตรวจสอบได้

---
## 23. End-to-End Reference Flows (เส้นทางการทำงานทั้งระบบ)

### 23.1 เปิดหน้ารายการ Company — DBRT01

```text
1) Browser เปิด /db/dbrt01
2) Angular Router ตรวจ AuthGuard + PermissionGuard (เพื่อ UX)
3) MainLayout แสดง Header/Sidebar; Active Menu = DBRT01
4) CompanyListComponent โหลด Column Config/Filter State
5) CompanyApiService ส่ง POST /api/db/dbrt01/search พร้อม SearchRequest JSON
6) Browser แนบ Secure Authentication Cookie + Anti-CSRF Token ตาม Policy
7) Web API ตรวจ Authenticated Session + DBRT01.VIEW + Data Scope
8) FluentValidation ตรวจ SearchRequest (page, pageSize, sort allowlist)
9) Application SearchCompanies ประสาน Query
10) Infrastructure EF Core Query + Projection + Skip/Take
11) API ส่ง {items,totalCount,pageNumber,pageSize}
12) Table แสดงผลหรือ Empty/No Results/Error; ปิด Read Loading
13) เปลี่ยน Search/Filter/Sort/Page จึงส่ง Request ใหม่ตาม Lifecycle ที่ควบคุม
```

### 23.2 สร้าง Company — Success

```text
1) ผู้ใช้เปิด Company Create Page
2) Reactive Form ตรวจ Required/MaxLength/Cross-field ที่เกี่ยวข้อง
3) กด Save: if isSubmitting -> ignore, if invalid -> display field errors
4) หาก Workflow ต้อง Confirm -> ConfirmDialogService เปิด Modal; Cancel -> no request
5) ตั้ง isSubmitting=true -> Disable Save + แสดงกำลังบันทึก
6) CompanyApiService ส่ง POST CreateCompanyRequest JSON
7) API ตรวจ Session/Permission/Company Scope + Anti-CSRF
8) FluentValidation ตรวจ Request (Code, Name, ...)
9) Application CreateCompany ตรวจ Business Rules/Code Uniqueness (พร้อม DB Constraint)
10) Infrastructure บันทึก Company และ Audit: DBRT01.CREATE / Actor / Entity ID / TraceId ใน Transaction เดียวกัน
11) Commit เมื่อทั้งข้อมูลและ Audit สำเร็จ; ส่วนใดล้มเหลวให้ Rollback ทั้งหน่วย
12) API ส่ง Result DTO, Angular แสดง NotificationService.success
13) finally/finalize -> isSubmitting=false; Navigate/Refresh ตาม UX
```

### 23.3 Create Company — Field Validation Failure

```text
1) Frontend ส่ง Request; Backend Validator พบ Code ว่าง
2) API ตอบ HTTP 400 + VALIDATION_ERROR + errors.Code[]
3) API Error Interceptor ตรวจพบ Field Validation ไม่ Toast Error ซ้ำ
4) FormErrorService แมป 'Code' => 'code' และ mark/setErrors
5) หน้า Form แสดง "กรุณาระบุรหัสบริษัท" ใต้ Input
6) Focus ไป Field ที่ผิด/Validation Summary ตามขนาดฟอร์ม
7) ปิด isSubmitting; ผู้ใช้แก้ข้อมูลแล้วส่งใหม่ได้
```

### 23.4 Update Company — Business Rule Failure

```text
1) ส่ง UpdateCompanyRequest ที่มี Id ใน Body (ไม่ใช่ URL)
2) Auth/Policy/Scope ผ่าน; FluentValidation ผ่าน
3) Application พบว่าบริษัทไม่อยู่ในสถานะที่แก้ไขได้
4) ส่ง Business Failure Code (เช่น COMPANY_INACTIVE) + Friendly Message
5) Global Error Handler -> Problem Details 409/422 ตาม Convention
6) Angular Interceptor -> Notification/Dialog ว่าเหตุใดจึงทำรายการไม่ได้
7) ไม่แสดง Raw Exception/SQL; isSubmitting กลับ false
8) Audit/Log เก็บ TraceId และ Result ตามนโยบาย
```

### 23.5 Organization Master Deactivate — Confirm, Pending & Retry

```text
1) ผู้ใช้เลือก Deactivate จาก Row Action (Organization Master ไม่มี Delete)
2) Shared ConfirmDialogService แสดง Context + Consequence + Cancel/Confirm
3) Cancel -> No API
4) Confirm -> ตั้ง Processing State, Disable Action และ Modal Confirmation
5) API ตรวจ Permission/Record Scope; หน่วยงานลูกต้องปิดแล้ว และไม่มี Assignment สิทธิ์ผู้ใช้ที่ผูกตรงและมีผลอยู่
6) หากยังมีลูกเปิดใช้งานหรือสิทธิ์ที่ต้องจัดการ -> Friendly Business Error แจ้งสิ่งที่ต้องดำเนินการก่อน; ไม่ Cascade ปิดลูก/ยกเลิกสิทธิ์ และไม่ลบ Reference ย้อนหลัง
7) Commit สถานะและ Success Audit ใน Transaction เดียวกันแล้ว -> Notification และอัปเดต Table
8) หาก Timeout/Network Error -> อย่าบอกว่า "ปิดใช้งานไม่สำเร็จแน่นอน"
9) ตรวจผลลัพธ์ผ่าน API/Idempotency Key ก่อน Retry เพื่อไม่ให้เกิด Duplicate Transaction
```

### 23.6 Login + Access Control

```text
1) Angular Login Form validate รูปแบบข้อมูล
2) ส่ง Credentials ทาง HTTPS ไป Auth Endpoint
3) Identity ตรวจ Username/Email, Password Hash, Lockout และสถานะต้องเปลี่ยนรหัสผ่านชั่วคราว (ไม่ใช้ MFA)
4) ผ่าน Flow ที่กำหนด -> Secure HttpOnly Cookie; Idle 30 นาที / Absolute 8 ชั่วโมง
5) Angular เรียก Current User/บริษัทที่มีสิทธิ์ และเลือกบริษัทที่กำลังทำงานเพื่อโหลด Permissions/Navigation
6) Sidebar แสดง DB/SU เมนูตาม Permission; Hide Menu ไม่ใช่ Security Boundary
7) Angular เรียก API ข้าม Origin พร้อม Credentials/CSRF -> Backend ตรวจ Session Revocation + Policy + Company Scope ทุก Request
8) Session Expired/Revoked -> API 401; Angular จัดการ Redirect Login
9) Forbidden -> API 403; ไม่เปิดเผยข้อมูลหรือทำ Write
10) Logout -> ยกเลิก Session/Cookie และเคลียร์ Client State ที่เหมาะสม
```

---

## 24. Architecture Decisions Register

**ADR = Architecture Decision Record**: ทุกข้อที่ยังรอตัดสินใจให้บันทึกเหตุผล ทางเลือก ผลกระทบ และ Decision Date เมื่อเลือกแล้ว เพื่อไม่ให้ทีมสับสนว่า “ตัวอย่างโค้ด” คือ “ข้อกำหนดตายตัว”

ปรับปรุงวันที่ **16 พฤศจิกายน 2026** ตามคำตอบผู้ใช้ในส่วน 0.4; สถานะยืนยันหมายถึงมติออกแบบ ส่วน Implementation/Tests ยังต้องทำตาม Roadmap

| ADR ID | เรื่อง | สถานะตอนนี้ | เกณฑ์ตัดสิน |
|---|---|---|---|
| ADR-001 | ใช้ Clean Architecture 4 Layers | **ยืนยัน** | ผู้ใช้กำหนด Application/Domain/Infrastructure/Web |
| ADR-002 | Angular Source อยู่ `Web/spa` | **ยืนยัน** | โครงสร้างโปรเจกต์เดิมตามที่ผู้ใช้ต้องการ |
| ADR-003 | Component TS/HTML/SCSS + Feature Routes | **ยืนยัน** | Maintainability + UX |
| ADR-004 | Body DTO รวม ID; ไม่ใช้ `/:id` | **ยืนยัน** | API Convention ตามที่ผู้ใช้กำหนด |
| ADR-005 | FluentValidation Backend | **ยืนยัน: 12.x + Pipeline กลางผ่าน MediatR** | Server-side Request Validation ผ่าน ValidateAsync ตาม ADR-060 |
| ADR-006 | Reactive Form Validation Frontend | **ยืนยัน** | Early Feedback |
| ADR-007 | Global Friendly Error + Central Notification | **ยืนยัน** | ไม่ส่ง Raw Technical Error |
| ADR-008 | Blocking Processing State ป้องกัน Submit ซ้ำ | **ยืนยัน** | ป้องกัน Duplicate User Actions |
| ADR-009 | Central Confirm Modal; No Native Browser Dialogs | **ยืนยัน** | Consistency/Accessibility |
| ADR-010 | Tailwind CSS + Angular CDK + Custom UI | **ยืนยัน: Tailwind 4.x** | CSS กลางสำหรับ Tailwind แยกจาก Component SCSS ตาม ADR-061 |
| ADR-011 | Responsive Sidebar/Header/Data Table Standards | **ยืนยัน** | Enterprise UI Shell |
| ADR-012 | Module DB Group A: DBRT01–07 | **ยืนยัน** | Program Code Registry |
| ADR-013 | Module SU: SURT01–09 | **ยืนยัน** | System Administration Registry |
| ADR-014 | Identity + Cookie + Policy Auth; ไม่ใช้ MFA | **ยืนยันล่าสุด; แทนข้อกำหนด MFA เดิม** | ผู้ใช้เลือกไม่ใช้ MFA |
| ADR-015 | DB Provider | **ยืนยัน: PostgreSQL** | ผู้ใช้เลือก |
| ADR-016 | Primary Key ของข้อมูลหลัก | **ยืนยัน: BIGINT ที่ฐานข้อมูลสร้าง** | แยก Technical ID จาก Business Code; ตารางชนิดอื่นกำหนดตอนออกแบบ |
| ADR-017 | Application Dispatch | **ยืนยัน: MediatR/CQRS** | แยก Command/Query และ Handler ตามงาน |
| ADR-018 | Persistence Abstraction | **ยืนยัน: DbContext ผ่าน Interface ใน Application** | ใช้ EF Core ผ่าน Interface แยกโมดูล |
| ADR-019 | API URL | **ยืนยัน: Program Code** | เช่น `/api/db/dbrt01/search`; ID ใน Body |
| ADR-020 | Context / Database / Schema | **ยืนยัน: Context ต่อโมดูล, Database เดียว, Schema เดียว** | Ownership/Migration แยกตาม Context |
| ADR-021 | Organization Hierarchy | **ยืนยัน: Company → Branch → Division → Department** | Fields/Constraints ย่อยออกแบบตามโครงสร้างนี้ |
| ADR-022 | Multi-Company Scope | **ยืนยัน: หลายบริษัท เลือกทำงานทีละบริษัท สลับได้** | Role/สิทธิ์ต่างกันได้รายบริษัท; SaaS Tenant แยกเป็นส่วนขยาย |
| ADR-023 | Identity Provider | **ยืนยัน: บัญชีภายในผ่าน ASP.NET Core Identity** | เตรียมรองรับ SSO ภายหลัง |
| ADR-024 | Hosting / Origins | **ยืนยัน: Frontend/API คนละโดเมน; Hosting เลือกภายหลัง** | เตรียม Build; โดเมนจริง/Cookie/CORS/CSRF กำหนดก่อน Deploy |
| ADR-025 | Audit Detail / Retention | **ยืนยัน: Before/After เฉพาะ Allowlist + Mask; Retention เลื่อนไว้** | Organization Master ตาม ADR-071; Allowlist ข้อมูลอื่นรอออกแบบ และ Retention ตัดสินใจก่อน Production |
| ADR-026 | Program Sequence >99 | **ยืนยันให้ตัดสินใจเมื่อใกล้เต็ม** | ตอนนี้คงเลข 2 หลัก |
| ADR-027 | UI Table Advanced Grid Library | **พิจารณาภายหลัง** | Need for Virtualization/Inline Editing |
| ADR-028 | Message Broker/Event-driven | **พิจารณาภายหลัง** | Async Jobs/Integration Need |
| ADR-029 | Microservices | **ไม่ใช่ค่าเริ่มต้น** | Team/Deploy/Scale Justification |
| ADR-030 | API Versioning | **ยืนยัน: ยังไม่ใส่ Version ใน URL** | กำหนด Versioning ก่อนเปิด External Clients |
| ADR-031 | Solution / Namespace | **ยืนยัน: `clean`** | ใช้ตัวพิมพ์ตามชื่อที่ผู้ใช้ระบุ |
| ADR-032 | Table Naming | **ยืนยัน: PascalCase + Prefix Db/Su** | เช่น `DbBranch`, `DbCostCenter`, `SuUser`, `SuUserPermission` |
| ADR-033 | Shared Organization Masters | **ยืนยัน: Position/BusinessUnit ใช้ร่วมกัน; CostCenter แยกบริษัท** | Role ผู้ดูแลข้อมูลส่วนกลางเท่านั้นที่สร้าง/แก้ไข/ปิดใช้งานข้อมูลร่วม |
| ADR-034 | Login Identifier | **ยืนยัน: Username หรือ Email** | แต่ละค่าต้องไม่ซ้ำทั้งระบบ; Username ห้ามมี `@` ตาม ADR-049 |
| ADR-035 | Account Provisioning / Recovery | **ยืนยัน: Admin สร้าง/รีเซ็ตด้วยรหัสผ่านชั่วคราว** | Admin ตั้งรหัสเองตามกฎ อายุ 24 ชั่วโมงนับจากออก; บังคับเปลี่ยนก่อนใช้ระบบ; ไม่มี Email/SMS Flow |
| ADR-036 | Session Lifetime | **ยืนยัน: Idle 30 นาที / Absolute 8 ชั่วโมง** | อย่างใดถึงก่อน; นับ Absolute จาก Login |
| ADR-037 | Session Revocation | **ยืนยัน: มีผลตั้งแต่ API Request ถัดไป** | ตรวจฝั่ง Server ทุก Request หลังเปลี่ยน Role/สิทธิ์ ปิดบัญชี หรือเปลี่ยน Password |
| ADR-038 | Technology Versions | **ยืนยัน: .NET 10 LTS + EF Core 10 + Npgsql EF Provider 10, Angular 22, PostgreSQL 18** | FluentValidation 12.x / Tailwind 4.x ตาม ADR-060/061; MediatR 12.5.0 ตาม ADR-059; ล็อกแพตช์ที่ออกจริงและเข้ากันได้ |
| ADR-039 | CI / Local Build & Test | **ยืนยัน: ยังไม่เชื่อม CI** | เตรียมคำสั่ง Build/Test สำหรับรันในเครื่อง |
| ADR-040 | Email / SMS | **ยืนยัน: ยังไม่ส่งในระยะแรก** | Admin จัดการบัญชีตาม Flow ที่เลือก |
| ADR-041 | User / Role Primary Key | **ยืนยัน: BIGINT ที่ฐานข้อมูลสร้าง** | ใช้ชนิดเดียวกับข้อมูลหลัก; ไม่กำหนดแทน Composite Keys ของตารางเชื่อม |
| ADR-042 | BIGINT ID Wire Format | **ยืนยัน: JSON string ใน Request/Response** | Angular เก็บ string; Backend ตรวจ/แปลงเป็น long; ครอบคลุม FK IDs |
| ADR-043 | Division / Department Code Uniqueness | **ยืนยัน: ไม่ซ้ำภายในบริษัท แยกตรวจตามประเภทข้อมูล** | แต่ละสาขาต้องใช้ชุดรหัสร่วมกัน; บังคับ Constraint ในฐานข้อมูล |
| ADR-044 | Organization Data Scope | **ยืนยัน: บริษัท/สาขา/แผนก** | สิทธิ์ระดับสาขาครอบคลุมแผนกใต้สาขา; ตรวจ Scope ร่วมกับ Action และบริษัท |
| ADR-045 | Shared Master Administration | **ยืนยัน: Role ผู้ดูแลข้อมูลส่วนกลางเท่านั้น** | ควบคุม Create/Update/Deactivate ของ Position/BusinessUnit |
| ADR-046 | Action Permission Assignment | **ยืนยัน: ผ่าน Role เท่านั้น** | ไม่มี Direct User Action Permission; ข้อยกเว้นให้จัดชุด Role |
| ADR-047 | Temporary Password Lifetime | **ยืนยัน: 24 ชั่วโมงนับจากออก** | หมดอายุต้องให้ Admin ออกใหม่; ต้องเปลี่ยนรหัสก่อนใช้ระบบ |
| ADR-048 | Temporary Password Issuance | **ยืนยัน: Admin ตั้งรหัสเองตามกฎรหัสผ่าน** | ใช้ทั้งสร้างบัญชีและรีเซ็ต; กฎตาม ADR-056/ADR-072; ส่งมอบโดยตรงหลังยืนยันตัวตนตาม ADR-073 |
| ADR-049 | Username Format | **ยืนยัน: ห้ามมี `@`** | ตรวจทั้งสร้างและแก้ไข; ยัง Login ด้วย Email ได้ |
| ADR-050 | Master Business Code Normalization | **ยืนยัน: Trim + Uppercase** | ตัดช่องว่างหัวท้ายและแปลงเป็นตัวพิมพ์ใหญ่ก่อนบันทึก/ตรวจซ้ำ เช่น ` ab01 ` → `AB01` |
| ADR-051 | Branch / CostCenter Code Uniqueness | **ยืนยัน: ไม่ซ้ำภายในบริษัท แยกตามประเภทข้อมูล** | คนละบริษัทใช้ Code เดียวกันได้; บังคับความไม่ซ้ำในฐานข้อมูลจริง |
| ADR-052 | Organization Master Removal | **ยืนยัน: ปิดใช้งานอย่างเดียว ไม่มี Hard Delete ผ่านหน้าจอ/API** | ครอบคลุม DBRT01–07 แม้ไม่เคยใช้; เก็บข้อมูลอ้างอิงและเปิดกลับได้ |
| ADR-053 | Parent Organization Deactivation | **ยืนยัน: ปิดหน่วยงานลูกก่อนหน่วยงานแม่** | บริษัท/สาขา/ฝ่ายที่มีลูกเปิดใช้งานอยู่ปิดไม่ได้; ไม่ Cascade ปิดลูก |
| ADR-054 | Assigned Access Before Deactivation | **ยืนยัน: Admin จัดการสิทธิ์ที่ผูกตรงก่อนปิดหน่วยงาน** | ย้ายหรือยกเลิก Assignment ก่อน; ไม่ปิดบัญชีผู้ใช้ทั้งระบบ |
| ADR-055 | Organization Reparenting After Use | **ยืนยัน: ไม่รองรับในระยะแรกเมื่อมีรายการธุรกิจอ้างอิง** | ย้ายฝ่ายข้ามสาขา/แผนกข้ามฝ่ายภายในบริษัทไม่ได้เมื่อใช้แล้ว; ปรับองค์กรด้วยหน่วยงานและรหัสใหม่ |
| ADR-056 | Password Rules | **ยืนยัน: 15–128 ตัวอักษร รองรับวลี/ช่องว่าง ไม่บังคับผสมชนิดอักขระ** | ใช้กับรหัสชั่วคราวและรหัสปกติ; ปฏิเสธ Common/Compromised Password |
| ADR-057 | Regular Password Lifetime | **ยืนยัน: ไม่บังคับเปลี่ยนตามรอบ** | บังคับเปลี่ยนเมื่อมีหลักฐานว่ารหัสถูกเปิดเผย; รหัสชั่วคราวยังหมดอายุ 24 ชั่วโมง |
| ADR-058 | Login Lockout | **ยืนยัน: ผิดติดต่อกัน 5 ครั้ง ล็อก 15 นาที** | รวม Username/Email ต่อบัญชี; มี Rate Limit เพิ่มเติม; Admin ปลดล็อกได้ตามสิทธิ์ |
| ADR-059 | MediatR Version / License | **ยืนยันเพิ่มเติมเมื่อเริ่มพัฒนา: 12.5.0 / Apache-2.0** | ผู้ใช้เลือกก่อนติดตั้ง; วางแผนอัปเกรดภายหลังโดยตรวจ License ของรุ่นใหม่ |
| ADR-060 | FluentValidation Version / Invocation | **ยืนยัน: 12.x + DI ที่เข้ากันได้ + Pipeline กลางผ่าน MediatR** | ทุก Command/Query ที่มี Validator ผ่าน ValidateAsync ก่อนเข้า Handler |
| ADR-061 | Tailwind Version / SCSS Boundary | **ยืนยัน: Tailwind 4.x ใน CSS กลาง แยกจาก Component SCSS** | คง .ts/.html/.scss ของ Component; Tailwind และ Sass ทำงานแยกขั้นตอน |
| ADR-062 | Identity Context Ownership | **ยืนยัน: Identity อยู่ใน SU DbContext เดียวกัน** | รวม User/Role/ตาราง Identity กับข้อมูล SU; Context ของ DB แยกตามโมดูล |
| ADR-063 | Cross-Module Organization Reads | **ยืนยัน: ผ่านบริการอ่านข้อมูลของโมดูล DB เช่น IOrganizationLookup** | SU รับ DTO ผ่าน Contract ที่ตรวจ Scope; ไม่เรียก IOrganizationDbContext ตรง |
| ADR-064 | Business Data / Audit Atomicity | **ยืนยัน: ข้อมูลธุรกิจและ Success Audit ใน Transaction เดียว** | ครอบคลุม Create/Update/Deactivate; Audit ล้มเหลวให้ Rollback ข้อมูล; ไม่รวม Login/Failure Logs |
| ADR-065 | Company Role Ownership | **ยืนยัน: Role งานภายในบริษัทแยกตามบริษัท** | เปลี่ยน Role บริษัท A ไม่กระทบชุด Role บริษัท B; Role ผู้ดูแลข้อมูลส่วนกลางแยกต่างหาก |
| ADR-066 | Business Role Cardinality | **ยืนยัน: หนึ่ง Role งานภายในบริษัทต่อผู้ใช้ต่อบริษัท** | หลาย Scope ภายใต้ Role เดียวกันได้; ไม่เพิ่ม Role บริษัทที่สองผ่าน Scope Assignment; สิทธิ์ระบบส่วนกลางแยกขอบเขตตาม ADR-045/ADR-074–076 |
| ADR-067 | Concurrent Edit Conflict UX | **ยืนยัน: แจ้งให้โหลดล่าสุดและแก้ใหม่ พร้อมเก็บค่าที่กรอกไว้อ้างอิง** | ตรวจเวอร์ชันเพื่อป้องกันเขียนทับ; ไม่มี UI เปรียบเทียบ/รวมการแก้ไขในระยะแรก; รูปแบบ Token เป็นรายละเอียดออกแบบ |
| ADR-068 | Organization Master Code Format | **ยืนยัน: 1–50 ตัวอักษร ใช้ A–Z, 0–9, - และ _** | ตัดช่องว่างหัวท้ายและแปลงเป็นตัวพิมพ์ใหญ่ก่อนตรวจ/บันทึก; ไม่มีช่องว่างภายในหรือรหัสภาษาไทย |
| ADR-069 | Organization Master Code Changes | **ยืนยัน: แก้ Code ได้ก่อนมีข้อมูลอื่นอ้างอิง จากนั้นล็อก** | ID คงเดิม; ตรวจรหัสซ้ำ/สิทธิ์/เวอร์ชันและ Audit; ระบุ Reference ที่ทำให้ล็อกและป้องกัน Race Condition ก่อน Implement |
| ADR-070 | Organization Master Names | **ยืนยัน: Name ช่องเดียว บังคับกรอก สูงสุด 200 ตัวอักษร** | รองรับชื่อไทยหรืออังกฤษ; ไม่แยก NameTh/NameEn ในระยะแรก |
| ADR-071 | Organization Master Audit Allowlist | **ยืนยัน: Code, Name, ID หน่วยงานแม่ และ IsActive** | เก็บ Before/After ของฟิลด์เหล่านี้; TaxId/ข้อมูลติดต่อ/ฟิลด์อื่นและข้อมูล SU ต้องกำหนด Allowlist แยก |
| ADR-072 | Password Blocklist Source / Availability | **ยืนยัน: รายการพบบ่อยภายใน + HIBP API ผ่าน Hash Prefix** | ตรวจตอนสร้าง/รีเซ็ต/เปลี่ยนรหัส; ตรวจไม่ได้ให้หยุดตั้งรหัสและลองใหม่; ไม่ส่งรหัสผ่าน/Hash เต็มและไม่ผูก Login เดิมกับ HIBP |
| ADR-073 | Temporary Password Handoff | **ยืนยัน: ส่งมอบโดยตรงหลังยืนยันตัวตน** | ต่อหน้าหรือโทรกลับเบอร์ที่องค์กรยืนยันไว้; รหัสชั่วคราวอายุ 24 ชั่วโมงตามเดิม; ไม่เพิ่มบริการส่งรหัส |
| ADR-074 | Account Administration Authority | **ยืนยัน: ผู้ดูแลบัญชีส่วนกลางเท่านั้น** | สร้าง/แก้ข้อมูล Login/รีเซ็ตรหัส/ปลดล็อก/ปิดบัญชี; แยกสิทธิ์จากผู้ดูแลข้อมูลส่วนกลางและ Company Admin; ผู้ใช้ยังเปลี่ยนรหัสตนเองตาม Flow เดิม |
| ADR-075 | Company Permission Delegation | **ยืนยัน: ผู้ดูแลบริษัทจัดการ Role/Scope ได้ภายในขอบเขตที่ส่วนกลางอนุญาต** | คุมบริษัทและสิทธิ์ที่มอบต่อได้; หนึ่ง Role งานบริษัทต่อผู้ใช้ต่อบริษัท; ป้องกัน Self-escalation และการเปลี่ยนสิทธิ์ส่วนกลาง |
| ADR-076 | Audit Read / Export Authority | **ยืนยัน: ให้สิทธิ์ผ่าน Role บริษัทได้ตาม Scope และแยก VIEW/EXPORT** | กรองบริษัท/สาขา/แผนกทั้งหน้าจอและไฟล์; ข้อมูลบัญชีส่วนกลาง/Login Log เฉพาะผู้มีสิทธิ์ตรวจสอบส่วนกลาง |

### 24.1 Decision Template

```markdown
# ADR-XXX: <Title>

- Status: Proposed | Accepted | Rejected | Superseded
- Context: เหตุผล/ปัญหาที่ต้องตัดสินใจ
- Options: ทางเลือกที่พิจารณา
- Decision: ทางเลือกที่เลือก
- Consequences: ผลดี/ข้อเสีย/ต้นทุน
- Security/Performance/Data Impact:
- Date/Owner:
```

---

## 25. ตัวเลือกเทคโนโลยี/Architecture ที่เคยอภิปราย (บันทึกไว้เพื่อไม่ให้ข้อมูลจากบทสนทนาหาย)

### 25.1 Programming Languages / Frameworks

| ภาษา/Framework | โครงการที่เหมาะ | ความเห็นจากการเปรียบเทียบ |
|---|---|---|
| C# / ASP.NET Core | Business Systems, ERP, APIs, Enterprise | **เป็น Backend ที่ใช้ใน Architecture ของเรา** |
| Java / Spring Boot | Enterprise, Banking, Long-running Services | ทางเลือกอื่น ไม่ได้เลือกสำหรับ Template นี้ |
| TypeScript / Node.js | SaaS APIs, Realtime, Web | ทางเลือก Backend อื่น ไม่ได้เลือกเป็นหลัก |
| Python / FastAPI / Django | AI, Data, Automation, Web APIs | อาจใช้ Worker/Service เฉพาะทางในอนาคต |
| Go | Network/Concurrent Services | ใช้เพิ่มได้เมื่อมีโจทย์เฉพาะ ไม่จำเป็นสำหรับงานเริ่มต้น |
| PHP / Laravel | Web Apps, CMS, E-commerce | ทางเลือกอื่น ไม่ใช่ Backend ที่ตกลง |
| Angular / TypeScript | Enterprise SPA, Feature-rich Frontend | **เป็น Frontend ที่ใช้ใน Architecture ของเรา** |

**หลักคิด:** ไม่ใช่ภาษาที่กำหนดขนาดโปรเจกต์โดยตัวมันเอง; การจัดการ Complexity, Team, Data, Deployment และ Quality เป็นตัวตัดสินความเหมาะสมมากกว่า

### 25.2 Architecture Comparison

| Architecture | Structure / Data Access Characteristic | ใช้กับแผนนี้ |
|---|---|---|
| Layered | Controllers/Services/Repositories/Models/Data + AppDbContext | เปรียบเทียบทางเลือก |
| Clean | API/Application/Domain/Infrastructure; DB Mapping ใน Infrastructure | **ตัวหลัก** |
| Modular Monolith | Modules แยก Business Boundary, Shared Deployment, Context/Schema per Module ได้ | แนวทางขยาย |
| Microservices | Service Deploy ต่างกัน, DB Ownership ต่างกัน, HTTP/gRPC/Messaging | ภายหลังเมื่อจำเป็น |
| Event-Driven | Event Publisher/Consumer, Broker, Retry, Idempotent Handler | ภายหลังเมื่อจำเป็น |

### 25.3 Database Access Options

| วิธี | ลักษณะ | ข้อควรคำนึง |
|---|---|---|
| EF Core | DbContext/DbSet/LINQ/Migrations | Mapping, Tracking, Query Performance |
| Dapper | SQL เอง + Object Mapping เบา | ควบคุม Query ง่าย แต่ต้องดูแล SQL/Parameterization |
| ADO.NET / Provider Driver | Connection/Command/Transaction เอง | ควบคุมละเอียดแต่ Boilerplate สูง |
| Stored Procedure / PostgreSQL Function | ย้ายการคำนวณ/Query บางส่วนไป DB | Versioning/Testability/DB Coupling |

โครงสร้าง Entity + Configuration + DbContext ที่ผู้ใช้เล่ามาสอดคล้อง EF Core; อย่างไรก็ตาม **Layered, Clean, Modular Monolith หรือ Microservices ไม่ได้บังคับว่าต้องใช้ ORM คนละแบบ**

### 25.4 ตัวอย่างงาน Fixed Asset ที่เคยใช้ประกอบการอธิบาย

โปรเจกต์ตัวอย่างก่อนหน้านี้เคยพูดถึง `CreateAsset`, `UpdateAsset`, `GetAsset`, `CalculateDepreciation`, `Transfer`, `Revaluation` เพื่ออธิบาย Business Logic ว่า:

- Application ดูแลการ Query/ประสาน Use Case/Save Transaction
- Domain ดูแล Business Invariants/Reusable Calculation เมื่อเหมาะสม
- Infrastructure ติดต่อ DB ผ่าน EF Core/Repository/Function
- Web รับ Request DTO แล้วส่งเข้า Application

**สถานะ:** เป็นกรณีศึกษาจากความคุ้นเคยด้าน ERP/Fixed Asset ไม่ได้ยืนยันว่าเป็นโมดูลเริ่มต้นของ Template นี้ (Initial Scope ยังคงเป็น DB + SU)

### 25.5 กลุ่ม Master ที่เคยพิจารณา (เฉพาะ Group A ที่เลือกเริ่มต้น)

| Group | ตัวอย่าง Master | Initial Scope |
|---|---|---|
| **A Organization** | Company, Branch, Department, Division, Position, Cost Center, Business Unit | **ใช่ — DBRT01–07** |
| B Location & Reference | Country, Province, District, Subdistrict, Postal Code, Currency, Language, Unit/Conversion | ไม่ใช่ |
| C Business Partner | Customer, Supplier, Groups, Contacts, Payment Terms/Methods | ไม่ใช่ |
| D Product & Inventory | Product, Category, Group, Brand, Warehouse, Storage, Tax Category | ไม่ใช่ |
| E Financial & Accounting | Chart of Accounts, Fiscal Period, Exchange Rate, Tax Code, Journal Type | ไม่ใช่ |
| F Fixed Asset | Asset Category, Group, Depreciation Method, Location, Condition, Status, Disposal/Transfer Reason, Asset Book | ไม่ใช่ |

System Administration (SU) แยกจาก Business Master เพราะ User/Role/Permission/Menu/Workflow/Audit/Settings เป็นระบบบริหารจัดการส่วนกลาง ไม่ใช่ Master ธุรกิจธรรมดาทั้งหมด

---

## 26. Implementation Guardrails — ข้อกำหนดสำหรับ Developer/AI Coding Agent

ถ้าใช้เอกสารนี้เป็นคำสั่งให้ทีม Developer หรือ AI Agent เริ่มพัฒนา ให้ปฏิบัติดังนี้:

1. อ่าน `project-plan.md` ทั้งไฟล์ก่อนเริ่มสร้าง Code/Schema; อย่าเปลี่ยนข้อกำหนด `[ยืนยัน]` โดยไม่แจ้ง
2. ยึดมติในส่วน 0.4/0.5/24; สิ่งที่ยัง `[รอตัดสินใจ]` ต้องบันทึกเหตุผลใน ADR ก่อนสร้างสิ่งที่แก้ย้อนหลังยาก; ใช้รุ่นหลักที่ผู้ใช้เลือกแล้ว และเสนอรุ่นแพ็กเกจเสริมที่ยังค้างก่อนติดตั้ง
3. สร้างงานทีละ Vertical Feature จนจบ Flow: API + Validator + Application + DB + Angular Route/Form + Error/Confirm/Loading + Tests
4. ทุก Feature ต้องใช้ Program Code ของ Registry และมี Permission ที่สอดคล้อง
5. ห้ามทำ CRUD โดย Inject EF DbContext ลง Angular/Controller หรือส่ง Entity ออก API ตรง ๆ
6. ต้องมี API Request Model สำหรับ ID + Additional Data; **ไม่ใส่ ID ใน URL Path**
7. Backend ใช้ FluentValidation และ Global Error Contract; ไม่แสดง DB/Stack Trace ต่อผู้ใช้
8. Frontend ทุกหน้าแยก TS/HTML/SCSS; ไม่บีบ Template/Code ทั้งหมดเป็น Inline/ไฟล์เดียวเพื่อลดจำนวนบรรทัด
9. ใช้ Tailwind + Angular CDK + Custom UI Components ตาม Design System; อย่าเพิ่ม Bootstrap/Material/PrimeNG โดยไม่จำเป็น
10. ห้ามใช้ Browser `alert`, `confirm`, `prompt`; Action Confirmation ต้องผ่าน ConfirmDialogService
11. ใช้ Processing State และ Disable/Gate สำหรับคำสั่งที่เปลี่ยนข้อมูล; เสริม Server Idempotency เมื่อมีความเสี่ยง
12. Validation UX ต้องแสดง Error ใต้ Field; Business/System Error ส่งให้ NotificationService ที่เป็นมิตร
13. UI ทุกหน้า/ทุก Modal ต้อง Responsive และทดสอบ Keyboard/Focus/ARIA/Color Contrast
14. Sidebar/Header/Table ต้องใช้ Component กลาง ไม่สร้าง Duplicate Pattern ประจำ Feature
15. Header/SideNav ไม่ใช่ Authorization; Backend ต้องตรวจทุก API และ Data Scope
16. ทุก DB Change ต้องมี Migration/Test/Constraint ที่เกี่ยวข้อง และ Audit Fields ตามนโยบาย
17. ป้องกัน Secrets Leak, SQL Injection, XSS, CSRF, IDOR/Object-level Authorization
18. อัปเดต Documentation/ADR/Test Cases เมื่อเปลี่ยน Requirement หรือ Convention

### 26.1 First Vertical Slice ที่แนะนำ

**DBRT01 — Company Management** เป็นเมนูตัวอย่างที่เหมาะสำหรับตรวจสอบ Architecture ทั้งระบบ เพราะครอบคลุม:

- Login/Session/Permission `DBRT01.VIEW/CREATE/UPDATE/ACTIVATE/DEACTIVATE`
- Sidebar Active Menu + Header Breadcrumb
- Company List Table: Search/Sort/Filter/Pagination
- Create/Edit Company Form: Reactive Forms + FluentValidation
- Request DTO Body ID + Data
- DbContext/EF Configuration/Unique Constraint/Migration
- Confirm Activate/Deactivate + Duplicate Submission Protection
- Validation Error → Form; Business/System Error → Notification
- Audit + TraceId + Error Handler
- Responsive/Accessibility/Test Cases

เมื่อ Slice แรกผ่าน Acceptance Criteria จึงค่อย Reuse ไปยัง DBRT02–07 และ SU ที่เกี่ยวข้อง

---

## 27. Consolidated Acceptance Checklist — ตรวจว่าครบตามบทสนทนาหรือไม่

### Architecture & Backend

- [ ] แยก `Application`, `Domain`, `Infrastructure`, `Web` ตามบทบาท
- [ ] `Application/Features` แยกตาม Module/Program/Use Case
- [ ] Entity/Entity Configuration/DbContext มีหน้าที่ชัดเจน
- [ ] PostgreSQL Database/Schema เดียว; Context/Migrations แยกโมดูล; Table Naming Db/Su ตรงมติ
- [ ] MediatR/CQRS + DbContext Interface; ข้อมูลหลักใช้ BIGINT ที่ฐานข้อมูลสร้าง
- [ ] User/Role ใช้ BIGINT; API ส่ง/รับ BIGINT ID เป็น string โดยไม่สูญเสียความแม่นยำ
- [ ] รหัสฝ่าย/แผนกไม่ซ้ำภายในบริษัท และตรวจแยกตามประเภทข้อมูลด้วย DB Constraint
- [ ] Code หลัง Normalize ยาว 1–50 ตัวอักษร ใช้เฉพาะ A–Z/0–9/-/_; รับ ` ab01 ` เป็น `AB01` และปฏิเสธรหัสว่าง/ภาษาไทย/ช่องว่างภายใน/ยาวเกินกำหนดทุกช่องทาง
- [ ] แก้ Code ก่อนถูกใช้อ้างอิงได้โดย ID คงเดิม; หลังถูกอ้างอิงแก้ไม่ได้ รวมกรณีข้อมูลอ้างอิงถูกปิดใช้งานหรือมีการเพิ่ม Reference พร้อมกับแก้รหัส; Audit สะท้อนผลจริง
- [ ] Name บังคับกรอก สูงสุด 200 ตัวอักษร รองรับไทย/อังกฤษในช่องเดียว; ปฏิเสธค่าว่าง/ช่องว่างล้วนและค่าที่ยาวเกินกำหนด
- [ ] Dependency Direction ถูก ไม่ให้ Domain อ้าง Infrastructure
- [ ] รับ ID ผ่าน Request DTO Body ไม่เป็น URL Path Parameter
- [ ] FluentValidation ครอบคลุม Request และตอบ Field Errors
- [ ] API Error Friendly ไม่เผย Raw Technical Exception
- [ ] Global Error Handler / TraceId / Consistent Error Contract
- [ ] Constraints, Transaction, Concurrency ปกป้อง Data Integrity
- [ ] ผู้ใช้สองคนเปิดเวอร์ชันเดียวกัน: คนแรกบันทึกสำเร็จ คนที่สองได้ Conflict โดยไม่เขียนทับหรือสร้าง Success Audit; โหลดล่าสุดแล้วคงค่าที่กรอกไว้ดูอ้างอิงและบันทึกใหม่ได้เมื่อผ่านสิทธิ์/Validation

### Frontend & UX

- [ ] Angular อยู่ `Web/spa`
- [ ] Component แยก TS/HTML/SCSS; โค้ดอ่านง่าย
- [ ] Feature Routing + Lazy Loading + Route Guards
- [ ] Reactive Form Validation + Server FormError Mapping
- [ ] Loading/Processing ป้องกันผู้ใช้กดรายการซ้ำระหว่าง API ทำงาน
- [ ] NotificationService + HTTP Error Interceptor กลาง
- [ ] ConfirmDialogService/Modal กลางพร้อม Default; No Browser Alert/Confirm/Prompt
- [ ] Tailwind CSS + Angular CDK + Custom UI Components
- [ ] HTML Tags/Control ตรวจ Semantic, UX, Accessibility และ Interaction States
- [ ] ทุกหน้า Responsive; Tested Viewports + Keyboard/Screen Reader
- [ ] Sidebar Expand/Collapse/Drawer + Permission Navigation
- [ ] Company Switcher เปลี่ยนบริบทข้อมูลและ Role/สิทธิ์ตามบริษัทที่เลือก
- [ ] Header Responsive + User Menu/Breadcrumb
- [ ] Table Search/Filter/Sort/Pagination/Row Actions/Loading/Empty/Error

### Program Registry & Initial Scope

- [ ] Module DB: DBRT01–DBRT07 ตรงกับ Organization Group A
- [ ] Module SU: SURT01–SURT09 ตรงกับ System Administration
- [ ] Convention `{MODULE}RT{NN}` เลข 2 หลัก เริ่ม 01 และ Unique
- [ ] Program Code ใช้ใน Permission/Menu/Audit/Feature Folders
- [ ] แยก Technical Program Code ออกจาก User-friendly Display Name
- [ ] ไม่ทำ Business Masters กลุ่ม B–F เป็น Initial Scope โดยไม่ได้รับอนุมัติ

### Security

- [ ] ASP.NET Core Identity + Cookie Auth เป็นค่าเริ่มต้น
- [ ] Policy-based/Role-based Authorization รายบริษัท; ไม่ใช้ MFA ตามมติล่าสุด
- [ ] Username/Email Login โดย Username ห้ามมี `@`; Admin ตั้งรหัสชั่วคราวเองตามกฎ อายุ 24 ชั่วโมง และบังคับเปลี่ยนก่อนใช้ระบบ
- [ ] CSRF/Secure HttpOnly Cookie/HTTPS/Rate Limit/Login Lockout
- [ ] API Data Scope Authorization ไม่พึ่งการซ่อน Menu
- [ ] Scope ระดับบริษัท/สาขา/แผนกและการสืบทอดถูกต้อง; Action Permissions ผ่าน Role เท่านั้น
- [ ] Role ผู้ดูแลข้อมูลส่วนกลางควบคุมการแก้ไข Position/BusinessUnit ที่ใช้ร่วมกัน
- [ ] Idle 30 นาที / Absolute 8 ชั่วโมง; Revocation มีผลตั้งแต่ API Request ถัดไป; Logout/401/403 ถูกต้อง
- [ ] Frontend/API คนละโดเมน; Cookie/CORS/CSRF ทดสอบตามโดเมนจริง
- [ ] Audit Log และ Security Events ไม่รั่ว Secret/PII เกินความจำเป็น
- [ ] เปิดช่องรองรับ OIDC/SSO ในอนาคต ไม่ทำ Custom Crypto

### Decisions & Delivery

- [ ] Decision Pending ถูกเปิด ADR ก่อน Implement ส่วนที่กระทบระยะยาว
- [ ] Solution `clean` ใช้ .NET 10 LTS / EF Core 10 / Npgsql EF Provider 10 / Angular 22 / PostgreSQL 18; มีคำสั่ง Build/Test ในเครื่อง; ยังไม่เชื่อม CI หรือ Email/SMS
- [ ] First Vertical Slice DBRT01 ผ่าน Integration/E2E/Security/A11y
- [ ] ขยาย DB 7 + SU 9 ตาม Scope พร้อม Tests
- [ ] Deploy/Backup/Monitoring/Recovery & Operations ผ่านการตรวจรับ

---

## 28. แหล่งมาตรฐานสำหรับใช้ตรวจสอบระหว่างลงมือพัฒนา

> รายการเหล่านี้เป็นจุดเริ่มต้นของ Documentation ทางการสำหรับตรวจการ Implement; ต้องตรวจ Version ที่ใช้งานจริงก่อนคัดลอกตัวอย่างโค้ด ไม่ถือว่าเป็น Specification เพิ่มใหม่ที่ผู้ใช้ยังไม่ได้ยืนยัน

- **Microsoft ASP.NET Core** — Authentication, Cookie Authentication, Identity, Authorization, Exception Handling, Antiforgery, Data Protection: <https://learn.microsoft.com/aspnet/core/>
- **Microsoft EF Core** — DbContext, Configuration, Keys, Migrations, Concurrency: <https://learn.microsoft.com/ef/core/>
- **FluentValidation** — Validators, DI, ASP.NET Core Integration: <https://docs.fluentvalidation.net/>
- **Angular** — Routing, Reactive Forms, HttpClient Interceptors, Accessibility, ControlValueAccessor: <https://angular.dev/>
- **Angular CDK** — Dialog, Overlay, Focus, A11y: <https://material.angular.dev/cdk/categories>
- **Tailwind CSS** — Responsive Design, Utility Classes: <https://tailwindcss.com/docs/responsive-design>
- **W3C WCAG 2.2** — Accessibility Criteria: <https://www.w3.org/TR/WCAG22/>
- **W3C WAI Authoring Practices** — Dialog, Navigation, Menus, Tables: <https://www.w3.org/WAI/ARIA/apg/>
- **MDN HTML** — Semantic Elements, Form Controls, Tables: <https://developer.mozilla.org/en-US/docs/Web/HTML>
- **IBM Carbon Design System** — Application Shell / UI Shell, Data Tables: <https://carbondesignsystem.com/>
- **OWASP** — Authentication, Session Management, CSRF, XSS, ASVS: <https://cheatsheetseries.owasp.org/>

---

## 29. เอกสารประกอบที่ควรสร้างคู่กับ `project-plan.md` เมื่อเริ่ม Implement

1. `docs/architecture/overview.md` — Diagram ระดับ System + Layer + Runtime/Dependencies
2. `docs/architecture/program-registry.md` — Source of Truth ของ DBRT/SURT และ Permission Keys
3. `docs/architecture/data-model.md` — ERD, PK/FK Decisions, Master Hierarchy, Audit
4. `docs/api/api-conventions.md` — Endpoints, DTOs, Error Contract, Status & Pagination
5. `docs/security/security-design.md` — Identity, Temporary Password Flow, Cookie, CSRF, Company Permissions/Scope, Session; บันทึกมติไม่ใช้ MFA
6. `docs/ui-design-system/ui-standards.md` — Tokens, Components, A11y, Responsive, Modal/Notification
7. `docs/operations/deployment.md` — Env, Migration, Secrets, Hosting, Monitoring, Backup
8. `docs/adr/ADR-*.md` — มติออกแบบสำหรับหัวข้อที่รอการตัดสินใจ
9. Tests & Acceptance Case Matrix ต่อ Feature

> เอกสารนี้ถือเป็น **Baseline Requirements & Architecture Plan** ไม่ใช่โค้ดที่นำไป Deploy ได้ทันที และไม่ได้แปลว่าทุกหัวข้อที่เป็น `[แนวทางแนะนำ]` ได้รับการอนุมัติเรียบร้อย ต้องทำตามสถานะ Decision ใน Section 0 และ ADR Register

---

## 30. Final Scope Statement

**สิ่งที่ยืนยันและต้องพัฒนา:**

- Backend Clean Architecture 4 Layers (`Application`, `Domain`, `Infrastructure`, `Web`)
- Angular SPA ใน `Web/spa`; Full Components; Feature Routing; Responsive Mobile-first
- PostgreSQL Database/Schema เดียว; Context แยกโมดูลผ่าน Interface ใน Application; ตารางรูปแบบ `DbBranch`, `DbCostCenter`, `SuUser`, `SuUserPermission`
- Identity User/Role อยู่ใน SU DbContext; SU อ่านข้อมูลองค์กรผ่านบริการของโมดูล DB เช่น IOrganizationLookup
- MediatR/CQRS; ข้อมูลหลัก/User/Role ใช้ BIGINT ที่ฐานข้อมูลสร้าง; BIGINT ID ใน JSON เป็น string; API ใช้ Program Code + Request DTO Body รวม ID และยังไม่มี Version ใน URL
- FluentValidation 12.x พร้อม DI ที่เข้ากันได้และ MediatR Validation Pipeline กลางเรียก ValidateAsync ก่อน Handler + Reactive Forms Frontend
- Friendly Global Error + Central Notification/FormError Services
- Loading/Disable/Gate เพื่อป้องกัน Duplicate Submission ระหว่าง API ประมวลผล
- Central Confirm Dialog ไม่ใช้ Browser Alert/Confirm/Prompt
- Tailwind 4.x ใน CSS กลาง แยกจาก SCSS เฉพาะ Component + Angular CDK + Custom UI Components, Semantic HTML/A11y/UX Review
- Standard Sidebar/Header/Data Table + Company Switcher; หลายบริษัทและ Role/สิทธิ์ต่างกันรายบริษัท; ชุด Role แยกตามบริษัทและผู้ใช้มีหนึ่ง Role งานภายในบริษัทต่อบริษัท ส่วนสิทธิ์ระบบส่วนกลางแยกขอบเขตตามส่วน 17.4
- Data Scope ระดับบริษัท/สาขา/แผนกพร้อมการสืบทอดสู่หน่วยลูก; Action Permissions ผ่าน Role เท่านั้น
- Company → Branch → Division → Department; Position/BusinessUnit ใช้ร่วมกัน และ CostCenter แยกตามบริษัท
- Role ผู้ดูแลข้อมูลส่วนกลางเท่านั้นที่สร้าง/แก้ไข/ปิดใช้งาน Position/BusinessUnit
- ผู้ดูแลบัญชีส่วนกลางเท่านั้นที่สร้าง/แก้ข้อมูล Login/รีเซ็ตรหัส/ปลดล็อก/ปิดบัญชี; สิทธิ์ดูแลบัญชีและดูแลข้อมูลส่วนกลางแยกกัน
- ผู้ดูแลบริษัทสร้าง/ปรับ Role และกำหนด Role/Scope ให้ผู้ใช้ได้เฉพาะขอบเขตที่ส่วนกลางอนุญาต; ห้ามเพิ่มสิทธิ์ตนเองเกินขอบเขตและห้ามข้ามบริษัท
- รหัสฝ่ายและรหัสแผนกไม่ซ้ำภายในบริษัท โดยตรวจแยกตามประเภทข้อมูล
- รหัสสาขาและศูนย์ต้นทุนไม่ซ้ำภายในบริษัท แยกตามประเภทข้อมูล; รหัสข้อมูลหลักตัดช่องว่างหัวท้ายและแปลงเป็นตัวพิมพ์ใหญ่ ยาว 1–50 ตัวอักษร ใช้เฉพาะ A–Z/0–9/-/_ และไม่มีช่องว่างภายใน
- Code ของข้อมูลหลักแก้ได้ก่อนมีข้อมูลอื่นอ้างอิง หลังจากนั้นล็อก โดย ID คงเดิม; Name ช่องเดียวบังคับกรอก สูงสุด 200 ตัวอักษร รองรับภาษาไทยหรืออังกฤษ
- Organization Master `DBRT01`–`DBRT07` ปิดใช้งานและเปิดกลับได้ ไม่มีลบจริงผ่านหน้าจอ/API แม้รายการยังไม่เคยใช้
- ปิดบริษัท/สาขา/ฝ่ายได้เมื่อปิดหน่วยงานลูกแล้ว และ Admin ย้ายหรือยกเลิกสิทธิ์ที่ผูกตรงก่อน; ไม่ปิดลูก/ยกเลิกสิทธิ์ให้อัตโนมัติ
- ระยะแรกไม่ย้ายฝ่ายข้ามสาขาหรือแผนกข้ามฝ่ายภายในบริษัทหลังมีรายการธุรกิจอ้างอิง; การปรับองค์กรใช้หน่วยงานและรหัสใหม่เพื่อคงประวัติเดิม
- Module DB Organization Master 7 โปรแกรม (`DBRT01`–`DBRT07`)
- Module SU System Administration 9 โปรแกรม (`SURT01`–`SURT09`)
- Login ด้วย Username/Email ผ่าน ASP.NET Core Identity + Secure Cookie + Policy-Based Permissions; ไม่ใช้ MFA; รองรับ SSO ภายหลัง
- Admin ตั้งรหัสผ่านชั่วคราวเองตามกฎเพื่อสร้าง/รีเซ็ตบัญชี อายุ 24 ชั่วโมงนับจากออกและบังคับเปลี่ยนก่อนใช้ระบบ; Username ห้ามมี `@`; ระยะแรกไม่ส่ง Email/SMS
- Admin ส่งมอบรหัสชั่วคราวโดยตรงหลังยืนยันตัวตน ต่อหน้าหรือโทรกลับเบอร์ที่องค์กรยืนยันไว้
- รหัสผ่าน 15–128 ตัวอักษร รองรับวลี/ช่องว่าง ไม่บังคับผสมชนิดอักขระ และปฏิเสธ Common/Compromised Password; รหัสปกติไม่เปลี่ยนตามรอบ แต่บังคับเปลี่ยนเมื่อมีหลักฐานว่าถูกเปิดเผย
- ตรวจรายการรหัสพบบ่อยภายในร่วมกับ HIBP API ผ่าน Hash Prefix ตอนสร้าง/รีเซ็ต/เปลี่ยนรหัส; ตรวจไม่ได้ให้หยุดตั้งรหัสและลองใหม่ ส่วน Login ด้วยรหัสเดิมไม่พึ่ง HIBP และยังใช้กฎบัญชีเดิม
- Login ผิดติดต่อกัน 5 ครั้งล็อก 15 นาที โดยนับ Username/Email รวมต่อบัญชี; มี Rate Limit เพิ่มเติม และ Admin ปลดล็อกได้ตามสิทธิ์
- Session Idle 30 นาที / Absolute 8 ชั่วโมง; ตรวจ Revocation ทุก API Request และมีผลตั้งแต่ Request ถัดไป
- Audit Before/After เฉพาะ Allowlist พร้อมปกปิดข้อมูลอ่อนไหว; Organization Master เก็บ Code, Name, ID หน่วยงานแม่ และ IsActive ส่วน TaxId/ข้อมูลติดต่อ/ฟิลด์อื่นและข้อมูล SU ต้องกำหนดแยก
- Audit บริษัทมอบ VIEW/EXPORT ผ่าน Role บริษัทได้ตามบริษัท/สาขา/แผนกที่ได้รับ ทั้งหน้าจอและไฟล์ส่งออก; ข้อมูลบัญชีส่วนกลางและ Login Log เฉพาะผู้มีสิทธิ์ตรวจสอบส่วนกลาง
- ข้อมูลธุรกิจและ Success Audit ของ Create/Update/Deactivate ใช้ Transaction เดียวกัน สำเร็จพร้อมกันหรือ Rollback ทั้งหมด; Login/Failure Logs แยกจากมตินี้
- ตรวจเวอร์ชันก่อนบันทึกเพื่อป้องกันเขียนทับ; Conflict ให้โหลดล่าสุดและแก้ใหม่ พร้อมเก็บค่าที่กรอกไว้ดูอ้างอิง ระยะแรกไม่มี UI เปรียบเทียบ/รวมการแก้ไข
- Solution/Namespace `clean`; Frontend/API คนละโดเมน; เตรียม Build/Test ในเครื่องและยังไม่เชื่อม CI
- เวอร์ชันหลัก: .NET 10 LTS + EF Core 10 + Npgsql EF Provider 10, Angular 22, PostgreSQL 18; ใช้แพตช์ที่ออกจริงและเข้ากันได้ในสายที่เลือก

**สิ่งที่เลื่อนไว้ตามคำตอบผู้ใช้:** Hosting/โดเมนจริงและ Audit Retention ตัดสินใจก่อน Production; Program Sequence >99 ตัดสินใจเมื่อใกล้เต็ม; API Versioning กำหนดก่อนเปิด External Clients ส่วน MediatR ผู้ใช้ยืนยัน 12.5.0 / Apache-2.0 ก่อนเริ่มติดตั้งแล้ว

**รายละเอียดออกแบบที่ยังต้องทำภายใต้มติที่ยืนยัน:** Fields เสริม/Constraints/Unique Scope ของข้อมูลอื่น และรายการ Reference ที่ทำให้ล็อก Code ตามมติที่ยืนยัน, Foreign Keys/DTO Contracts และกลไก Transaction ข้าม Context, Keys ของตารางระบบอื่นนอกจาก User/Role, ตาราง Role/Scope Assignment ที่บังคับ Role แยกบริษัทและหนึ่ง Role ต่อผู้ใช้ต่อบริษัท, ชุดสิทธิ์/Scope ที่ส่วนกลางอนุญาตให้มอบต่อและ Policy ของผู้ดูแล/ผู้ตรวจสอบแต่ละขอบเขตตามมติที่ยืนยัน, รูปแบบ Concurrency Token ตามพฤติกรรม Conflict ที่ยืนยัน, Audit Allowlist ของข้อมูล SU/ฟิลด์เพิ่มเติม, ชุดรายการรหัสพบบ่อยภายใน/วิธีปรับปรุงและรายละเอียด HIBP Integration ตามมติที่ยืนยัน, ขั้นตอนตรวจตัวตนและแหล่งเบอร์ที่องค์กรยืนยันสำหรับส่งมอบรหัสโดยตรง และ Cookie/CORS/CSRF ตามโดเมนจริง รายการเหล่านี้ไม่ถือว่าได้รับการยืนยันรายละเอียดโดยอัตโนมัติ

**หลักการตัดสินใจ:** เริ่มจากแกนระบบเล็กที่สุดที่ครอบคลุมข้อกำหนดที่ยืนยันจริง ทำ DBRT01 เป็น Full Vertical Slice ที่ผ่าน Tests ก่อน แล้วนำรูปแบบเดียวกันไปขยาย DB/SU โดยไม่เร่งเพิ่ม Microservices, Event Broker หรือ Business Masters อื่นจนมีความจำเป็น

---

*สิ้นสุดเอกสาร `project-plan.md`*
