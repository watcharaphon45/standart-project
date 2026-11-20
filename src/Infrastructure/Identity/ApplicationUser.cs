using Microsoft.AspNetCore.Identity;
namespace clean.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<long>
{
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? TemporaryPasswordExpiresAtUtc { get; set; }
    public long Version { get; set; } = 1;
}
public sealed class ApplicationRole : IdentityRole<long>
{
    public long? CompanyId { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public bool CodeLocked { get; set; }
    public long Version { get; set; } = 1;
}
