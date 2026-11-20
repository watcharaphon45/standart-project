using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT01;
using clean.Web.Controllers.DB;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt01")]
public sealed class SURT01Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT01.VIEW")]
    public Task<Page<UserDto>> Search(SearchUsersRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT01.VIEW")]
    public Task<UserDto> Detail(GetUserDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "SURT01.CREATE")]
    public Task<UserDto> Create(CreateUserRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT01.UPDATE")]
    public Task<UserDto> Update(UpdateUserRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("reset"), Authorize(Policy = "SURT01.RESET")]
    public Task<UserDto> Reset(ResetTemporaryPasswordRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("unlock"), Authorize(Policy = "SURT01.UNLOCK")]
    public Task<UserDto> Unlock(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new UnlockUserRequest(request.Id, request.Version), cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "SURT01.ACTIVATE")]
    public Task<UserDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeUserStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("disable"), Authorize(Policy = "SURT01.DISABLE")]
    public Task<UserDto> Disable(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeUserStatusRequest(request.Id, request.Version, false), cancellationToken);
}

