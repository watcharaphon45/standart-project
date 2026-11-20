using clean.Web.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace clean.Web.Authorization;

public sealed class ValidateCsrfFilter(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method)
            || HttpMethods.IsHead(context.HttpContext.Request.Method)
            || HttpMethods.IsOptions(context.HttpContext.Request.Method)) return;
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException)
        {
            var problem = new ProblemDetails
            {
                Status = 400, Title = "ข้อมูลยืนยันคำขอหมดอายุ กรุณาโหลดหน้าใหม่แล้วลองอีกครั้ง"
            };
            problem.Extensions["code"] = "CSRF_VALIDATION_FAILED";
            problem.Extensions["traceId"] = RequestObservabilityMiddleware.ResolveTraceId(context.HttpContext);
            context.Result = new BadRequestObjectResult(problem);
        }
    }
}
