namespace clean.Domain.Entities.System;

public sealed class CompanyAssignment
{
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public long RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    // DBRT01 operates on the entire company. Narrower grants never authorize it.
    public string ScopeType { get; set; } = "Company";
    public long? ScopeId { get; set; }
    public long Version { get; set; } = 1;
}
