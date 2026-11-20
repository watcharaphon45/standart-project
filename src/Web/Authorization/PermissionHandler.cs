using clean.Application.Common.Interfaces;
using clean.Domain.Security;
using clean.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;

namespace clean.Web.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionHandler(AccessService access, ICurrentActor actor) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (actor.UserId == 0 || context.User.HasClaim("must_change_password", "true")) return;
        var snapshot = await access.SnapshotAsync(actor.UserId, CancellationToken.None);
        if (snapshot.GlobalPermissions.Contains(requirement.Permission)
            || snapshot.Companies.Any(x =>
                ProgramRegistry.AllowsScope(x.ScopeType, requirement.Permission)
                && x.Permissions.Contains(requirement.Permission)))
        {
            context.Succeed(requirement);
        }
    }
}
