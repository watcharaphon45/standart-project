using System.Security.Claims;
using clean.Application.Common.Interfaces;
using clean.Application.Common.Models;
using clean.Application.Features.Auth;
using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace clean.Web.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(ISender sender, IIdentityService identity, ICurrentActor actor) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf(IAntiforgery antiforgery)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request), cancellationToken);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new(ClaimTypes.NameIdentifier, result.UserId),
            new("session_id", result.SessionId)
        ], CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        return Ok(new { result.MustChangePassword });
    }

    [HttpGet("me"), Authorize]
    public Task<CurrentUserDto> Me(CancellationToken cancellationToken) => identity.CurrentAsync(actor.UserId, cancellationToken);

    [HttpPost("change-password"), Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangePasswordCommand(request), cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await identity.LogoutAsync(User.FindFirstValue("session_id")!, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
