# นโยบายไม่มีค่า License / Subscription

ผู้ใช้ยืนยันให้เลือกสิ่งที่ใช้ฟรีได้โดยไม่มีค่าใช้จ่ายสำหรับโปรเจกต์ รวมการใช้งานเชิงพาณิชย์ในองค์กร ไม่เลือก Trial, Paid Plan, Pay-per-use หรือ Community Tier ที่ต้องผ่านเกณฑ์รายได้/ขนาดองค์กร

## รายการที่เลือก

| ส่วน | การตัดสินใจ |
|---|---|
| MediatR | คง 12.5.0 / Apache-2.0 ไม่มีค่า License ไม่อัปเกรดข้ามไป License ใหม่ หากต้องเปลี่ยนให้ใช้ตัวแทนฟรี |
| .NET / ASP.NET Core / Identity / EF Core | รุ่นโอเพนซอร์สที่ใช้อยู่ ไม่มี Paid Support เป็นข้อกำหนด |
| FluentValidation / Npgsql | ใช้แพ็กเกจโอเพนซอร์สตามรุ่นใน Directory.Packages.props |
| Angular / CDK / Tailwind / RxJS | ใช้ส่วนโอเพนซอร์ส ไม่ซื้อ UI Kit/Template/Icon/Font หรือ Tailwind Plus |
| PostgreSQL | PostgreSQL License ใช้งานได้โดยไม่มีค่า License ติดตั้งเองหรือรันผ่าน Podman |
| HIBP | ใช้เฉพาะ Pwned Passwords API ที่ฟรี ไม่ใช้ Breach Search/Domain Monitoring แบบมี Subscription |
| เครื่องมือทดสอบ | xUnit, Playwright, axe-core ส่วนโอเพนซอร์ส ไม่ใช้บริการ Cloud/Enterprise แบบเสียเงิน |
| Container สำหรับพัฒนา | เลือก Podman / Podman Desktop (Apache-2.0) แทนการกำหนดให้ใช้ Docker Desktop |
| Hosting / Domain / Email / SMS | ยังไม่สั่งซื้อหรือเปิดบริการเสียเงิน ใช้เครื่องที่มีอยู่สำหรับพัฒนา |

## สิ่งที่แยกออกจากข้อกำหนด

Docker Desktop มีเงื่อนไขที่บางองค์กรต้องซื้อ Subscription จึงไม่ใช้เป็นเครื่องมือบังคับของโปรเจกต์ สคริปต์เริ่มฐานข้อมูลใหม่เลือก Podman เครื่องปัจจุบันยังมีฐานข้อมูลเดิมใน Docker และยังไม่ได้ย้ายข้อมูลไป Podman ซึ่งยังไม่ได้ติดตั้ง การเปลี่ยนเครื่องมือจะต้องสำรองและย้ายข้อมูลก่อน ไม่สร้างฐานใหม่ทับหรือทำให้บัญชีเดิมหายโดยอัตโนมัติ

IDE เป็นตัวเลือกของผู้พัฒนา โปรเจกต์ Build/Test ได้ด้วย .NET CLI และ npm ไม่บังคับ Visual Studio รุ่นเสียเงินหรือ Extension แบบเสียเงิน สิทธิ์ของ IDE/Windows/เครื่องมือที่ผู้ใช้มีอยู่เป็นคนละส่วนกับ Library ของแอป

การไม่มีค่า License ไม่ได้แปลว่าไม่มีค่าเครื่อง ไฟฟ้า อินเทอร์เน็ต Domain หรือ Cloud ให้เลือกใช้ทรัพยากรที่มีอยู่และไม่ผูกบริการที่เรียกเก็บเงิน

## ตรวจ Dependency และการอัปเกรด

- ตรวจ License และเงื่อนไขเชิงพาณิชย์ของรุ่นที่ติดตั้งจริง รวม Dependency ทางอ้อมก่อนเพิ่ม/อัปเกรด
- หากข้อมูล License ไม่ชัดเจน ให้ค้างการเพิ่มแพ็กเกจนั้นและเลือกตัวแทนที่ยืนยันใช้ฟรีได้
- เก็บ License/Copyright/NOTICE ที่แพ็กเกจกำหนดเมื่อแจกจ่าย ตัวแอปไม่ได้ต้องเปิด Source ทั้งหมดเพียงเพราะใช้ MediatR Apache-2.0
- Lockfile ระบุรุ่นจริง ห้ามเปลี่ยน MediatR ไป License ใหม่โดยอัตโนมัติ

## แหล่งตรวจสอบ

- [MediatR 12.5.0](https://www.nuget.org/packages/MediatR/12.5.0), [Apache-2.0](https://www.apache.org/licenses/LICENSE-2.0)
- [PostgreSQL License](https://www.postgresql.org/about/licence/)
- [HIBP Pwned Passwords ใช้ฟรีโดยไม่ต้อง Subscription](https://support.haveibeenpwned.com/hc/en-au/articles/15316377846671-I-wish-to-use-the-Pwned-Passwords-API-How-can-I-get-started)
- [Docker Desktop License](https://docs.docker.com/subscription-billing/desktop-license/)
- [Podman License](https://github.com/containers/podman/blob/main/LICENSE), [Podman Desktop License](https://github.com/podman-desktop/podman-desktop/blob/main/LICENSE)
