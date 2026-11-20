namespace clean.Domain.Entities.System;

public sealed class RolePermission
{
    public long RoleId { get; set; }
    public string Permission { get; set; } = "";
}
