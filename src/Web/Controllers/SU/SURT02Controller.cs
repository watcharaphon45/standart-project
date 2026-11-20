using clean.Application.Common.Models;
using clean.Application.Features.SU.SURT02;
using clean.Web.Controllers.DB;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace clean.Web.Controllers.SU;

[ApiController, Authorize, Route("api/su/surt02")]
public sealed class SURT02Controller(ISender sender) : ControllerBase
{
    [HttpPost("search"), Authorize(Policy = "SURT02.VIEW")]
    public Task<Page<RoleDto>> Search(SearchRolesRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("detail"), Authorize(Policy = "SURT02.VIEW")]
    public Task<RoleDto> Detail(GetRoleDetailRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("create"), Authorize(Policy = "SURT02.CREATE")]
    public Task<RoleDto> Create(CreateRoleRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPut("update"), Authorize(Policy = "SURT02.UPDATE")]
    public Task<RoleDto> Update(UpdateRoleRequest request, CancellationToken cancellationToken) =>
        sender.Send(request, cancellationToken);

    [HttpPost("activate"), Authorize(Policy = "SURT02.ACTIVATE")]
    public Task<RoleDto> Activate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeRoleStatusRequest(request.Id, request.Version, true), cancellationToken);

    [HttpPost("deactivate"), Authorize(Policy = "SURT02.DEACTIVATE")]
    public Task<RoleDto> Deactivate(StatusRequest request, CancellationToken cancellationToken) =>
        sender.Send(new ChangeRoleStatusRequest(request.Id, request.Version, false), cancellationToken);
}

