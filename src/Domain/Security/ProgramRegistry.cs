namespace clean.Domain.Security;

public sealed record ProgramDefinition(string Code, string Name, string Route, bool Implemented);

public static class ProgramRegistry
{
    public static readonly IReadOnlyList<ProgramDefinition> Programs =
    [
        new("DBRT01", "ข้อมูลบริษัท", "/db/dbrt01", true),
        new("DBRT02", "ข้อมูลสาขา", "/db/dbrt02", true),
        new("DBRT03", "ข้อมูลแผนก", "/db/dbrt03", true),
        new("DBRT04", "ข้อมูลฝ่าย", "/db/dbrt04", true),
        new("DBRT05", "ตำแหน่งงาน", "/db/dbrt05", true),
        new("DBRT06", "ศูนย์ต้นทุน", "/db/dbrt06", true),
        new("DBRT07", "หน่วยธุรกิจ", "/db/dbrt07", true),
        new("SURT01", "ผู้ใช้งาน", "/su/surt01", true),
        new("SURT02", "บทบาท", "/su/surt02", true),
        new("SURT03", "สิทธิ์", "/su/surt03", true),
        new("SURT04", "ขอบเขตผู้ใช้งาน", "/su/surt04", true),
        new("SURT05", "เมนู", "/su/surt05", true),
        new("SURT06", "เลขที่เอกสาร", "/su/surt06", true),
        new("SURT07", "ขั้นตอนอนุมัติ", "/su/surt07", true),
        new("SURT08", "ตั้งค่าระบบ", "/su/surt08", true),
        new("SURT09", "ประวัติการใช้งาน", "/su/surt09", true)
    ];

    public static readonly string[] Actions =
        ["VIEW", "CREATE", "UPDATE", "ACTIVATE", "DEACTIVATE"];

    public static readonly string[] OrganizationProgramCodes =
        ["DBRT01", "DBRT02", "DBRT03", "DBRT04", "DBRT05", "DBRT06", "DBRT07"];

    public static readonly string[] CompanyPermissions =
        ["DBRT01.VIEW", "DBRT01.CREATE", "DBRT01.UPDATE", "DBRT01.ACTIVATE", "DBRT01.DEACTIVATE"];

    public static readonly string[] OrganizationPermissions =
        OrganizationProgramCodes.SelectMany(code => Actions.Select(action => $"{code}.{action}")).ToArray();

    public static readonly string[] CompanyScopedOrganizationPermissions =
        new[] { "DBRT02", "DBRT03", "DBRT04", "DBRT06" }
            .SelectMany(code => Actions.Select(action => $"{code}.{action}")).ToArray();

    public static readonly string[] SharedMasterPermissions =
        new[] { "DBRT05", "DBRT07" }
            .SelectMany(code => Actions.Select(action => $"{code}.{action}")).ToArray();

    public static readonly string[] UserManagementPermissions =
        ["SURT01.VIEW", "SURT01.CREATE", "SURT01.UPDATE", "SURT01.RESET", "SURT01.UNLOCK", "SURT01.ACTIVATE", "SURT01.DISABLE"];

    public static readonly string[] RoleManagementPermissions =
        Actions.Select(action => $"SURT02.{action}").ToArray();

    public static readonly string[] PermissionManagementPermissions =
        ["SURT03.VIEW", "SURT03.UPDATE"];

    public static readonly string[] UserScopePermissions =
        ["SURT04.VIEW", "SURT04.UPDATE"];

    public static readonly string[] CompanyAccessAdminPermissions =
        RoleManagementPermissions
            .Concat(PermissionManagementPermissions)
            .Concat(UserScopePermissions)
            .ToArray();

    public static readonly string[] MenuConfigPermissions =
        ["SURT05.VIEW", "SURT05.UPDATE", "SURT05.ACTIVATE", "SURT05.DEACTIVATE"];

    public static readonly string[] DocumentSequencePermissions =
        Actions.Select(action => $"SURT06.{action}").ToArray();

    public static readonly string[] ApprovalWorkflowPermissions =
        Actions.Select(action => $"SURT07.{action}").ToArray();

    public static readonly string[] SystemSettingPermissions =
        ["SURT08.VIEW", "SURT08.CREATE", "SURT08.UPDATE"];

    public static readonly string[] AuditLogPermissions =
        ["SURT09.VIEW", "SURT09.EXPORT"];

    public static readonly string[] SystemOperationsPermissions =
        MenuConfigPermissions
            .Concat(DocumentSequencePermissions)
            .Concat(ApprovalWorkflowPermissions)
            .Concat(SystemSettingPermissions)
            .Concat(AuditLogPermissions)
            .ToArray();

    public static readonly string[] SystemPermissions =
        UserManagementPermissions
            .Concat(CompanyAccessAdminPermissions)
            .Concat(SystemOperationsPermissions)
            .ToArray();

    public static readonly string[] AllPermissions =
        OrganizationPermissions
            .Concat(SystemPermissions)
            .ToArray();

    public static readonly string[] GlobalOnlyPermissions =
    [
        "DBRT01.CREATE",
        "DBRT05.CREATE", "DBRT05.UPDATE", "DBRT05.ACTIVATE", "DBRT05.DEACTIVATE",
        "DBRT07.CREATE", "DBRT07.UPDATE", "DBRT07.ACTIVATE", "DBRT07.DEACTIVATE",
        ..UserManagementPermissions,
        ..MenuConfigPermissions
    ];

    public static readonly string[] CompanyGrantablePermissions =
        AllPermissions.Except(GlobalOnlyPermissions).ToArray();

    public static readonly IReadOnlySet<string> CompanyContextViewPermissions =
        OrganizationProgramCodes
            .Concat(["SURT02", "SURT03", "SURT04", "SURT06", "SURT07", "SURT08", "SURT09"])
            .Select(code => $"{code}.VIEW")
            .ToHashSet(StringComparer.Ordinal);

    public static bool AllowsScope(string scopeType, string permission)
    {
        if (GlobalOnlyPermissions.Contains(permission)) return false;
        var dot = permission.IndexOf('.');
        if (dot <= 0) return false;
        var program = permission[..dot];
        return program switch
        {
            "DBRT01" or "DBRT06" => scopeType == "Company",
            "DBRT02" => permission == "DBRT02.CREATE"
                ? scopeType == "Company"
                : scopeType is "Company" or "Branch",
            "DBRT04" => scopeType is "Company" or "Branch",
            "DBRT03" => permission == "DBRT03.CREATE"
                ? scopeType is "Company" or "Branch"
                : scopeType is "Company" or "Branch" or "Department",
            "DBRT05" or "DBRT07" => permission is "DBRT05.VIEW" or "DBRT07.VIEW"
                && scopeType is "Company" or "Branch" or "Department",
            "SURT02" or "SURT03" or "SURT04" or "SURT06" or "SURT07" or "SURT08" => scopeType == "Company",
            "SURT09" => scopeType is "Company" or "Branch" or "Department",
            _ => scopeType == "Company"
        };
    }
}
