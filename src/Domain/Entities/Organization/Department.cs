namespace clean.Domain.Entities.Organization;

public sealed class Department
{
    private Department() { }

    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public long DivisionId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public bool CodeLocked { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long CreatedBy { get; private set; }
    public long UpdatedBy { get; private set; }

    public static Department Create(long companyId, long divisionId, string code, string name, long actor, DateTimeOffset now)
    {
        var (normalized, trimmedName) = OrganizationRules.NormalizeAndValidate(code, name);
        return new Department
        {
            CompanyId = companyId,
            DivisionId = divisionId,
            Code = normalized,
            Name = trimmedName,
            CreatedBy = actor,
            CreatedAtUtc = now,
            UpdatedBy = actor,
            UpdatedAtUtc = now
        };
    }

    public void Update(long divisionId, string code, string name, long expectedVersion, long actor, DateTimeOffset now)
    {
        OrganizationRules.CheckVersion(expectedVersion, Version);
        OrganizationRules.EnsureActiveForUpdate(IsActive, "DEPARTMENT_INACTIVE");
        OrganizationRules.EnsureReparentUnlocked(CodeLocked, divisionId, DivisionId);
        var (normalized, trimmedName) = OrganizationRules.NormalizeAndValidate(code, name);
        OrganizationRules.EnsureCodeUnlocked(CodeLocked, normalized, Code);
        DivisionId = divisionId;
        Code = normalized;
        Name = trimmedName;
        Touch(actor, now);
    }

    public void ChangeStatus(
        bool active,
        long expectedVersion,
        bool parentActive,
        bool hasAssignments,
        long actor,
        DateTimeOffset now)
    {
        OrganizationRules.CheckVersion(expectedVersion, Version);
        OrganizationRules.EnsureCanChangeStatus(active, IsActive, parentActive, false, hasAssignments);
        IsActive = active;
        Touch(actor, now);
    }

    public void LockCode()
    {
        if (CodeLocked) return;
        CodeLocked = true;
        Version = checked(Version + 1);
    }

    private void Touch(long actor, DateTimeOffset now)
    {
        UpdatedBy = actor;
        UpdatedAtUtc = now;
        Version = checked(Version + 1);
    }
}

