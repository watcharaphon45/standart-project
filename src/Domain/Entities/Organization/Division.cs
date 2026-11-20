namespace clean.Domain.Entities.Organization;

public sealed class Division
{
    private Division() { }

    public long Id { get; private set; }
    public long CompanyId { get; private set; }
    public long BranchId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public bool CodeLocked { get; private set; }
    public long Version { get; private set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public long CreatedBy { get; private set; }
    public long UpdatedBy { get; private set; }

    public static Division Create(long companyId, long branchId, string code, string name, long actor, DateTimeOffset now)
    {
        var (normalized, trimmedName) = OrganizationRules.NormalizeAndValidate(code, name);
        return new Division
        {
            CompanyId = companyId,
            BranchId = branchId,
            Code = normalized,
            Name = trimmedName,
            CreatedBy = actor,
            CreatedAtUtc = now,
            UpdatedBy = actor,
            UpdatedAtUtc = now
        };
    }

    public void Update(long branchId, string code, string name, long expectedVersion, long actor, DateTimeOffset now)
    {
        OrganizationRules.CheckVersion(expectedVersion, Version);
        OrganizationRules.EnsureActiveForUpdate(IsActive, "DIVISION_INACTIVE");
        OrganizationRules.EnsureReparentUnlocked(CodeLocked, branchId, BranchId);
        var (normalized, trimmedName) = OrganizationRules.NormalizeAndValidate(code, name);
        OrganizationRules.EnsureCodeUnlocked(CodeLocked, normalized, Code);
        BranchId = branchId;
        Code = normalized;
        Name = trimmedName;
        Touch(actor, now);
    }

    public void ChangeStatus(
        bool active,
        long expectedVersion,
        bool parentActive,
        bool hasActiveChildren,
        long actor,
        DateTimeOffset now)
    {
        OrganizationRules.CheckVersion(expectedVersion, Version);
        OrganizationRules.EnsureCanChangeStatus(active, IsActive, parentActive, hasActiveChildren, false);
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

