using System.Text.RegularExpressions;
using clean.Domain.Exceptions;

namespace clean.Domain.Entities.Organization;

public sealed partial class Company
{
    private Company() { }
    public long Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public bool CodeLocked { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long CreatedBy { get; private set; }
    public long UpdatedBy { get; private set; }

    public static string NormalizeCode(string? code) => (code ?? "").Trim().ToUpperInvariant();

    public static Company Create(string code, string name, long actor, DateTimeOffset now)
    {
        var company = new Company { CreatedBy = actor, CreatedAtUtc = now };
        company.SetValues(code, name);
        company.UpdatedBy = actor;
        company.UpdatedAtUtc = now;
        return company;
    }

    public void Update(string code, string name, long expectedVersion, long actor, DateTimeOffset now)
    {
        CheckVersion(expectedVersion);
        if (!IsActive) throw new RuleException("COMPANY_INACTIVE", "กรุณาเปิดใช้งานบริษัทก่อนแก้ไขข้อมูล");
        var normalized = NormalizeCode(code);
        if (CodeLocked && normalized != Code)
            throw new RuleException("CODE_LOCKED", "รหัสบริษัทถูกใช้อ้างอิงแล้ว จึงเปลี่ยนรหัสไม่ได้");
        SetValues(normalized, name);
        Touch(actor, now);
    }

    public void ChangeStatus(bool active, long expectedVersion, bool hasActiveChildren,
        bool hasAssignments, long actor, DateTimeOffset now)
    {
        CheckVersion(expectedVersion);
        if (!active && hasActiveChildren)
            throw new RuleException("ACTIVE_CHILDREN", "กรุณาปิดหน่วยงานลูกก่อนปิดบริษัท");
        if (!active && hasAssignments)
            throw new RuleException("ACTIVE_ASSIGNMENTS", "กรุณาย้ายหรือยกเลิกสิทธิ์ที่ผูกกับบริษัทก่อน");
        if (IsActive == active) throw new RuleException("STATUS_UNCHANGED", "สถานะข้อมูลเปลี่ยนไปแล้ว กรุณาโหลดข้อมูลล่าสุด");
        IsActive = active;
        Touch(actor, now);
    }

    public void LockCode()
    {
        if (CodeLocked) return;
        CodeLocked = true;
        Version++;
    }

    private void CheckVersion(long expected)
    {
        if (expected != Version) throw new RuleException("CONCURRENCY_CONFLICT", "ข้อมูลถูกแก้ไขแล้ว กรุณาโหลดข้อมูลล่าสุดและแก้ใหม่");
    }

    private void SetValues(string code, string name)
    {
        code = NormalizeCode(code);
        if (!CodePattern().IsMatch(code)) throw new RuleException("INVALID_CODE", "รหัสต้องยาว 1–50 ตัว ใช้ A–Z, 0–9, - หรือ _", 400);
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new RuleException("INVALID_NAME", "กรุณาระบุชื่อไม่เกิน 200 ตัวอักษร", 400);
        Code = code;
        Name = name.Trim();
    }

    private void Touch(long actor, DateTimeOffset now)
    {
        UpdatedBy = actor;
        UpdatedAtUtc = now;
        Version = checked(Version + 1);
    }

    [GeneratedRegex(@"\A[A-Z0-9_-]{1,50}\z", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}
