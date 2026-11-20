using System.Security.Claims;
using clean.Application.Common.Interfaces;
using clean.Web.Middleware;

namespace clean.Web.Authorization;

public sealed class CurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    public long UserId => long.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public string TraceId => RequestObservabilityMiddleware.ResolveTraceId(accessor.HttpContext);
}
