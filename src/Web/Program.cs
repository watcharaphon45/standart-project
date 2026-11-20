using System.Threading.RateLimiting;
using clean.Application;
using clean.Application.Common.Interfaces;
using clean.Domain.Security;
using clean.Infrastructure;
using clean.Infrastructure.Services;
using clean.Web.Authorization;
using clean.Web.Configuration;
using clean.Web.ExceptionHandling;
using clean.Web.Health;
using clean.Web.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.AddLocalDevelopmentDatabase();
var readiness = builder.ConfigureProductionReadiness();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCleanHealthChecks();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentActor, CurrentActor>();
builder.Services.AddScoped<SessionCookieEvents>();
builder.Services.AddScoped<ValidateCsrfFilter>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions["traceId"] = RequestObservabilityMiddleware.ResolveTraceId(context.HttpContext);
    context.ProblemDetails.Extensions.TryAdd("code", context.ProblemDetails.Status switch
    {
        401 => "LOGIN_REQUIRED", 403 => "FORBIDDEN", 404 => "NOT_FOUND",
        429 => "RATE_LIMITED", _ => "REQUEST_FAILED"
    });
    context.ProblemDetails.Title = context.ProblemDetails.Status switch
    {
        401 => "กรุณาเข้าสู่ระบบอีกครั้ง", 403 => "คุณไม่มีสิทธิ์ทำรายการนี้",
        429 => "มีการทำรายการถี่เกินไป กรุณารอสักครู่", _ => "ไม่สามารถทำรายการได้"
    };
});
builder.Services.AddControllers(options => options.Filters.AddService<ValidateCsrfFilter>())
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ProblemDetails { Status = 400, Title = "รูปแบบข้อมูลไม่ถูกต้อง" };
        problem.Extensions["code"] = "INVALID_REQUEST";
        problem.Extensions["traceId"] = RequestObservabilityMiddleware.ResolveTraceId(context.HttpContext);
        return new BadRequestObjectResult(problem);
    });
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-clean.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = readiness.CookieSameSite;
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "__Host-clean.session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = readiness.CookieSameSite;
    options.SlidingExpiration = false;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.EventsType = typeof(SessionCookieEvents);
});
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in ProgramRegistry.AllPermissions)
        options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission)));
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(readiness.AllowedOrigins).WithMethods("GET", "POST", "PUT", "OPTIONS")
        .WithHeaders("Content-Type", "X-CSRF-TOKEN")
        .WithExposedHeaders("X-Trace-Id")
        .AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 30, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();
if (args.Contains("--bootstrap", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<BootstrapService>().InitializeAsync(CancellationToken.None);
    Console.WriteLine("Bootstrap completed. Use the supplied temporary password and change it on first login.");
    return;
}
if (readiness.ReverseProxyEnabled) app.UseForwardedHeaders();
app.UseMiddleware<RequestObservabilityMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.User.HasClaim("must_change_password", "true")
        && !context.Request.Path.StartsWithSegments("/api/auth", StringComparison.OrdinalIgnoreCase)
        && !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = 403, Title = "กรุณาเปลี่ยนรหัสผ่านก่อนใช้งาน",
            Extensions =
            {
                ["code"] = "PASSWORD_CHANGE_REQUIRED",
                ["traceId"] = RequestObservabilityMiddleware.ResolveTraceId(context)
            }
        });
        return;
    }
    await next(context);
});
app.MapCleanHealthChecks();
app.MapControllers();
await app.RunAsync();
public partial class Program;
