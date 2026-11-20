# เชื่อม PostgreSQL จากเครื่อง B ไปเครื่อง A

เครื่อง A เป็นเครื่องที่รัน Container ส่วนเครื่อง B เป็นเครื่องที่เปิด DBeaver หรือรัน API ให้ใช้ IP/ชื่อของเครื่อง A และ Host Port 55432 ส่วน 5432 เป็น Port ภายใน Container

## เครื่อง A

เพิ่มบรรทัดต่อไปนี้ใน .local/database.env โดยเก็บ POSTGRES_PASSWORD เดิมไว้:

```dotenv
POSTGRES_BIND_ADDRESS=0.0.0.0
```

Compose ใช้ 127.0.0.1 เป็นค่าเริ่มต้น เมื่อกำหนด 0.0.0.0 จึงรับการเชื่อมต่อจาก Network Interface ของเครื่อง A

```powershell
docker compose --env-file .local/database.env -f compose.yaml up -d --wait
docker compose --env-file .local/database.env -f compose.yaml ps
```

การเปลี่ยน Port Binding จะ Recreate Container และทำให้การเชื่อมต่อเดิมขาดช่วง แต่ยังใช้ Volume เดิม ไม่ต้อง down -v

หาก Windows Firewall ยังไม่อนุญาต ให้เปิด PowerShell แบบ Administrator บนเครื่อง A และแทนที่ IP เครื่อง B ให้ตรง:

```powershell
New-NetFirewallRule -DisplayName "clean PostgreSQL from machine B" -Direction Inbound -Action Allow -Protocol TCP -LocalPort 55432 -RemoteAddress "<IP เครื่อง B>" -Profile Domain,Private
```

กฎนี้อนุญาตเฉพาะเครื่อง B บน Domain/Private Network ไม่ต้องปิด Firewall ทั้งระบบ หากเครื่องอยู่คนละ Subnet ให้ตรวจ Routing/Firewall ของเครือข่ายด้วย

## เครื่อง B

| ช่อง | ค่า |
|---|---|
| Host | IP หรือชื่อเครื่อง A |
| Port | 55432 |
| Database | clean_dev |
| Username | clean_dev |
| Password | POSTGRES_PASSWORD จาก .local/database.env บนเครื่อง A |

รหัสนี้เป็นรหัสบัญชีฐานข้อมูล ไม่ใช่รหัส Login ของ demo และไม่ควรนำรหัสที่สร้างใหม่บนเครื่อง B มาใช้แทนของเครื่อง A

```powershell
Test-NetConnection <IP เครื่อง A> -Port 55432
```

หาก TcpTestSucceeded เป็น False ให้ตรวจ Host, Port Binding, Firewall และเส้นทางเครือข่ายก่อน หากเป็น True แล้ว DBeaver ยังเข้าไม่ได้ ให้ตรวจข้อความผิดพลาดของ PostgreSQL และชื่อผู้ใช้/รหัสผ่าน

หาก API รันบนเครื่อง B ด้วย ให้กำหนด ConnectionStrings__Database เป็น Host=<IP เครื่อง A>;Port=55432;Database=clean_dev;Username=clean_dev;Password=<รหัสฐานข้อมูลเครื่อง A> ผ่าน Environment/User Secrets แทนค่า localhost ห้าม Commit รหัสผ่านลงไฟล์ที่ Git ติดตาม

[อ้างอิง Docker Port Publishing](https://docs.docker.com/engine/network/port-publishing/)
