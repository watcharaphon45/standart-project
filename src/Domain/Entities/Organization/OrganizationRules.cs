using System.Text.RegularExpressions;
using clean.Domain.Exceptions;

namespace clean.Domain.Entities.Organization;

public static partial class OrganizationRules
{
    public static string NormalizeCode(string? code) => (code ?? "").Trim().ToUpperInvariant();

    public static (string Code, string Name) NormalizeAndValidate(string code, string name)
    {
        var normalized = NormalizeCode(code);
        if (!CodePattern().IsMatch(normalized))
            throw new RuleException("INVALID_CODE", "รหัสต้องยาว 1–50 ตัว ใช้ A–Z, 0–9, - หรือ _", 400);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            throw new RuleException("INVALID_NAME", "กรุณาระบุชื่อไม่เกิน 200 ตัวอักษร", 400);
        return (normalized, name.Trim());
    }

    public static void CheckVersion(long expected, long actual)
    {
        if (expected != actual)
            throw new RuleException("CONCURRENCY_CONFLICT", "ข้อมูลถูกแก้ไขแล้ว กรุณาโหลดข้อมูลล่าสุดและแก้ใหม่");
    }

    public static void EnsureActiveForUpdate(bool isActive, string inactiveCode)
    {
        if (!isActive)
            throw new RuleException(inactiveCode, "กรุณาเปิดใช้งานข้อมูลก่อนแก้ไข");
    }

    public static void EnsureCodeUnlocked(bool codeLocked, string normalized, string current)
    {
        if (codeLocked && normalized != current)
            throw new RuleException("CODE_LOCKED", "รหัสถูกใช้อ้างอิงแล้ว จึงเปลี่ยนรหัสไม่ได้");
    }

    public static void EnsureReparentUnlocked(bool codeLocked, long nextParentId, long currentParentId)
    {
        if (codeLocked && nextParentId != currentParentId)
            throw new RuleException("REPARENT_LOCKED", "ข้อมูลถูกใช้อ้างอิงแล้ว จึงย้ายหน่วยงานแม่ไม่ได้");
    }

    public static void EnsureCanChangeStatus(
        bool targetActive,
        bool currentActive,
        bool parentActive,
        bool hasActiveChildren,
        bool hasAssignments)
    {
        if (targetActive && !parentActive)
            throw new RuleException("PARENT_INACTIVE", "กรุณาเปิดใช้งานหน่วยงานแม่ก่อน");
        if (!targetActive && hasActiveChildren)
            throw new RuleException("ACTIVE_CHILDREN", "กรุณาปิดหน่วยงานลูกก่อนปิดรายการนี้");
        if (!targetActive && hasAssignments)
            throw new RuleException("ACTIVE_ASSIGNMENTS", "กรุณาย้ายหรือยกเลิกสิทธิ์ที่ผูกกับหน่วยงานนี้ก่อน");
        if (currentActive == targetActive)
            throw new RuleException("STATUS_UNCHANGED", "สถานะข้อมูลเปลี่ยนไปแล้ว กรุณาโหลดข้อมูลล่าสุด");
    }

    [GeneratedRegex(@"\A[A-Z0-9_-]{1,50}\z", RegexOptions.CultureInvariant)]
    private static partial Regex CodePattern();
}

